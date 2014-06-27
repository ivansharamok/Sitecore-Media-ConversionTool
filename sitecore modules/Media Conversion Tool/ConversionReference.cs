namespace Sitecore.Modules.MediaConversionTool
{
    using Sitecore.Data;

    public class ConversionReference
    {
        public ItemUri ItemUri { get; set; }
        public bool Recursive { get; set; }

        public ConversionReference(ItemUri uri, bool recursive)
        {
            this.ItemUri = uri;
            this.Recursive = recursive;
        }
    }
}