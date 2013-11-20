using System;
using System.Collections.Generic;

using Sitecore.Jobs;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Configuration;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Shell.Framework;

namespace Sitecore.Modules.MediaConversionTool.UI
{
   using System.Linq;

   public class MainPage : DialogForm
   {
      protected Radiogroup TargetGroup;
      protected Toolbutton Databases;
      protected DataContext DataContext;
      protected Listview ItemList;
      protected DataTreeview Treeview;

      private static readonly string MessageNoItemsSelected = "You should select at least one media item";
      private static readonly string DialogName = "MediaConversionToolMainForm";
      private static readonly string ConvertToFile = "File";
      private static readonly string ConvertToBlob = "Blob";
      
      protected override void OnLoad(EventArgs e)
      {
         if (!Context.ClientPage.IsEvent)
         {
            this.DataContext.GetFromQueryString();
            this.Databases.Header = Context.ContentDatabase.Name;
            this.InitializeControls();
         }
         base.OnLoad(e);
      }

      public static void RedirectTo()
      {
         string url = UIUtil.GetUri(string.Format("control:{0}", DialogName));
         Context.ClientPage.ClientResponse.SetLocation(url);
      }

      public void ListContextMenu()
      {
         if (Context.ClientPage.FindControl(Context.ClientPage.ClientRequest.Source) is ListviewItem)
         {
            Context.ClientPage.ClientResponse.DisableOutput();
            Menu parent = new Menu();
            MenuItem control = new MenuItem();
            Context.ClientPage.AddControl(parent, control);
            control.Header = "Exclude";
            control.Icon = "applications/16x16/delete2.png";
            control.Click = "Remove(\"" + Context.ClientPage.ClientRequest.Source + "\")";
            Context.ClientPage.ClientResponse.EnableOutput();
            Context.ClientPage.ClientResponse.ShowContextMenu(string.Empty, "right", parent);
         }
      }

      public void OnDoubleClick()
      {
         Item selectedItem = DataContext.GetFolder();
         if (selectedItem != null && selectedItem.Children.Count == 0)
         {
            this.AddItem();
         }
      }

      public void ConvertTargetClick(string controlId)
      {
         foreach (var button in this.TargetGroup.Controls.OfType<Radiobutton>())
         {
            if (button.ID.Equals(controlId))
            {
               button.Checked = true;
               this.TargetGroup.Value = button.Value;
            }
            else
            {
               button.Checked = false;
            }
         }
      }

      public void AddItem()
      {
         this.AddEntry(this.DataContext.GetFolder(), "single", "software/16x16/element.png");
      }

      public void AddTree()
      {
         this.AddEntry(this.DataContext.GetFolder(), "recursive", "software/16x16/branch.png");
      }

      public void Remove(string id)
      {
         ListviewItem[] selectedItems;
         selectedItems = id.Length == 0 ? this.ItemList.SelectedItems : new ListviewItem[] { Context.ClientPage.FindControl(id) as ListviewItem };

         foreach (ListviewItem item in selectedItems.Where(item => item != null))
         {
            item.Parent.Controls.Remove(item);
            Context.ClientPage.ClientResponse.Remove(item.ID, true);
         }
      }

      protected override void OnOK(object sender, EventArgs args)
      {
         if (this.ItemList.Items.Length == 0)
         {
            Context.ClientPage.ClientResponse.Alert(MessageNoItemsSelected);
            return;
         }

         List<ConversionReference> itemsToProcess = new List<ConversionReference>();
         foreach (ListviewItem item in this.ItemList.Items)
         {
            string[] textArray = item.Value.Split(new char[] { ':' }, 2);
            ItemUri uri = ItemUri.Parse(textArray[1]);
            if (uri != null)
            {
               itemsToProcess.Add(new ConversionReference(uri, textArray[0] == "recursive"));
            }
         }

         ConversionType conversionType = this.TargetGroup.Value.Equals(ConvertToBlob, StringComparison.InvariantCultureIgnoreCase) ? ConversionType.Blob : ConversionType.File;
         //Job job = MigrationWorker.CreateJob(itemsToProcess.ToArray(), conversionType);
         var options = new ConversionOptions(conversionType, false);
         Job job = MediaConversionManager.StartConversion(itemsToProcess, options, Context.User);
         job.Options.CustomData = options;
         JobManager.Start(job);
         string url = "/sitecore/shell/default.aspx?xmlcontrol=MediaConversionToolWorkingForm&handle=" + job.Handle;
         SheerResponse.SetLocation(url);
      }

      protected void ChangeDatabase(string name)
      {
         DataContext.Parameters = "databasename=" + name;
         Treeview.RefreshRoot();
         Databases.Header = name;
      }

      protected void OnRefreshClick()
      {
         this.Treeview.RefreshRoot();
      }

      protected void ShowDatabases()
      {
         Menu contextMenu = new Menu();
         string text = StringUtil.GetString(ServerProperties["Database"]);
         Context.ClientPage.ClientResponse.DisableOutput();
         try
         {
            foreach (string text2 in Factory.GetDatabaseNames())
            {
               if (!Factory.GetDatabase(text2).ReadOnly)
               {
                  MenuItem child = new MenuItem();
                  contextMenu.Controls.Add(child);
                  child.Header = text2;
                  child.Icon = "Business/16x16/data.png";
                  child.Click = "ChangeDatabase(\"" + text2 + "\")";
                  child.Checked = text2 == text;
               }
            }
         }
         finally
         {
            Context.ClientPage.ClientResponse.EnableOutput();
         }
         Context.ClientPage.ClientResponse.ShowContextMenu("DatabaseSelector", "dropdown", contextMenu);
      }

      protected override void OnCancel(object sender, EventArgs args)
      {
         Windows.Close();
      }

      #region Private methods

      private void AddEntry(Item item, string type, string icon)
      {
         Context.ClientPage.ClientResponse.DisableOutput();
         ListviewItem control;
         try
         {
            control = new ListviewItem();
            control.ID = Control.GetUniqueID("ListItem");
            Context.ClientPage.AddControl(this.ItemList, control);
            string text = item.Uri.ToString();
            control.Icon = icon;
            control.Header = string.Format("{0}, {1}:{2}", type, item.Database.Name, item.Paths.Path);
            control.Value = string.Format("{0}:{1}", type, text);
         }
         finally
         {
            Context.ClientPage.ClientResponse.EnableOutput();
         }
         Context.ClientPage.ClientResponse.Refresh(this.ItemList);
      }

      private void InitializeControls()
      {
         string[] sources = new string[] { ConvertToBlob, ConvertToFile };
         foreach (var source in sources)
         {
            Radiobutton button = new Radiobutton
            {
               ID = Control.GetUniqueID("Radiobutton"),
               Header = source,
               Value = source.ToLowerInvariant(),
               Checked = source == sources[0]
            };

            button.Click = "ConvertTargetClick(\"" + button.ID + "\")";
            this.TargetGroup.Controls.Add(button);
         }

         this.TargetGroup.Value = sources[0].ToLowerInvariant();
      }

      #endregion Private methods
   }
}
