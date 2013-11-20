namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using System.Collections.Generic;
   using Sitecore.Data;

   public class ConversionCandidate
   {
      public ConversionCandidate(ItemUri uri, bool deep)
      {
         this.Uri = uri;
         this.ItemId = uri.ItemID;
         this.DatabaseName = uri.DatabaseName;
         this.Deep = deep;
      }

      public ConversionCandidate(ItemUri uri) : this(uri, false){}

      public bool Deep { get; set; }

      public IEnumerable<ConversionCandidate> Children
      {
         get
         {
            return ConversionQueue.GetChildIterator(this.Uri, this.Deep);
         }
      }

      public ID ItemId { get; set; }

      public ItemUri Uri { get; set; }

      public string DatabaseName { get; set; }
   }
}