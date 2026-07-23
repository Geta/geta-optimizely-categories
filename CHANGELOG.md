# Changelog

All notable changes to this project will be documented in this file.

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
