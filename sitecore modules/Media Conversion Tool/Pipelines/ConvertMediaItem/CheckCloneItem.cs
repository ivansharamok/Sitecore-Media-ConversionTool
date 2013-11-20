namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   using Sitecore.Diagnostics;
   using Sitecore.StringExtensions;

   public class CheckCloneItem : ConvertMediaItemProcessor
   {
      /// <summary>
      /// Main entry method for CheckCloneItem pipeline processor.
      /// </summary>
      /// <param name="context"></param>
      public override void Process(ConvertMediaItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         if (context.Item.IsClone)
         {
            context.AbortPipeline(ConversionAction.Skipped, "Item: {0} - {1} is a clone.".FormatWith(new object[] { context.Item.Name, context.Item.Uri }));
         }
      }
   }
}