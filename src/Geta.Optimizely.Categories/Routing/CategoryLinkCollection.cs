using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;

namespace Geta.Optimizely.Categories.Extensions
{
    // Helper class to allow url caching of the collection
    public class CategoryLinkCollection
    {
        // Only used for caching the value
        private const string Separator = ",";

        public CategoryLinkCollection(IEnumerable<ContentReference> categoryLinks)
        {
            CategoryLinks = categoryLinks ?? Enumerable.Empty<ContentReference>();
        }

        public IEnumerable<ContentReference> CategoryLinks { get; }

        public override string ToString()
        {
            return string.Join(Separator, CategoryLinks.Select(x => x.ToString()));
        }
    }
}