using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.CandidatesResult;
using OES.Blazor.Services.Interfaces.EquationTemplate;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.Venue;
using OES.Helper.Dtos.Candidate;
using OES.Helper.Dtos.CTRExam;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.UserPapersDto;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;

namespace OES.Blazor.Pages.Results
{
    /// <summary>
    /// Result Generation Page - Supports two modes:
    ///
    /// SIMPLE MODE: Generate results for ALL forms/venues within a date range.
    ///   - Entry Point: ExportAllByDateAsync()
    ///   - Calls: BlazEquationTemplateService.GenerateResultsBatchAsync()
    ///
    /// ADVANCED MODE: Generate results for SELECTED forms/venues.
    ///   - Entry Point: ExportCandidatesForDownloadAsync()
    ///   - Calls: BlazEquationTemplateService.ExportCandidatesDataBatchAsync()
    /// </summary>
    public partial class ResultGeneration
    {
        #region ==================== SERVICES AND FIELDS ====================

        [Inject] private IBlazPaperService BlazPaperService { get; set; }
        [Inject] private IBlazVenueService BlazVenueService { get; set; }
        [Inject] private IBlazFormService BlazFormService { get; set; }
        [Inject] private IBlazEquationTemplateService BlazEquationTemplateService { get; set; }
        [Inject] private IBlazCandidatesResultService BlazCandidatesResultService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IJSRuntime JS { get; set; }

        // UI State
        private DateRange _dateRange = new DateRange(DateTimeHelper.Now.Date, DateTimeHelper.Now.AddDays(5).Date);
        private bool _isSimpleExpanded = true;
        private bool _isAdvancedExpanded = false;
        private bool _isGenerating = false;
        private bool _isGenerated = false;
        private bool _isLoading = false;
        private DateTime? _generationDate;

        // Data Collections
        private List<GetUserPapersDto> _papers = [];
        private List<GetFormDto> _forms = [];
        private List<GetVenueResponseDto> _venues = [];
        private List<ExportResultDto> _exportResults = [];

        // Advanced Mode Selections
        private IEnumerable<GetUserPapersDto> _selectedPapers = new HashSet<GetUserPapersDto>();
        private IEnumerable<GetFormDto> _selectedForms = new HashSet<GetFormDto>();
        private IEnumerable<GetVenueResponseDto> _selectedVenues = new HashSet<GetVenueResponseDto>();

        // Combined File Output
        private string _combinedFileName = string.Empty;
        private string _combinedFileContent = string.Empty;
        private bool _hasCombinedFile = false;

        // Cache for sync jobs - fetched once for the date range
        private Dictionary<(long FormId, long VenueId, string VenueCode), CTRExamSyncJobDto> _syncJobsCache = [];

        #endregion

        #region ==================== INITIALIZATION ====================

        protected override async Task OnInitializedAsync()
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// [COMMON] Loads initial data (papers and venues) for both modes.
        /// </summary>
        private async Task LoadDataAsync()
        {
            _isLoading = true;

            var papersTask = BlazPaperService.GetPapersWithEquationTemplateAsync();
            var venuesTask = BlazVenueService.GetAllVenuesAsync();

            await Task.WhenAll(papersTask, venuesTask);

            _papers = await papersTask;
            _venues = await venuesTask;

            _isLoading = false;
        }

        #endregion

        #region ==================== ADVANCED MODE - UI Selection Handlers ====================

        /// <summary>
        /// [ADVANCED MODE] Handles paper selection change. Loads forms for selected papers.
        /// </summary>
        private async Task OnPaperSelectionChanged(IEnumerable<GetUserPapersDto> selectedPapers)
        {
            _selectedPapers = selectedPapers ?? new HashSet<GetUserPapersDto>();

            if (!_selectedPapers.Any())
            {
                _forms.Clear();
                _selectedForms = new HashSet<GetFormDto>();
                return;
            }

            await LoadFormsForSelectedPapersAsync();
        }

        /// <summary>
        /// [ADVANCED MODE] Loads forms for selected papers.
        /// </summary>
        private async Task LoadFormsForSelectedPapersAsync()
        {
            var paperIds = _selectedPapers.Select(p => p.Id).ToList();

            _forms = await BlazFormService.GetFormsWithEquationsByPaperIdsAsync(paperIds) ?? [];

            var validFormIds = _forms.Select(f => f.Id).ToHashSet();
            _selectedForms = _selectedForms.Where(f => validFormIds.Contains(f.Id)).ToList();

            StateHasChanged();
        }

        /// <summary>
        /// [ADVANCED MODE] Handles form selection change.
        /// </summary>
        private Task OnFormSelectionChanged(IEnumerable<GetFormDto> selectedForms)
        {
            _selectedForms = selectedForms ?? new HashSet<GetFormDto>();
            return Task.CompletedTask;
        }

        /// <summary>
        /// [ADVANCED MODE] Handles venue selection change.
        /// </summary>
        private Task OnVenueSelectionChanged(IEnumerable<GetVenueResponseDto> selectedVenues)
        {
            _selectedVenues = selectedVenues ?? new HashSet<GetVenueResponseDto>();
            return Task.CompletedTask;
        }

        #endregion

        #region ==================== MODE ROUTER ====================

        /// <summary>
        /// [ROUTER] Determines which mode to use and calls the appropriate method.
        /// - Simple Mode: ExportAllByDateAsync()
        /// - Advanced Mode: ExportCandidatesForDownloadAsync()
        /// </summary>
        private async Task GenerateResults()
        {
            if (_isAdvancedExpanded)
            {
                if (!_selectedPapers.Any())
                {
                    Snackbar.Add(Resource.PleaseSelectAtLeastOnePaper, Severity.Warning);
                    return;
                }

                if (!_selectedForms.Any())
                {
                    Snackbar.Add(Resource.PleaseSelectAtLeastOneForm, Severity.Warning);
                    return;
                }

                if (!_selectedVenues.Any())
                {
                    Snackbar.Add(Resource.PleaseSelectAtLeastOneVenue, Severity.Warning);
                    return;
                }

                await ExportCandidatesForDownloadAsync();
            }
            else if (_isSimpleExpanded)
            {
                await ExportAllByDateAsync();
            }
        }

        #endregion

        #region ==================== SIMPLE MODE - Result Generation ====================
        //
        // Simple Mode generates results for ALL forms/venues within a date range.
        // Uses the optimized batch endpoint for best performance.
        //
        // API Call: BlazEquationTemplateService.GenerateResultsBatchAsync()
        // Backend: EquationTemplateService.GenerateResultsBatchAsync()
        //

        /// <summary>
        /// [SIMPLE MODE] Main entry point. Generates results for ALL forms/venues in date range.
        /// </summary>
        private async Task ExportAllByDateAsync()
        {
            _isGenerating = true;
            _isGenerated = false;
            _exportResults.Clear();
            _hasCombinedFile = false;

            if (_dateRange?.Start == null || _dateRange?.End == null) return;

            var selectedStartDate = _dateRange.Start.Value;
            var selectedEndDate = _dateRange.End.Value;

            // Load all forms for name lookup
            _selectedPapers = new HashSet<GetUserPapersDto>(_papers);
            await LoadFormsForSelectedPapersAsync();

            // Pre-fetch all sync jobs for the date range (single API call instead of N calls)
            await PreloadSyncJobsCacheAsync(DateOnly.FromDateTime(selectedStartDate), DateOnly.FromDateTime(selectedEndDate));

            var request = new ExportCandidateRequestByDateDto
            {
                StartDate = DateOnly.FromDateTime(selectedStartDate),
                EndDate = DateOnly.FromDateTime(selectedEndDate)
            };

            // Using optimized batch endpoint for better performance with large datasets
            var response = await BlazEquationTemplateService.GenerateResultsBatchAsync(request);

            if (response.StatusCode != HttpStatusCode.OK || response.Data == null)
            {
                Snackbar.Add(Resource.NoDataAvailableForSelectedDateRange, Severity.Warning);
                _isGenerating = false;
                _isGenerated = true;
                StateHasChanged();
                return;
            }

            var exportResponses = DeserializeExportDataList(response.Data);
            var allExportData = new List<(ExportResultDto Result, ExportCandidatesResponseDto ExportData)>();

            foreach (var exportData in exportResponses)
            {
                var form = _forms.FirstOrDefault(f => f.Id == exportData.FormId);
                var venue = _venues.FirstOrDefault(v => v.Id == exportData.VenueId);

                var perRequest = new ExportCandidatesRequestDto
                {
                    FormId = exportData.FormId,
                    VenueId = exportData.VenueId,
                    VenueCode = exportData.VenueCode,
                    SentToCTR = false,
                    StartDate = selectedStartDate,
                    EndDate = selectedEndDate
                };

                var exportResult = new ExportResultDto
                {
                    FormId = exportData.FormId,
                    FormName = form?.Name ?? $"Form {exportData.FormId}",
                    VenueId = exportData.VenueId,
                    VenueName = venue?.Name ?? exportData.VenueCode ?? $"Venue {exportData.VenueId}",
                    VenueCode = exportData.VenueCode,
                    FileName = exportData.FileName,
                    FileContent = exportData.FileContent,
                    CandidateCount = exportData.CandidateCount,
                    RegistrationIds = exportData.RegistrationIds,
                    IsSuccess = !string.IsNullOrEmpty(exportData.FileName),
                    Request = perRequest,
                    IsSyncing = false
                };

                // Check sync status from pre-loaded cache (no API call)
                var syncStatus = CheckSyncStatusFromCache(exportData.FormId, exportData.VenueId, exportData.VenueCode);
                exportResult.IsSynced = syncStatus.IsSynced;
                exportResult.SyncJobId = syncStatus.SyncJobId;

                if (!exportResult.IsSuccess)
                    exportResult.ErrorMessage = syncStatus.IsSynced ? "Already synced - no file available" : "No data available for selected criteria";

                _exportResults.Add(exportResult);
                StateHasChanged();

                if (exportResult.IsSuccess)
                    allExportData.Add((exportResult, exportData));
            }

            // Combine all dat content and build one ZIP
            if (allExportData.Count != 0)
            {
                var dateNow = selectedStartDate.ToString("yyyy-MM-dd");

                var combinedCandidate = CombineDatFiles(allExportData.Select(e => e.ExportData.CandidateDat).ToList());
                var combinedItem = CombineDatFiles(allExportData.Select(e => e.ExportData.ItemDat).ToList());
                var combinedSect = CombineDatFiles(allExportData.Select(e => e.ExportData.SectDat).ToList());
                var combinedExam = CombineDatFiles(allExportData.Select(e => e.ExportData.ExamDat).ToList());

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
                _combinedFileContent = Convert.ToBase64String(zipStream.ToArray());
                _combinedFileName = $"Nemr-{dateNow}.zip";
                _hasCombinedFile = true;
            }

            _isGenerated = true;
            _generationDate = DateTime.Now;

            var successCount = _exportResults.Count(r => r.IsSuccess);

            if (successCount > 0)
                Snackbar.Add(string.Format(Resource.SuccessfullyGeneratedResults, successCount), Severity.Success);
            else
                Snackbar.Add(Resource.ResultsFailedDateRangeNoData, Severity.Warning);

            _isGenerating = false;
            StateHasChanged();
        }

        #endregion

        #region ==================== ADVANCED MODE - Result Generation ====================
        //
        // Advanced Mode generates results for SELECTED forms/venues within a date range.
        // User selects specific Paper -> Forms -> Venues combinations.
        //
        // API Call: BlazEquationTemplateService.ExportCandidatesDataBatchAsync()
        // Backend: EquationTemplateService.ExportCandidatesDataBatchAsync()
        //

        /// <summary>
        /// [ADVANCED MODE] Main entry point. Generates results for SELECTED forms/venues.
        /// </summary>
        private async Task ExportCandidatesForDownloadAsync()
        {
            _isGenerating = true;
            _isGenerated = false;
            _exportResults.Clear();
            _hasCombinedFile = false;

            if (_dateRange?.Start == null || _dateRange?.End == null) return;

            var selectedStartDate = _dateRange.Start.Value;
            var selectedEndDate = _dateRange.End.Value;

            // Pre-fetch all sync jobs for the date range (single API call instead of N calls)
            await PreloadSyncJobsCacheAsync(DateOnly.FromDateTime(selectedStartDate), DateOnly.FromDateTime(selectedEndDate));

            var allExportData = new List<(ExportResultDto Result, ExportCandidatesResponseDto ExportData)>();

            // Build batch request with all selected combinations
            var batchRequest = new ExportCandidatesBatchRequestDto
            {
                Combinations = [.. _selectedForms
                    .SelectMany(form => _selectedVenues.Select(venue => new FormVenueCombination
                    {
                        FormId = form.Id,
                        VenueId = venue.Id,
                        VenueCode = venue.Code ?? ""
                    }))],
                StartDate = selectedStartDate,
                EndDate = selectedEndDate,
                SentToCTR = false
            };

            // Single batch API call instead of N×M calls
            var batchResponse = await BlazEquationTemplateService.ExportCandidatesDataBatchAsync(batchRequest);

            // Process batch response
            if (batchResponse.StatusCode == HttpStatusCode.OK && batchResponse.Data != null)
            {
                var exportDataList = DeserializeExportDataList(batchResponse.Data);

                foreach (var exportData in exportDataList)
                {
                    var form = _selectedForms.FirstOrDefault(f => f.Id == exportData.FormId);
                    var venue = _selectedVenues.FirstOrDefault(v => v.Id == exportData.VenueId || v.Code == exportData.VenueCode);

                    if (form == null || venue == null) continue;

                    var syncStatus = CheckSyncStatusFromCache(exportData.FormId, exportData.VenueId, exportData.VenueCode ?? "");

                    var perRequest = new ExportCandidatesRequestDto
                    {
                        FormId = exportData.FormId,
                        VenueId = exportData.VenueId,
                        VenueCode = exportData.VenueCode ?? "",
                        SentToCTR = false,
                        StartDate = selectedStartDate,
                        EndDate = selectedEndDate
                    };

                    var exportResult = new ExportResultDto
                    {
                        FormId = exportData.FormId,
                        FormName = form.Name ?? "",
                        VenueId = exportData.VenueId,
                        VenueName = venue.Name ?? exportData.VenueCode ?? $"Venue {exportData.VenueId}",
                        VenueCode = exportData.VenueCode ?? "",
                        IsSuccess = true,
                        CandidateCount = exportData.CandidateCount,
                        RegistrationIds = exportData.RegistrationIds,
                        IsSynced = syncStatus.IsSynced,
                        SyncJobId = syncStatus.SyncJobId,
                        IsSyncing = false,
                        FileName = exportData.FileName,
                        FileContent = exportData.FileContent,
                        Request = perRequest
                    };

                    _exportResults.Add(exportResult);
                    allExportData.Add((exportResult, exportData));
                }

                StateHasChanged();
            }
            else
            {
                foreach (var form in _selectedForms)
                {
                    foreach (var venue in _selectedVenues)
                    {
                        _exportResults.Add(new ExportResultDto
                        {
                            FormId = form.Id,
                            FormName = form.Name ?? "",
                            VenueId = venue.Id,
                            VenueCode = venue.Code ?? "",
                            IsSuccess = false,
                            CandidateCount = 0,
                            ErrorMessage = batchResponse.Message ?? "No data found"
                        });
                    }
                }
                StateHasChanged();
            }

            // Combine all dat content and build one ZIP
            if (allExportData.Count != 0)
            {
                var dateNow = selectedStartDate.ToString("yyyy-MM-dd");

                var combinedCandidate = CombineDatFiles(allExportData.Select(e => e.ExportData.CandidateDat).ToList());
                var combinedItem = CombineDatFiles(allExportData.Select(e => e.ExportData.ItemDat).ToList());
                var combinedSect = CombineDatFiles(allExportData.Select(e => e.ExportData.SectDat).ToList());
                var combinedExam = CombineDatFiles(allExportData.Select(e => e.ExportData.ExamDat).ToList());

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
                _combinedFileContent = Convert.ToBase64String(zipStream.ToArray());
                _combinedFileName = $"Nemr-{dateNow}.zip";
                _hasCombinedFile = true;
            }

            _isGenerated = true;
            _generationDate = DateTimeHelper.Now;

            var successCount = _exportResults.Count(r => r.IsSuccess);

            if (successCount > 0)
                Snackbar.Add(string.Format(Resource.SuccessfullyGeneratedResults, successCount), Severity.Success);
            else
                Snackbar.Add(Resource.ResultsFailedDateRangeNoData, Severity.Warning);

            _isGenerating = false;
            StateHasChanged();
        }

        #endregion

        #region ==================== COMMON METHODS - Shared by Both Modes ====================

        /// <summary>
        /// [COMMON] Combines multiple DAT file contents into a single file.
        /// Used by both Simple Mode and Advanced Mode.
        /// </summary>
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

        /// <summary>
        /// [COMMON] Creates export result DTO and checks sync status.
        /// </summary>
        private ExportResultDto CreateExportResult(GetFormDto form, GetVenueResponseDto venue, ApiResponse response, ExportCandidatesRequestDto request)
        {
            var result = new ExportResultDto
            {
                FormId = form.Id,
                FormName = form.Name,
                VenueId = venue.Id,
                VenueName = venue.Name,
                VenueCode = venue.Code,
                Request = request,
                IsSyncing = false
            };

            // Check sync status from pre-loaded cache (no API call)
            var syncStatus = CheckSyncStatusFromCache(form.Id, venue.Id, venue.Code);

            // Check if export generation was successful
            if (response.StatusCode == HttpStatusCode.OK && response.Data != null)
            {
                var exportData = DeserializeExportData(response.Data);

                if (exportData != null && !string.IsNullOrEmpty(exportData.FileName))
                {
                    result.FileName = exportData.FileName;
                    result.FileContent = exportData.FileContent;
                    result.CandidateCount = exportData.CandidateCount;
                    result.IsSuccess = true;
                    result.IsSynced = syncStatus.IsSynced;
                    result.SyncJobId = syncStatus.SyncJobId;
                }
                else
                {
                    result.IsSuccess = false;
                    result.IsSynced = syncStatus.IsSynced;
                    result.SyncJobId = syncStatus.SyncJobId;
                    result.ErrorMessage = "Invalid data structure";
                }
            }
            else
            {
                result.IsSynced = syncStatus.IsSynced;
                result.SyncJobId = syncStatus.SyncJobId;
                result.CandidateCount = syncStatus.TotalCandidates;
                result.IsSuccess = false;
                result.ErrorMessage = syncStatus.IsSynced ? "Already synced - no new data" : "No data available for selected criteria";
            }

            return result;
        }

        #endregion

        #region Common - Sync Status Cache

        /// <summary>
        /// [COMMON] Pre-loads all sync jobs for the date range in a single API call.
        /// Caches results to avoid N+1 API calls when checking sync status.
        /// </summary>
        private async Task PreloadSyncJobsCacheAsync(DateOnly startDate, DateOnly endDate)
        {
            _syncJobsCache.Clear();

            var response = await BlazCandidatesResultService.GetAllCTRExamSyncJobsForDateRangeAsync(startDate, endDate);

            if (response.StatusCode == HttpStatusCode.OK && response.Data != null)
            {
                var syncJobs = DeserializeSyncJobsList(response.Data);

                if (syncJobs != null)
                {
                    // Build lookup dictionary keyed by (PaperFormId, VenueId, VenueCode)
                    // Keep only the most recent successful job per combination
                    foreach (var job in syncJobs.OrderByDescending(j => j.JobStartTime))
                    {
                        // PaperFormId in CTRExamSyncJobs corresponds to FormId in the export data
                        var key = (job.PaperFormId, job.VenueId, job.VenueCode);
                        _syncJobsCache.TryAdd(key, job);
                    }
                }
            }
        }

        /// <summary>
        /// [COMMON] Checks sync status from pre-loaded cache (no API call).
        /// </summary>
        private (bool IsSynced, long SyncJobId, int TotalCandidates) CheckSyncStatusFromCache(long formId, long venueId, string venueCode)
        {
            // formId from export data corresponds to PaperFormId in CTRExamSyncJobs
            var key = (formId, venueId, venueCode);

            if (_syncJobsCache.TryGetValue(key, out var syncJob))
            {
                return (IsSynced: true, SyncJobId: syncJob.Id, TotalCandidates: (int)syncJob.TotalCandidates);
            }

            return (IsSynced: false, SyncJobId: 0, TotalCandidates: 0);
        }

        #endregion

        #region Common - Deserialization Helpers

        private static ExportCandidatesResponseDto? DeserializeExportData(object data)
        {
            if (data is null) return null;

            var jsonString = data is JsonElement je ? je.GetRawText() : data.ToString();

            if (string.IsNullOrWhiteSpace(jsonString)) return null;

            try
            {
                return JsonSerializer.Deserialize<ExportCandidatesResponseDto>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[DeserializeExportData] Failed: {ex.Message} | Preview: {jsonString[..Math.Min(200, jsonString.Length)]}");
                return null;
            }
        }

        private static List<ExportCandidatesResponseDto> DeserializeExportDataList(object data)
        {
            if (data is null) return [];

            var jsonString = data is JsonElement je ? je.GetRawText() : data.ToString();

            if (string.IsNullOrWhiteSpace(jsonString)) return [];

            try
            {
                var trimmed = jsonString.TrimStart();
                if (trimmed.StartsWith('{'))
                {
                    var single = JsonSerializer.Deserialize<ExportCandidatesResponseDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return single is null ? [] : [single];
                }
                return JsonSerializer.Deserialize<List<ExportCandidatesResponseDto>>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? [];
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[DeserializeExportDataList] Failed: {ex.Message} | Preview: {jsonString[..Math.Min(200, jsonString.Length)]}");
                return [];
            }
        }

        private static List<CTRExamSyncJobDto> DeserializeSyncJobsList(object data)
        {
            if (data is null) return [];

            var jsonString = data is JsonElement je ? je.GetRawText() : data.ToString();

            if (string.IsNullOrWhiteSpace(jsonString)) return [];

            try
            {
                return JsonSerializer.Deserialize<List<CTRExamSyncJobDto>>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? [];
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[DeserializeSyncJobsList] Failed: {ex.Message} | Preview: {jsonString[..Math.Min(200, jsonString.Length)]}");
                return [];
            }
        }

        #endregion

        #region Common - Download Methods

        private async Task DownloadFileAsync(string fileName, string base64Content)
        {
            await JS.InvokeVoidAsync("downloadBase64File", fileName, base64Content);
            Snackbar.Add($"Downloaded: {fileName}", Severity.Success);
        }

        private async Task DownloadAllSuccessfulAsync()
        {
            var successfulExports = _exportResults.Where(r => r.IsSuccess).ToList();

            if (successfulExports.Count == 0)
            {
                Snackbar.Add("No successful exports to download", Severity.Warning);
                return;
            }

            foreach (var export in successfulExports)
            {
                await DownloadFileAsync(export.FileName, export.FileContent);
            }

            Snackbar.Add($"Downloaded all {successfulExports.Count} files", Severity.Success);
        }

        #endregion

        #region Common - CTR Sync Methods

        private async Task SyncCTRJobAsync(ExportResultDto export)
        {
            if (export.Request == null)
            {
                Snackbar.Add("Invalid export request", Severity.Warning);
                return;
            }

            if (export.IsSynced)
            {
                Snackbar.Add($"{export.FormName} - {export.VenueName} is already synced", Severity.Info);
                return;
            }

            export.IsSyncing = true;
            StateHasChanged();

            var response = await BlazCandidatesResultService.AddCTRExamSyncJobsAsync(export.Request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var syncData = DeserializeSyncData(response.Data);

                if (syncData != null && syncData.IsSuccess)
                {
                    export.SyncJobId = syncData.SyncJobId;
                    export.IsSynced = true;
                    Snackbar.Add($"{export.FormName} - {export.VenueName} synced to CTR successfully", Severity.Success);
                }
                else
                {
                    export.IsSynced = false;
                    export.ErrorMessage = "Sync failed - check server logs";
                    Snackbar.Add($"Failed to sync: {export.FormName} - {export.VenueName}", Severity.Error);
                }
            }
            else
            {
                export.IsSynced = false;
                export.ErrorMessage = response.Message ?? "Sync failed";
                Snackbar.Add($"Failed to sync: {export.FormName} - {export.VenueName}", Severity.Error);
            }

            export.IsSyncing = false;
            StateHasChanged();
        }

        private async Task SyncAllCTRJobsAsync()
        {
            var successfulExports = _exportResults.Where(r => r.IsSuccess && !r.IsSynced).ToList();

            if (successfulExports.Count == 0)
            {
                Snackbar.Add("No results to sync (either failed or already synced)", Severity.Warning);
                return;
            }

            var syncedCount = 0;

            foreach (var export in successfulExports)
            {
                export.IsSyncing = true;
                StateHasChanged();

                var response = await BlazCandidatesResultService.AddCTRExamSyncJobsAsync(export.Request);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var syncData = DeserializeSyncData(response.Data);

                    if (syncData != null && syncData.IsSuccess)
                    {
                        export.SyncJobId = syncData.SyncJobId;
                        export.IsSynced = true;
                        syncedCount++;
                    }
                    else
                    {
                        export.IsSynced = false;
                        export.ErrorMessage = "Sync failed - check server logs";
                    }
                }
                else
                {
                    export.IsSynced = false;
                    export.ErrorMessage = response.Message ?? "Sync failed";
                }

                export.IsSyncing = false;
                StateHasChanged();
            }

            Snackbar.Add(
                $"{syncedCount} of {successfulExports.Count} synced successfully",
                syncedCount > 0 ? Severity.Success : Severity.Warning
            );
        }

        private async Task SendCombinedFileToCTRAsync()
        {
            var relatedExports = _exportResults
                .Where(r => r.IsSuccess && !r.IsSynced && r.Request != null)
                .ToList();

            if (relatedExports.Count == 0)
            {
                Snackbar.Add(Resource.NoResultFound, Severity.Warning);
                return;
            }

            foreach (var export in relatedExports)
                export.IsSyncing = true;

            StateHasChanged();

            var allRegistrationIds = relatedExports
                .Where(x => x.RegistrationIds != null)
                .SelectMany(x => x.RegistrationIds)
                .ToList();

            var fileResponse = await BlazCandidatesResultService.SaveCombinedFileToCTRAsync(
                new SaveFileToCTRDto
                {
                    FileName = _combinedFileName,
                    Base64Content = _combinedFileContent,
                    RegistrationIds = allRegistrationIds,
                    Combinations = relatedExports.ConvertAll(e => new FormVenueCombination
                    {
                        FormId = e.Request.FormId,
                        VenueCode = e.Request.VenueCode
                    })
                }
            );

            if (fileResponse.StatusCode != HttpStatusCode.OK)
            {
                Snackbar.Add(string.Format(Resource.FailedWithMessage, fileResponse.Message), Severity.Error);

                foreach (var export in relatedExports)
                    export.IsSyncing = false;

                StateHasChanged();

                return;
            }

            foreach (var export in relatedExports)
            {
                export.IsSynced = true;
                export.IsSyncing = false;
            }

            Snackbar.Add(string.Format(Resource.FileSentToCTRSuccessfully, _combinedFileName), Severity.Success);
            StateHasChanged();
        }

        private static SyncJobResponseDto DeserializeSyncData(object data)
        {
            var jsonString = data.ToString();
            return JsonSerializer.Deserialize<SyncJobResponseDto>(
                jsonString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        #endregion
    }
}