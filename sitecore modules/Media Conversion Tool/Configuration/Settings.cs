namespace Sitecore.Modules.MediaConversionTool.Configuration
{
   public class Settings
   {
      public static readonly string BlobFieldName = "blob";
      public static readonly string FilePathFieldName = "file path";

      public static bool DeleteConvertedFiles
      {
         get
         {
            return Sitecore.Configuration.Settings.GetBoolSetting("MediaConversion.DeleteConvertedFiles", false);
         }
      }

      public static bool EnableDebugLogging
      {
         get
         {
            return Sitecore.Configuration.Settings.GetBoolSetting("MediaConversion.EnableDebugLogging", false);
         }
      }

      public static bool DeleteConvertedBlobs
      {
         get
         {
            return Sitecore.Configuration.Settings.GetBoolSetting("MediaConversion.DeleteConvertedBlobs", false);
         }
      }

      public static int ConsecutiveErrorLimit
      {
         get
         {
            return Sitecore.Configuration.Settings.GetIntSetting("MediaConversion.ConsecutiveErrorLimit", 20);
         }
      }
   }
}