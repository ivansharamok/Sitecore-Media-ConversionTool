namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   using Sitecore.Diagnostics;
   using Sitecore.Pipelines;

   public static class ConvertMediaItemPipeline
   {
      public static void Run(ConvertMediaItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         CorePipeline.Run("convertMediaItem", context);
      }
   }
}