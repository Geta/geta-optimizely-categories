using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;

namespace AlloyMvcTemplates.Models.Categories
{
    [ContentType(GUID = "CED5399C-1460-4D97-8FC8-7525D7CF22EB")]
    public class SiteCategory : CategoryData
    {
        [CultureSpecific]
        public virtual string SomeValue { get; set; }
    }
}