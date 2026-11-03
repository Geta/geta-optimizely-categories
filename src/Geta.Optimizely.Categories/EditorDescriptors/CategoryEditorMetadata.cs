// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.DataAnnotations;
using EPiServer.Framework.Localization;
using EPiServer.ServiceLocation;
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
            CategorySettings categorySettings)
        {
            var categoryRepositoryDescriptor = contentRepositoryDescriptors
                .First(x => x.Key == CategoryContentRepositoryDescriptor.RepositoryKey);

            // For a packaged module the editor JS must be addressed via its served URL
            // (module client-resource path + version). Paths.ToClientResource keeps the
            // version automatic, so this still works after the package version is bumped.
            metadata.ClientEditingClass = Paths.ToClientResource(
                Constants.ModuleName,
                "ClientResources/Scripts/editors/CategorySelector.js");

            ApplyAllowedTypes(metadata);

            // The property being edited, so the Selectable / CreatableTypes endpoints can apply its allowed
            // types — the code [AllowedTypes] and any admin-mode settings — exactly as publishing does.
            metadata.EditorConfiguration["ownerContentTypeId"] = ResolveOwnerContentTypeId(metadata);
            metadata.EditorConfiguration["propertyName"] = metadata.PropertyName;

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

            // The picker's search queries the package's own provider (CMS 13 has none for categories).
            metadata.EditorConfiguration["searchProviderId"] = CategorySearchProvider.ProviderKey;

            // The editing language, so the picker can evaluate culture-specific IsSelectable in it.
            // CMS resolves the editor's content language for this metadata request.
            metadata.EditorConfiguration["contentLanguage"] = ResolveContentLanguage();

            // The picker's UI texts in the editor's UI language; the client falls back to English
            // for any key missing here.
            metadata.EditorConfiguration["labels"] = ResolveLabels(LocalizationService.Current);

            // CMS 13: opt into the native ES6 module editor pipeline. Without this flag,
            // CMS would treat ClientEditingClass as a legacy Dojo module id.
            metadata.EditorConfiguration["isJavascriptModule"] = true;
        }

        /// <summary>
        /// The picker's label keys, as the client (labels.js) names them. Each is translated in the
        /// lang file under <c>/getacategories/editor/{key in lower case}</c>.
        /// </summary>
        internal static readonly string[] LabelKeys =
        {
            "selectButton", "empty", "remove", "popupTitle", "ok", "cancel", "searchPlaceholder",
            "createButton", "createPrompt", "createSave", "createNameRequired", "createNoTypes", "createFailed",
            "searching", "noMatches", "searchFailed", "loading", "noCategories", "loadFailed",
            "typeNotAllowed", "notSelectable", "unknownError"
        };

        internal static IDictionary<string, string> ResolveLabels(LocalizationService localizationService)
        {
            var labels = new Dictionary<string, string>();
            foreach (var key in LabelKeys)
            {
                if (localizationService.TryGetString("/getacategories/editor/" + key.ToLowerInvariant(), out var text))
                {
                    labels[key] = text;
                }
            }

            return labels;
        }

        /// <summary>
        /// Writes the property's allowed/restricted category types (issue #56 point 7) into the editor
        /// configuration. The client hands them to the Selectable endpoint, which disables disallowed
        /// nodes rather than hiding them, so container categories stay navigable.
        /// </summary>
        internal static void ApplyAllowedTypes(ExtendedMetadata metadata)
        {
            // Read ExtendedMetadata.Attributes, the property's own attributes on both wiring paths. On the
            // [Categories] path CMS hands CreateDisplayMetadata the ExtendedMetadata class's attributes,
            // so the context never carries [AllowedTypes]. These keys follow the CMS convention (CMS's
            // AllowedTypesMetadataExtender overwrites them when the property declares allowed types); the
            // picker itself has the server apply the property's rules (CategoryTypesPropertyValidator).
            var attributes = metadata.Attributes?.OfType<Attribute>().ToArray() ?? Array.Empty<Attribute>();
            var allowedTypeIdentifiers = ToTypeIdentifiers(ResolveAllowedTypes(attributes));

            metadata.EditorConfiguration["AllowedTypes"] = allowedTypeIdentifiers;
            metadata.EditorConfiguration["AllowedDndTypes"] = allowedTypeIdentifiers;
            metadata.OverlayConfiguration["AllowedDndTypes"] = allowedTypeIdentifiers;
        }

        private static int? ResolveOwnerContentTypeId(ExtendedMetadata metadata)
        {
            var ownerType = metadata.ContainerType;
            if (ownerType == null
                || !ServiceLocator.Current.TryGetExistingInstance<EPiServer.DataAbstraction.IContentTypeRepository>(out var repository))
            {
                return null;
            }

            return repository.Load(ownerType)?.ID;
        }

        internal static Type[] ResolveAllowedTypes(IEnumerable<Attribute> propertyAttributes)
        {
            var allowedTypesAttribute = propertyAttributes?.OfType<AllowedTypesAttribute>().FirstOrDefault();
            var declared = allowedTypesAttribute?.AllowedTypes;

            // No explicit [AllowedTypes]: default to the base CategoryData so the picker shows all
            // category types. Otherwise keep the category types and the interfaces (CMS 13 contracts or
            // plain interfaces a category type implements — publish-time validation matches those with
            // IsInstanceOfType); a stray non-category class must never widen the picker to arbitrary
            // content.
            if (declared == null || declared.Length == 0)
            {
                return new[] { typeof(CategoryData) };
            }

            var categoryTypes = declared
                .Where(t => t.IsInterface || typeof(CategoryData).IsAssignableFrom(t))
                .ToArray();
            return categoryTypes.Length > 0 ? categoryTypes : new[] { typeof(CategoryData) };
        }

        internal static string[] ToTypeIdentifiers(IEnumerable<Type> types)
        {
            return types.Select(TypeIdentifier.Of).Where(id => id != null).ToArray();
        }

        private static string ResolveContentLanguage()
        {
            return ServiceLocator.Current.TryGetExistingInstance<IContentLanguageAccessor>(out var accessor)
                ? accessor.Language?.Name
                : null;
        }

        private static string ResolveShellBasePath()
        {
            var moduleResource = Paths.ToClientResource(Constants.ModuleName, "_");
            var moduleIndex = moduleResource.IndexOf(Constants.ModuleName, StringComparison.OrdinalIgnoreCase);
            return moduleIndex > 0 ? moduleResource.Substring(0, moduleIndex) : "/Optimizely/";
        }
    }
}
