// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.DataAnnotations;
using EPiServer.Shell;
using EPiServer.Shell.ObjectEditing;

namespace Geta.Optimizely.Categories.EditorDescriptors
{
    /// <summary>
    /// Shared metadata setup for the CMS 13 ES6 module category-picker editor. Both
    /// <see cref="DataAnnotations.CategoriesAttribute"/> (the <c>[Categories]</c> path) and
    /// <see cref="CategoriesEditorDescriptor"/> (the <c>UIHint = "Categories"</c> path) wire the
    /// exact same editor onto a property, so the configuration lives here to avoid drift.
    /// </summary>
    internal static class CategoryEditorMetadata
    {
        public static void Apply(
            ExtendedMetadata metadata,
            IEnumerable<IContentRepositoryDescriptor> contentRepositoryDescriptors,
            CategorySettings categorySettings,
            IEnumerable<Attribute> propertyAttributes = null)
        {
            // Issue #56 point 7: honour the property's own [AllowedTypes] in the picker instead of
            // always widening to every CategoryData subtype. The concrete allowed/restricted types are
            // pushed to the client, which asks the Selectable endpoint to evaluate them per node and
            // disables the ones that don't match — the same "grey, don't hide" approach the native
            // picker takes (ContentTree + disableRestrictedTypes).
            var attributes = ResolvePropertyAttributes(propertyAttributes, metadata?.Attributes);
            var allowedTypes = ResolveAllowedTypes(attributes);
            var restrictedTypes = ResolveRestrictedTypes(attributes);
            var allowedTypeIdentifiers = ToTypeIdentifiers(allowedTypes);
            var restrictedTypeIdentifiers = ToTypeIdentifiers(restrictedTypes);

            var categoryRepositoryDescriptor = contentRepositoryDescriptors
                .First(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey);

            // For a packaged module the editor JS must be addressed via its served URL
            // (module client-resource path + version). Paths.ToClientResource keeps the
            // version automatic, so this still works after the package version is bumped.
            metadata.ClientEditingClass = Paths.ToClientResource(
                Constants.ModuleName,
                "ClientResources/Scripts/editors/CategorySelector.js");

            // Type identifiers (lower-cased full names), not Type instances: this is the shape the CMS
            // convention uses for these keys, and the only shape that survives serialisation to the
            // client. CMS's own AllowedTypesMetadataExtender overwrites AllowedTypes/AllowedDndTypes for
            // properties that declare [AllowedTypes], but a property using the package's default
            // (all category types) is only ever set here.
            metadata.EditorConfiguration["AllowedTypes"] = allowedTypeIdentifiers;
            metadata.EditorConfiguration["AllowedDndTypes"] = allowedTypeIdentifiers;
            metadata.OverlayConfiguration["AllowedDndTypes"] = allowedTypeIdentifiers;

            // Type identifiers the client forwards to the Selectable endpoint so it can disable nodes
            // whose type isn't allowed. Deliberately NOT sent to the shell stores as `typeIdentifiers`:
            // see the comment on appendBaseTypeIdentifier in categoryApi.js.
            metadata.EditorConfiguration["allowedTypeIdentifiers"] = allowedTypeIdentifiers;
            metadata.EditorConfiguration["restrictedTypeIdentifiers"] = restrictedTypeIdentifiers;

            // Issue #56 point 3: the client must address the built-in CMS shell stores relative to
            // the resolved shell base (e.g. "/Optimizely/" or, under OptimizelyIdentity, "/ui/")
            // instead of a hardcoded "/Optimizely/" literal, which 404s when the UI is served
            // elsewhere. Derive it from a known-good client-resource URL.
            metadata.EditorConfiguration["shellBasePath"] = ResolveShellBasePath();

            metadata.EditorConfiguration["categorySettings"] = categorySettings;
            metadata.EditorConfiguration["repositoryKey"] = CategoryContentRepositoryDescriptor.RepositoryKey;
            metadata.EditorConfiguration["settings"] = categoryRepositoryDescriptor;
            metadata.EditorConfiguration["roots"] = categoryRepositoryDescriptor.Roots;
            metadata.EditorConfiguration["multiple"] = true;

            // CMS 13: opt into the native ES6 module editor pipeline. Without this flag,
            // CMS would treat ClientEditingClass as a legacy Dojo module id.
            metadata.EditorConfiguration["isJavascriptModule"] = true;
        }

        /// <summary>
        /// Resolves the attribute set to read <see cref="AllowedTypesAttribute"/> from.
        /// </summary>
        /// <remarks>
        /// The two wiring paths do not supply the same thing:
        /// <list type="bullet">
        /// <item><description>
        /// <see cref="CategoriesEditorDescriptor"/> (the <c>UIHint</c> path) is an
        /// <c>IMetadataExtender</c>, invoked by CMS as
        /// <c>ApplyExtendersToMetadataAsync(propertyMetadata, propertyMetadata.Attributes, …)</c>, so it
        /// receives the property's real attributes.
        /// </description></item>
        /// <item><description>
        /// <see cref="DataAnnotations.CategoriesAttribute"/> (the <c>[Categories]</c> path) is an
        /// <c>IDisplayMetadataProvider</c> attribute, invoked by CMS's
        /// <c>ExtensibleMetadataProvider.ApplyMetadataAwareAttributes</c>, which builds the context with
        /// <c>ModelAttributes.GetAttributesForType(metadata.GetType())</c> — the attributes of the
        /// <see cref="ExtendedMetadata"/> CLR class, NOT of the property. So <c>context.Attributes</c>
        /// never contains the property's <c>[AllowedTypes]</c> and this path silently saw nothing.
        /// </description></item>
        /// </list>
        /// <see cref="ExtendedMetadata.Attributes"/> is the property's own attribute list (set by
        /// <c>InitializeFromAttributes</c>, and what CMS's own <c>AllowedTypesMetadataExtender</c>
        /// reads), so it is a reliable source on both paths.
        /// </remarks>
        internal static IEnumerable<Attribute> ResolvePropertyAttributes(
            IEnumerable<Attribute> suppliedAttributes,
            IEnumerable<Attribute> metadataAttributes)
        {
            var supplied = suppliedAttributes?.ToArray();
            if (supplied != null && supplied.OfType<AllowedTypesAttribute>().Any())
            {
                return supplied;
            }

            var fromMetadata = metadataAttributes?.OfType<Attribute>().ToArray();
            if (fromMetadata != null && fromMetadata.Length > 0)
            {
                return fromMetadata;
            }

            return supplied ?? Enumerable.Empty<Attribute>();
        }

        internal static Type[] ResolveAllowedTypes(IEnumerable<Attribute> propertyAttributes)
        {
            var allowedTypesAttribute = propertyAttributes?.OfType<AllowedTypesAttribute>().FirstOrDefault();
            var declared = allowedTypesAttribute?.AllowedTypes;

            // No explicit [AllowedTypes]: default to the base CategoryData so the picker shows all
            // category types. Otherwise keep only the category types (a stray non-category entry
            // must never widen the picker to arbitrary content).
            if (declared == null || declared.Length == 0)
            {
                return new[] { typeof(CategoryData) };
            }

            var categoryTypes = declared.Where(t => typeof(CategoryData).IsAssignableFrom(t)).ToArray();
            return categoryTypes.Length > 0 ? categoryTypes : new[] { typeof(CategoryData) };
        }

        internal static Type[] ResolveRestrictedTypes(IEnumerable<Attribute> propertyAttributes)
        {
            var allowedTypesAttribute = propertyAttributes?.OfType<AllowedTypesAttribute>().FirstOrDefault();
            return allowedTypesAttribute?.RestrictedTypes ?? Array.Empty<Type>();
        }

        internal static string[] ToTypeIdentifiers(IEnumerable<Type> types)
        {
            return types
                .Where(t => t?.FullName != null)
                .Select(t => t.FullName.ToLowerInvariant())
                .ToArray();
        }

        private static string ResolveShellBasePath()
        {
            var moduleResource = Paths.ToClientResource(Constants.ModuleName, "_");
            var moduleIndex = moduleResource.IndexOf(Constants.ModuleName, StringComparison.OrdinalIgnoreCase);
            return moduleIndex > 0 ? moduleResource.Substring(0, moduleIndex) : "/Optimizely/";
        }
    }
}
