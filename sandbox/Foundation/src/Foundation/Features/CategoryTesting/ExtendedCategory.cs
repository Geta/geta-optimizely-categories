using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;

namespace Foundation.Features.CategoryTesting
{
    [ContentType]
    public class ExtendedCategory : BasicCategory
    {
        [CultureSpecific]
        public virtual XhtmlString MainBody { get; set; }
    }
}
