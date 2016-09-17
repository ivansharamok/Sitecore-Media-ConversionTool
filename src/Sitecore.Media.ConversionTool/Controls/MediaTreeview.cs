namespace Sitecore.Media.ConversionTool.Controls
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Web.UI;
    using Data.Items;
    using Diagnostics;
    using Utils;
    using Web.UI.HtmlControls;

    public class MediaTreeview : DataTreeview
    {
        private const string FieldSize = "size";
        private const string FieldStoredAs = "stored_as";
        private const string FieldIsMultilanguage = "sizeis_multilanguaged";

        private static readonly string[] PredefinedFieldNamesArr = new string[] { FieldSize, FieldStoredAs, FieldIsMultilanguage };
        private static readonly List<string> PredefinedFieldNames = new List<string>(PredefinedFieldNamesArr);

        protected override TreeNode GetTreeNode(Item item, System.Web.UI.Control parent)
        {
            DataTreeNode dataTreeNode = (DataTreeNode)base.GetTreeNode(item, parent);
            foreach (string fieldName in ColumnNames.Keys)
            {
                if (PredefinedFieldNames.IndexOf(fieldName) != -1)
                {
                    string fieldValue = GetPredefinedFieldValue(item, fieldName);
                    dataTreeNode.ColumnValues[fieldName] = fieldValue;
                }
            }
            return dataTreeNode;
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // To preventing bug with a TreeHeader, we're storing rendered control in a view state
            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                System.Web.UI.Control header = Controls[0];
                StringBuilder sb = new StringBuilder();
                header.RenderControl(new HtmlTextWriter(new StringWriter(sb)));
                Sitecore.Context.ClientPage.ServerProperties[ViewStateKey] = sb.ToString();
            }
        }

        protected override void Render(HtmlTextWriter output)
        {

            if (Sitecore.Context.ClientPage.IsEvent)
            {
                if (!(Controls[0] is LiteralControl))
                {
                    Controls.AddAt(0, new LiteralControl());
                }

                LiteralControl control = Controls[0] as LiteralControl;
                string html = (string)Sitecore.Context.ClientPage.ServerProperties[ViewStateKey];
                Assert.IsNotNullOrEmpty(html, "Cannot find header's html in the ViewState");
                if (control != null) control.Text = html;
            }
            base.Render(output);
        }

        private static string GetPredefinedFieldValue(Item item, string fieldName)
        {
            Error.AssertNotNull(item, "item");
            if (!Utils.IsMediaItem(item))
                return string.Empty;

            switch (fieldName)
            {
                case FieldSize:
                    return ValueOfColumnFilesize(item);

                case FieldIsMultilanguage:
                    if (!Utils.IsMediaItem(item))
                        return string.Empty;
                    else
                        return Utils.IsMultilanguagePicture(item) ? "Versioned" : "Unversioned";

                case FieldStoredAs:
                    return ValueOfColumnStoredAs(item);

                default:
                    throw new ApplicationException($"Unexpected predefined field value '{fieldName}'");
            }
        }

        private static string ValueOfColumnFilesize(Item item)
        {
            Item[] versions = Utils.GetAllVersionsWithMedia(item);

            if (versions.Length > 1)
            {
                int size = 0;
                foreach (Item version in versions)
                {
                    int temp;
                    if (int.TryParse(version["size"], out temp))
                        size += temp;
                }
                return Utils.GetFriendlyFileSize(size) + " in " + versions.Length + " versions";
            }
            else
            {
                int size;
                if (int.TryParse(item["size"], out size))
                    return Utils.GetFriendlyFileSize(size);
                else
                    return string.Empty;
            }
        }

        private static string ValueOfColumnStoredAs(Item item)
        {
            int storedInFiles = 0;
            int storedInDatabase = 0;

            Item[] versions = Utils.GetAllVersionsWithMedia(item);
            foreach (Item version in versions)
            {
                if (MediaStorageManager.IsFileBased(version))
                    storedInFiles++;
                else
                    storedInDatabase++;
            }

            if (storedInFiles == 0 && storedInDatabase == 0)
                return "";

            if (storedInFiles == 0)
                return "blob";

            if (storedInDatabase == 0)
                return "file";

            return "file & blob";
        }

        private string ViewStateKey => ID + "_MediaTreeview";
    }
}
