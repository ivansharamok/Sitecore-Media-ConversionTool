namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   public abstract class ConvertMediaItemProcessor
   {
      /// <summary>
      /// Main entry method for {type} pipeline processor.
      /// </summary>
      /// <param name="context"></param>
      public abstract void Process(ConvertMediaItemContext context);
   }
}