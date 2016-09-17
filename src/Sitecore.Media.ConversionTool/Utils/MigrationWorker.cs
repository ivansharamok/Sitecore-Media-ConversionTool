namespace Sitecore.Media.ConversionTool.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Configuration;
    using Data;
    using Data.Items;
    using Diagnostics;
    using Jobs;

    internal class MigrationWorker
    {
        private readonly ItemReference[] dataToPrecess;
        private readonly bool convertToBlob;

        private MigrationWorker(ItemReference[] elements, bool convertToBlob)
        {
            Assert.ArgumentNotNull(elements, "elements");
            this.dataToPrecess = elements;
            this.convertToBlob = convertToBlob;
        }

        public static Job CreateJob(ItemReference[] elements, bool convertToBlob)
        {
            MigrationWorker worker = new MigrationWorker(elements, convertToBlob);
            JobOptions options = new JobOptions("MediaMigration", "MediaMigration", "shell", worker, "ThreadEntry");
            return new Job(options);
        }

        public void ThreadEntry()
        {
            try
            {
                KeyValuePair<Item, int>[] allItems = GetAllItems(this.dataToPrecess);

                int versionsCount = allItems.Sum(itemPair => itemPair.Value);

                Context.Job.Status.Total = versionsCount;
                Context.Job.Status.Processed = 0;

                foreach (KeyValuePair<Item, int> pair in allItems)
                {
                    Item item = pair.Key;

                    Context.Job.Status.Messages.Add($"Current item is {GetItemPath(item)}");

                    if (this.convertToBlob)
                    {
                        MediaStorageManager.ChangeToDatabase(item, false);
                    }
                    else
                    {
                        MediaStorageManager.ChangeToFileSystem(item, false);
                    }

                    Context.Job.Status.Processed += pair.Value;
                }
            }
            catch (Exception e)
            {
                Log.Error("Unexpected error", e, typeof(MigrationWorker));
            }
        }

        #region Private methods

        private static KeyValuePair<Item, int>[] GetAllItems(IEnumerable<ItemReference> references)
        {
            List<KeyValuePair<Item, int>> result = new List<KeyValuePair<Item, int>>();
            foreach (ItemReference reference in references)
            {
                Database database = Factory.GetDatabase(reference.ItemUri.DatabaseName);
                Item item = database.GetItem(reference.ItemUri.ItemID);

                DoJob(item, reference.Recursive,
                   delegate (Item it)
                   {
                       Item[] itemsWithMedia = Utils.GetAllVersionsWithMedia(it);
                       if (itemsWithMedia != null && itemsWithMedia.Length > 0)
                       {
                           result.Add(new KeyValuePair<Item, int>(it, itemsWithMedia.Length));
                       }
                   });
            }

            return result.ToArray();
        }

        private static string GetItemPath(Item item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            MediaItem mediaItem = new MediaItem(item);
            return item.Database.Name + ": " + mediaItem.MediaPath;
        }

        private delegate void OperationForItem(Item item);

        private static void DoJob(Item item, bool recursive, OperationForItem operation)
        {
            item.Reload();
            operation(item);

            if (recursive)
            {
                foreach (Item childItem in item.Children)
                {
                    DoJob(childItem, true, operation);
                }
            }
        }

        #endregion Private methods

        #region ItemReference class

        internal class ItemReference
        {
            public ItemUri ItemUri;
            public bool Recursive;

            public ItemReference(ItemUri uri, bool recursive)
            {
                this.ItemUri = uri;
                this.Recursive = recursive;
            }
        }

        #endregion   ItemReference class
    }
}