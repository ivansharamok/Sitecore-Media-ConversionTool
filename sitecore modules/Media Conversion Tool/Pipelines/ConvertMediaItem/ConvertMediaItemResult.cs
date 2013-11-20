namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   public class ConvertMediaItemResult
   {
      public ConvertMediaItemResult(ConversionAction action, string message)
      {
         this.Action = action;
         this.Message = message;
      }

      public ConversionAction Action { get; set; }
      public string Message { get; set; }
   }
}