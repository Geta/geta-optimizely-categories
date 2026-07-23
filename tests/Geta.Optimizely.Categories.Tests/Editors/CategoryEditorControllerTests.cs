// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using EPiServer;
using EPiServer.Core;
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

            var controller = new CategoryEditorController(repo.Object);

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
                .Setup(r => r.Save(It.IsAny<IContent>(), SaveAction.Save, AccessLevel.Edit))
                .Returns(new ContentReference(77));

            var controller = new CategoryEditorController(repo.Object);

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
            repo.Verify(r => r.Save(category, SaveAction.Save, AccessLevel.Edit), Times.Once);
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
            var controller = new CategoryEditorController(repo.Object);

            var result = controller.Create(new CreateCategoryRequest { ParentLink = "5", Name = name });

            Assert.IsType<BadRequestObjectResult>(result);
            repo.VerifyNoOtherCalls();
        }

        [Fact]
        public void Create_WithInvalidParentLink_ReturnsBadRequest()
        {
            var repo = new Mock<IContentRepository>(MockBehavior.Strict);
            var controller = new CategoryEditorController(repo.Object);

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
            var controller = new CategoryEditorController(repo.Object);

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

            var controller = new CategoryEditorController(repo.Object);

            var result = controller.Selectable("10, 20, 30, not-a-link");

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<CategorySelectableResponse>>(ok.Value);

            Assert.True(list.Single(x => x.Id == "10").Selectable);   // IsSelectable = true
            Assert.False(list.Single(x => x.Id == "20").Selectable);  // IsSelectable = false → disabled in picker
            Assert.True(list.Single(x => x.Id == "30").Selectable);   // not resolvable → don't restrict
            Assert.True(list.Single(x => x.Id == "not-a-link").Selectable); // unparsable → don't restrict
        }

        [Fact]
        public void Selectable_WithNoIds_ReturnsEmptyList()
        {
            var repo = new Mock<IContentRepository>(MockBehavior.Strict);
            var controller = new CategoryEditorController(repo.Object);

            var result = controller.Selectable(null);

            var ok = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<CategorySelectableResponse>>(ok.Value);
            Assert.Empty(list);
            repo.VerifyNoOtherCalls();
        }
    }
}
