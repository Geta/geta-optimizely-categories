namespace Geta.Optimizely.Categories.UIDescriptors
{
    [UIDescriptorRegistration]
    public class CategoryDataUIDescriptor : UIDescriptor<CategoryData>
    {
        public CategoryDataUIDescriptor() : base("epi-iconObjectCategory")
        {
            CommandIconClass = "epi-iconCategory";
            IsPrimaryType = true;
            ContainerTypes = new[]
            {
                typeof (CategoryData)
            };
        }
    }
}