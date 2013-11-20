namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using System.Collections.Generic;
   using Sitecore.Jobs;
   using Sitecore.Pipelines;
   using Sitecore.Security.Accounts;

   public class ConvertMediaContext : PipelineArgs
   {
      public ConvertMediaContext(List<ConversionReference> references)
      {
         this.References = references;
         this.Queue = new List<IEnumerable<ConversionCandidate>>();
         this.Statistics = new ConversionStatistics();
      }

      #region Properties
      
      public Job Job { get; set; }
      public List<IEnumerable<ConversionCandidate>> Queue { get; private set; }
      public ConversionStatistics Statistics { get; private set; }
      public User User { get; set; }
      public List<ConversionReference> References { get; set; }
      public ConversionOptions Options { get; set; }

      #endregion Properties
   }
}