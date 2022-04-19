# Geta Optimizely Categories

![](http://tc.geta.no/app/rest/builds/buildType:(id:GetaPackages_EPiCategories_00ci_2),branch:master/statusIcon)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Geta_geta-optimizely-categories&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Geta_geta-optimizely-categories)
[![Platform](https://img.shields.io/badge/Platform-.NET%205-blue.svg?style=flat)](https://docs.microsoft.com/en-us/dotnet/)
[![Platform](https://img.shields.io/badge/Optimizely-%2012-orange.svg?style=flat)](http://world.episerver.com/cms/)
## Description
An alternative to Optimizely's default category functionality, where categories are instead stored as localizable IContent.

## Features
* Localization (no more language XML files)
* More user friendly edit UI
* Access rights support (some editors should perhaps have limited category access)
* Shared and site specific categories in multisite solutions
* Partial routing of category URL segments
  
## Installation
Install NuGet package from Episerver NuGet Feed:

	dotnet add package 

# Configuration

Add the categories configuration in the Startup.cs in the `ConfigureServices` method. Below is an example with all available configuration you can set (values below are defaults and can be left out).

```
public void ConfigureServices(IServiceCollection services)
{
    services.AddCategories(o =>
		{
			o.CategorySeparator = "__";
			o.DisableCategoryAsLinkableType = false;
			o.HideDisallowedRootCategories = false;
			o.ShowDefaultCategoryProperty = false;
		});
...
}
```

In addition, the configuration can be read from the `appsettings.json`:

```
"Geta": {
    "Categories": {
        "CategorySeparator":  "__",
		"DisableCategoryAsLinkableType": false,
		"HideDisallowedRootCategories": false,
		"ShowDefaultCategoryProperty": false
    }
}
```

The configuration from the `appsettings.json` will override any configuration set in the Startup.

## Settings

**CategorySeparator**: Default category separator is a double underscore "__": i.e. "/articles/entertainment__sports/"

**DisableCategoryAsLinkableType**: By default set to false, allows you to disable linking to categories in the 'Create link' modal window in the CMS (e.g. in TinyMCE or on a LinkItem)

**HideDisallowedRootCategories**: By default set to false. The default behavior is to show all categories in the selector, while it's only possible to select based on [AllowedTypes] setting. If you want to hide all root categories that doesn't match AllowedTypes.

**ShowDefaultCategoryProperty**: By default set to false, hides the default Episerver category property

## How to use

Start by creating a category content type that inherits from CategoryData. You can have multiple if you need. Note that CategoryData is included in the package, there is no need to create your own.

	[ContentType]
	public class BasicCategory : CategoryData
	{
	}
	
	[ContentType]
	public class ExtendedCategory : BasicCategory
	{
		[CultureSpecific]
		public virtual XhtmlString MainBody { get; set; }
	}

### Edit categories

Instead of going to admin mode to manage categories, you now do it in edit mode, under the "Categories" tab in the main navigation component to the left. You work with them like normal pages, and it's possible to translate them. You can create categories that are shared between multiple sites or you can create site specific categories.

![ScreenShot](/docs/extended-category-tree.jpg)

### ICategorizableContent interface

Implement ICategorizableContent on your content type class to categorize your content.

	public class MyPageType : PageData, ICategorizableContent
	{
		[Categories]
		public virtual IList<ContentReference> Categories { get; set; }
	}
	
Above property will look familiar if you have used standard Episerver categories before.

![ScreenShot](/docs/category-selector.jpg)
![ScreenShot](/docs/category-selector-dialog.jpg)

There is a context menu in the selector where you quickly can create and auto publish a new category and automatically get back to the selector with the new category selected:

![ScreenShot](/docs/category-selector-dialog-create-new.jpg)

If you prefer to use the native content reference list editor for your categories you can skip the CategoriesAttribute:

	[AllowedTypes(typeof(CategoryData))]
	public virtual IList<ContentReference> Categories { get; set; }

![ScreenShot](/docs/content-reference-list.jpg)

If you want a single category on your content type just add a ContentReference property:

	[UIHint(CategoryUIHint.Category)]
	public virtual ContentReference MainCategory { get; set; }

### ICategoryContentLoader interface

There is an implementation of ICategoryContentLoader that you can use to load categories:

```
public interface ICategoryContentLoader
{
    T Get<T>(ContentReference categoryLink) where T : CategoryData;
    IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink) where T : CategoryData;
    IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink, CultureInfo culture) where T : CategoryData;
    IEnumerable<T> GetChildren<T>(ContentReference parentCategoryLink, LoaderOptions loaderOptions) where T : CategoryData;
    T GetFirstBySegment<T>(string urlSegment) where T : CategoryData;
    T GetFirstBySegment<T>(string urlSegment, CultureInfo culture) where T : CategoryData;
    T GetFirstBySegment<T>(string urlSegment, LoaderOptions loaderOptions) where T : CategoryData;
    T GetFirstBySegment<T>(ContentReference parentLink, string urlSegment, LoaderOptions loaderOptions) where T : CategoryData;
    IEnumerable<T> GetGlobalCategories<T>() where T : CategoryData;
    IEnumerable<T> GetGlobalCategories<T>(CultureInfo culture) where T : CategoryData;
    IEnumerable<T> GetGlobalCategories<T>(LoaderOptions loaderOptions) where T : CategoryData;
    IEnumerable<T> GetSiteCategories<T>() where T : CategoryData;
    IEnumerable<T> GetSiteCategories<T>(CultureInfo culture) where T : CategoryData;
    IEnumerable<T> GetSiteCategories<T>(LoaderOptions loaderOptions) where T : CategoryData;
    bool TryGet<T>(ContentReference categoryLink, out T category) where T : CategoryData;
}
```

Inject it in your controller as you are used to:

```
public class MyController : Controller
{
    private readonly ICategoryContentLoader _categoryLoader;

    public MyController(ICategoryContentLoader categoryLoader)
    {
        _categoryLoader = categoryLoader;	
    }
}
```

### IContentInCategoryLocator interface

You can use IContentInCategoryLocator to find content in certain categories:

```
public interface IContentInCategoryLocator
{
    IEnumerable<T> GetDescendents<T>(ContentReference contentLink, IEnumerable<ContentReference> categories) where T : ICategorizableContent, IContent;
    IEnumerable<T> GetDescendents<T>(ContentReference contentLink, IEnumerable<ContentReference> categories, CultureInfo culture) where T : ICategorizableContent, IContent;
    IEnumerable<T> GetDescendents<T>(ContentReference contentLink, IEnumerable<ContentReference> categories, LoaderOptions loaderOptions) where T : ICategorizableContent, IContent;
    IEnumerable<T> GetChildren<T>(ContentReference contentLink, IEnumerable<ContentReference> categories) where T : ICategorizableContent, IContent;
    IEnumerable<T> GetChildren<T>(ContentReference contentLink, IEnumerable<ContentReference> categories, CultureInfo culture) where T : ICategorizableContent, IContent;
    IEnumerable<T> GetChildren<T>(ContentReference contentLink, IEnumerable<ContentReference> categories, LoaderOptions loaderOptions) where T : ICategorizableContent, IContent;
    IEnumerable<T> GetReferencesToCategories<T>(IEnumerable<ContentReference> categories) where T : ICategorizableContent, IContent;
    IEnumerable<T> GetReferencesToCategories<T>(IEnumerable<ContentReference> categories, CultureInfo culture) where T : ICategorizableContent, IContent;
    IEnumerable<T> GetReferencesToCategories<T>(IEnumerable<ContentReference> categories, LoaderOptions loaderOptions) where T : ICategorizableContent, IContent;
}
```

## Routing

Two routes are mapped during initialization. One for site categories and one for global categories. This means you can create templates for your category content types. They are routed in a similar way as normal pages. You can set the root segment on the "For This Site" and "For All Sites" category nodes in the Categories tree.

![ScreenShot](/docs/for-this-site.jpg)

Using above example, the URL "/siteassets/topics/sports/" would be routed to the site category called "Sports". Similarly you could go to "/globalassets/topics/global-category-1" for the global category "Global category 1".

### ICategoryRoutableContent interface

Implement this on your content type:

    public class ArticleListPage: PageData, ICategoryRoutableContent 
	{
	}

  It will be possible to route category URL segments with the help of a partial router shipped in this package. Let's say you have an article list page with the URL "/articles/" on your site. If you have a category with the url segment of "sports", you can add it to the end of your list page URL, "/articles/sports/", and the category data will be added to the route values with the key "currentCategory". Your controller action method could look something like this:

	public ActionResult Index(ArticleListPage currentPage, IList<CategoryData> currentCategories)
	{
	}

You can also have multiple category URL segments separated with the configured category separator: /articles/entertainment__sports/, currentCategories will now contain both "Sports" and "Entertainment".

	
There are a couple of UrlHelper and UrlResolver extension methods included to get content URL with category segment added:

	@Url.CategoryRoutedContentUrl(/*ContentReference*/ contentLink, /*ContentReference*/ categoryContentLink) // Single category
	@Url.CategoryRoutedContentUrl(/*ContentReference*/ contentLink, /*IEnumerable<ContentReference>*/ categoryContentLinks) // Multiple categories

	@UrlResolver.Current.GetCategoryRoutedUrl(/*ContentReference*/ contentLink, /*ContentReference*/ categoryContentLink) // Single category
	@UrlResolver.Current.GetCategoryRoutedUrl(/*ContentReference*/ contentLink, /*IEnumerable<ContentReference>*/ categoryContentLinks) // Multiple categories

## Sandbox App
Sandbox application is testing poligon for package new features and bug fixes.

CMS username: admin@example.com
Password: Episerver123!

## Package maintainer

https://github.com/MattisOlsson

https://github.com/brianweet
