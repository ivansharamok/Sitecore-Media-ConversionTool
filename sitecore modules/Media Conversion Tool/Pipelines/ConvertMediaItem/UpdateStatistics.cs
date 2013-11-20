namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   using Sitecore.Diagnostics;
   using Sitecore.Jobs;
   using Sitecore.StringExtensions;

   public class UpdateStatistics : ConvertMediaItemProcessor
   {
      /// <summary>
      /// Main entry method for UpdateStatistics pipeline processor.
      /// </summary>
      /// <param name="context"></param>
      public override void Process(ConvertMediaItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         this.UpdateContextStatistics(context);
         this.UpdateJobStatistics(context);
         this.TraceInfo(context);
      }

      private void TraceInfo(ConvertMediaItemContext context)
      {
         if (TraceToLog)
         {
            var result = context.Result;
            var item = context.Item;
            string action = result != null ? result.Action.ToString() : "(null)";
            string message = result != null ? result.Message : "(null)";

            Log.Info("##MCT Item: {0} - {1}".FormatWith(item.Name, item.Uri), this);
            Log.Info("##MCT Action: {0}".FormatWith(action), this);
            Log.Info("##MCT Explanation: {0}".FormatWith(message), this);
         }
      }

      private void UpdateJobStatistics(ConvertMediaItemContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         var result = context.Result;
         if (result != null && result.Action != ConversionAction.None)
         {
            Job job = context.Job;
            if (job != null)
            {
               JobStatus status = job.Status;
               status.Processed++;
            }
         }
      }

      private void UpdateContextStatistics(ConvertMediaItemContext context)
      {
         var result = context.Result;
         var convertMediaContext = context.MediaContext;
         if (result != null && convertMediaContext != null)
         {
            switch (result.Action)
            {
               case ConversionAction.None:
               case ConversionAction.Skipped:
                  convertMediaContext.Statistics.Skipped++;
                  break;
               case ConversionAction.Processed:
                  convertMediaContext.Statistics.Processed++;
                  convertMediaContext.Statistics.ConsecutiveErrors = 0;
                  break;
               case ConversionAction.Failed:
                  var statistics = convertMediaContext.Statistics;
                  statistics.Failed.Add(context.Item.Uri, result.Message);
                  convertMediaContext.Statistics.ConsecutiveErrors++;
                  break;
            }
         }
      }

      public virtual bool TraceToLog { get; set; }
   }
}