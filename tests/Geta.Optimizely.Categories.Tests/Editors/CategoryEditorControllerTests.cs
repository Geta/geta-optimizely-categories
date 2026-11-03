// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Security;
using Geta.Optimizely.Categories.Editors;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Geta.Optimizely.Categories.Tests.Editors
{
    public class CategoryEditorControllerTests
    {
        // A validator whose rules allow exactly the given model types (anything, when it isn't asked).
        private static Mock<CategoryTypesPropertyValidator> ValidatorAllowing(params System.Type[] allowed)
        {
            var rules = new Mock<IPropertyTypeRules>();
            rules.Setup(r => r.Allows(It.IsAny<System.Type>(), It.IsAny<int>()))
                .Returns<System.Type, int>((type, _) => allowed.Any(a => a.IsAssignableFrom(type)));
            return ValidatorReturning(rules.Object);
        }

        private static Mock<CategoryTypesPropertyValidator> ValidatorReturning(IPropertyTypeRules rules)
        {
            var validator = new Mock<CategoryTypesPropertyValidator>(null!, null!, null!);
            validator.Setup(g => g.For(It.IsAny<int?>(), It.IsAny<string>())).Returns(rules);
            return validator;
        }

        private static CategoryEditorController CreateController(
            Mock<IContentRepository> repo,
            Mock<CategoryTypesPropertyValidator>? validator = null,
            Mock<IContentAccessEvaluator>? access = null,
            Mock<ContentTypeAvailabilityService>? availability = null)
        {
            return new CategoryEditorController(
                repo.Object,
                (access ?? new Mock<IContentAccessEvaluator>()).Object,
                (validator ?? ValidatorAllowing(typeof(CategoryData))).Object,
                (availability ?? new Mock<ContentTypeAvailabilityService>()).Object);
        }

        private static T Typed<T>(int contentTypeId) where T : CategoryData, new()
        {
            return new T { IsSelectable = true, ContentTypeID = contentTypeId };
        }

        // Grants Read on everything except the given content.
        private static Mock<IContentAccessEvaluator> ReadAccessExcept(params IContent[] denied)
        {
            var access = new Mock<IContentAccessEvaluator>();
            access
                .Setup(a => a.HasAccess(It.IsAny<IContent>(), It.IsAny<IPrincipal>(), AccessLevel.Read))
                .Returns<IContent, IPrincipal, AccessLevel>((content, _, _) => !denied.Contains(content));
            return access;
        }

        private static CategoryData Category(int id, string name, string? providerName = null)
        {
            var category = new CategoryData { IsSelectable = true };
            ((IContent)category).ContentLink = new ContentReference(id, providerName);
            ((IContent)category).Name = name;
            return category;
        }

        private static List<CategorySelectableResponse> SelectableList(IActionResult result)
        {
            return Assert.IsType<List<CategorySelectableResponse>>(Assert.IsType<OkObjectResult>(result).Value);
        }

        // A repository that resolves exactly the given ids; any other id is not found.
        private static Mock<IContentRepository> RepoWith(params (int Id, CategoryData Category)[] items)
        {
            var repo = new Mock<IContentRepository>();
            foreach (var (id, category) in items)
            {
                var content = category;
                repo.Setup(r => r.TryGet<CategoryData>(It.Is<ContentReference>(c => c.ID == id), It.IsAny<LoaderOptions>(), out content)).Returns(true);
            }

            return repo;
        }

        // Distinct category types, so type verdicts are stubbed per type — as they are decided in
        // production — instead of per call order.
        private class AllowedCategory : CategoryData
        {
        }

        private class OtherCategory : CategoryData
        {
        }

        [Fact]
        public void Create_WithValidRequest_CreatesAndReturnsContentLink()
        {
            // Arrange
            var repo = new Mock<IContentRepository>();
            var category = new CategoryData();
            repo
                .Setup(r => r.GetDefault<CategoryData>(It.IsAny<ContentReference>(), It.IsAny<int>()))
                .Returns(category);
            repo
                .Setup(r => r.Save(It.IsAny<IContent>(), SaveAction.Publish, AccessLevel.Edit))
                .Returns(new ContentReference(42));

            var controller = CreateController(repo);

            // Act
            var result = controller.Create(new CreateCategoryRequest
            {
                ParentLink = "5",
                Name = "New Cat",
                ContentTypeId = 99
            });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<CreateCategoryResponse>(ok.Value);
            Assert.Equal("42", body.ContentLink);
            Assert.Equal("New Cat", body.Name);
            Assert.True(body.Published);
            Assert.Equal("New Cat", ((IContent)category).Name);

            repo.Verify(r => r.GetDefault<CategoryData>(
                It.Is<ContentReference>(p => p.ID == 5),
                99), Times.Once);
            repo.Verify(r => r.Save(category, SaveAction.Publish, AccessLevel.Edit), Times.Once);
        }

        // Issue #56 point 5: a category type with a [Required] custom property must not 500 on
        // quick-create. Publish is attempted first; when it fails validation the action saves a
        // draft instead and reports Published = false so the editor completes it in edit mode.
        [Fact]
        public void Create_WhenPublishFailsValidation_SavesDraftAndReportsUnpublished()
        {
            var repo = new Mock<IContentRepository>();
            var category = new CategoryData();
            repo
                .Setup(r => r.GetDefault<CategoryData>(It.IsAny<ContentReference>(), It.IsAny<int>()))
                .Returns(category);
            repo
                .Setup(r => r.Save(It.IsAny<IContent>(), SaveAction.Publish, AccessLevel.Edit))
                .Throws(new ValidationException("Property 'Label' is required."));
            repo
                .Setup(r => r.Save(It.IsAny<IContent>(), SaveAction.Save | SaveAction.SkipValidation, AccessLevel.Edit))
                .Returns(new ContentReference(77));

            var controller = CreateController(repo);

            var result = controller.Create(new CreateCategoryRequest
            {
                ParentLink = "5",
                Name = "Grouping",
                ContentTypeId = 3
            });

            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<CreateCategoryResponse>(ok.Value);
            Assert.Equal("77", body.ContentLink);
            Assert.Equal("Grouping", body.Name);
            Assert.False(body.Published);

            repo.Verify(r => r.Save(category, SaveAction.Publish, AccessLevel.Edit), Times.Once);
            repo.Verify(r => r.Save(category, SaveAction.Save | SaveAction.SkipValidation, AccessLevel.Edit), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithBlankName_ReturnsBadRequest(string name)
        {
            AssertBadRequestForName(name);
        }

        [Fact]
        public void Create_WithNullName_ReturnsBadRequest()
        {
            AssertBadRequestForName(null);
        }

        private static void AssertBadRequestForName(string? name)
        {
            var repo = new Mock<IContentRepository>(MockBehavior.Strict);
            var controller = CreateController(repo);

            var result = controller.Create(new CreateCategoryRequest { ParentLink = "5", Name = name });

            Assert.IsType<BadRequestObjectResult>(result);
            repo.VerifyNoOtherCalls();
        }

        [Fact]
        public void Create_WithInvalidParentLink_ReturnsBadRequest()
        {
            var repo = new Mock<IContentRepository>(MockBehavior.Strict);
            var controller = CreateController(repo);

            var result = controller.Create(new CreateCategoryRequest
            {
                ParentLink = "not-a-link",
                Name = "New Cat"
            });

            Assert.IsType<BadRequestObjectResult>(result);
            repo.VerifyNoOtherCalls();
        }

        [Fact]
        public void Create_WithNullRequest_ReturnsBadRequest()
        {
            var repo = new Mock<IContentRepository>(MockBehavior.Strict);
            var controller = CreateController(repo);

            var result = controller.Create(null);

            Assert.IsType<BadRequestObjectResult>(result);
            repo.VerifyNoOtherCalls();
        }

        // Issue #56 point 6: the Selectable endpoint reports IsSelectable per category so the picker
        // can disable non-selectable ("grouping") nodes. A selectable category → true, a
        // non-selectable one → false, and anything that isn't a resolvable category → true (never
        // over-restrict).
        [Fact]
        public void Selectable_ReportsIsSelectablePerCategory_UnknownDefaultsToSelectable()
        {
            var repo = RepoWith((10, new CategoryData { IsSelectable = true }), (20, new CategoryData { IsSelectable = false }));

            var list = SelectableList(CreateController(repo).Selectable("10, 20, 30, not-a-link"));

            Assert.True(list.Single(x => x.Id == "10").Selectable);   // IsSelectable = true
            Assert.False(list.Single(x => x.Id == "20").Selectable);  // IsSelectable = false → disabled in picker
            Assert.True(list.Single(x => x.Id == "30").Selectable);   // not resolvable → don't restrict
            Assert.True(list.Single(x => x.Id == "not-a-link").Selectable); // unparsable → don't restrict
        }

        // Issue #56 point 7: the Selectable endpoint also applies the property's allowed types, through
        // CategoryTypesPropertyValidator (covered by CategoryTypesPropertyValidatorTests); these cover the controller's own logic.
        [Fact]
        public void Selectable_DisablesCategoriesThePropertyDoesNotAllow()
        {
            var repo = RepoWith((10, Typed<AllowedCategory>(1)), (20, Typed<OtherCategory>(2)));
            var validator = ValidatorAllowing(typeof(AllowedCategory));

            var list = SelectableList(CreateController(repo, validator).Selectable("10, 20", ownerContentTypeId: 7, propertyName: "Categories"));

            Assert.True(list.Single(x => x.Id == "10").Selectable);
            Assert.Null(list.Single(x => x.Id == "10").Reason);
            var disallowed = list.Single(x => x.Id == "20");
            Assert.False(disallowed.Selectable);
            Assert.Equal(CategoryEditorController.TypeNotAllowedReason, disallowed.Reason);
            validator.Verify(g => g.For(7, "Categories"), Times.Once);
        }

        [Fact]
        public void Selectable_EvaluatesEachContentTypeOnce()
        {
            var repo = RepoWith((10, Typed<AllowedCategory>(1)), (11, Typed<AllowedCategory>(1)), (20, Typed<OtherCategory>(2)));
            var rules = new Mock<IPropertyTypeRules>();
            rules.Setup(r => r.Allows(It.IsAny<System.Type>(), 1)).Returns(true);
            var validator = ValidatorReturning(rules.Object);

            var list = SelectableList(CreateController(repo, validator).Selectable("10, 11, 20", 7, "Categories"));

            Assert.Equal(new[] { true, true, false }, list.Select(x => x.Selectable));
            rules.Verify(r => r.Allows(It.IsAny<System.Type>(), 1), Times.Once);
            rules.Verify(r => r.Allows(It.IsAny<System.Type>(), 2), Times.Once);
        }

        [Fact]
        public void Selectable_IsSelectableTakesPrecedenceOverTheTypeCheck()
        {
            var category = Typed<OtherCategory>(2);
            category.IsSelectable = false;
            var rules = new Mock<IPropertyTypeRules>();
            var repo = RepoWith((10, category));

            var list = SelectableList(CreateController(repo, ValidatorReturning(rules.Object)).Selectable("10", 7, "Categories"));

            Assert.False(list.Single().Selectable);
            // The more specific reason: the category itself is a grouping node, regardless of its type.
            Assert.Equal(CategoryEditorController.NotSelectableReason, list.Single().Reason);
            // A non-selectable (grouping) category is never type-checked, so HideDisallowedRootCategories
            // keeps it and the allowed categories beneath it stay reachable.
            rules.VerifyNoOtherCalls();
        }

        [Fact]
        public void Selectable_UnresolvableIds_StaySelectableAndSkipTheTypeCheck()
        {
            var rules = new Mock<IPropertyTypeRules>();

            var list = SelectableList(CreateController(RepoWith(), ValidatorReturning(rules.Object)).Selectable("999, not-a-link", 7, "Categories"));

            Assert.All(list, x => Assert.True(x.Selectable));
            rules.VerifyNoOtherCalls();
        }

        // ---- CreatableTypes -------------------------------------------------------------------------

        [Fact]
        public void CreatableTypes_ListsAvailableCategoryTypesThePropertyAllows()
        {
            IContent parent = Category(5, "Root");
            var repo = new Mock<IContentRepository>();
            repo.Setup(r => r.TryGet<IContent>(It.Is<ContentReference>(c => c.ID == 5), out parent)).Returns(true);
            var availability = new Mock<ContentTypeAvailabilityService>();
            availability
                .Setup(a => a.ListAvailable(parent, false, It.IsAny<IPrincipal>()))
                .Returns(new List<ContentType>
                {
                    new ContentType { ID = 1, Name = "Allowed", ModelType = typeof(AllowedCategory) },
                    new ContentType { ID = 2, Name = "Other", ModelType = typeof(OtherCategory) },
                    new ContentType { ID = 3, Name = "Page", ModelType = typeof(PageData) }
                });

            var result = CreateController(repo, ValidatorAllowing(typeof(AllowedCategory)), availability: availability)
                .CreatableTypes("5", 7, "Categories");

            var type = Assert.Single(Assert.IsType<List<CategoryContentTypeResponse>>(Assert.IsType<OkObjectResult>(result).Value));
            Assert.Equal(1, type.Id);
        }

        // IsSelectable is culture-specific, so it must be read in the editing language.
        [Fact]
        public void Selectable_LoadsCategoriesInTheRequestedLanguage()
        {
            LoaderOptions? used = null;
            CategoryData category = new CategoryData { IsSelectable = false };
            var repo = new Mock<IContentRepository>();
            repo.Setup(r => r.TryGet<CategoryData>(It.IsAny<ContentReference>(), It.IsAny<LoaderOptions>(), out category))
                .Callback(new TryGetCallback((ContentReference _, LoaderOptions options, out CategoryData c) => { used = options; c = category; }))
                .Returns(true);

            CreateController(repo).Selectable("10", language: "sv");

            var languageOption = Assert.IsType<LanguageLoaderOption>(Assert.Single(used!));
            Assert.Equal("sv", languageOption.Language.Name);
        }

        private delegate void TryGetCallback(ContentReference link, LoaderOptions options, out CategoryData content);

        // ---- Children -------------------------------------------------------------------------------

        [Fact]
        public void Children_ListsReadableChildren_WithFullReferencesAndAPagedChildCheck()
        {
            var parent = Category(5, "Parent");
            var readable = Category(10, "Readable", "catalog");
            var hidden = Category(11, "Hidden");
            var grandchild = Category(20, "Grandchild");
            IContent parentContent = parent;
            var repo = new Mock<IContentRepository>();
            repo.Setup(r => r.TryGet<IContent>(It.Is<ContentReference>(c => c.ID == 5), It.IsAny<LoaderOptions>(), out parentContent)).Returns(true);
            repo.Setup(r => r.GetChildren<CategoryData>(It.Is<ContentReference>(c => c.ID == 5), It.IsAny<LoaderOptions>()))
                .Returns(new[] { readable, hidden });
            repo.Setup(r => r.GetChildren<CategoryData>(It.Is<ContentReference>(c => c.ID == 10), It.IsAny<LoaderOptions>(), 0, 1))
                .Returns(new[] { grandchild });

            var result = CreateController(repo, access: ReadAccessExcept(hidden)).Children("5");

            var child = Assert.Single(Assert.IsType<List<CategoryChildResponse>>(Assert.IsType<OkObjectResult>(result).Value));
            Assert.Equal("10__catalog", child.Id);
            Assert.Equal("Readable", child.Name);
            Assert.True(child.HasChildren);
            // Only the paged overload is used for HasChildren, never a full listing of the grandchildren.
            repo.Verify(r => r.GetChildren<CategoryData>(It.Is<ContentReference>(c => c.ID == 10), It.IsAny<LoaderOptions>()), Times.Never);
        }

        // Issue #20: the picker lists a level in category order - SortIndex, then name.
        [Fact]
        public void Children_AreListedBySortIndexThenName()
        {
            var parent = Category(5, "Parent");
            var sport = Category(10, "Sport");
            var news = Category(11, "News");
            var culture = Category(12, "Culture");
            var archive = Category(13, "Archive");
            news.SortIndex = 1;
            sport.SortIndex = 2;
            culture.SortIndex = 2;
            archive.SortIndex = 9;
            IContent parentContent = parent;
            var repo = new Mock<IContentRepository>();
            repo.Setup(r => r.TryGet<IContent>(It.Is<ContentReference>(c => c.ID == 5), It.IsAny<LoaderOptions>(), out parentContent)).Returns(true);
            repo.Setup(r => r.GetChildren<CategoryData>(It.Is<ContentReference>(c => c.ID == 5), It.IsAny<LoaderOptions>()))
                .Returns(new[] { archive, sport, news, culture });

            var result = CreateController(repo, access: ReadAccessExcept()).Children("5");

            var names = Assert.IsType<List<CategoryChildResponse>>(Assert.IsType<OkObjectResult>(result).Value).Select(c => c.Name);
            Assert.Equal(new[] { "News", "Culture", "Sport", "Archive" }, names);
        }

        // The endpoint isn't routed content, so CMS doesn't set the current culture to the editing language:
        // names are compared in the language the picker sends, as a site page in that language lists them.
        [Fact]
        public void Children_CompareNamesInTheEditingLanguage()
        {
            var parent = Category(5, "Parent");
            IContent parentContent = parent;
            var repo = new Mock<IContentRepository>();
            repo.Setup(r => r.TryGet<IContent>(It.Is<ContentReference>(c => c.ID == 5), It.IsAny<LoaderOptions>(), out parentContent)).Returns(true);
            repo.Setup(r => r.GetChildren<CategoryData>(It.Is<ContentReference>(c => c.ID == 5), It.IsAny<LoaderOptions>()))
                .Returns(new[] { Category(10, "Öl"), Category(11, "Zon") });
            var controller = CreateController(repo, access: ReadAccessExcept());

            var swedish = Assert.IsType<List<CategoryChildResponse>>(Assert.IsType<OkObjectResult>(controller.Children("5", "sv")).Value);
            var english = Assert.IsType<List<CategoryChildResponse>>(Assert.IsType<OkObjectResult>(controller.Children("5", "en")).Value);

            Assert.Equal(new[] { "Zon", "Öl" }, swedish.Select(c => c.Name));
            Assert.Equal(new[] { "Öl", "Zon" }, english.Select(c => c.Name));
        }

        [Fact]
        public void Children_OfAParentTheUserCannotRead_ListsNothing()
        {
            var parent = Category(5, "Parent");
            IContent parentContent = parent;
            var repo = new Mock<IContentRepository>();
            repo.Setup(r => r.TryGet<IContent>(It.IsAny<ContentReference>(), It.IsAny<LoaderOptions>(), out parentContent)).Returns(true);

            var result = CreateController(repo, access: ReadAccessExcept(parent)).Children("5");

            Assert.Empty(Assert.IsType<List<CategoryChildResponse>>(Assert.IsType<OkObjectResult>(result).Value));
            repo.Verify(r => r.GetChildren<CategoryData>(It.IsAny<ContentReference>(), It.IsAny<LoaderOptions>()), Times.Never);
        }

        [Fact]
        public void Create_WithALanguage_CreatesTheCategoryInThatLanguage()
        {
            var repo = new Mock<IContentRepository>();
            var category = new CategoryData();
            repo
                .Setup(r => r.GetDefault<CategoryData>(It.IsAny<ContentReference>(), It.IsAny<int>(), It.IsAny<CultureInfo>()))
                .Returns(category);
            repo
                .Setup(r => r.Save(It.IsAny<IContent>(), SaveAction.Publish, AccessLevel.Edit))
                .Returns(new ContentReference(42));

            CreateController(repo).Create(new CreateCategoryRequest { ParentLink = "5", Name = "Nyheter", ContentTypeId = 99, Language = "sv" });

            repo.Verify(r => r.GetDefault<CategoryData>(
                It.Is<ContentReference>(p => p.ID == 5),
                99,
                It.Is<CultureInfo>(c => c.Name == "sv")), Times.Once);
        }

        [Fact]
        public void Selectable_WithNoIds_ReturnsEmptyList()
        {
            var repo = new Mock<IContentRepository>(MockBehavior.Strict);
            var controller = CreateController(repo);

            var result = controller.Selectable(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<CategorySelectableResponse>>(ok.Value);
            Assert.Empty(list);
            repo.VerifyNoOtherCalls();
        }
    }
}
