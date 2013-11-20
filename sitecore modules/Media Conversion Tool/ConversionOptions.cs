namespace Sitecore.Modules.MediaConversionTool
{
   public class ConversionOptions
   {
      public ConversionOptions() {}

      public ConversionOptions(ConversionType conversionType, bool forceStop)
      {
         this.ConversionType = conversionType;
         this.ForceStop = forceStop;
      }

      public bool ForceStop { get; set; }
      public ConversionType ConversionType { get; set; }
   }
}