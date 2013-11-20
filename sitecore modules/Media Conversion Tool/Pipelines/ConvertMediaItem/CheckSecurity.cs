namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   using Sitecore.Diagnostics;
   using Sitecore.Modules.MediaConversionTool.Utils;

   public class CheckSecurity : ConvertMediaItemProcessor
   {
      /// <summary>
      /// Main entry method for CheckSecurity pipeline processor.
      /// </summary>
      /// <param name="context"></param>
      public override void Process(ConvertMediaItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         string message = string.Empty;
         if (!Utils.CanConvert(context.Item, context.User, ref message))
         {
            context.AbortPipeline(ConversionAction.Skipped, message);
         }
      }
   }
}
