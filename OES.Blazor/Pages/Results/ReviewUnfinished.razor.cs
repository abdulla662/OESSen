using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.CandidatesResult;
using OES.Blazor.Models.Results;
using OES.Blazor.Services.Interfaces.CandidatesResult;
using OES.Helper.Dtos.CandidatesResult;
using OES.Helper.Dtos.QuestionIndicator;
using OES.Helper.Dtos.Results;
using OES.Helper.Dtos.TrackingLog;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using SharedHelper.RolesNames;
using System.Text.Json;
using Resource = OES.Helper.ResourceFiles.Resource;
using ReviewStatusEnum = OES.Helper.Enums.ReviewStatus;

namespace OES.Blazor.Pages.Results
{
    public partial class ReviewUnfinished
    {
        [Inject] private IBlazCandidatesResultService CandidatesResultService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private GlobalUserContext GlobalUserContext { get; set; }

        private List<UnfinishedCandidateModel> _candidates = [];
        private UnfinishedCandidateModel? _selectedCandidate = null;
        private List<ActivityLogModel> _activityLogs = [];
        private string _searchString = "";
        private ReviewStatusEnum? _selectedReviewStatus = ReviewStatusEnum.Pending;
        private bool _isLoading;
        private MudTable<UnfinishedCandidateModel> _table = null!;

        private PaginationSearchModel _paginationSearch = new()
        {
            PageIndex = 0,
            PageSize = 100,
            PaginationOff = false,
            OrderBy = SearchInKey.DESC
        };

        protected override Task OnInitializedAsync() => Task.CompletedTask;

        private bool HasResultReviewerAccess()
        {
            return GlobalUserContext.SsoUserRoles.Any(r => r.Name == AdminRoles.SuperAdmin || r.Name == AdminRoles.Entity_Admin) || GlobalUserContext.OesUserRoles.Any(r => r.Name == OesTemplateRoleConstants.ResultReviewer);
        }

        private async Task<TableData<UnfinishedCandidateModel>> ServerReload(TableState state, CancellationToken token)
        {
            _paginationSearch.PageIndex = state.Page;
            _paginationSearch.PageSize = state.PageSize;
            _paginationSearch.PaginationOff = false;

            _paginationSearch.SearchKey = _searchString;

            if (_selectedReviewStatus == null)
            {
                List<ReviewStatusEnum> allStatuses = Enum.GetValues(typeof(ReviewStatusEnum))
                    .Cast<ReviewStatusEnum>()
                    .Where(x => x != ReviewStatusEnum.NotApplicable)
                    .ToList();

                _paginationSearch.FilterObj = allStatuses;
            }
            else
            {
                _paginationSearch.FilterObj = Enum.GetValues(typeof(ReviewStatusEnum))
                    .Cast<ReviewStatusEnum>()
                    .Where(x => x == _selectedReviewStatus)
                    .ToList();
            }

            var response = await CandidatesResultService.GetUnfinishedCandidatesForReviewAsync(_paginationSearch);

            if (response?.Items != null && response.Items.Any())
            {
                _candidates = [.. response.Items.Select(c => new UnfinishedCandidateModel
                {
                    Id = c.Id,
                    CandidateId = c.CandidateId,
                    CandidateExamTrialId = c.ExamTrialId ?? 0,
                    PaperId = c.OriginalPaperId,
                    PaperFormId = c.PaperFormIdActual,
                    ScheduleId = c.ScheduleIdActual,
                    ExamDate = (c.ExamTrialStartDate ?? c.CandidateExamDate),
                    RegistrationId = c.RegistrationId,
                    CandidateName = c.CandidateDisplayName ?? "N/A",
                    NationalId = c.CandidateNationalId ?? "N/A",
                    ExamSeries = $"{c.PaperName} - {c.PaperFormName}",
                    ReviewStatus = c.ReviewStatus,
                    TimeSpentInSession = FormatTimeSpent(c.TotalElapsedTimeInSeconds),
                    ViewedQuestions = c.QuestionsVisited,
                    CorrectAnswers   = c.CorrectAnswers,
                    IncorrectAnswers = c.IncorrectAnswers,
                    SkippedQuestions = c.TotalQuestionsInDatabase - c.QuestionsAnsweredCount,
                    //SessionsViewed = $"{c.TrialNumber}/{c.TrialNumber}",
                    TotalSections = c.TotalSections,
                    PaperCode = c.PaperCode,
                    VenueCode = c.VenueCode,
                    ScheduleName = c.ScheduleName,
                    TotalQuestions = c.TotalQuestionsInDatabase,
                    QuestionsAnswered = c.QuestionsAnsweredCount,
                    TotalExamDuration = c.TotalExamDuration,
                    TotalExamSessions = c.TotalExamSessions
                })];
            }
            else
            {
                _candidates = [];
                Snackbar.Add(Resource.NoUnfinishedCandidatesFound, Severity.Info);
            }

            _isLoading = false;
            return new TableData<UnfinishedCandidateModel>
            {
                Items = _candidates,
                TotalItems = response?.TotalItems ?? 0
            };
        }

        private static string FormatTimeSpent(double? seconds)
        {
            if (!seconds.HasValue || seconds.Value == 0)
                return "00:00";

            var timeSpan = TimeSpan.FromSeconds(seconds.Value);

            return $"{(int)timeSpan.TotalMinutes:D2}:{timeSpan.Seconds:D2}";
        }

        private async Task OnReviewStatusChanged(ReviewStatusEnum? value)
        {
            _selectedReviewStatus = value;
            await _table.ReloadServerData();
        }

        private async Task ViewCandidateDetails(UnfinishedCandidateModel candidate)
        {
            _selectedCandidate = candidate;
            await LoadActivityLogAsync();

            var parameters = new DialogParameters<CandidateTrackingLogsDetailsDialog>
            {
                { x => x.Candidate, candidate },
                { x => x.ActivityLogs, _activityLogs },
                { x => x.IsReview, true }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.ExtraLarge,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<CandidateTrackingLogsDetailsDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is ReviewStatusEnum status)
            {
                if (status == ReviewStatusEnum.Accepted)
                    await AcceptReview();
                else if (status == ReviewStatusEnum.Rejected)
                    await RejectReview();
            }

            _selectedCandidate = null;
            _activityLogs.Clear();
        }

        private async Task LoadActivityLogAsync()
        {
            _activityLogs.Clear();

            if (_selectedCandidate == null || _selectedCandidate.CandidateExamTrialId == 0)
            {
                Snackbar.Add(Resource.NoActivityLogAvailable, Severity.Warning);
                return;
            }

            var request = new GetTrackingLogRequestDto
            {
                CandidateId = _selectedCandidate.CandidateId,
                CandidateExamTrialId = _selectedCandidate.CandidateExamTrialId,
                PaperId = _selectedCandidate.PaperId,
                PaperFormId = _selectedCandidate.PaperFormId,
                ScheduleId = _selectedCandidate.ScheduleId
            };

            var response = await CandidatesResultService.GetTrackingLogsByRequestAsync(request);

            if (response?.Data == null)
            {
                Snackbar.Add(Resource.NoActivityLogAvailable, Severity.Warning);
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                var trackingLogs = System.Text.Json.JsonSerializer.Deserialize<List<GetTrackingLogDto>>(response.Data.ToString()!, options);

                if (trackingLogs == null || trackingLogs.Count == 0)
                {
                    Snackbar.Add(Resource.NoActivityLogAvailable, Severity.Info);
                    return;
                }

                int logIndex = 1;

                foreach (var log in trackingLogs)
                {
                    if (string.IsNullOrWhiteSpace(log.SessionLog) || log.SessionLog == "{}")
                        continue;

                    var entries = JsonSerializer.Deserialize<List<TrackingLogEntryDto>>(log.SessionLog, options);
                    if (entries == null || entries.Count == 0) continue;

                    entries = [.. entries.Where(e => e.Time.Date == entries[0].Time.Date)];

                    _activityLogs.AddRange(entries.Select(entry => new ActivityLogModel
                    {
                        No = logIndex++,
                        Action = entry.Action,
                        LogMessage = entry.LogMessage ?? string.Empty,
                        Time = entry.Time,
                        SectionId = entry.SectionId,
                        SectionName = entry.SectionName ?? string.Empty,
                        QuestionId = entry.QuestionId,
                        QuestionStatus = entry.QuestionStatus,
                        DurationInSeconds = entry.DurationInSeconds,
                        ElementType = entry.ElementType ?? string.Empty,
                        Url = entry.Url ?? string.Empty,
                        Pathname = entry.Pathname ?? string.Empty,
                        IpAddress = entry.IpAddress ?? string.Empty,
                    }));
                }

                if (_activityLogs.Count == 0)
                    Snackbar.Add(Resource.NoActivityLogAvailable, Severity.Info);
            }
            catch (JsonException ex)
            {
                Snackbar.Add($"Error Parsing Activity Log: {ex.Message}", Severity.Error);
            }
        }

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

        private async Task AcceptReview()
        {
            if (!HasResultReviewerAccess())
            {
                Snackbar.Add(Resource.NotAuthorizedToAcceptReview, Severity.Error);
                return;
            }

            if (_selectedCandidate != null)
            {
                string reviewedBy = GlobalUserContext.UserEmail;

                var dto = new UpdateReviewStatusDto
                {
                    Id = _selectedCandidate.Id,
                    ReviewStatus = ReviewStatusEnum.Accepted,
                    ReviewedBy = reviewedBy
                };

                var response = await CandidatesResultService.UpdateReviewStatusAsync(dto);

                if (response.CustomCodeStatus == CustomCodeStatus.Success)
                {
                    _selectedCandidate.ReviewStatus = ReviewStatusEnum.Accepted;
                    Snackbar.Add($"{_selectedCandidate.CandidateName} {Resource.Accepted}", Severity.Success);
                    await _table.ReloadServerData();
                }
                else
                {
                    Snackbar.Add(response.Message ?? Resource.FailedToUpdateReviewStatus, Severity.Error);
                }
            }
        }

        private async Task RejectReview()
        {
            if (!HasResultReviewerAccess())
            {
                Snackbar.Add(Resource.NotAuthorizedToRejectReview, Severity.Error);
                return;
            }

            if (_selectedCandidate != null)
            {
                string reviewedBy = GlobalUserContext.UserEmail;

                var dto = new UpdateReviewStatusDto
                {
                    Id = _selectedCandidate.Id,
                    ReviewStatus = ReviewStatusEnum.Rejected,
                    ReviewedBy = reviewedBy
                };

                var response = await CandidatesResultService.UpdateReviewStatusAsync(dto);

                if (response.CustomCodeStatus == CustomCodeStatus.Success)
                {
                    _selectedCandidate.ReviewStatus = ReviewStatusEnum.Rejected;
                    Snackbar.Add($"{_selectedCandidate.CandidateName} {Resource.Rejected}", Severity.Warning);
                    await _table.ReloadServerData();
                }
                else
                {
                    Snackbar.Add(response.Message ?? Resource.FailedToUpdateReviewStatus, Severity.Error);
                }
            }
        }
    }
}
