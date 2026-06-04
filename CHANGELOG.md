# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

First release targeting **.NET 10** and **Optimizely CMS 13**. This is a major version; upgrading from 1.x requires retargeting and recompiling your solution. See [Upgrading to Optimizely CMS 13](README.md#upgrading-to-optimizely-cms-13) in the README.

### Changed

- **BREAKING:** Upgraded to .NET 10 and Optimizely CMS 13 (`EPiServer.CMS.* 13.1.0`). Earlier versions target .NET 6 / Optimizely CMS 12.
- Rewrote the category property editor as native ES6 JavaScript modules, replacing the legacy Dojo/Dijit widgets (required by the CMS 13 editing UI).

### Removed

- **BREAKING:** `[AllowedTypes(typeof(CategoryData))]` on a `ContentReference` / `IList<ContentReference>` property no longer renders a category picker. On CMS 13 the native content reference picker does not resolve the base `CategoryData` type to its concrete category types, so the picker comes up empty. Use the `[Categories]` attribute, `[UIHint(CategoryUIHint.Categories)]`, or `[UIHint(CategoryUIHint.Category)]` instead. This is a source-only change — stored category references are unaffected and no data migration is required.
- Removed the legacy Dojo category selector widgets (`module/ClientResources/Scripts/widget/*`).

## [1.1.3] - 2025-01-31

- Removed an unused constructor parameter from `DefaultCategoryContentLoader` ([#41](https://github.com/Geta/geta-optimizely-categories/pull/41)).

## [1.1.2] - 2025-01-23

- Category hierarchy handling and routing improvements ([#30](https://github.com/Geta/geta-optimizely-categories/pull/30)).

## [1.1.1] - 2023-12-27

- Removed version constraints on the `Geta.Optimizely.Categories.Find` package.

## [1.1.0] - 2023-12-27

- Build/release pipeline improvements.

## [1.0.0] - 2022-05-27

- First stable release for Optimizely CMS 12 / .NET 6.

## [0.0.1] - 2022-05-18

- Initial release.

[Unreleased]: https://github.com/Geta/geta-optimizely-categories/compare/v1.1.3...HEAD
[1.1.3]: https://github.com/Geta/geta-optimizely-categories/compare/v1.1.2...v1.1.3
[1.1.2]: https://github.com/Geta/geta-optimizely-categories/compare/v1.1.1...v1.1.2
[1.1.1]: https://github.com/Geta/geta-optimizely-categories/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/Geta/geta-optimizely-categories/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Geta/geta-optimizely-categories/compare/v0.0.1...v1.0.0
[0.0.1]: https://github.com/Geta/geta-optimizely-categories/releases/tag/v0.0.1
