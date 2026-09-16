using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.MarkingScheme;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.MarkingScheme;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FifthStep
{
    public partial class MarkingScheme : ComponentBase
    {
        [Inject] IBlazPaperService BlazPaperService { get; set; }
        [Inject] IBlazMarkingSchemeService BlazMarkingSchemeService { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        private FormDetailsDto CurrentSelectedForm { get; set; } = new(0, "", "", "");
        private CollectivePaperFormSectionQuestionResponseDto CollectiveManualPaperFormSectionQuestionResponseDto { get; set; } = new([], [], []);
        private List<GetDifficultyLevelMarkingSchemeResponseDto> CollectiveManualDifficultyLevelsDistributions { get; set; } = [];
        private List<GetItemBanksMarkingSchemeResponseDto> CollectiveManualItemBanksDistributions { get; set; } = [];
        private List<ManuallySelectedQuestionsResponseDto> CurrentManualSelectedFormQuestions { get; set; } = [];
        private List<GetDifficultyLevelMarkingSchemeResponseDto> CurrentManualDifficultyLevelsDistributions { get; set; } = [];
        private List<GetItemBanksMarkingSchemeResponseDto> CurrentManualItemBanksDistributions { get; set; } = [];

        private List<AutoSelectedQuestionsResponseDto> AutoQuestions { get; set; } = [];
        private List<GetAutoQuestionMarkInfoResponseDto> AutoQuestionMarks { get; set; } = [];
        private List<GetDifficultyLevelMarkingSchemeResponseDto> AutoDifficultyLevelsDistributions { get; set; } = [];
        private List<GetItemBanksMarkingSchemeResponseDto> AutoItemBanksDistributions { get; set; } = [];

        private bool IsStandardAutoPaper =>
            PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard && PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Auto;
        private bool IsStandardManualPaper =>
            PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Standard && PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Manual;
        private bool IsMarkingSchemeEditingDisabled => PaperMetadataResultedParamsDto.OutputFormsCount >= 2;

        private string _selectedScheme = string.Empty;
        private double currentTotal = 0;
        private double currentDifficultyLevelTotal = 0;
        private double currentItemBankTotal = 0;
        private bool _isAutoOrManualValid = true;
        private bool _isDifficultyLevelValid = true;
        private bool _isItemBankValid = true;
        private bool _isWeightedExamValid = true;
        private readonly HashSet<long> _expandedQuestionIds = [];
        private MarkingSchemeDto _paperMarkingSchemeDto;
        StepperOperationalMode _thisComponentCurrentOperationalMode; // NOTE: You may need this in the future
        PaperStepperFormsMode _paperStepperFormsMode; // NOTE: You may need this in the future
        private bool _isLoading = true;


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            _isLoading = true;

            try
            {
                _paperMarkingSchemeDto = await BlazMarkingSchemeService.GetPaperMarkingSchemeAsync(PaperMetadataResultedParamsDto.PaperId);

                if (_paperMarkingSchemeDto.Id > 0)
                {
                    _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;
                }

                if (IsStandardAutoPaper)
                {
                    AutoQuestions = await BlazPaperService.GetAutoSelectedQuestionsForMarkingSchemeAsync(PaperMetadataResultedParamsDto.PaperId);

                    _selectedScheme = _paperMarkingSchemeDto.ScoreType.ToString(); // Initialize marking scheme with the incoming score type, whether you are in addition or update mode
                }
                else if (IsStandardManualPaper)
                {
                    var paperStepperFormsMode = await BlazSessionStorageService.GetValue<int>(nameof(PaperStepperFormsMode));
                    _paperStepperFormsMode = (PaperStepperFormsMode)paperStepperFormsMode;

                    CollectiveManualPaperFormSectionQuestionResponseDto = await BlazPaperService.GetManuallySelectedQuestionsWithFormsForMarkingSchemeAsync(PaperMetadataResultedParamsDto.PaperId);

                    _selectedScheme = _paperMarkingSchemeDto.ScoreType.ToString(); // Initialize marking scheme with the incoming score type, whether you are in addition or update mode

                    var formToBeInitiallySelected = CollectiveManualPaperFormSectionQuestionResponseDto.Forms.LastOrDefault();

                    OnFormSelected(formToBeInitiallySelected);
                }

                SchemeChanged(_selectedScheme);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void OnFormSelected(FormDetailsDto formDetailsDto)
        {
            if (formDetailsDto == null) return;

            CurrentSelectedForm = formDetailsDto;

            CurrentManualSelectedFormQuestions = [.. CollectiveManualPaperFormSectionQuestionResponseDto.Questions.Where(x => x.FormId == formDetailsDto.FormId)];
            CurrentManualDifficultyLevelsDistributions = [.. CollectiveManualDifficultyLevelsDistributions.Where(x => x.FormId == formDetailsDto.FormId)];
            CurrentManualItemBanksDistributions = [.. CollectiveManualItemBanksDistributions.Where(x => x.FormId == formDetailsDto.FormId)];

            StateHasChanged();
        }

        private void InitializeAutoQuestionsMarks()
        {
            AutoQuestionMarks.Clear();

            foreach (var question in AutoQuestions)
            {
                for (int i = 0; i < question.SelectedCount; i++)
                {
                    var code = question.QuestionCodes != null && i < question.QuestionCodes.Count
                        ? question.QuestionCodes[i]
                        : string.Empty;

                    AutoQuestionMarks.Add(new GetAutoQuestionMarkInfoResponseDto
                    {
                        AutoPaperItemBankQuestionSectionId = question.Id,
                        Code = code,
                        ItemBankName = question.ItemBankName,
                        DifficultyLevelName = question.DifficultyLevelName,
                        QuestionTypeName = question.QuestionTypeName,
                        SubQuestionsCount = question.SubQuestionCount,
                        Mark = question.Mark
                    });
                }
            }
        }

        private void DistributeMarksEquallyForAuto()
        {
            if (AutoQuestionMarks.Count > 0)
            {
                double markPerQuestion = Math.Round(PaperMetadataResultedParamsDto.TotalExamMark / AutoQuestionMarks.Count, 3);

                AutoQuestionMarks.ForEach(q => q.Mark = markPerQuestion);

                RecalculateAndValidateQuestionsMarksForAuto();
            }
        }

        /*  
            NOTE:
            Apply equal distribution ONLY to the last added form.
            Previous forms already have finalized scores.
            Applying equal distribution to all forms would overwrite them, so only the last form is initialized.
        */

        private void DistributeMarksEquallyForManual()
        {
            if (CollectiveManualPaperFormSectionQuestionResponseDto.Questions.Count > 0)
            {
                var lastFormId = CollectiveManualPaperFormSectionQuestionResponseDto.Forms
                    .OrderByDescending(f => f.FormId)
                    .Select(f => f.FormId)
                    .FirstOrDefault();

                var lastFormQuestions = CollectiveManualPaperFormSectionQuestionResponseDto.Questions
                    .Where(q => q.FormId == lastFormId)
                    .ToList();

                if (lastFormQuestions.Count == 0)
                    return;

                var totalUnits = lastFormQuestions.Sum(q => q.SubQuestionsCount);
                double markPerUnit = Math.Round(PaperMetadataResultedParamsDto.TotalExamMark / totalUnits, 3);

                foreach (var q in lastFormQuestions)
                {
                    if (q.SubQuestionsCount > 1 && q.SubQuestions?.Any() == true)
                    {
                        q.Mark = Math.Round(markPerUnit * q.SubQuestionsCount, 3);

                        foreach (var sub in q.SubQuestions)
                        {
                            sub.Mark = markPerUnit;
                        }
                    }
                    else
                    {
                        q.Mark = markPerUnit;
                    }
                }

                RecalculateAndValidateQuestionsMarksForManual();
            }
        }

        private void InitializeDifficultyLevelsDistributions()
        {
            if (IsStandardAutoPaper)
            {
                AutoDifficultyLevelsDistributions.Clear();

                foreach (var question in AutoQuestions)
                {
                    var existingLevel = AutoDifficultyLevelsDistributions.Find(d => d.DifficultyLevelId == question.DifficultyLevelId);

                    if (existingLevel != null)
                    {
                        existingLevel.SelectedCount += question.SelectedCount;
                    }
                    else
                    {
                        var difficultyLevelDistribution = new GetDifficultyLevelMarkingSchemeResponseDto
                        {
                            DifficultyLevelId = question.DifficultyLevelId,
                            DifficultyLevelName = question.DifficultyLevelName,
                            SelectedCount = question.SelectedCount,
                            MarkPerQuestion = 0
                        };

                        AutoDifficultyLevelsDistributions.Add(difficultyLevelDistribution);
                    }
                }
            }
            else if (IsStandardManualPaper)
            {
                var lastFormId = CollectiveManualPaperFormSectionQuestionResponseDto.Forms
                    .OrderByDescending(f => f.FormId)
                    .Select(f => f.FormId)
                    .FirstOrDefault();

                CollectiveManualDifficultyLevelsDistributions.Clear();

                var manualGroups = CollectiveManualPaperFormSectionQuestionResponseDto
                    .Questions
                    .GroupBy(q => new { q.DifficultyLevelId, q.DifficultyLevelName, q.FormId })
                    .Select(g =>
                    {
                        var totalMarks = g.Sum(q => q.Mark ?? 0);
                        var totalUnits = g.Sum(q => q.SubQuestionsCount);
                        var markPerQuestion = totalUnits == 0 ? 0 : totalMarks / totalUnits;

                        return new GetDifficultyLevelMarkingSchemeResponseDto
                        {
                            DifficultyLevelId = g.Key.DifficultyLevelId,
                            DifficultyLevelName = g.Key.DifficultyLevelName,
                            SelectedCount = totalUnits,
                            SubQuestionCount = totalUnits,
                            MarkPerQuestion = Math.Round(markPerQuestion, 3),
                            FormId = g.Key.FormId
                        };
                    })
                    .ToList();

                CollectiveManualDifficultyLevelsDistributions.AddRange(manualGroups);

                if (CurrentSelectedForm != null)
                {
                    CurrentManualDifficultyLevelsDistributions = CollectiveManualDifficultyLevelsDistributions
                        .Where(x => x.FormId == CurrentSelectedForm.FormId)
                        .ToList();

                    RecalculateAndValidateMarksDifficultyLevelForManual();
                }
            }
        }

        private void InitializeItemBanksDistributions()
        {
            if (IsStandardAutoPaper)
            {
                AutoItemBanksDistributions.Clear();

                var itemBankGroups = AutoQuestions
                    .GroupBy(q => new { q.ItemBankId, q.ItemBankName })
                    .Select(g => new GetItemBanksMarkingSchemeResponseDto
                    {
                        ItemBankId = g.Key.ItemBankId,
                        ItemBankName = g.Key.ItemBankName,
                        ItemBankMark = 0,
                        QuestionCount = g.Sum(q => q.SelectedCount),
                        QuestionMark = 0.0
                    });

                AutoItemBanksDistributions.AddRange(itemBankGroups);
            }
            else if (IsStandardManualPaper)
            {
                var lastFormId = CollectiveManualPaperFormSectionQuestionResponseDto.Forms
                    .OrderByDescending(f => f.FormId)
                    .Select(f => f.FormId)
                    .FirstOrDefault();

                CollectiveManualItemBanksDistributions.Clear();

                var itemBankGroups = CollectiveManualPaperFormSectionQuestionResponseDto
                    .Questions
                    .GroupBy(q => new { q.ItemBankId, q.ItemBankName, q.FormId })
                    .Select(g =>
                    {
                        var totalUnits = g.Sum(q => q.SubQuestionsCount > 0 ? q.SubQuestionsCount : 1);
                        var totalMarks = g.Sum(q => q.Mark ?? 0);
                        var markPerQuestion = totalUnits == 0 ? 0 : totalMarks / totalUnits;

                        return new GetItemBanksMarkingSchemeResponseDto
                        {
                            ItemBankId = g.Key.ItemBankId,
                            ItemBankName = g.Key.ItemBankName,
                            ItemBankMark = Math.Round(totalMarks, 3),
                            QuestionCount = totalUnits,
                            QuestionMark = Math.Round(markPerQuestion, 3),
                            FormId = g.Key.FormId
                        };
                    })
                    .ToList();

                CollectiveManualItemBanksDistributions.AddRange(itemBankGroups);

                if (CurrentSelectedForm != null)
                {
                    CurrentManualItemBanksDistributions = [.. CollectiveManualItemBanksDistributions.Where(x => x.FormId == CurrentSelectedForm.FormId)];

                    RecalculateAndValidateMarksItemBankForManual();
                }
            }
        }

        private void SchemeChanged(string value)
        {
            _selectedScheme = value;

            if (IsStandardAutoPaper && _selectedScheme == nameof(ScoreSchemaType.EqualDistribution))
            {
                currentTotal = 0;

                InitializeAutoQuestionsMarks();

                DistributeMarksEquallyForAuto();
            }
            else if (IsStandardManualPaper && _selectedScheme == nameof(ScoreSchemaType.EqualDistribution))
            {
                currentTotal = 0;

                DistributeMarksEquallyForManual();
            }
            else if (_selectedScheme == nameof(ScoreSchemaType.DifficultyLevelBasedDistribution))
            {
                currentDifficultyLevelTotal = 0;

                InitializeDifficultyLevelsDistributions();
            }
            else if (_selectedScheme == nameof(ScoreSchemaType.ItemBankBasedDistribution))
            {
                currentItemBankTotal = 0;

                InitializeItemBanksDistributions();
            }

            StateHasChanged();
        }

        private void ResetToEqualDistribution()
        {
            if (IsStandardAutoPaper)
                DistributeMarksEquallyForAuto();
            else if (IsStandardManualPaper)
                DistributeMarksEquallyForManual();
        }


        // FORM SUBMIT METHODS

        public async Task<bool> OnFifthStepMarkingSchemeSubmitAsync()
        {
            if (PaperMetadataResultedParamsDto.SelectedPaperType == PaperType.Adaptive)
            {
                var response = await BlazMarkingSchemeService.AddOrUpdateMarkingSchemeAsync(new MarkingSchemeDto
                {
                    PaperId = PaperMetadataResultedParamsDto.PaperId,
                    ScoreType = ScoreSchemaType.AdaptiveDistribution
                });

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                    return false;
                }
            }

            if (HasAnyZeroScore())
            {
                Snackbar.Add(Resource.ScoreCannotBeZero, Severity.Error);
                return false;
            }

            if (ValidateMarkingScheme())
            {
                MarkingSchemeDto markingSchemeDto = new()
                {
                    Id = 0,
                    Name = string.Format(Resource.MarkingSchemeName, _selectedScheme),
                    ScoreType = Enum.Parse<ScoreSchemaType>(_selectedScheme),
                    Data = GenerateMarkingSchemeContent(),
                    PaperId = PaperMetadataResultedParamsDto.PaperId
                };

                var apiResponse = await BlazMarkingSchemeService.AddOrUpdateMarkingSchemeAsync(markingSchemeDto);

                if (apiResponse.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(apiResponse.Message, Severity.Success);

                    return true;
                }
                else
                {
                    Snackbar.Add(apiResponse.Message, Severity.Error);
                }
            }

            return false;
        }

        private string GenerateMarkingSchemeContent()
        {
            var content = new GenericMarkingSchemeApplicationDto<object>(
                PaperMetadataResultedParamsDto.TotalExamMark,
                GetSchemeData()
            );

            return JsonSerializer.Serialize(content);
        }

        private object GetSchemeData() => _selectedScheme switch
        {
            nameof(ScoreSchemaType.EqualDistribution) when IsStandardAutoPaper =>
                AutoQuestionMarks.ConvertAll(q => new EqualDistributionForAutoDto(q.AutoPaperItemBankQuestionSectionId, q.Mark)),

            nameof(ScoreSchemaType.EqualDistribution) when IsStandardManualPaper =>
                CurrentManualSelectedFormQuestions.Select(q => new EqualDistributionForManualDto(q.QuestionMetadataId, q.Mark, q.FormId)),

            nameof(ScoreSchemaType.DifficultyLevelBasedDistribution) when IsStandardAutoPaper =>
                AutoDifficultyLevelsDistributions.Select(d => new DifficultyLevelDistributionDto(d.DifficultyLevelId, d.DifficultyLevelName, d.SelectedCount, d.MarkPerQuestion, d.FormId)),

            nameof(ScoreSchemaType.DifficultyLevelBasedDistribution) when IsStandardManualPaper =>
                CurrentManualDifficultyLevelsDistributions.OrderBy(d => d.FormId).Select(d => new DifficultyLevelDistributionDto(d.DifficultyLevelId, d.DifficultyLevelName, d.SelectedCount, d.MarkPerQuestion, d.FormId)),

            nameof(ScoreSchemaType.ItemBankBasedDistribution) when IsStandardAutoPaper =>
                AutoItemBanksDistributions.Select(i => new ItemBankDistributionDto(i.ItemBankId, i.ItemBankName, i.ItemBankMark, i.QuestionCount, i.QuestionMark, i.FormId)),

            nameof(ScoreSchemaType.ItemBankBasedDistribution) when IsStandardManualPaper =>
                CurrentManualItemBanksDistributions.OrderBy(i => i.FormId).Select(i => new ItemBankDistributionDto(i.ItemBankId, i.ItemBankName, i.ItemBankMark, i.QuestionCount, i.QuestionMark, i.FormId)),

            nameof(ScoreSchemaType.WeightedExamDistribution) => GetWeightedExamData(),

            _ => null
        };

        private object GetWeightedExamData()
        {
            if (IsStandardAutoPaper)
            {
                var weightedExamAuto = AutoQuestionMarks.Select(q => new WeightedExamDistributionDto(
                    q.AutoPaperItemBankQuestionSectionId,
                    q.DeltaValue,
                    null
                ));

                return weightedExamAuto;
            }
            else if (IsStandardManualPaper)
            {
                var weightedExamManual = CollectiveManualPaperFormSectionQuestionResponseDto
                    .Questions
                    .OrderBy(q => q.FormId)
                    .Select(q => new WeightedExamDistributionDto(
                        q.QuestionMetadataId,
                        q.DeltaValue,
                        q.FormId
                    ));

                return weightedExamManual;
            }

            return null;
        }


        // VALIDATION METHODS

        private bool ValidateMarkingScheme()
        {
            if (IsStandardAutoPaper && _selectedScheme == nameof(ScoreSchemaType.EqualDistribution))
            {
                var isFinalDistributionValid = RecalculateAndValidateQuestionsMarksForAuto();

                if (!isFinalDistributionValid)
                {
                    Snackbar.Add(Resource.MarkingSchemaCommonErrorMessage, Severity.Error);
                    return false;
                }
            }
            else if (IsStandardManualPaper && _selectedScheme == nameof(ScoreSchemaType.EqualDistribution))
            {
                var isFinalDistributionValid = RecalculateAndValidateQuestionsMarksForManual();

                if (!isFinalDistributionValid)
                {
                    Snackbar.Add(Resource.MarkingSchemaCommonErrorMessage, Severity.Error);
                    return false;
                }
            }
            else if (IsStandardAutoPaper && _selectedScheme == nameof(ScoreSchemaType.DifficultyLevelBasedDistribution))
            {
                var isFinalDistributionValid = RecalculateAndValidateMarksDifficultyLevelForAuto();

                if (!isFinalDistributionValid)
                {
                    Snackbar.Add(Resource.MarkingSchemaCommonErrorMessage, Severity.Error);
                    return false;
                }
            }
            else if (IsStandardManualPaper && _selectedScheme == nameof(ScoreSchemaType.DifficultyLevelBasedDistribution))
            {
                return _isDifficultyLevelValid;
            }
            else if (IsStandardAutoPaper && _selectedScheme == nameof(ScoreSchemaType.ItemBankBasedDistribution))
            {
                var isFinalDistributionValid = RecalculateAndValidateMarksItemBankForAuto();

                if (!isFinalDistributionValid)
                {
                    Snackbar.Add(Resource.MarkingSchemaCommonErrorMessage, Severity.Error);
                    return false;
                }
            }
            else if (IsStandardManualPaper && _selectedScheme == nameof(ScoreSchemaType.ItemBankBasedDistribution))
            {
                var isFinalDistributionValid = RecalculateAndValidateMarksItemBankForManual();

                if (!isFinalDistributionValid)
                {
                    Snackbar.Add(Resource.MarkingSchemaCommonErrorMessage, Severity.Error);
                    return false;
                }
            }
            else if (_selectedScheme == nameof(ScoreSchemaType.WeightedExamDistribution))
            {
                var isFinalDistributionValid = RecalculateAndValidateMarksWeightedExam();

                if (!isFinalDistributionValid)
                {
                    Snackbar.Add(Resource.MarkingSchemaCommonErrorMessage, Severity.Error);
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool RecalculateAndValidateQuestionsMarksForAuto()
        {
            currentTotal = Math.Round(AutoQuestionMarks.Sum(q => q.Mark ?? 0), 3);

            _isAutoOrManualValid = currentTotal == PaperMetadataResultedParamsDto.TotalExamMark;

            return _isAutoOrManualValid;
        }

        private bool RecalculateAndValidateQuestionsMarksForManual()
        {
            // Per form:

            currentTotal = Math.Round(CurrentManualSelectedFormQuestions.Sum(q => q.Mark ?? 0), 3);

            _isAutoOrManualValid = currentTotal == PaperMetadataResultedParamsDto.TotalExamMark;

            // For all forms:

            var total = Math.Round(CollectiveManualPaperFormSectionQuestionResponseDto.Questions.Sum(q => q.Mark ?? 0), 3);

            var totalFormsDistributionValid = total == (PaperMetadataResultedParamsDto.TotalExamMark * PaperMetadataResultedParamsDto.OutputFormsCount);

            return _isAutoOrManualValid;
        }

        private bool RecalculateAndValidateMarksDifficultyLevelForAuto()
        {
            currentDifficultyLevelTotal = AutoDifficultyLevelsDistributions.Sum(d => d.MarkPerQuestion * d.SelectedCount);

            _isDifficultyLevelValid = currentDifficultyLevelTotal == PaperMetadataResultedParamsDto.TotalExamMark;

            return _isDifficultyLevelValid;
        }

        private bool RecalculateAndValidateMarksDifficultyLevelForManual()
        {
            // Per form:

            currentDifficultyLevelTotal = CurrentManualDifficultyLevelsDistributions.Sum(d => d.MarkPerQuestion * d.SelectedCount);

            _isDifficultyLevelValid = currentDifficultyLevelTotal == PaperMetadataResultedParamsDto.TotalExamMark;

            // For all forms:

            var total = CollectiveManualDifficultyLevelsDistributions.Sum(d => d.MarkPerQuestion * d.SelectedCount);

            var totalFormsDistributionValid = total == (PaperMetadataResultedParamsDto.TotalExamMark * PaperMetadataResultedParamsDto.OutputFormsCount);

            return _isDifficultyLevelValid;
        }

        private bool RecalculateAndValidateMarksItemBankForAuto()
        {
            currentItemBankTotal = AutoItemBanksDistributions.Sum(b => b.ItemBankMark);

            _isItemBankValid = currentItemBankTotal == PaperMetadataResultedParamsDto.TotalExamMark;

            if (_isItemBankValid)
            {
                foreach (var bank in AutoItemBanksDistributions.Where(b => b.QuestionCount > 0))
                {
                    bank.QuestionMark = bank.ItemBankMark / bank.QuestionCount;
                }
            }

            return _isItemBankValid;
        }

        private bool RecalculateAndValidateMarksItemBankForManual()
        {
            // Per form:

            currentItemBankTotal = CurrentManualItemBanksDistributions.Sum(b => b.ItemBankMark);

            _isItemBankValid = currentItemBankTotal == PaperMetadataResultedParamsDto.TotalExamMark;

            if (_isItemBankValid)
            {
                foreach (var bank in CurrentManualItemBanksDistributions.Where(b => b.QuestionCount > 0))
                {
                    bank.QuestionMark = bank.ItemBankMark / bank.QuestionCount;
                }
            }

            return _isItemBankValid;
        }

        private bool RecalculateAndValidateMarksWeightedExam()
        {
            if (IsStandardAutoPaper)
                _isWeightedExamValid = AutoQuestionMarks.TrueForAll(question => question.DeltaValue >= 0.0m && question.DeltaValue <= 1.0m);
            else if (IsStandardManualPaper)
                _isWeightedExamValid = CurrentManualSelectedFormQuestions.TrueForAll(question => question.DeltaValue >= 0.0m && question.DeltaValue <= 1.0m);

            return _isWeightedExamValid;
        }

        private bool HasAnyZeroScore()
        {
            if (_selectedScheme == nameof(ScoreSchemaType.EqualDistribution))
            {
                if (IsStandardAutoPaper)
                {
                    return AutoQuestionMarks.Any(q => (q.Mark ?? 0) == 0);
                }
                else if (IsStandardManualPaper)
                {
                    return CurrentManualSelectedFormQuestions.Any(q => (q.Mark ?? 0) == 0);
                }
            }
            else if (_selectedScheme == nameof(ScoreSchemaType.DifficultyLevelBasedDistribution))
            {
                if (IsStandardAutoPaper)
                {
                    var lastFormId = AutoDifficultyLevelsDistributions.OrderByDescending(f => f.FormId)
                        .Select(f => f.FormId)
                        .FirstOrDefault();

                    return AutoDifficultyLevelsDistributions.Any(d => d.MarkPerQuestion == 0 && d.FormId == lastFormId);

                }
                else if (IsStandardManualPaper)
                {
                    return CurrentManualDifficultyLevelsDistributions.Any(d => d.MarkPerQuestion == 0);
                }
            }
            else if (_selectedScheme == nameof(ScoreSchemaType.ItemBankBasedDistribution))
            {
                if (IsStandardAutoPaper)
                {
                    var lastFormId = AutoItemBanksDistributions.OrderByDescending(f => f.FormId)
                        .Select(f => f.FormId)
                        .FirstOrDefault();

                    return AutoItemBanksDistributions.Any(b => b.ItemBankMark == 0 && b.FormId == lastFormId);
                }
                else if (IsStandardManualPaper)
                {
                    return CurrentManualItemBanksDistributions.Any(b => b.ItemBankMark == 0);
                }
            }

            return false;
        }

        private void ToggleQuestionBodyExpansion(long questionId)
        {
            if (!_expandedQuestionIds.Remove(questionId))
            {
                _expandedQuestionIds.Add(questionId);
            }

            StateHasChanged();
        }

        private static string GetQuestionPreviewText(string body)
        {
            var clean = HtmlTagsCleaner.Clean(body ?? string.Empty);

            return clean.Length > MiscConstants.QuestionBodyPreviewThreshold
                ? clean[..MiscConstants.QuestionBodyPreviewThreshold] + "…"
                : clean;
        }
    }
}
