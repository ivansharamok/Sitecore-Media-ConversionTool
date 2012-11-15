namespace Sitecore.Modules.MediaConversionTool.Commands
{
   using Sitecore.Diagnostics;
   using Sitecore.Shell.Framework.Commands;
   using Sitecore.Text;

   public class OpenMediaConversionTool : Command
   {
      public override void Execute(CommandContext context)
      {
         Assert.ArgumentNotNull(context, "context");
         if (context.Items.Length == 1)
         {
            UrlString parameters = new UrlString();
            parameters.Append("fo", context.Items[0].ID.ToString());
            Shell.Framework.Windows.RunApplication("Media Conversion Tool", parameters.ToString());
         }
      }
   }
}