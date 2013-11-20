namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   using Sitecore.Data.Items;
   using Sitecore.Diagnostics;
   using Sitecore.Modules.MediaConversionTool.Configuration;
   using Sitecore.StringExtensions;

   /// <summary>
   /// Evaluates whether a media should be converted.
   /// </summary>
   public class EvaluateMediaHandling : ConvertMediaItemProcessor
   {
      /// <summary>
      /// Main entry method for EvaluateMediaHandling pipeline processor.
      /// </summary>
      /// <param name="context"></param>
      public override void Process(ConvertMediaItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");

         bool skipProcessing = false;
         string message = string.Empty;
         switch (context.MediaContext.Options.ConversionType)
         {
            case ConversionType.Blob:
               skipProcessing = this.SkipBlobConversion(context, ref message);
               break;
            case ConversionType.File:
               skipProcessing = this.SkipFileConversion(context, ref message);
               break;
         }

         if (skipProcessing)
         {
            context.AbortPipeline(ConversionAction.Skipped, message);
         }
      }

      private bool SkipFileConversion(ConvertMediaItemContext context, ref string message)
      {
         var item = context.Item;
         var filePath = item[Settings.FilePathFieldName];
         if (filePath.Length > 0)
         {
            var blobField = item.Fields[Settings.BlobFieldName];
            context.CleanupReference = blobField != null ? blobField.Value : string.Empty;
            message = "Item {0} - {1} already has a file path.".FormatWith(item.Name, item.Uri.ToString());
            return true;
         }

         return false;
      }

      private bool SkipBlobConversion(ConvertMediaItemContext context, ref string message)
      {
         var item = context.Item;
         MediaItem mediaItem = item;
         var blobField = item.Fields[Settings.BlobFieldName];
         if (blobField != null)
         {
            if (blobField.HasBlobStream)
            {
               context.CleanupReference = mediaItem.FilePath;
               message = "Item: {0} - {1} already has a blob stream assigned to it.".FormatWith(item.Name,
                  item.Uri.ToString());
               return true;
            }
         }
         else
         {
            message = "Item {0} - {1} does not have a blob field.".FormatWith(item.Name, item.Uri.ToString());
            return true;
         }

         return false;
      }
   }
}