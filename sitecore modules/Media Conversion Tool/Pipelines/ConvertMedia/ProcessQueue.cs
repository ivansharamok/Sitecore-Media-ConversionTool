namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using System.Collections.Generic;
   using System.Linq;
   using Sitecore.Data;
   using Sitecore.Data.Items;
   using Sitecore.Diagnostics;
   using Sitecore.Jobs;
   using Sitecore.Modules.MediaConversionTool.Configuration;
   using Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem;
   using Sitecore.Modules.MediaConversionTool.Utils;
   using Sitecore.StringExtensions;

   public class ProcessQueue : ConvertMediaProcessor
   {
      /// <summary>
      /// Main entry method for ProcessQueue pipeline processor.
      /// </summary>
      /// <param name="context">ConvertMediaContext object.</param>
      public override void Process(ConvertMediaContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         foreach (IEnumerable<ConversionCandidate> candidates in context.Queue)
         {
            if (context.Options.ForceStop)
               break;
            this.ProcessEntries(candidates, context);
         }
         this.UpdateJobStatus(context);
      }

      private void UpdateJobStatus(ConvertMediaContext context)
      {
         Job job = context.Job;
         if (job != null)
         {
            job.Status.Messages.Add("Items processed: {0}".FormatWith(new []{ context.Statistics.Processed }));
            job.Status.Messages.Add("Items skipped: {0}".FormatWith(new[] { context.Statistics.Skipped }));
            job.Status.Messages.Add("Items failed: {0}".FormatWith(new[] { context.Statistics.Failed.Count }));
         }
      }

      protected virtual void ProcessEntries(IEnumerable<ConversionCandidate> entries, ConvertMediaContext context)
      {
         foreach (ConversionCandidate entry in entries)
         {
            if (context.Statistics.ConsecutiveErrors >= Settings.ConsecutiveErrorLimit)
               context.Options.ForceStop = true;
            if (context.Options.ForceStop) break;

            foreach (var versionCandidate in this.GetCandidateVersions(entry))
            {
               ConvertMediaItemPipeline.Run(this.CreateMediaItemContext(versionCandidate, context));
            }

            if (entry.Deep)
            {
               this.ProcessEntries(entry.Children, context);
            }
         }
      }

      private ConvertMediaItemContext CreateMediaItemContext(ConversionCandidate entry, ConvertMediaContext context)
      {
         Assert.ArgumentNotNull(entry, "entry");
         var mediaItemContext = new ConvertMediaItemContext(entry, context);
         return mediaItemContext;
      }

      protected virtual IEnumerable<ConversionCandidate> GetCandidateVersions(ConversionCandidate candidate)
      {
         Item item = Database.GetItem(candidate.Uri);
         var versions = Utils.GetAllVersionsWithMedia(item).Select(ver => new ConversionCandidate(ver.Uri, candidate.Deep));
         return versions;
      }
   }
}