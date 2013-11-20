namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using System.Collections.Generic;
   using Sitecore.Data;
   using Sitecore.Data.Items;
   using Sitecore.Diagnostics;
   using Sitecore.Modules.MediaConversionTool.Utils;

   public class ConversionQueue
   {
      #region Fields

      private readonly Database _database; 

      #endregion Fields

      #region Constructor

      public ConversionQueue(Database database)
      {
         Assert.ArgumentNotNull(database, "database");
         this._database = database;
      } 

      #endregion Constructor

      public static IEnumerable<ConversionCandidate> GetContentBranch(ConversionReference reference)
      {
         Assert.ArgumentNotNull(reference, "converstion reference");
         yield return new ConversionCandidate(reference.ItemUri, reference.Recursive);
      }

      public static IEnumerable<ConversionCandidate> GetChildIterator(ItemUri uri, bool deep)
      {
         Assert.ArgumentNotNull(uri, "itemUri");
         Item item = Utils.GetItem(uri);
         if (item != null)
         {
            foreach (Item child in item.Children)
            {
               yield return new ConversionCandidate(child.Uri, deep);
            }
         }
      }

      public static IEnumerable<ConversionCandidate> GetChildIterator(ItemUri uri)
      {
         return GetChildIterator(uri, false);
      }
   }
}