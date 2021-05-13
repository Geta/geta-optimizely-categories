using EPiServer.Core;

namespace AlloyMvcTemplates.Models.Pages
{
    public interface IHasRelatedContent
    {
        ContentArea RelatedContentArea { get; }
    }
}