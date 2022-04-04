using EPiServer.DataAnnotations;
using Geta.Optimizely.Categories;

namespace AlloyMvcTemplates.Models.Categories
{
    [ContentType]
	public class BasicCategory : CategoryData
	{
        public virtual bool IsOverview { get; set; }
    }
}
