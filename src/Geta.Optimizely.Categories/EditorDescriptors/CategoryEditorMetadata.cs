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
            // always widening to every CategoryData subtype. The concrete allowed types are pushed
            // to the client as type identifiers so the shell stores filter the tree/search
            // polymorphically (a base type matches all its subtypes), matching the native picker.
            var allowedTypes = ResolveAllowedTypes(propertyAttributes);
            var restrictedTypes = ResolveRestrictedTypes(propertyAttributes);

            var categoryRepositoryDescriptor = contentRepositoryDescriptors
                .First(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey);

            // For a packaged module the editor JS must be addressed via its served URL
            // (module client-resource path + version). Paths.ToClientResource keeps the
            // version automatic, so this still works after the package version is bumped.
            metadata.ClientEditingClass = Paths.ToClientResource(
                Constants.ModuleName,
                "ClientResources/Scripts/editors/CategorySelector.js");

            metadata.EditorConfiguration["AllowedTypes"] = allowedTypes;
            metadata.EditorConfiguration["AllowedDndTypes"] = allowedTypes;
            metadata.OverlayConfiguration["AllowedDndTypes"] = allowedTypes;

            // Type identifiers (lower-cased full names) the client passes as `typeIdentifiers` to the
            // shell stores so the tree/search are server-side filtered to the allowed category types.
            metadata.EditorConfiguration["allowedTypeIdentifiers"] = ToTypeIdentifiers(allowedTypes);
            metadata.EditorConfiguration["restrictedTypeIdentifiers"] = ToTypeIdentifiers(restrictedTypes);

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

        private static Type[] ResolveRestrictedTypes(IEnumerable<Attribute> propertyAttributes)
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
