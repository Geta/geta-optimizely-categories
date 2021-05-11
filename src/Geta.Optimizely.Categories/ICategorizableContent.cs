using System.Collections.Generic;

namespace Geta.Optimizely.Categories
{
    public interface ICategorizableContent
    {
         IList<ContentReference> Categories { get; set; }
    }
}