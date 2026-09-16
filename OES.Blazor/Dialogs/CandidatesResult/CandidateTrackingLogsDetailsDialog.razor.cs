using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Models.Results;
using OES.Blazor.Services.Interfaces.CandidatesResult;
using OES.Helper.Dtos.QuestionIndicator;
using OES.Helper.Dtos.Results;
using OES.Helper.ResourceFiles;
using System.Text.Json;
using ReviewStatusEnum = OES.Helper.Enums.ReviewStatus;

namespace OES.Blazor.Dialogs.CandidatesResult
{
    public partial class CandidateTrackingLogsDetailsDialog
    {
        [Inject] private IBlazCandidatesResultService CandidatesResultService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IJSRuntime JS { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;
        [Parameter] public UnfinishedCandidateModel? Candidate { get; set; }
        [Parameter] public List<ActivityLogModel> ActivityLogs { get; set; } = [];
        [Parameter] public bool IsReview { get; set; } = false;

        private bool HasMultipleIPs()
        {
            var ips = ActivityLogs
                .Where(x => !string.IsNullOrEmpty(x.IpAddress))
                .Select(x => x.IpAddress)
                .Distinct()
                .ToList();

            return ips.Count > 1;
        }

        private static string FormatDuration(double? seconds)
        {
            if (!seconds.HasValue || seconds == 0)
                return "0 sec";

            var time = TimeSpan.FromSeconds(seconds.Value);

            if (time.TotalMinutes >= 1)
                return $"{(int)time.TotalMinutes}m {time.Seconds}s";

            return $"{time.Seconds}s";
        }

        private int SectionsVisited => ActivityLogs
            .Where(x => !string.IsNullOrEmpty(x.SectionName))
            .Select(x => x.SectionName.Trim())
            .Distinct()
            .Count();

        private async Task ExportCandidateAsync(UnfinishedCandidateModel candidate)
        {
            var request = new ExportUnfinishedReviewRequestDto
            {
                Id = candidate.Id,
                CandidateId = candidate.CandidateId,
                CandidateExamTrialId = candidate.CandidateExamTrialId,
                PaperId = candidate.PaperId,
                PaperFormId = candidate.PaperFormId,
                ScheduleId = candidate.ScheduleId,
                ExamDate = candidate.ExamDate
            };

            var response = await CandidatesResultService.ExportUnfinishedReviewAsync(request);

            if (response?.Data == null)
            {
                Snackbar.Add(response?.Message ?? Resource.FailedToDownloadFilePleaseTryAgain, Severity.Error);
                return;
            }

            var file = response.Data is JsonElement jsonElement
                ? JsonSerializer.Deserialize<DownloadFileDto>(
                    jsonElement.ValueKind == JsonValueKind.String ? jsonElement.GetString()! : jsonElement.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                : null;

            if (file == null)
            {
                Snackbar.Add(Resource.FailedToDownloadFilePleaseTryAgain, Severity.Error);
                return;
            }

            await JS.InvokeVoidAsync("downloadBase64File", file.FileName, file.FileContent);
            Snackbar.Add($"Downloaded: {file.FileName}", Severity.Success);
        }

        private string SessionsViewed => Candidate is null ? "0/0" : $"{SectionsVisited}/{Candidate.TotalSections}";

        private void OnAccept() => MudDialog.Close(DialogResult.Ok(ReviewStatusEnum.Accepted));

        private void OnReject() => MudDialog.Close(DialogResult.Ok(ReviewStatusEnum.Rejected));

        private void OnClose() => MudDialog.Cancel();
    }
}
