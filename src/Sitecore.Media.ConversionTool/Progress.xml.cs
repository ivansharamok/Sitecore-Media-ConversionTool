namespace Sitecore.Media.ConversionTool
{
    using Jobs;
    using Media.ConversionTool.Controls;
    using Shell.Framework;
    using Web.UI.HtmlControls;
    using Web.UI.Sheer;
    using Web.UI.WebControls;

    public class Progress : BaseForm
    {
        private const string RefreshProgressMessage = "Progress:refresh";
        private static readonly int WaitingPeriod = 500;

        protected Border brdProgress;
        protected GridPanel pnlButtons;
        protected Button BtnClose;
        protected Button BtnConvertMore;

        [HandleMessage(RefreshProgressMessage)]
        public void RefreshState(Message msg)
        {
            Job job = JobManager.GetJob(JobHandle);
            this.UpdateProgress(job);

            if (job.IsDone)
            {
                this.BtnClose.Disabled = false;
                this.BtnConvertMore.Disabled = false;
            }
            else
            {
                Context.ClientPage.ClientResponse.Timer(RefreshProgressMessage, WaitingPeriod);
            }
        }

        protected void OnBackClick()
        {
            MainPage.RedirectTo();
        }

        protected void OnExitClick()
        {
            Windows.Close();
        }

        private void UpdateProgress(Job job)
        {
            int total = (int)job.Status.Total;
            int progress = 0;
            if (total > 0)
            {
                progress = (int)((job.Status.Processed * 100L) / total);
            }

            string message;
            if (job.IsDone)
            {
                progress = 100;
                string part = (total == 1) ? " was" : "s were";
                message = $"Convestion finished<br/>{total} version{part} processed";
            }
            else
            {
                message = "Converting items<br/>";
                if (total <= 0)
                {
                    message += "Preparing";
                }
                else
                {
                    if (job.Status.Messages.Count > 0)
                    {
                        string lastMessage = job.Status.Messages[job.Status.Messages.Count - 1];
                        if (!string.IsNullOrEmpty(lastMessage))
                            message += lastMessage + "<br/>";
                    }

                    string pluralEnding = (total == 1) ? string.Empty : "s";
                    message += $"{job.Status.Processed} of {total} version{pluralEnding}";
                }
            }

            this.brdProgress.InnerHtml = ProgressBar.GetHtml(progress, message);
        }

        private static Handle JobHandle => Handle.Parse(Context.Request.QueryString["handle"]);
    }
}
