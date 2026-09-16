using Microsoft.AspNetCore.Components;
using OES.Helper.General;
using System.Globalization;

namespace OES.Blazor.InternalHelperTypes.General
{
    public static class DirectionHelper
    {
        public static string GetDirection()
        {
            var culture = CultureInfo.CurrentUICulture;
            return culture.TextInfo.IsRightToLeft ? MiscConstants.Rtl : MiscConstants.Ltr;
        }

        public static string GetQuestionBodyClass()
        {
            return $"mcq-question-body mcq-content-{GetDirection()}";
        }

        public static MarkupString GetDirectionalContent(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                return new MarkupString(string.Empty);
            }

            var currentDirection = GetDirection();

            string processedHtml;

            if (currentDirection == MiscConstants.Rtl)
            {
                processedHtml = htmlContent.Replace($"direction: {MiscConstants.Ltr}", $"direction: {MiscConstants.Rtl}");
            }
            else
            {
                processedHtml = htmlContent.Replace($"direction: {MiscConstants.Rtl}", $"direction: {MiscConstants.Ltr}");
            }

            return new MarkupString(processedHtml);
        }
    }
}