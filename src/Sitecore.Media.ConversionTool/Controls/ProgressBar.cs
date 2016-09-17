namespace Sitecore.Media.ConversionTool.Controls
{
    using System;

    public static class ProgressBar
    {
        private static readonly string BackgroundProgressBorder =
            "/sitecore%20modules/Media%20Conversion%20Tool/Controls/Images/progress_border.png";

        private static readonly string BackgroundProgressBar =
            "/sitecore%20modules/Media%20Conversion%20Tool/Controls/Images/progress_bar.png";

        private static readonly string Html = @"
           <table style="" border: 0px; background-image: url('{4}'); "" cellpadding=""0"" cellspacing=""0"" >
              <tr style=""height: 4px;""> <td/> </tr>
              <tr>
               <td style=""width: 5px;"" />
               <td style=""width: {0}px; height: 16px; background-repeat: repeat-x; background-image: url('{3}');  border-color: black; border-width: 1px; border-style: single; ""/>
               <td style=""width: {1}px; height: 16px""/>
               <td style=""width: 5px;"" />
              </tr>
              <tr style=""height: 6px;""/>
           </table>
           <br/>{2} ";
        private static readonly int ProgressBarWidth = 88;
        private static readonly int Divider = 8;

        public static string GetHtml(int progress, string Message)
        {
            progress = (progress*ProgressBarWidth)/100;
            progress -= progress%Divider;
            return String.Format(Html, progress.ToString(), (ProgressBarWidth - progress).ToString(), Message,
                BackgroundProgressBar, BackgroundProgressBorder);
        }
    }
}
