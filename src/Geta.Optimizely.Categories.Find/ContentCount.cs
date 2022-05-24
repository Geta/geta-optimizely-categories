using EPiServer.Core;

namespace Geta.Optimizely.Categories.Find
{
    public class ContentCount
    {
        public readonly string Name;
        public readonly ContentReference ContentLink;
        public readonly int Count;

        public ContentCount(IContent content, int count)
        {
            Name = content.Name;
            ContentLink = content.ContentLink;
            Count = count;
        }
    }
}