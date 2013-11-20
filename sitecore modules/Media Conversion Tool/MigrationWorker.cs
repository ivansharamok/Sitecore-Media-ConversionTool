namespace Sitecore.Modules.MediaConversionTool
{
   using System;
   using System.Collections.Generic;
   using System.Diagnostics;

   using Sitecore.Data;
   using Sitecore.Data.Items;
   using Sitecore.Diagnostics;
   using Sitecore.Configuration;
   using Sitecore.Jobs;
   using Sitecore.StringExtensions;

   internal class MigrationWorker
   {
      private readonly ConversionReference[] _dataToProcess;
      private readonly ConversionType _conversionType;

      private MigrationWorker(ConversionReference[] elements, ConversionType conversionType)
      {
         Assert.ArgumentNotNull(elements, "elements");
         this._dataToProcess = elements;
         this._conversionType = conversionType;
      }

      public static Job CreateJob(ConversionReference[] elements, ConversionType conversionType)
      {
         MigrationWorker worker = new MigrationWorker(elements, conversionType);
         JobOptions options = new JobOptions("MediaMigration", "MediaMigration", "shell", worker, "ThreadEntry");
         return new Job(options);
      }

      public void ThreadEntry()
      {
         try
         {
            ProcessReferences(this._dataToProcess);
         }
         catch (Exception e)
         {
            Log.Error("Unexpected error", e, typeof(MigrationWorker));
         }
      }

      private void ProcessReferences(ConversionReference[] conversionReferences)
      {
         Context.Job.Status.Processed = 0;

         foreach (var reference in conversionReferences)
         {
            Database database = Factory.GetDatabase(reference.ItemUri.DatabaseName);
            Item item = database.GetItem(reference.ItemUri.ItemID);

            ProcessEntries(GetItemEnumerator(item), reference.Recursive);
         }
      }

      private IEnumerable<Item> GetItemEnumerator(Item item)
      {
         if (item != null)
         {
            yield return item;
         }
      }

      private IEnumerable<Item> GetChildEnumerator(Item item)
      {
#if DEBUG
Stopwatch timer = Stopwatch.StartNew();
#endif
         if (item != null)
         {
            foreach (Item child in item.Children)
            {
               yield return child;
            }

         }
#if DEBUG
timer.Stop();
Log.Debug("GetChildEnumerator for item '{0}' took '{1:c}'".FormatWith(new object[] { item.Uri.ToString(), timer.Elapsed }), this);
#endif
      }

      private void ProcessEntries(IEnumerable<Item>  items, bool recursive)
      {
         try
         {
            foreach (Item item in items)
            {
               int processedEntities = 0;

               Context.Job.Status.Messages.Add(string.Format("Current item is {0}", GetItemPath(item)));

               switch (this._conversionType)
               {
                  case ConversionType.Blob:
#if DEBUG
Stopwatch timer = Stopwatch.StartNew();
#endif
                     processedEntities = MediaStorageManager.ChangeToDatabase(item);
#if DEBUG
timer.Stop();
Log.Debug("ChangeToDatabase for item '{0}' took '{1:c}'".FormatWith(new object[]{ item.Uri.ToString(), timer.Elapsed}), this);
#endif
                     break;
                  case ConversionType.File:
#if DEBUG
timer = Stopwatch.StartNew();
#endif
                     processedEntities = MediaStorageManager.ChangeToFileSystem(item);
#if DEBUG
                  timer.Stop();
                  Log.Debug("ChangeToFileSystem for item '{0}' took '{1:c}'".FormatWith(new object[] { item.Uri.ToString(), timer.Elapsed }), this);
#endif
                     break;
               }

               Context.Job.Status.Processed += processedEntities;

               if (recursive)
               {
                  ProcessEntries(GetChildEnumerator(item), true);
               }
            }
         }
         catch (Exception e)
         {
            Log.Error("Unexpected error", e, typeof(MigrationWorker));
         }
      }

      #region Private methods

      private static string GetItemPath(Item item)
      {
         if (item == null)
         {
            return string.Empty;
         }

         MediaItem mediaItem = new MediaItem(item);
         return item.Database.Name + ": " + mediaItem.MediaPath;
      }

      #endregion Private methods
   }
}