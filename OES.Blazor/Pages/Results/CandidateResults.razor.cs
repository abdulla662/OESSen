using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Dialogs.CandidatesResult;
using OES.Blazor.Services.Interfaces.EquationTemplate;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Results;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using System.Data;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace OES.Blazor.Pages.Results
{
    public partial class CandidateResults
    {
        [Inject] private IBlazEquationTemplateService BlazEquationTemplateService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;

        private string _searchInput = "";
        private List<ExamSessionModel> _examSessions = [];
        private bool _isBatchDownloading = false;
        private HashSet<long> _loadingSessionIds = [];

        private async Task GetCandidateDetails()
        {
            if (string.IsNullOrWhiteSpace(_searchInput))
            {
                Snackbar.Add(Resource.PleaseEnterAtLeastOneID, Severity.Warning);
                return;
            }

            var ids = _searchInput
                .Split(',')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var request = new ExportCandidatesRequestDto
            {
                CandidateList = ids
            };

            await GetCandidateResult(request);
        }

        private async Task GetCandidateResult(ExportCandidatesRequestDto request)
        {
            var data = await BlazEquationTemplateService.GetCandidateResultsAsync(request);

            if (data?.Any() != true)
            {
                Snackbar.Add(Resource.NoResultsAvailable, Severity.Info);
                return;
            }

            _examSessions = data.ConvertAll(c =>
            {
                var dictList = JsonSerializer.Deserialize<List<Dictionary<string, decimal>>>(
                    c.FinalScore ?? "[]"
                ) ?? [];

                var flatCategories = dictList
                    .SelectMany(d => d)
                    .ToList();

                double finalScore = (double)c.ParsedFinalScore;

                var resolvedCategories = flatCategories
                .Select(x =>
                {
                    string categoryDisplayName = x.Key;

                    if (long.TryParse(x.Key, out var catId))
                    {
                        if (c.CategoryIdNameMap != null &&
                            c.CategoryIdNameMap.TryGetValue(catId, out var nameFromServer))
                        {
                            categoryDisplayName = nameFromServer;
                        }
                        else
                        {
                            return new { Name = x.Key, x.Value };
                        }
                    }

                    return new { Name = categoryDisplayName, x.Value };
                })
                .Where(x => x != null)
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.Value).First())
                .ToList();

                return new ExamSessionModel
                {
                    RegistrationId = c.RegistrationId,
                    NationalId = c.ClientCandidateID,
                    PaperFormId = c.PaperFormId,
                    CandidateName = $"{c.FirstName} {c.MiddleName} {c.LastName}".Trim(),
                    SentToCTRFlag = c.SentToCTR,
                    CandidateStartedExam = c.CandidateStartedExam,
                    CandidateEndedExam = c.CandidateEndedExam,
                    ReviewStatus = (ReviewStatus)c.ReviewStatus,
                    ReviewedDate = c.ReviewedAt,
                    Venue = c.VenueCode,
                    SentToCTRAt = c.SentToCTRAt,
                    SentToCTRDateTime = c.SentToCTRAt,
                    AttendantStatus = c.CandidateStartedExam ? AttendantsStatus.Attended : AttendantsStatus.Absent,
                    Grade = c.CandidateEndedExam ? GradeStatus.Taken : GradeStatus.Unfinished,
                    ReviewerAccount = c.ReviewerAccount,
                    ExamSeries = c.ExamSeriesCode,
                    ExamDate = c.ExamStartDate ?? DateTime.MinValue,
                    PaperFormCode = c.PaperFormCode,
                    PaperType = c.PaperType,
                    TotalEquation = c.TotalEquation,
                    TotalFinalScore = finalScore,

                    EquationCategories = resolvedCategories.ConvertAll(x =>
                    {
                        var equationCat = c.CategoryEquations?.FirstOrDefault(e =>
                            e.CategoryName.Equals(x.Name, StringComparison.OrdinalIgnoreCase));

                        return new EquationCategoryModel
                        {
                            Category = x.Name,
                            CategoryEquation = c.PaperType == PaperType.Adaptive
                                ? Resource.Average
                                : equationCat?.OriginalEquation ?? "",
                            CalculatedValue = (double)x.Value
                        };
                    })
                };
            });
        }

        private async Task ShowSessionDetails(ExamSessionModel session)
        {
            var parameters = new DialogParameters
            {
                { nameof(SessionDetailsDialog.Session), session }
            };

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseOnEscapeKey = true,
                CloseButton = true
            };

            var dialog = await DialogService.ShowAsync<SessionDetailsDialog>(
                Resource.ExamSessionDetails,
                parameters,
                options
            );

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is ExamSessionModel selectedSession)
            {
                await JS.InvokeVoidAsync(
                    "downloadPageAsPDFAuto",
                    "pdfFullResult",
                    $"Exam-Results-{selectedSession.RegistrationId}"
                );

                Snackbar.Add(
                    string.Format(Resource.DownloadingExamResultsForRegistrationId,
                    selectedSession.RegistrationId),
                    Severity.Info
                );
            }
        }

        private async Task DownloadSessionFiles(ExamSessionModel session)
        {
            _loadingSessionIds.Add(session.RegistrationId);

            var request = new ExportCandidatesRequestDto
            {
                FormId = session.PaperFormId,
                VenueCode = session.Venue,
                CandidateList = [session.RegistrationId.ToString()]
            };

            var apiResponse = await BlazEquationTemplateService.ExportCandidatesData(request);

            if (apiResponse?.Data is JsonElement data)
            {
                var exportData = JsonSerializer.Deserialize<ExportCandidatesResponseDto>(
                    data.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (exportData?.FileContent != null)
                {
                    await JS.InvokeVoidAsync(
                        "downloadBase64File",
                        exportData.FileName,
                        exportData.FileContent,
                        exportData.ContentType
                    );

                    Snackbar.Add(
                        string.Format(Resource.DownloadingExamFilesForSession, session.RegistrationId, session.ExamSeries),
                        Severity.Success
                    );

                    _loadingSessionIds.Remove(session.RegistrationId);

                    return;
                }
            }

            Snackbar.Add(Resource.NoResultsAvailable, Severity.Warning);

            _loadingSessionIds.Remove(session.RegistrationId);
        }

        private async Task DownloadAllSessionFiles()
        {
            if (_examSessions.Count == 0)
            {
                Snackbar.Add(Resource.NoResultsAvailable, Severity.Warning);
                return;
            }

            _isBatchDownloading = true;

            var request = new ExportCandidatesBatchRequestDto
            {
                Combinations = [.. _examSessions
            .Select(s => new FormVenueCombination
            {
                FormId = s.PaperFormId,
                VenueCode = s.Venue
            })
            .DistinctBy(x => new { x.FormId, x.VenueCode })],

                CandidateList = [.. _examSessions
            .Select(s => s.RegistrationId.ToString())
            .Distinct()],

                StartDate = _examSessions.Min(s => s.ExamDate),
                EndDate = _examSessions.Max(s => s.ExamDate).AddDays(1)
            };

            var apiResponse = await BlazEquationTemplateService.ExportCandidatesDataBatchAsync(request);

            if (apiResponse?.Data is not JsonElement data)
            {
                Snackbar.Add(Resource.NoResultsAvailable, Severity.Warning);
                _isBatchDownloading = false;
                return;
            }

            var results = JsonSerializer.Deserialize<List<ExportCandidatesResponseDto>>(
                data.GetRawText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            var validResults = results?.Where(r => r != null).ToList() ?? [];

            if (validResults.Count == 0)
            {
                Snackbar.Add(Resource.NoResultsAvailable, Severity.Warning);
                _isBatchDownloading = false;
                return;
            }

            // Combine all DAT files into a single ZIP
            var dateNow = DateTime.Now.ToString("yyyy-MM-dd");

            var combinedCandidate = CombineDatFiles(validResults.ConvertAll(r => r.CandidateDat));
            var combinedItem = CombineDatFiles(validResults.ConvertAll(r => r.ItemDat));
            var combinedSect = CombineDatFiles(validResults.ConvertAll(r => r.SectDat));
            var combinedExam = CombineDatFiles(validResults.ConvertAll(r => r.ExamDat));

            await using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                void AddEntry(string fileName, string content)
                {
                    var entry = archive.CreateEntry(fileName);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream, new UTF8Encoding(false));
                    writer.Write(content);
                }

                AddEntry($"cand-{dateNow}.dat", combinedCandidate);
                AddEntry($"item-{dateNow}.dat", combinedItem);
                AddEntry($"sect-{dateNow}.dat", combinedSect);
                AddEntry($"exam-{dateNow}.dat", combinedExam);
            }

            zipStream.Position = 0;
            var base64Zip = Convert.ToBase64String(zipStream.ToArray());
            var zipFileName = $"Nemr-{dateNow}.zip";

            await JS.InvokeVoidAsync("downloadBase64File", zipFileName, base64Zip, "application/zip");

            Snackbar.Add(
                string.Format(Resource.DownloadingExamFilesForAllSessionsCount, _examSessions.Count),
                Severity.Success
            );

            _isBatchDownloading = false;
        }

        private static string CombineDatFiles(List<string> datContents)
        {
            var sb = new StringBuilder();
            var isFirst = true;

            foreach (var content in datContents)
            {
                if (string.IsNullOrWhiteSpace(content)) continue;

                var lines = content
                    .Replace("\r\n", "\n")
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                if (isFirst)
                {
                    sb.AppendLine(lines[0]);
                    foreach (var line in lines.Skip(1))
                        sb.AppendLine(line);
                    isFirst = false;
                }
                else
                {
                    foreach (var line in lines.Skip(1))
                        sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }
    }
}
