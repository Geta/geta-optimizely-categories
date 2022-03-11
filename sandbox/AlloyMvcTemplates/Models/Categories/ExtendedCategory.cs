using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace AlloyMvcTemplates.Models.Categories
{

    [ContentType]
	public class ExtendedCategory : BasicCategory
	{
        [CultureSpecific]
		public virtual XhtmlString MainBody { get; set; }
	}
}
