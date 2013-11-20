namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using System.Linq;

   using Sitecore.Diagnostics;

   public class AddMediaItemsToQueue : ConvertMediaProcessor
   {
      //TODO: add support to fill the queue from a file with failed attempts records. File may contain timestamp of a session to offer a user to choose a starting point.
      /// <summary>
      /// Main entry method for AddMediaItemsToQueue pipeline processor.
      /// </summary>
      /// <param name="context">ConvertMediaContext object.</param>
      public override void Process(ConvertMediaContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         context.Queue.Add(context.References.Select(reference => new ConversionCandidate(reference.ItemUri, reference.Recursive)));
      }
   }
}