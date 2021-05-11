using System.Collections.Generic;
using EPiServer.Core;

namespace Geta.Optimizely.Categories
{
    public interface ICategorizableContent
    {
         IList<ContentReference> Categories { get; set; }
    }
}