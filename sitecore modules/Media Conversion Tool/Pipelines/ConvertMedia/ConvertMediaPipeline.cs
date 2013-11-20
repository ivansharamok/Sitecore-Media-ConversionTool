namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using Sitecore.Diagnostics;
   using Sitecore.Pipelines;

   public static class ConvertMediaPipeline
   {
      public static void Run(ConvertMediaContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         CorePipeline.Run("convertMedia", context);
      }
   }
}