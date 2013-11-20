namespace Sitecore.Modules.MediaConversionTool
{
   using System.Collections.Generic;
   using Sitecore.Data;

   public class ConversionStatistics
   {
      public ConversionStatistics()
      {
         this.Failed = new Dictionary<ItemUri, string>();
      }

      /// <summary>
      /// Count of processed items.
      /// </summary>
      public int Processed { get; set; }

      /// <summary>
      /// Count of skipped items.
      /// </summary>
      public int Skipped { get; set; }

      //TODO: replace this property with a file pointer that records failed attempts.
      /// <summary>
      /// Count of failed items.
      /// </summary>
      public Dictionary<ItemUri, string> Failed { get; set; }

      /// <summary>
      /// Count of consecutive failures.
      /// </summary>
      internal int ConsecutiveErrors { get; set; }
   }
}