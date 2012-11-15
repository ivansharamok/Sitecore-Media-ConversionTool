namespace Sitecore.Resources.Media
{
   using System;
   using System.Collections.Generic;
   using System.IO;
   using System.Linq;

   using Sitecore.Data.Fields;
   using Sitecore.Data.Items;
   using Sitecore.Diagnostics;
   using Sitecore.IO;
   using Sitecore.StringExtensions;

   public enum MediaStorageType
   {
      Unchanged,
      Database,
      FileSystem
   }

   public delegate MediaStorageType MediaStorageFilter(MediaItem mediaItem);

   /// <summary>
   /// This class is used to change the type of storing (file/database) of media items
   /// </summary>
   public class MediaStorageManager
   {
      private delegate void OperationForItem(Item item);
      private delegate bool FileIsAcceptable(string fileName);

      #region Public methods

      /// <summary>
      /// Changes storage of specific items based on what filter returns
      /// </summary>
      /// <param name="item">The item.</param>
      /// <param name="filter">The filter.</param>
      public static void ChangeStorage(Item item, MediaStorageFilter filter)
      {
         ChangeStorage(item, false, filter);
      }

      /// <summary>
      /// Changes storage of a specific item (with subitems optionally) based on what filter returns.
      /// </summary>
      /// <param name="item">The item.</param>
      /// <param name="filter">The filter.</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively.</param>
      public static void ChangeStorage(Item item, bool recursive, MediaStorageFilter filter)
      {
         Assert.ArgumentNotNull(item, "item");
         Assert.ArgumentNotNull(filter, "filter");

         IterateItems(item, recursive, true, delegate(Item it)
                                   {
                                      if(!MediaManager.HasMediaContent(it))
                                         return;

                                      MediaStorageType storageType = filter(it);
                                      switch(storageType)
                                      {
                                         case MediaStorageType.Unchanged:
                                            break;
                                         case MediaStorageType.FileSystem:
                                            ChangeToFileSystem(it, false);
                                            break;
                                         case MediaStorageType.Database:
                                            ChangeToDatabase(it, false);
                                            break;
                                         default:
                                            throw  new ApplicationException(string.Format("Unexpected value of media storage type '{0}'", storageType));
                                      }
                                   });
      }

      /// <summary>
      /// Migrates media items and stores the destination media in database.
      /// </summary>
      /// <param name="mediaItem">The media item</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively</param>
      public static void ChangeToDatabase(MediaItem mediaItem, bool recursive)
      {
         Assert.ArgumentNotNull(mediaItem, "mediaItem");

         ChangeToDatabase(mediaItem.InnerItem, recursive);
      }

      /// <summary>
      /// Migrates media items and stores the destination media in database.
      /// </summary>
      /// <param name="item">The item.</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively.</param>
      public static void ChangeToDatabase(Item item, bool recursive)
      {
         Assert.ArgumentNotNull(item, "item");

         List<string> filesToDelete = new List<string>();
         IterateItems(item, recursive, true,
            delegate(Item it)
            {
               ConvertToBlob(it, filesToDelete);
            });

         if (Settings.DeleteConvertedFiles)
         {
            DeleteFiles(filesToDelete);
         }
      }

      /// <summary>
      /// This method migrates media items and stores the destination media as files.
      /// </summary>
      /// <param name="mediaItem">The media item.</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively.</param>
      public static void ChangeToFileSystem(MediaItem mediaItem, bool recursive)
      {
         Assert.ArgumentNotNull(mediaItem, "mediaItem");

         ChangeToFileSystem(mediaItem.InnerItem, recursive);
      }

      /// <summary>
      /// This method migrates media items and stores the destination media as files.
      /// </summary>
      /// <param name="item">The item.</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively.</param>
      public static void ChangeToFileSystem(Item item, bool recursive)
      {
         Assert.ArgumentNotNull(item, "item");

         IterateItems(item, recursive, true,
            delegate(Item it)
            {
               ConvertToFile(it);
            });
      }

      /// <summary>
      /// Checks whether an item with media is data stored as a file
      /// </summary>
      /// <param name="item">An item with some media data</param>
      /// <returns></returns>
      public static bool IsFileBased(Item item)
      {
         Assert.ArgumentNotNull(item, "item");
         return new MediaItem(item).FileBased;
      }

      #endregion

      #region Protected methods

      protected static void ConvertToBlob(MediaItem mediaItem, List<string> filesToDelete)
      {
         Assert.ArgumentNotNull(mediaItem, "mediaItem");
         Assert.ArgumentNotNull(filesToDelete, "filesToDelete");

         string path = mediaItem.InnerItem["path"];
         string filePath = mediaItem.InnerItem["file path"];
         string extension = mediaItem.InnerItem["extension"];

         // Downloading a file and saving them to a blob field
         Stream memoryStream = null;
         string downloadUrl = string.Empty;
         try
         {
            Media mediaData = MediaManager.GetMedia(mediaItem);

            memoryStream = mediaItem.GetMediaStream();
            if (memoryStream == null)
            {
               Log.Warn(string.Format("Cannot find media data at item '{0}'", mediaItem.MediaPath), typeof(MediaStorageManager));
               return;
            }

            // Check whether storage is allowed for media content
            if (memoryStream.Length > Configuration.Settings.Media.MaxSizeInDatabase)
            {
               return;
            }

            try
            {
               using (new EditContext(mediaItem.InnerItem))
               {
                  mediaItem.InnerItem.RuntimeSettings.ReadOnlyStatistics = true;
                  if (!string.IsNullOrEmpty(mediaItem.InnerItem["Path"]))
                  {
                     mediaItem.InnerItem["Path"] = string.Empty;
                  }

                  mediaItem.InnerItem["file path"] = string.Empty;
                  Field blobField = mediaItem.InnerItem.Fields[mediaData.MediaData.DataFieldName];
                  blobField.SetBlobStream(memoryStream);
               }

               LogInfo("Media item '{0}' with a file reference '{1}' was converted into BLOB".FormatWith(new object[]
               {
                  mediaItem.InnerItem.Uri.ToString(),
                  filePath
               }));
            }
            catch (Exception ex)
            {
               mediaItem.InnerItem.Editing.RejectChanges();
               Log.Error("Failed to convert file '{0}' into BLOB for '{1}' media item".FormatWith(new object[]
               {
                  filePath, mediaItem.InnerItem.Uri.ToString()
               }), ex, typeof(MediaStorageManager));
               throw;
            }

            string fullPath = FileUtil.MapPath(filePath);
            if (filesToDelete.IndexOf(fullPath) < 0)
            {
               filesToDelete.Add(fullPath);
            }
         }
         catch (IOException ex)
         {
            Log.Error(string.Format("Can't insert blob to item '{0}' from '{1}'", mediaItem.InnerItem.Paths.FullPath, downloadUrl), ex, typeof(MediaStorageManager));
         }
         catch (Exception ex)
         {
            Log.Error(string.Format("Can't insert blob to item '{0}' from '{1}'", mediaItem.InnerItem.Paths.FullPath, downloadUrl), ex, typeof(MediaStorageManager));
         }
         finally
         {
            if (memoryStream != null)
               memoryStream.Close();
         }
      }

      protected static void ConvertToFile(MediaItem mediaItem)
      {
         Assert.ArgumentNotNull(mediaItem, "mediaItem");

         if (mediaItem.InnerItem["file path"].Length > 0)
            return;

         // Have to use "blob" field in order to workaround an issue with mediaItem.GetStream() 
         // as it uses mediaItem.FileBased property that returns "true" if at least one version has a file based media asset.
         var blobField = mediaItem.InnerItem.Fields["blob"];
         Stream stream = blobField.GetBlobStream();
         if (stream == null)
         {
            Log.Warn(string.Format("Cannot find media data at item '{0}'", mediaItem.MediaPath), typeof(MediaStorageManager));
            return;
         }
         
         string fileName = GetFilename(mediaItem, stream);
         string relativePath = FileUtil.UnmapPath(fileName);
         try
         {
            if (!File.Exists(fileName))
            {
               SaveToFile(stream, fileName);
            }

            Item innerItem = mediaItem.InnerItem;
            using (new EditContext(innerItem))
            {
               innerItem["file path"] = relativePath;
            }

            LogInfo("BLOB stream of '{0}' media item was converted to '{1}' file".FormatWith(new object[]
            {
               mediaItem.InnerItem.Uri.ToString(),
               relativePath
            }));

            ClearBlobField(mediaItem);
         }
         catch (Exception ex)
         {
            Log.Error(string.Format("Cannot convert BLOB stream of '{0}' media item to '{1}' file", mediaItem.MediaPath, relativePath), ex, typeof(MediaStorageManager));
         }
      }

      /// <summary>
      /// Gets all versions of the item that has media content
      /// </summary>
      /// <param name="item">The item</param>
      /// <returns></returns>
      protected static Item[] GetAllVersionsWithMedia(Item item)
      {
         if (item == null)
            return null;

         if (!IsMultilanguagePicture(item))
            return new Item[] { item };

         bool multilanguage = IsMultilanguagePicture(item);
         return item.Versions.GetVersions(multilanguage).Where(MediaManager.HasMediaContent).ToArray();
      }

      #endregion Protected methods

      #region Private methods

      private static string GetFilename(MediaItem mediaItem, Stream stream)
      {
         // Creating directory if in not exists
         string relatedPath = GetDirectoryPath(mediaItem);
         string dirPath = FileUtil.MapPath(FileUtil.MakePath(Configuration.Settings.Media.FileFolder, relatedPath, '/'));
         if (!Directory.Exists(dirPath))
         {
            Directory.CreateDirectory(dirPath);
         }

         string extension = mediaItem.InnerItem["extension"] ?? string.Empty;
         string fileName = mediaItem.InnerItem.ID + mediaItem.Name;

         // Searching for an unused filename
         string filePath;
         for (int i = 0;; i++)
         {
            filePath = FileUtil.MakePath(FileUtil.MakePath(dirPath, fileName + (i == 0 ? string.Empty : i.ToString()), '/'), extension, '.');
            if (!File.Exists(filePath) || FileContainsStream(filePath, stream))
               break;
         }

         return FileUtil.MapPath(filePath);
      }

      /// <summary>
      /// Returns path to a physical directory in the same format as MediaCreator.GetMediaStorageFolder method.
      /// </summary>
      /// <param name="item">MediaItem instance</param>
      /// <returns>Path to directory</returns>
      private static string GetDirectoryPath(MediaItem item)
      {
         string itemId = item.InnerItem.ID.ToString();
         return string.Format("/{0}/{1}/{2}", itemId[1], itemId[2], itemId[3]);
      }

      private static bool IsMultilanguagePicture(Item item)
      {
         Media media = MediaManager.GetMedia(item);
         if (media == null || media.MediaData == null)
            return false;

         Field mediaField = item.Fields[media.MediaData.DataFieldName];
         if (mediaField == null)
            return false;

         return !mediaField.Shared;
      }

      private static void ClearBlobField(MediaItem mediaItem)
      {
         string fieldName = MediaManager.GetMedia(mediaItem).MediaData.DataFieldName;
         Item innerItem = mediaItem.InnerItem;
         try
         {
            using (new EditContext(innerItem))
            {
               innerItem[fieldName] = null;
               if (Settings.EnableDetailedLogging)
               {
                  LogInfo("Blob field for media item '{0}' was cleared".FormatWith(new object[]
                  {
                     innerItem.Uri
                  }));
               }
            }
         }
         catch (Exception ex)
         {
            Log.Warn("Failed to clear BLOB field for '{0}' media item".FormatWith(new[]
            {
               mediaItem.InnerItem.Paths.MediaPath
            }), ex, typeof(MediaStorageManager));
         }
      }

      private static void SaveToFile(Stream stream, string fileName)
      {
         byte[] buffer = new byte[8192];
         using (FileStream fs = File.Create(fileName))
         {
            int length;
            do
            {
               length = stream.Read(buffer, 0, buffer.Length);
               fs.Write(buffer, 0, length);
            }
            while (length > 0);

            fs.Flush();
            fs.Close();
         }
      }

      private static void DeleteFiles(IEnumerable<string> files)
      {
         foreach (string pathToFile in files)
         {
            try
            {
               File.Delete(pathToFile);
               LogInfo("File was deleted from path '{0}'".FormatWith(new object[]
               {
                  FileUtil.UnmapPath(pathToFile)
               }));
            }
            catch (Exception ex)
            {
               Log.Error(string.Format("Error deleting file from path '{0}'", FileUtil.UnmapPath(pathToFile)), ex,
                         typeof(MediaStorageManager));
            }
         }
      }

      private static void IterateItems(Item item, bool recursive, bool allVersions, OperationForItem operation)
      {
         item.Reload();
         if (allVersions)
         {
            Item[] versions = GetAllVersionsWithMedia(item);
            if (versions != null)
            {
               foreach (Item version in versions)
               {
                  operation(version);
               }
            }
         }
         else
         {
            operation(item);
         }

         if (recursive)
         {
            foreach (Item childItem in item.Children)
            {
               IterateItems(childItem, true, allVersions, operation);
            }
         }
      }

      // This check may consume more time then just deleting and recreating the file.
      // TODO: look into optimizing/removing this code.
      private static bool FileContainsStream(string fileName, Stream stream)
      {
         FileInfo fi = new FileInfo(fileName);
         if(fi.Length != stream.Length)
         {
            return false;
         }

         try 
         {
            using (FileStream fs = File.OpenRead(fileName))
            {
               return StreamsAreSimilar(fs, stream);
            }
         }
         finally
         {
            stream.Seek(0, SeekOrigin.Begin);   
         }
      }

      private static bool StreamsAreSimilar(Stream stream1, Stream stream2)
      {
         if(stream1.Length != stream2.Length)
            return false;

         const int bufLenght = 8192;
         byte[] buffer1 = new byte[bufLenght];
         byte[] buffer2 = new byte[bufLenght];

         int length;
         do
         {
            length = stream1.Read(buffer1, 0, bufLenght);
            stream2.Read(buffer2, 0, bufLenght);

            for(int i=0; i<length; i++)
            {
               if(buffer1[i] != buffer2[i])
                  return false;
            }
         }
         while (length > 0);
         

         return true;
      }

      private static void LogInfo(string message)
      {
         if (Settings.EnableDetailedLogging)
         {
            Log.Info(message, typeof(MediaStorageManager));
         }
      }

      #endregion private methods

      #region Settings class

      public static class Settings
      {
         public static bool DeleteConvertedFiles
         {
            get
            {
               return Configuration.Settings.GetBoolSetting("MediaStorageManager.DeleteConvertedFiles", false);
            }
         }

         public static bool EnableDetailedLogging
         {
            get
            {
               return Configuration.Settings.GetBoolSetting("MediaStorageManager.EnableDetailedLogging", false);
            }
         }
      }

      #endregion Settings class
   }
}
