using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;

namespace Foundation.Features.CategoryTesting
{
    [ContentType]
    public class BasicCategory : CategoryData
    {
        public virtual bool IsOverview { get; set; }
    }
}
