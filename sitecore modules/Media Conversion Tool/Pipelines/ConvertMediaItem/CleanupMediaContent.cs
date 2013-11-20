namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   using System;
   using System.Collections.Generic;
   using Sitecore.Configuration;
   using Sitecore.Data;
   using Sitecore.Data.DataProviders.Sql;
   using Sitecore.Data.Items;
   using Sitecore.Data.SqlServer;
   using Sitecore.Diagnostics;
   using Sitecore.IO;
   using Sitecore.Security.Accounts;
   using Sitecore.StringExtensions;
   using Sitecore.Threading;

   public class CleanupMediaContent : ConvertMediaItemProcessor
   {
      /// <summary>
      /// Main entry method for CleanupMediaContent pipeline processor.
      /// </summary>
      /// <param name="context"></param>
      public override void Process(ConvertMediaItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         this.DetermineCleanupOption(context);
      }

      protected virtual void DetermineCleanupOption(ConvertMediaItemContext context)
      {
         var conversionType = context.MediaContext.Options.ConversionType;
         var keyValue = new KeyValuePair<Item, string>(context.Item, context.CleanupReference);
         switch (conversionType)
         {
            case ConversionType.Blob:
               if (Configuration.Settings.DeleteConvertedFiles && !string.IsNullOrEmpty(context.CleanupReference))
                  ManagedThreadPool.QueueUserWorkItem(this.RemoveFile, context);
               break;
            case ConversionType.File:
               if (Configuration.Settings.DeleteConvertedBlobs && !string.IsNullOrEmpty(context.CleanupReference))
                  ManagedThreadPool.QueueUserWorkItem(this.RemoveBlob, context);
               break;
         }
      }

      protected virtual void RemoveFile(object stateObj)
      {
         //var keyValue = (KeyValuePair<Item, string>) stateObj;
         var context = (ConvertMediaItemContext) stateObj;
         var user = context.User;
         var item = context.Item;
         string reference = context.CleanupReference;
         try
         {
            using (new UserSwitcher(user))
            {
               this.CleanFilePathField(item);
               using (new LongRunningOperationWatcher(250, "Deleting file '{0}'", new string[] {reference}))
               {
                  FileUtil.Delete(reference);
               }
            }
            if (Configuration.Settings.EnableDebugLogging)
               Log.Info("##MCT File removed: '{0}'. Item reference: {1} - {2}".FormatWith(reference, item.Name, item.Uri.ToString()), this);
         }
         catch (Exception exception)
         {
            Log.Warn("##MCT Failed to delete file '{0}'. Item reference: {1} - {2}".FormatWith(reference, item.Name, item.Uri.ToString()), exception, this);
         }
      }

      protected virtual void RemoveBlob(object stateObj)
      {
         var context = (ConvertMediaItemContext) stateObj;
         var item = context.Item;
         string reference = context.CleanupReference;
         try
         {
            var sqlDataApi = this.CreateDataApi(item.Database.ConnectionStringName);
            using (new UserSwitcher(context.User))
            {
               this.RemoveBlobFromDatabase(item, sqlDataApi, reference);
            }
         }
         catch (Exception exception)
         {
            Log.Warn("##MCT Failed to remove blob '{0}'. Item reference: {1} - {2}.\n\r{3}".FormatWith(reference, item.Name, item.Uri.ToString(), exception.ToString()), this);
         }
      }

      protected virtual void RemoveBlobFromDatabase(Item item, SqlDataApi sqlDataApi, string reference)
      {
         Assert.ArgumentNotNull(sqlDataApi, "sqlDataApi");
         Assert.ArgumentNotNullOrEmpty(reference, "reference");
         string[] sqlTables = {"SharedFields", "UnversionedFields", "VersionedFields", "ArchivedFields"};
         string sql = "DELETE FROM {0}tableName{1} WHERE {0}Value{1} LIKE {2}blobId{3}";
         try
         {
            //// Transaction may cause deadlocks on large data sets. It's likely to happen for VersionedFields as there is a lot of entries in there.
            ////using (DataProviderTransaction transaction = sqlDataApi.CreateTransaction())
            ////{
            //foreach (string table in sqlTables)
            //{
            //   int entryCount;
            //   using (new LongRunningOperationWatcher(1000, "Cleaning references for BlobId '{0}' from table '{1}'", new [] {reference, table}))
            //   {
            //      entryCount = sqlDataApi.Execute(sql.Replace("tableName", table),
            //         new object[] {"blobId", reference});
            //   }
            //   if (Configuration.Settings.EnableDebugLogging)
            //      Log.Info(
            //         "##MCT Clean blob references: {0} related blob entries removed from {1} table".FormatWith(
            //            entryCount, table), this);
            //}
            using (new LongRunningOperationWatcher(1000, "Cleaning blob field for item: {0} - {1}", new[] {item.Name, item.Uri.ToString()}))
            {
               this.CleanBlob(item, reference);
            }
            ////transaction.Complete();
            if (Configuration.Settings.EnableDebugLogging)
               Log.Info("##MCT Removed blob with id '{0}'".FormatWith(reference), this);
            ////}
         }
         catch (Exception exception)
         {
            Log.Warn("##MCT Exception occured while cleaning blob related entries.", exception, this);
         }
      }

      private void CleanBlob(Item item, string reference)
      {
         Assert.ArgumentNotNull(item, "item");
         Assert.ArgumentNotNullOrEmpty(reference, "reference");
         //item.Database.DataManager.DataSource.RemoveBlobStream(MainUtil.GetGuid(reference));
         // Clean cache references
         using (new EditContext(item))
         {
            item[Configuration.Settings.BlobFieldName] = string.Empty;
         }
      }

      private void CleanFilePathField(Item item)
      {
         using (new EditContext(item))
         {
            item[Configuration.Settings.FilePathFieldName] = string.Empty;
         }
      }

      protected virtual SqlDataApi CreateDataApi(string connectionStringName)
      {
         Assert.ArgumentNotNullOrEmpty(connectionStringName, "connectionStringName");
         string connectionString = Settings.GetConnectionString(connectionStringName);
         Assert.ArgumentNotNullOrEmpty(connectionString, "connectionString");
         return new SqlServerDataApi(connectionString);
      }
   }
}