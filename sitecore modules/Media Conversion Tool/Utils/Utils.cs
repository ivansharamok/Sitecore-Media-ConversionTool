namespace Sitecore.Modules.MediaConversionTool.Utils
{
   using System.Linq;
   using Sitecore.Configuration;
   using Sitecore.Data;
   using Sitecore.Data.Fields;
   using Sitecore.Data.Items;
   using Sitecore.Diagnostics;
   using Sitecore.Resources.Media;
   using Sitecore.Security.AccessControl;
   using Sitecore.Security.Accounts;
   using Sitecore.SecurityModel;
   using Sitecore.StringExtensions;

   internal static class Utils
   {
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

      /// <summary>
      /// Checks whether an item with media is data stored as a file
      /// </summary>
      /// <param name="item">An item with some media data</param>
      /// <returns></returns>
      public static bool IsFileBased(Item item)
      {
         Assert.ArgumentNotNull(item, "item");
         return new MediaItem(item).FilePath.Length > 0;
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

      public static Item GetItem(ID itemId, string databaseName)
      {
         Item item = null;
         Database database = Factory.GetDatabase(databaseName);
         if (database != null)
         {
            item = database.GetItem(itemId);
         }

         return item;
      }
      
      public static Item GetItem(ItemUri uri)
      {
         return Database.GetItem(uri);
      }

      public static bool CanConvert(Item item, User user, ref string message)
      {
         using (new SecurityEnabler())
         {
            bool flag = AuthorizationManager.IsAllowed(item, AccessRight.ItemRead, user) &&
                        AuthorizationManager.IsAllowed(item, AccessRight.ItemWrite, user);
            if (!flag)
            {
               message = "User does not have the required Read/Write access. To convert a media asset the user must have read and write access. User: {0}, Item: {1}".FormatWith(new object[] {user.Name, item.Uri});
            }
            return flag;
         }
      }
   }
}
