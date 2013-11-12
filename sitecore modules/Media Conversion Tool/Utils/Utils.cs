using System.Collections.Generic;

namespace Sitecore.Modules.MediaConversionTool
{
   using System.Linq;

   using Data.Fields;
   using Data.Items;
   using Resources.Media;

   internal sealed class Utils
   {
      private Utils()
      {
      }

      /// <summary>
      /// Gets all versions of the item that has media content.
      /// </summary>
      /// <param name="item">The item</param>
      /// <returns>Item[]</returns>
      public static Item[] GetAllVersionsWithMedia(Item item)
      {
         bool versionable = IsVersionable(item);

         if (!versionable && IsMediaItem(item))
            return new Item[] { item };

         return item.Versions.GetVersions(versionable).Where(IsMediaItem).ToArray();
      }

      public static bool IsMediaItem(Item item)
      {
         return MediaManager.HasMediaContent(item);
      }

      public static bool IsVersionable(Item item)
      {
         Field mediaField = item.Fields["blob"];
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
