using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using EPiServer.Personalization.VisitorGroups;
using Geta.Optimizely.Categories.EditorDescriptors;

namespace Geta.Optimizely.Categories
{
    public class CategoryCriterionSettings : CriterionModelBase
    {
        [Required]
        [CriterionPropertyEditor(SelectionFactoryType = typeof(CategoryListing))]
        [DisplayName("Category")]
        public string CategoryId { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        [CriterionPropertyEditor(DefaultValue = 0)]
        [DisplayName("Viewed at least")]
        public int ViewedTimes { get; set; }

        public override ICriterionModel Copy() => ShallowCopy();
    }
}
