# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added

- A **Sort index** (`SortIndex`) on categories ([#20](https://github.com/Geta/geta-optimizely-categories/issues/20)). Categories are listed by it, then by name, in the picker (search results after their match: exact, then starts with, then the rest), and by `ICategoryContentLoader.GetChildren`, `GetGlobalCategories` and `GetSiteCategories`. It defaults to 0, so the order stays alphabetical until it's set. `InCategoryOrder()` orders other lists of a level the same way.
- `ICategoryContentLoader.Get<T>` overloads that take a `CultureInfo` or `LoaderOptions` ([#49](https://github.com/Geta/geta-optimizely-categories/issues/49)). They have default implementations, so existing custom loaders keep compiling. The defaults load straight from CMS's `IContentLoader`, so a custom loader or decorator that should handle these calls implements them.

### Fixed

- Category URLs such as `/articles/sports/` and `/articles/sports__news/` no longer return 404 on CMS 13 ([#54](https://github.com/Geta/geta-optimizely-categories/issues/54)). Since 2.0.0 the router found the categories but left their URL segment for CMS to read as an action. Categories are also read across several URL segments when the configuration calls for it: with `/` as `CategorySeparator` (`/articles/sports/news/`), and category paths with `UseUrlPathForCategoryRetrieval`, which the URL helpers now also write. With the default `__` separator, categories are listed in one segment (`/articles/sports__news/`). A segment after the categories, such as an action, is left to route; the action needs a `currentCategories` parameter.
- Categories under a second categories root, for example one an import added, are no longer hidden ([#48](https://github.com/Geta/geta-optimizely-categories/issues/48)). The picker, its search and category routing use every root in the assets folder, and `GetGlobalCategories`/`GetSiteCategories` return the categories of each, the root in use's first. Before, only one root was used and the categories under the others couldn't be picked or routed until someone moved them.
- The root in use (where new categories are created, and looked in first) is now chosen the same way on every request: a root that holds categories wins over an empty one, otherwise CMS's child order decides, as before (the root whose name sorts first), and of roots with the same name the oldest. Before, roots with the same name were picked in no stable order.
- New `GetOrCreateGlobalCategoriesRoots()`/`GetOrCreateSiteCategoriesRoots()` extensions and `CategorySettings.GlobalCategoriesRoots`/`SiteCategoriesRoots` list every root, the one in use first.
- The `Description` and `IsSelectable` captions are translated on category types that derive from `CategoryData`, not only on `CategoryData` itself.
- In the CMS 13.3 side-by-side edit view, the category picker no longer reverts to the published selection after the first change to a published page, and the next change no longer builds on the reverted selection ([#62](https://github.com/Geta/geta-optimizely-categories/issues/62)).
- The package's translations (`lang/geta.epicategories.xml`) are now embedded in the assembly, so category labels are translated again ([#43](https://github.com/Geta/geta-optimizely-categories/issues/43), [#44](https://github.com/Geta/geta-optimizely-categories/pull/44)).
- The category picker is shown in the editor's UI language (English, Swedish, Norwegian and Finnish are included); since 2.0.0 it was English only. Texts set per property in the editor settings (`selectLabel`, `popupTitle` and so on) still take precedence.
- The category picker now enforces `[AllowedTypes]` ([#58](https://github.com/Geta/geta-optimizely-categories/issues/58), [#59](https://github.com/Geta/geta-optimizely-categories/pull/59)). The 2.1.0 fix for [#56](https://github.com/Geta/geta-optimizely-categories/issues/56) point 7 never took effect on the `[Categories]` path, and on the `UIHint` path it emptied the tree when allowed categories sat under a category of another type. Categories of a disallowed type are now shown disabled, so nested allowed categories stay reachable.
- The picker applies the property's allowed types (the code `[AllowedTypes]` and any allowed types set in admin mode) by the same rules as publishing, so it no longer offers a category that then fails when the page is published.
- Search in the picker works again. CMS 13 has no search provider for categories, so the package now includes one (`CategorySearchProvider`), which needs neither Find nor Optimizely Graph. Categories can also be found in the edit-mode search.
- Search results respect `IsSelectable`, `[AllowedTypes]` and read access, and names with special characters display correctly.
- `IsSelectable` is evaluated in the language being edited.
- Tree levels with more categories than CMS's content tree shows (3000 by default) are listed in full.
- `HideDisallowedRootCategories` works again. It hides top-level categories of a disallowed type, but keeps non-selectable grouping categories so the allowed categories under them stay reachable.
- "Create category" only creates a category type the property allows, in the editing language.
- Fixed picker glitches: a category ticked while its check was still loading could be committed even when it wasn't selectable, a slow search could overwrite newer results, and large tree levels could exceed IIS's query-string limit and skip the check.

### Changed

- The picker lists every tree level through the package's `Children` endpoint, so it can follow `SortIndex`. If that endpoint fails, it falls back to CMS's content-structure store.
- The `Selectable` endpoint takes the property being edited (`ownerContentTypeId`, `propertyName`) instead of type lists, and returns a `reason` for disabled categories. New endpoints: `Children` and `CreatableTypes`. Like `Create`, they are authorized by `EditorAuthenticationSchemes`.

## [2.1.0] - 2026-07-23

### Fixed

Addresses the integration issues reported in [#56](https://github.com/Geta/geta-optimizely-categories/issues/56) (CMS 13 / .NET 10, incl. OptimizelyIdentity where the management UI is served under `/ui`):

- The module-copy MSBuild target now ships under `buildTransitive/` so the protected Shell UI module is deployed even when the package is referenced transitively (e.g. from a class library). Previously it only ran for the project referencing the package directly.
- The package registers its own assembly as an MVC application part, so `CategoryEditorController` (the "Create category" endpoint) is discovered without relying on module deployment/host wiring — fixes a 404 on create.
- The category picker's client no longer hardcodes the `/Optimizely` shell path; the shell/stores base is resolved server-side and injected into the editor config, so the built-in CMS stores are reached under non-default UI paths (e.g. OptimizelyIdentity's `/ui`).
- The editor endpoints are authorized via a policy whose authentication scheme(s) can be pinned through the new `EditorAuthenticationSchemes` option, instead of a scheme-less `[Authorize]` that fails with a 500 when the app has no global default challenge scheme.
- Quick-create no longer 500s for a category type with `[Required]` custom properties: it falls back to saving a draft (reported via `Published` in the response) so the editor can complete the required fields in edit mode.
- The picker now honours `CategoryData.IsSelectable` — non-selectable ("grouping") categories are disabled in the tree and search results.
- The picker now honours `[AllowedTypes]`: the property's concrete allowed category types are passed to the shell stores so the tree/search are filtered (polymorphically) to those types.

### Added

- `CategoriesOptions.EditorAuthenticationSchemes` — comma-separated authentication scheme(s) used to authorize the category editor endpoints. Leave empty to use the application's default scheme; set it (e.g. to the CMS editor cookie scheme) under OptimizelyIdentity.

## [2.0.0] - 2026-06-04

### Changed

- **Breaking:** Upgraded to .NET 10 and Optimizely CMS 13 (`EPiServer.CMS.* 13.1.0`). Earlier versions target .NET 6 / Optimizely CMS 12; consuming solutions must retarget and recompile.
- Reimplemented the category property editor as native ES6 JavaScript modules, replacing the legacy Dojo/Dijit widgets, as required by the Optimizely CMS 13 editing UI.

### Removed

- **Breaking:** Dropped the `Geta.Optimizely.Categories.Find` search-provider package; it is no longer part of this release.
- **Breaking:** `[AllowedTypes(typeof(CategoryData))]` on a `ContentReference` / `IList<ContentReference>` property no longer renders the category picker. On CMS 13 the native content reference picker does not resolve the base `CategoryData` type to its concrete category types, so the picker comes up empty. Use the `[Categories]` attribute, `[UIHint(CategoryUIHint.Categories)]`, or `[UIHint(CategoryUIHint.Category)]` instead. This is a source-only change — stored category references are unaffected. See "Upgrading to Optimizely CMS 13" in the README.
- Removed the legacy Dojo category selector widgets (`module/ClientResources/Scripts/widget/*`).

## [1.1.3] - 2025-01-31

- Removed an unused constructor parameter from `DefaultCategoryContentLoader`.

## [1.1.2] - 2025-01-23

- Category hierarchy handling and routing improvements.

## [1.1.1] - 2023-12-27

- Removed version constraints on the `Geta.Optimizely.Categories.Find` package.

## [1.1.0] - 2023-12-27

- Build/release pipeline improvements.

## [1.0.0] - 2022-05-27

- First stable release for Optimizely CMS 12 / .NET 6.

## [0.0.1] - 2022-05-18

- Initial release.
