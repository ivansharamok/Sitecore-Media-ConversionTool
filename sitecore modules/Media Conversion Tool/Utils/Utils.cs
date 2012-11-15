namespace Sitecore.Modules.MediaConversionTool
{
   using System.Linq;

   using Sitecore.Data.Fields;
   using Sitecore.Data.Items;
   using Sitecore.Resources.Media;

   internal sealed class Utils
   {
      private Utils()
      {
      }

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
         if (media == null || media.MediaData == null)
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
         if (sizeInBytes < 1000)
         {
            return sizeInBytes + " bytes";
         }
         if (sizeInBytes < 1000000)
         {
            return (sizeInBytes / 1000) + " kb";
         }

         long temp = sizeInBytes / 10000;
         double megs = temp / 100.0;

         return megs.ToString("n") + " mb";
      }
   }
}
