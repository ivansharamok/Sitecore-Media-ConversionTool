namespace Sitecore.Modules.MediaConversionTool.Commands
{
   using System.Reflection;

   using Data.DataProviders;
   using Reflection;
   using Shell.Applications.Dialogs.ProgressBoxes;
   using StringExtensions;
   using Diagnostics;
   using Shell.Framework.Commands;

   public class CleanBlobs : Command
   {
      public override void Execute([NotNull] CommandContext context)
      {
         DataProvider[] providers = Client.ContentDatabase.GetDataProviders();
         CallContext callContext = new CallContext(Client.ContentDatabase.DataManager, providers.Length);
         var parameters = new object[] {providers, callContext};

         ProgressBox.Execute("CleanBlobs", "Cleaning blobs", CleanUnusedBlobs, parameters);
      }

      private void CleanUnusedBlobs(params object[] parameters)
      {
         var providers = parameters[0] as DataProvider[];
         var callContext = parameters[1] as CallContext;
         
         var parameterObjects = new object[] {callContext};
         foreach (DataProvider dataProvider in providers)
         {
            MethodInfo methdInfo = ReflectionUtil.GetMethod(dataProvider, "CleanupBlobs", true, true, parameterObjects);
            if (methdInfo != null)
            {
               ReflectionUtil.InvokeMethod(methdInfo, parameterObjects, dataProvider);
               Log.Info("{0} method of {1} type was called.".FormatWith(new object[] {methdInfo, dataProvider}), this);
            }
         }
      }
   }
}