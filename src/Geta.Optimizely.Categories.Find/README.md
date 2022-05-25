# Geta.Optimizely.Categories.Find

## Description

CMS category search provider and extensions methods for projects with Geta.Optimizely.Categories and Optimizely Find (Search & Navigation).

## Features

* CMS search provider powered by Optimizely Find.
* Category filter and facet extension methods for ITypeSearch and IHasFacetResults.

## How to install
Install NuGet package from Optimizely NuGet Feed:

``` 
dotnet add package  Geta.Optimizely.Categories.Find
```

## How to use
### ITypeSearch<T> extension methods:

```csharp
ITypeSearch<T> FilterByCategories<T>(this ITypeSearch<T> search, IEnumerable<ContentReference> categories) where T : ICategorizableContent
ITypeSearch<T> FilterHitsByCategories<T>(this ITypeSearch<T> search, IEnumerable<ContentReference> categories) where T : ICategorizableContent
ITypeSearch<T> ContentCategoriesFacet<T>(this ITypeSearch<T> request) where T : ICategorizableContent
```

### IHasFacetResults<T> extension methods:

```csharp
IEnumerable<ContentCount> ContentCategoriesFacet<T>(this IHasFacetResults<T> result) where T : ICategorizableContent
```

## Package maintainer
https://github.com/brianweet
https://github.com/MattisOlsson