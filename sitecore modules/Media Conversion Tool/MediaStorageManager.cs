namespace Sitecore.Modules.MediaConversionTool
{
   using System;
   using System.Collections.Generic;
   using System.Diagnostics;
   using System.IO;
   using Sitecore.Data.Items;
   using Sitecore.Diagnostics;
   using Sitecore.IO;
   using Sitecore.Resources.Media;
   using Sitecore.SecurityModel;
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
      /// Changes storage of a specific item (with subitems optionally) based on what filter returns.
      /// </summary>
      /// <param name="item">The item.</param>
      /// <param name="filter">The filter.</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively.</param>
      public static void ChangeStorage(Item item, MediaStorageFilter filter)
      {
         Assert.ArgumentNotNull(item, "item");
         Assert.ArgumentNotNull(filter, "filter");

         IterateItems(item, true, delegate(Item it)
                                   {
                                      if(!MediaManager.HasMediaContent(it))
                                         return;

                                      MediaStorageType storageType = filter(it);
                                      switch(storageType)
                                      {
                                         case MediaStorageType.Unchanged:
                                            break;
                                         case MediaStorageType.FileSystem:
                                            ChangeToFileSystem(it);
                                            break;
                                         case MediaStorageType.Database:
                                            ChangeToDatabase(it);
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
      public static void ChangeToDatabase(MediaItem mediaItem)
      {
         Assert.ArgumentNotNull(mediaItem, "mediaItem");

         ChangeToDatabase(mediaItem.InnerItem);
      }

      /// <summary>
      /// Migrates media items and stores the destination media in database.
      /// </summary>
      /// <param name="item">The item.</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively.</param>
      public static int ChangeToDatabase(Item item)
      {
         Assert.ArgumentNotNull(item, "item");

         List<string> filesToDelete = new List<string>();
         int processedItems = IterateItems(item, true, it => ConvertToBlob(it, filesToDelete));

         if (Settings.DeleteConvertedFiles)
         {
            DeleteFiles(filesToDelete);
         }

         return processedItems;
      }

      /// <summary>
      /// This method migrates media items and stores the destination media as files.
      /// </summary>
      /// <param name="mediaItem">The media item.</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively.</param>
      public static void ChangeToFileSystem(MediaItem mediaItem)
      {
         Assert.ArgumentNotNull(mediaItem, "mediaItem");

         ChangeToFileSystem(mediaItem.InnerItem);
      }

      /// <summary>
      /// This method migrates media items and stores the destination media as files.
      /// </summary>
      /// <param name="item">The item.</param>
      /// <param name="recursive">if set to <c>true</c> the method will be run recursively.</param>
      public static int ChangeToFileSystem(Item item)
      {
         Assert.ArgumentNotNull(item, "item");

         return IterateItems(item, true,
            it => ConvertToFile(it));
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

         // Skip media that already in database.
         // NOTE: cannot use mediaItem.FileBased property as it returns true when there is at least one FileBased version on the item.
         //if (!mediaItem.FileBased)
#if DEBUG
Stopwatch timer = Stopwatch.StartNew();
#endif
         Item innerItem = mediaItem.InnerItem;
         var filePath = innerItem["file path"];
         var blobField = innerItem.Fields["blob"];
         if (blobField.HasBlobStream)
         {
            LogInfo("Media item '{0}' already has BLOB content. Processing of the item is skipped".FormatWith(new object[]
            {
               innerItem.Uri.ToString()
            }));
            ScheduleFileToBeRemoved(filePath, filesToDelete);
            ClearFilePath(innerItem);
            return;
         }
#if DEBUG
timer.Stop();
Log.Debug("ConvertToBlob: mediaItem.HasBlogStream for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
#endif
         // Downloading a file and saving them to a blob field
         Stream memoryStream = null;
         string downloadUrl = string.Empty;
         try
         {
#if DEBUG
timer.Reset();
timer.Start();
#endif
            Media mediaData = MediaManager.GetMedia(mediaItem);
#if DEBUG
timer.Stop();
Log.Debug("ConvertToBlob:MediaManager.GetMedia(item) for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
timer.Reset();
timer.Start();
#endif
            memoryStream = mediaItem.GetMediaStream();
#if DEBUG
timer.Stop();
Log.Debug("ConvertToBlob:mediaItem.GetStream() for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
#endif
            if (memoryStream == null)
            {
               Log.Warn(string.Format("Cannot find media data at item '{0}'", mediaItem.MediaPath),
                  typeof (MediaStorageManager));
               return;
            }

            // Check whether storage is allowed for media content
            if (memoryStream.Length > Configuration.Settings.Media.MaxSizeInDatabase)
            {
               return;
            }

            try
            {
#if DEBUG
timer.Reset();
timer.Start();
#endif
               ClearFilePath(innerItem);
               using (new EditContext(innerItem))
               {
                  blobField.SetBlobStream(memoryStream);
               }
#if DEBUG
timer.Stop();
Log.Debug("ConvertToBlob: empty file path and set BLOB value for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
#endif

               LogInfo("Media item '{0}' with a file reference '{1}' was converted into BLOB".FormatWith(new object[]
               {
                  innerItem.Uri.ToString(),
                  filePath
               }));
            }
            catch (Exception ex)
            {
               innerItem.Editing.RejectChanges();
               Log.Error("Failed to convert file '{0}' into BLOB for '{1}' media item".FormatWith(new object[]
               {
                  filePath, innerItem.Uri.ToString()
               }), ex, typeof (MediaStorageManager));
               throw;
            }

            ScheduleFileToBeRemoved(filePath, filesToDelete);
         }
         catch (IOException ex)
         {
            Log.Error(
               string.Format("Can't insert blob to item '{0}' from '{1}'", innerItem.Paths.FullPath,
                  downloadUrl), ex, typeof (MediaStorageManager));
         }
         catch (Exception ex)
         {
            Log.Error(
               string.Format("Can't insert blob to item '{0}' from '{1}'", innerItem.Paths.FullPath,
                  downloadUrl), ex, typeof (MediaStorageManager));
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

         Item innerItem = mediaItem.InnerItem;

         if (innerItem["file path"].Length > 0)
         {
            LogInfo("Media item '{0}' already has file based content. Processing of the item is skipped".FormatWith(new object[]
            {
               innerItem.Uri.ToString()
            }));

            return;
         }

         // Have to use "blob" field in order to workaround an issue with mediaItem.GetStream() 
         // as it uses mediaItem.FileBased property that returns "true" if at least one version has a file based media asset.
         var blobField = innerItem.Fields["blob"];
#if DEBUG
Stopwatch timer = Stopwatch.StartNew();
#endif
         Stream stream = blobField.GetBlobStream();
#if DEBUG
timer.Stop();
Log.Debug("ConvertToFile: blobField.GetBlobStream() for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
#endif
         if (stream == null)
         {
            Log.Warn(string.Format("Cannot find media data at item '{0}'", mediaItem.MediaPath), typeof(MediaStorageManager));
            return;
         }

#if DEBUG
timer.Reset();
timer.Start();
#endif
         string fileName = GetFilename(mediaItem, stream);
#if DEBUG
timer.Stop();
Log.Debug("ConvertToFile: GetFilename() for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
timer.Reset();
timer.Start();
#endif
         string relativePath = FileUtil.UnmapPath(fileName);
#if DEBUG
timer.Stop();
Log.Debug("ConvertToFile: FileUtil.UnmapPath() for fileName '{0}' took '{1:c}'".FormatWith(new object[] { fileName, timer.Elapsed }), typeof(MediaStorageManager));
#endif
         try
         {
            if (!File.Exists(fileName))
            {
#if DEBUG
timer.Reset();
timer.Start();
#endif
               SaveToFile(stream, fileName);
#if DEBUG
timer.Stop();
Log.Debug("ConvertToFile: SaveToFile() for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
#endif
            }

#if DEBUG
timer.Reset();
timer.Start();
#endif
            using (new EditContext(innerItem))
            {
               innerItem["file path"] = relativePath;
            }
#if DEBUG
timer.Stop();
Log.Debug("ConvertToFile: setting file path for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
#endif

            LogInfo("BLOB stream of '{0}' media item was converted to '{1}' file".FormatWith(new object[]
            {
               innerItem.Uri.ToString(),
               relativePath
            }));

#if DEBUG
timer.Reset();
timer.Start();
#endif
            if (Settings.DeleteBlobDBContent)
            {
               // Delegate blob clearing process to a background job.
               Sitecore.Threading.ManagedThreadPool.QueueUserWorkItem(ClearBlobField, innerItem);
            }
#if DEBUG
timer.Stop();
Log.Debug("ConvertToFile: clearing BLOB for item '{0}' took '{1:c}'".FormatWith(new object[] { innerItem.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
#endif
         }
         catch (Exception ex)
         {
            Log.Error(string.Format("Cannot convert BLOB stream of '{0}' media item to '{1}' file", mediaItem.MediaPath, relativePath), ex, typeof(MediaStorageManager));
         }
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
            {
               break;
            }
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

      private static void ClearBlobField(object stateObj)
      {
         Item item = (Item) stateObj;
         var fieldName = "blob";

         try
         {
#if DEBUG
            Stopwatch timer = Stopwatch.StartNew();
#endif
            using (new EditContext(item, SecurityCheck.Disable))
            {
               // This may causes performance issue. The code triggers removal of old blobs (Item.Delete(true)) which generates time comsuming query for each blob that is cleared.
               // Sitecore.Data.DataProviders.Sql.RemoveOldBlobs().
               // This potentially may cause removal of all reference to the media items from other fields.
               
               // MediaData.ReleaseStream() sets blob field value to empty string.
               item[fieldName] = string.Empty;
               //item[fieldName] = null;
            }

            if (Settings.EnableDetailedLogging)
            {
               LogInfo("Blob field for media item '{0}' was cleared".FormatWith(new object[]
                  {
                     item.Uri
                  }));
            }
#if DEBUG
            timer.Stop();
            Log.Debug("ClearBlobField: clearing BLOB field for item '{0}' took '{1:c}'".FormatWith(new object[] { item.Uri.ToString(), timer.Elapsed }), typeof(MediaStorageManager));
#endif
         }
         catch (Exception ex)
         {
            Log.Warn("Failed to clear BLOB field for '{0}' media item".FormatWith(new[]
            {
               item.Paths.MediaPath
            }), ex, typeof(MediaStorageManager));
         }
      }

      private static void ClearFilePath(Item innerItem)
      {
         using (new EditContext(innerItem))
         {
            innerItem.RuntimeSettings.ReadOnlyStatistics = true;
            if (!string.IsNullOrEmpty(innerItem["Path"]))
            {
               innerItem["Path"] = string.Empty;
            }

            innerItem["file path"] = string.Empty;
         }
      }

      private static void ScheduleFileToBeRemoved(string filePath, List<string> filesToDelete)
      {
         string fullPath = FileUtil.MapPath(filePath);
         if (filesToDelete.IndexOf(fullPath) < 0)
         {
            filesToDelete.Add(FileUtil.MapPath(filePath));
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

      private static int IterateItems(Item item, bool allVersions, OperationForItem operation)
      {
         int processedEntities = 0;
         item.Reload();
         if (allVersions)
         {
            var versions = Modules.MediaConversionTool.Utils.GetAllVersionsWithMedia(item);
            foreach (Item version in versions)
            {
               operation(version);
               processedEntities++;
            }
         }
         else
         {
            operation(item);
            processedEntities++;
         }

         return processedEntities;
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
         catch (Exception ex)
         {
             Log.Error("Failed to compare file stream of '{0}' to a blob stream.", ex, typeof(MediaStorageManager));
             return false;
         }
         finally
         {
             if (stream.CanSeek)
             {
                 stream.Seek(0, SeekOrigin.Begin);
             }
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

         public static bool DeleteBlobDBContent
         {
            get
            {
               return Configuration.Settings.GetBoolSetting("MediaStorageManager.DeleteBlobDBContent", false);
            }
         }

      //   public static long CompareStreamSizeFrom
      //   {
      //      get
      //      {
      //         // Default value is 2MB.
      //         return Configuration.Settings.GetLongSetting("MediaStorageManager.CompareStreamSizeFrom", 2 * 1048576);
      //      }
      //   }
      }

      #endregion Settings class
   }
}
