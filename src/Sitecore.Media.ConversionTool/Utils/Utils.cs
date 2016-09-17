namespace Sitecore.Media.ConversionTool.Utils
{
    using System.Linq;
    using Data.Fields;
    using Data.Items;
    using Resources.Media;

    public static class Utils
    {
        public static Item[] GetAllVersionsWithMedia(Item item)
        {
            if (item == null)
                return null;

            bool multilanguage = IsMultilanguagePicture(item);

            if (!multilanguage && IsMediaItem(item))
                return new Item[] { item };

            return item.Versions.GetVersions(multilanguage).Where(IsMediaItem).ToArray();
        }

        public static bool IsMediaItem(Item item)
        {
            return MediaManager.HasMediaContent(item);
        }

        public static bool IsMultilanguagePicture(Item item)
        {
            Media media = MediaManager.GetMedia(item);
            if (media?.MediaData == null)
            {
                return false;
            }

            Field mediaField = item.Fields[media.MediaData.DataFieldName];
            if (mediaField == null)
            {
                return false;
            }

            return !mediaField.Shared;
        }

        public static string GetFriendlyFileSize(long sizeInBytes)
        {
            if (sizeInBytes < KB)
            {
                return sizeInBytes + " Bytes";
            }
            if (sizeInBytes < MB)
            {
                return (sizeInBytes / KB) + " KB";
            }
            if (sizeInBytes < GB)
            {
                return (sizeInBytes/MB) + " MB";
            }

            var gb = sizeInBytes / GB;
            //double megs = temp / 100.0;

            return gb.ToString("n") + " GB";
        }

        public static int KB => 1024;

        public static int MB => KB*1024;

        public static int GB => MB*1024;
    }
}
