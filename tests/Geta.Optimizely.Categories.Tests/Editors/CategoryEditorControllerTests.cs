// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

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
            Assert.Equal("New Cat", ((IContent)category).Name);

            repo.Verify(r => r.GetDefault<CategoryData>(
                It.Is<ContentReference>(p => p.ID == 5),
                99), Times.Once);
            repo.Verify(r => r.Save(category, SaveAction.Publish, AccessLevel.Edit), Times.Once);
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
    }
}
