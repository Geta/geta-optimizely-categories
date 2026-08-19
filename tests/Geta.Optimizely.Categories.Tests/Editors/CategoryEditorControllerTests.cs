// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.Shell;
using Geta.Optimizely.Categories.Editors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Geta.Optimizely.Categories.Tests.Editors
{
    public class CategoryEditorControllerTests
    {
        // UIDescriptorRegistry.HasMatchedTypeIdentifier is public virtual, so it can be stubbed.
        // We stub rather than build real UIDescriptors because UIDescriptor's public constructor
        // resolves ViewConfigurations / IContentTypeRepository through ServiceLocator. The polymorphic
        // ancestor-chain matching itself is CMS's contract (and the same call CMS's own
        // DefaultContentQueryHelper.FilterTypeIdentifier makes), so these tests cover the controller's
        // own logic: which gate wins, and which reason is reported.
        private static Mock<UIDescriptorRegistry> CreateRegistry()
        {
            return new Mock<UIDescriptorRegistry>(
                Enumerable.Empty<UIDescriptor>(),
                Enumerable.Empty<UIDescriptorProvider>(),
                Mock.Of<ILogger<UIDescriptorRegistry>>());
        }

        private static CategoryEditorController CreateController(
            Mock<IContentRepository> repo,
            Mock<UIDescriptorRegistry> registry = null)
        {
            return new CategoryEditorController(repo.Object, (registry ?? CreateRegistry()).Object);
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
            var repo = new Mock<IContentRepository>();

            CategoryData selectable = new CategoryData { IsSelectable = true };
            CategoryData grouping = new CategoryData { IsSelectable = false };
            CategoryData missing = null;

            repo.Setup(r => r.TryGet<CategoryData>(It.Is<ContentReference>(c => c.ID == 10), out selectable)).Returns(true);
            repo.Setup(r => r.TryGet<CategoryData>(It.Is<ContentReference>(c => c.ID == 20), out grouping)).Returns(true);
            repo.Setup(r => r.TryGet<CategoryData>(It.Is<ContentReference>(c => c.ID == 30), out missing)).Returns(false);

            var controller = CreateController(repo);

            var result = controller.Selectable("10, 20, 30, not-a-link");

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<CategorySelectableResponse>>(ok.Value);

            Assert.True(list.Single(x => x.Id == "10").Selectable);   // IsSelectable = true
            Assert.False(list.Single(x => x.Id == "20").Selectable);  // IsSelectable = false → disabled in picker
            Assert.True(list.Single(x => x.Id == "30").Selectable);   // not resolvable → don't restrict
            Assert.True(list.Single(x => x.Id == "not-a-link").Selectable); // unparsable → don't restrict
        }

        // Issue #56 point 7: the Selectable endpoint is also where [AllowedTypes] is enforced, because
        // narrowing the shell store's `typeIdentifiers` instead would remove container categories from
        // the tree and make everything nested under them unreachable.
        [Fact]
        public void Selectable_WithAllowedTypes_DisablesCategoriesOfOtherTypes()
        {
            var repo = new Mock<IContentRepository>();
            CategoryData allowed = new CategoryData { IsSelectable = true };
            CategoryData other = new CategoryData { IsSelectable = true };
            repo.Setup(r => r.TryGet<CategoryData>(It.Is<ContentReference>(c => c.ID == 10), out allowed)).Returns(true);
            repo.Setup(r => r.TryGet<CategoryData>(It.Is<ContentReference>(c => c.ID == 20), out other)).Returns(true);

            // Both ids resolve to the same CLR type here, so drive the verdict off the id ordering by
            // stubbing the single call the controller makes per node.
            var registry = CreateRegistry();
            registry
                .SetupSequence(r => r.HasMatchedTypeIdentifier(typeof(CategoryData), It.IsAny<IEnumerable<string>>()))
                .Returns(true)    // id 10 → type is allowed
                .Returns(false);  // id 20 → type is not allowed

            var result = CreateController(repo, registry).Selectable("10, 20", "some.allowed.type");

            var list = Assert.IsType<List<CategorySelectableResponse>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(list.Single(x => x.Id == "10").Selectable);
            Assert.Null(list.Single(x => x.Id == "10").Reason);

            var disallowed = list.Single(x => x.Id == "20");
            Assert.False(disallowed.Selectable);
            Assert.Equal(CategoryEditorController.TypeNotAllowedReason, disallowed.Reason);
        }

        [Fact]
        public void Selectable_WithNoAllowedTypes_DoesNotConsultTheRegistry()
        {
            var repo = new Mock<IContentRepository>();
            CategoryData category = new CategoryData { IsSelectable = true };
            repo.Setup(r => r.TryGet<CategoryData>(It.IsAny<ContentReference>(), out category)).Returns(true);

            var registry = CreateRegistry();

            var result = CreateController(repo, registry).Selectable("10");

            var list = Assert.IsType<List<CategorySelectableResponse>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.True(list.Single().Selectable);
            registry.Verify(
                r => r.HasMatchedTypeIdentifier(It.IsAny<System.Type>(), It.IsAny<IEnumerable<string>>()),
                Times.Never);
        }

        [Fact]
        public void Selectable_RestrictedTypeWins_EvenWhenTheTypeIsAlsoAllowed()
        {
            var repo = new Mock<IContentRepository>();
            CategoryData category = new CategoryData { IsSelectable = true };
            repo.Setup(r => r.TryGet<CategoryData>(It.IsAny<ContentReference>(), out category)).Returns(true);

            var registry = CreateRegistry();
            // Restricted is evaluated first and short-circuits, so the allow-list is never consulted.
            registry
                .Setup(r => r.HasMatchedTypeIdentifier(
                    typeof(CategoryData),
                    It.Is<IEnumerable<string>>(ids => ids.Contains("blocked.type"))))
                .Returns(true);

            var result = CreateController(repo, registry)
                .Selectable("10", allowedTypes: "allowed.type", restrictedTypes: "blocked.type");

            var list = Assert.IsType<List<CategorySelectableResponse>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.False(list.Single().Selectable);
            Assert.Equal(CategoryEditorController.TypeNotAllowedReason, list.Single().Reason);
        }

        [Fact]
        public void Selectable_IsSelectableTakesPrecedenceOverTheTypeCheck()
        {
            var repo = new Mock<IContentRepository>();
            CategoryData grouping = new CategoryData { IsSelectable = false };
            repo.Setup(r => r.TryGet<CategoryData>(It.IsAny<ContentReference>(), out grouping)).Returns(true);

            var registry = CreateRegistry();

            var result = CreateController(repo, registry).Selectable("10", "some.allowed.type");

            var list = Assert.IsType<List<CategorySelectableResponse>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.False(list.Single().Selectable);
            // The more specific reason: the category itself is a grouping node, regardless of its type.
            Assert.Equal(CategoryEditorController.NotSelectableReason, list.Single().Reason);
            registry.Verify(
                r => r.HasMatchedTypeIdentifier(It.IsAny<System.Type>(), It.IsAny<IEnumerable<string>>()),
                Times.Never);
        }

        [Fact]
        public void Selectable_UnresolvableIds_StaySelectableAndSkipTheTypeCheck()
        {
            var repo = new Mock<IContentRepository>();
            CategoryData missing = null;
            repo.Setup(r => r.TryGet<CategoryData>(It.IsAny<ContentReference>(), out missing)).Returns(false);

            var registry = CreateRegistry();

            var result = CreateController(repo, registry).Selectable("999, not-a-link", "some.allowed.type");

            var list = Assert.IsType<List<CategorySelectableResponse>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.All(list, x => Assert.True(x.Selectable));
            registry.Verify(
                r => r.HasMatchedTypeIdentifier(It.IsAny<System.Type>(), It.IsAny<IEnumerable<string>>()),
                Times.Never);
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
