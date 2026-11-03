// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer.DataAbstraction;
using EPiServer.DataAbstraction.RuntimeModel;
using EPiServer.DataAnnotations;
using EPiServer.Validation;

namespace Geta.Optimizely.Categories.Editors
{
    /// <summary>
    /// Decides whether a category type may be stored in a given property, by the same rules CMS applies
    /// when the page is published, so the picker never offers a category that publishing then rejects.
    /// A category must pass both gates CMS validates independently:
    /// <list type="bullet">
    /// <item><description>the property's code <see cref="AllowedTypesAttribute"/> (its own data-annotation
    /// validation: restricted types first, then the allowed types, matched with <c>IsInstanceOfType</c>);
    /// and</description></item>
    /// <item><description>every <see cref="AllowedTypesValidationSettings"/> configured on the property
    /// definition in admin mode (CMS's <c>AllowedTypesPropertyValidator</c>: base types matched by
    /// instance, content types matched by exact ID, contracts expanded to the types that implement
    /// them).</description></item>
    /// </list>
    /// </summary>
    public class CategoryTypesPropertyValidator
    {
        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IPropertyValidationSettingsRepository _validationSettingsRepository;
        private readonly IContentTypeBaseResolver _contentTypeBaseResolver;

        public CategoryTypesPropertyValidator(
            IContentTypeRepository contentTypeRepository,
            IPropertyValidationSettingsRepository validationSettingsRepository,
            IContentTypeBaseResolver contentTypeBaseResolver)
        {
            _contentTypeRepository = contentTypeRepository;
            _validationSettingsRepository = validationSettingsRepository;
            _contentTypeBaseResolver = contentTypeBaseResolver;
        }

        /// <summary>
        /// The rules for one property. Without an owner content type or property name (or when they can't
        /// be resolved) every category type is allowed, so the picker never over-restricts.
        /// </summary>
        public virtual IPropertyTypeRules For(int? ownerContentTypeId, string propertyName)
        {
            if (ownerContentTypeId == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return PropertyTypeRules.AllowAll;
            }

            var owner = _contentTypeRepository.Load(ownerContentTypeId.Value);
            if (owner == null)
            {
                return PropertyTypeRules.AllowAll;
            }

            var attribute = owner.ModelType?
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?
                .GetCustomAttribute<AllowedTypesAttribute>(inherit: true);

            var definition = owner.PropertyDefinitions?
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            var adminRules = definition == null
                ? new List<AdminRule>()
                : _validationSettingsRepository.List(definition)
                    .OfType<AllowedTypesValidationSettings>()
                    .Select(ToAdminRule)
                    .Where(rule => rule != null)
                    .ToList();

            return new PropertyTypeRules(
                attribute?.AllowedTypes ?? Array.Empty<Type>(),
                attribute?.RestrictedTypes ?? Array.Empty<Type>(),
                adminRules);
        }

        // CMS's AllowedTypesPropertyValidator.IsValid / ResolveBaseTypesAndContentTypes, per settings item.
        private AdminRule ToAdminRule(AllowedTypesValidationSettings settings)
        {
            var allContentTypes = _contentTypeRepository.List().ToList();
            var allowed = Resolve(allContentTypes, settings.AllowedTypes, settings.AllowedContentTypeGuids, out var allowsAnyItem);
            var restricted = Resolve(allContentTypes, settings.RestrictedTypes, settings.RestrictedContentTypeGuids, out _);

            // An allowed "item" contract with nothing restricted switches the rule off.
            if (allowsAnyItem && restricted.IsEmpty)
            {
                return null;
            }

            return new AdminRule(allowed, restricted);
        }

        private TypeSet Resolve(List<ContentType> allContentTypes, IEnumerable<string> baseTypeNames, IEnumerable<Guid> contentTypeGuids, out bool hasItemContract)
        {
            var guids = new HashSet<Guid>(contentTypeGuids ?? Enumerable.Empty<Guid>());
            var configured = allContentTypes.Where(ct => guids.Contains(ct.GUID)).ToList();

            hasItemContract = configured.Any(ct => IsGlobalContract(ct, "globalcontract:_Item"));
            if (hasItemContract)
            {
                return TypeSet.Empty;
            }

            var baseTypes = (baseTypeNames ?? Enumerable.Empty<string>())
                .Select(name => _contentTypeBaseResolver.Resolve(new ContentTypeBase(name)))
                .Where(t => t != null)
                .ToList();

            if (configured.Any(ct => IsGlobalContract(ct, "globalcontract:_AssetItem")))
            {
                baseTypes.AddRange(new[] { ContentTypeBase.Media, ContentTypeBase.Image, ContentTypeBase.Video }
                    .Select(b => _contentTypeBaseResolver.Resolve(b))
                    .Where(t => t != null));
            }
            else if (configured.Any(ct => IsGlobalContract(ct, "globalcontract:_ImageItem")))
            {
                var image = _contentTypeBaseResolver.Resolve(ContentTypeBase.Image);
                if (image != null) baseTypes.Add(image);
            }

            // Contracts stand for every content type that implements them.
            var contentTypeIds = new HashSet<int>();
            foreach (var contentType in configured.Where(ct => !IsGlobalContract(ct)))
            {
                if (contentType.IsContract)
                {
                    foreach (var implementer in allContentTypes.Where(ct => ct.Contracts.Any(c => c.GUID == contentType.GUID)))
                    {
                        contentTypeIds.Add(implementer.ID);
                    }
                }
                else
                {
                    contentTypeIds.Add(contentType.ID);
                }
            }

            return new TypeSet(baseTypes, contentTypeIds);
        }

        private static bool IsGlobalContract(ContentType contentType, string name = null)
        {
            return string.Equals(contentType.Source, "globalcontract", StringComparison.OrdinalIgnoreCase)
                && (name == null || string.Equals(contentType.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        internal sealed class TypeSet
        {
            public static readonly TypeSet Empty = new TypeSet(Array.Empty<Type>(), new HashSet<int>());

            public TypeSet(IReadOnlyCollection<Type> baseTypes, IReadOnlySet<int> contentTypeIds)
            {
                BaseTypes = baseTypes;
                ContentTypeIds = contentTypeIds;
            }

            public IReadOnlyCollection<Type> BaseTypes { get; }
            public IReadOnlySet<int> ContentTypeIds { get; }
            public bool IsEmpty => BaseTypes.Count == 0 && ContentTypeIds.Count == 0;

            public bool Matches(Type modelType, int contentTypeId)
            {
                return BaseTypes.Any(t => t.IsAssignableFrom(modelType)) || ContentTypeIds.Contains(contentTypeId);
            }
        }

        internal sealed class AdminRule
        {
            private readonly TypeSet _allowed;
            private readonly TypeSet _restricted;

            public AdminRule(TypeSet allowed, TypeSet restricted)
            {
                _allowed = allowed;
                _restricted = restricted;
            }

            public bool Allows(Type modelType, int contentTypeId)
            {
                if (_restricted.Matches(modelType, contentTypeId)) return false;
                return _allowed.IsEmpty || _allowed.Matches(modelType, contentTypeId);
            }
        }

        internal sealed class PropertyTypeRules : IPropertyTypeRules
        {
            public static readonly PropertyTypeRules AllowAll =
                new PropertyTypeRules(Array.Empty<Type>(), Array.Empty<Type>(), new List<AdminRule>());

            private readonly Type[] _codeAllowed;
            private readonly Type[] _codeRestricted;
            private readonly List<AdminRule> _adminRules;

            public PropertyTypeRules(Type[] codeAllowed, Type[] codeRestricted, List<AdminRule> adminRules)
            {
                _codeAllowed = codeAllowed;
                _codeRestricted = codeRestricted;
                _adminRules = adminRules;
            }

            public bool Allows(Type modelType, int contentTypeId)
            {
                // AllowedTypesAttribute's own validation (AllowedTypesContentValidator, four-argument form).
                if (_codeRestricted.Any(t => t.IsAssignableFrom(modelType))) return false;
                if (_codeAllowed.Length > 0 && !_codeAllowed.Any(t => t.IsAssignableFrom(modelType))) return false;

                // Each admin-mode settings item must pass as well.
                return _adminRules.All(rule => rule.Allows(modelType, contentTypeId));
            }
        }
    }

    /// <summary>The allowed-types rules of one property, as publish-time validation applies them.</summary>
    public interface IPropertyTypeRules
    {
        /// <summary>Whether a category of this model type and content type may be stored in the property.</summary>
        bool Allows(Type modelType, int contentTypeId);
    }
}
