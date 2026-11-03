// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using EPiServer.Core;
using Geta.Optimizely.Categories.EditorDescriptors;
using Moq;

namespace Geta.Optimizely.Categories.Tests.EditorDescriptors
{
    public class CategoryListingTests
    {
        // The visitor-group criterion's list takes every level from the category loader, so each is in category
        // order, not only the top level.
        [Fact]
        public void EveryLevel_IsListedThroughTheCategoryLoader()
        {
            var sport = Category(10, "Sport");
            var football = Category(11, "Football");
            var archive = Category(12, "Archive");
            var categories = new Mock<ICategoryContentLoader>();
            categories.Setup(c => c.GetGlobalCategories<CategoryData>()).Returns(new[] { sport });
            categories.Setup(c => c.GetChildren<CategoryData>(sport.ContentLink)).Returns(new[] { football, archive });

            var items = new CategoryListing(categories.Object).GetSelectListItems(typeof(IList<ContentReference>));

            Assert.Equal(new[] { "Sport", "-Football", "-Archive" }, items.Select(i => i.Text));
        }

        private static CategoryData Category(int id, string name) => new() { ContentLink = new ContentReference(id), Name = name };
    }
}
