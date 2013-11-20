namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   using Sitecore.Data.Items;
   using Sitecore.Diagnostics;
   using Sitecore.Jobs;
   using Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia;
   using Sitecore.Modules.MediaConversionTool.Utils;
   using Sitecore.Pipelines;
   using Sitecore.Security.Accounts;

   public class ConvertMediaItemContext : PipelineArgs
   {
      public ConvertMediaItemContext(ConversionCandidate candidate, ConvertMediaContext convertMediaContext)
      {
         Assert.ArgumentNotNull(candidate, "candidate");
         Assert.ArgumentNotNull(convertMediaContext, "mediaContext");

         this.Job = convertMediaContext.Job;
         this.User = convertMediaContext.User;
         this.MediaContext = convertMediaContext;
         this.ConversionOptions = convertMediaContext.Options;
         this.Item = Utils.GetItem(candidate.Uri);
      }

      public virtual void AbortPipeline(ConversionAction action, string message)
      {
         this.Result = new ConvertMediaItemResult(action, message);
         base.AbortPipeline();
      }

      public ConvertMediaContext MediaContext { get; set; }
      public Job Job { get; set; }
      public User User { get; set; }
      public Item Item { get; set; }
      public ConversionOptions ConversionOptions { get; set; }
      public ConvertMediaItemResult Result { get; set; }
      public string CleanupReference { get; set; }
   }
}