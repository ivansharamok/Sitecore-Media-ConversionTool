namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   public abstract class ConvertMediaProcessor
   {
      /// <summary>
      /// Main entry method for ConvertMedia pipeline processor.
      /// </summary>
      /// <param name="context">ConvertMediaContext object.</param>
      public abstract void Process(ConvertMediaContext context);
   }
}