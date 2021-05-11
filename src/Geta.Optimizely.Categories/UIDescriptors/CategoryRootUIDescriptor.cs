using EPiServer.Shell;

namespace Geta.Optimizely.Categories.UIDescriptors
{
    [UIDescriptorRegistration]
    public class CategoryRootUIDescriptor : UIDescriptor<CategoryRoot>, IEditorDropBehavior
    {
        public CategoryRootUIDescriptor() : base("epi-iconObjectFolder")
        {
            DefaultView = "formedit";
            EditorDropBehaviour = EditorDropBehavior.CreateLink;
            ContainerTypes = new[] { typeof(CategoryData) };
        }

        public EditorDropBehavior EditorDropBehaviour { get; set; }
    }
}