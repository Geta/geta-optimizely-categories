namespace Geta.Optimizely.Categories.EditorDescriptors
{
    [EditorDescriptorRegistration(TargetType = typeof(ContentReference), UIHint = CategoryUIHint.Category)]
    public class CategoryEditorDescriptor : ContentReferenceEditorDescriptor<CategoryData>
    {
        public override string RepositoryKey => CategoryContentRepositoryDescriptor.RepositoryKey;
    }
}