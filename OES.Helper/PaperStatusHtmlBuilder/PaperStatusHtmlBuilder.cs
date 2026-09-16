using OES.Helper.Dtos.Form;
using OES.Helper.Enums;
using System.Text;

namespace OES.Helper.PaperStatusHtmlBuilder
{
    public static class PaperStatusHtmlBuilder
    {
        public static string BuildFormsStatusSummary(List<FormStatusCountDto> formsStatusCounts)
        {
            if (formsStatusCounts == null || !formsStatusCounts.Any())
            {
                return "<audio style='display:none;'></audio><span style='color: #9e9e9e;'></span>";
            }

            var html = new StringBuilder();
            html.Append("<audio style='display:none;'></audio>");

            html.Append("<div style='display: flex; flex-direction: column; gap: 6px; align-items: center; padding: 4px;'>");

            foreach (var statusCount in formsStatusCounts.OrderBy(x => x.Status))
            {
                html.Append(GenerateChipHtml(statusCount));
            }

            html.Append("</div>");

            return html.ToString();
        }

        private static string GenerateChipHtml(FormStatusCountDto statusCount)
        {
            var (bgColor, textColor) = GetStatusColors(statusCount.Status);

            return $@"
                <div style='
                    background-color: {bgColor};
                    color: {textColor};
                    padding: 0px 5px;
                    border-radius: 10px;
                    font-size: 0.65rem;
                    font-weight: 600;
                    display: inline-block;
                    white-space: nowrap;
                    border: 1px solid {textColor};
                    min-width: 50px; 
                    text-align: center;
                '>
                    {statusCount.StatusDisplay}: {statusCount.Count}
                </div>";
        }

        private static (string bgColor, string textColor) GetStatusColors(AvailabilityStatus status) => status switch
        {
            AvailabilityStatus.Active => ("rgba(46, 125, 50, 0.15)", "#2E7D32"),
            AvailabilityStatus.Synced => ("rgba(41, 182, 246, 0.12)", "#29B6F6"),
            AvailabilityStatus.Expired => ("rgba(255, 167, 38, 0.12)", "#FFA726"),
            AvailabilityStatus.Suspended => ("rgba(239, 83, 80, 0.12)", "#EF5350"),
            _ => ("rgba(189, 189, 189, 0.12)", "#BDBDBD")
        };
    }
}