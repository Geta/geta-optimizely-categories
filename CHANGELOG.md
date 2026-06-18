# Changelog

All notable changes to this project will be documented in this file.

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
