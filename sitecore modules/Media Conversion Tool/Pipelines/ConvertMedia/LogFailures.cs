namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using System.Collections.Generic;
   using Sitecore.Data;
   using Sitecore.Diagnostics;
   using Sitecore.StringExtensions;

   public class LogFailures : ConvertMediaProcessor
   {
      /// <summary>
      /// Main entry method for LogFailures pipeline processor.
      /// </summary>
      /// <param name="context">ConvertMediaContext object.</param>
      public override void Process(ConvertMediaContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         var failures = context.Statistics.Failed;
         if (failures.Count > 0)
         {
            Log.Warn("<MCT> Failed Converstions: {0}".FormatWith(new []{ failures.Count }), this);
         }
         foreach (KeyValuePair<ItemUri, string> failure in failures)
         {
            Log.Info(
               DetailedLogging
                  ? "<MCT> reference: {0}, message: {1}".FormatWith(new object[] {failure.Key, failure.Value})
                  : "<MCT> reference: {0}".FormatWith(new object[] {failure.Key}), this);
         }
      }

      public bool DetailedLogging { get; set; }
   }
}