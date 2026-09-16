using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Paper.Sectioning.ManualSectioning;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.SectionDistributionDto.Common;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.FourthStep.QuestionsSectioning
{
    public partial class ManuallySelectedQuestionsSectioning
    {
        [Inject] private IBlazPaperService BlazPaperService { get; set; }

        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Inject] private IBlazFormService BlazFormService { get; set; }

        [Inject] private ISnackbar Snackbar { get; set; }

        [Inject] private IDialogService DialogService { get; set; }

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        private bool IsCurrentFormsModeContinuePendingForm => _paperStepperFormsMode == PaperStepperFormsMode.ContinuePendingForm;

        private bool IsCurrentFormsModeSingleManualForm =>
            (_paperStepperFormsMode is PaperStepperFormsMode.AddNormalSingleManualForm or PaperStepperFormsMode.AddExcelSheetSingleManualForm) || IsCurrentFormsModeContinuePendingForm;

        private List<FormMetadataDto> _formsMetadata = [];
        private readonly Dictionary<string, List<SectionRequestDto>> _formSections = [];
        private Dictionary<string, List<QuestionDistributionInfo>> _formQuestionDistribution = [];
        private Dictionary<string, int> _sectionQuestionCount = [];
        private readonly SectionRequestDto _newSectionModel = new(string.Empty, string.Empty);
        private MudDropContainer<ManuallySelectedQuestionsResponseDto> _dropContainer;
        private int _dropContainerChangingKey;
        private bool _addSectionFormOpened;
        private readonly HashSet<long> _expandedQuestionIds = [];
        private string _bulkTargetSectionIdentifier = string.Empty;
        private string _formName;
        private string _formCode;
        private string _formDescribtion;
        private FormDistributionMode _distributionMode = FormDistributionMode.DragDrop;
        private PaperStepperFormsMode _paperStepperFormsMode;
        private readonly List<long> _selectedQuestionIdsToBeMovedToSection = [];
        private string _manualQuestionsSearchQuery = string.Empty;
        private readonly HashSet<string> _expandedItemBankNames = [];
        private readonly Dictionary<string, string> _sectionSearchQueries = [];

        private FormMetadataDto SelectedFormMetadata { get; set; } = new();

        private List<ManuallySelectedQuestionsResponseDto> ManualPaperBoundQuestions { get; set; } = [];

        private List<ManuallySelectedQuestionsResponseDto> _availableQuestionsInPool = [];
        private List<ManuallySelectedQuestionsResponseDto> _questionsForCurrentForm = [];
        private List<ManuallySelectedQuestionsResponseDto> AvailableQuestionsInPool => _availableQuestionsInPool;
        private List<ManuallySelectedQuestionsResponseDto> QuestionsForCurrentForm => _questionsForCurrentForm;

        private void RecalculateCurrentFormQuestions()
        {
            if (ManualPaperBoundQuestions == null)
            {
                _availableQuestionsInPool = [];
                _questionsForCurrentForm = [];
                _sectionQuestionCount = [];
                return;
            }

            _availableQuestionsInPool = [.. ManualPaperBoundQuestions.Where(q => q.SectioningIdentifier == MiscConstants.ManualQuestionsSelectionPool)];

            if (SelectedFormMetadata != null && _formQuestionDistribution.TryGetValue(SelectedFormMetadata.Name, out var questions))
            {
                var ids = questions.Select(x => x.PaperItemBankQuestionId).ToHashSet();
                _questionsForCurrentForm = [.. ManualPaperBoundQuestions.Where(q => ids.Contains(q.Id)).DistinctBy(q => q.Id)];
            }
            else
            {
                _questionsForCurrentForm = [];
            }

            _sectionQuestionCount = ManualPaperBoundQuestions
                .GroupBy(q => q.SectioningIdentifier)
                .ToDictionary(g => g.Key, g => g.Count());
        }


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            var paperStepperFormsMode = await BlazSessionStorageService.GetValue<int>(nameof(PaperStepperFormsMode));
            _paperStepperFormsMode = (PaperStepperFormsMode)paperStepperFormsMode;

            var responseDto = await BlazPaperService.GetPaperManualSectionsWithQuestionsAsync(PaperMetadataResultedParamsDto.PaperId);

            ManualPaperBoundQuestions = responseDto.PaperManualQuestions;

            _sectionQuestionCount = ManualPaperBoundQuestions
                .GroupBy(q => q.SectioningIdentifier)
                .ToDictionary(g => g.Key, g => g.Count());

            if (responseDto.FormNames?.Count > 0)
            {
                RestoreFormsAndSectionsForUpdateMode(responseDto);
            }
            else
            {
                GenerateFormsAndSectionsForCreateMode();
            }

            var formToBeInitiallySelected = IsCurrentFormsModeSingleManualForm
                ? _formsMetadata.LastOrDefault()
                : _formsMetadata.FirstOrDefault();

            MaintainCurrentFormMetadataState(formToBeInitiallySelected);
            RecalculateCurrentFormQuestions();
        }

        private void GenerateFormsAndSectionsForCreateMode()
        {
            if (PaperMetadataResultedParamsDto?.OutputFormsCount > 0)
            {
                bool isArabicLanguage = PaperMetadataResultedParamsDto.LanguageName?.Contains("Arabic", StringComparison.OrdinalIgnoreCase) == true ||
                                        PaperMetadataResultedParamsDto.LanguageName?.Contains("عربي", StringComparison.OrdinalIgnoreCase) == true;

                string formWord = isArabicLanguage ? "نموذج" : "Form";

                for (int i = 1; i <= PaperMetadataResultedParamsDto.OutputFormsCount; i++)
                {
                    var formName = $"{formWord} {i}";
                    var formCode = $"{formWord}-{Resource.Code}-{i}";
                    var formDescription = $"{formWord}-{Resource.Description}-{i}";

                    _formsMetadata.Add(new FormMetadataDto(0, formName, formCode, formDescription));

                    _formSections[formName] =
                    [
                        new(MiscConstants.ManualQuestionsSelectionPool, MiscConstants.ManualQuestionsSelectionPool),
                    ];
                }
            }
        }

        private void RestoreFormsAndSectionsForUpdateMode(GetPaperManualSectionsWithQuestionsResponseDto getPaperManualSectionsWithQuestionsResponseDto)
        {
            if (!(getPaperManualSectionsWithQuestionsResponseDto.FormNames?.Count > 0 &&
                  getPaperManualSectionsWithQuestionsResponseDto.PaperManualQuestions?.Count > 0)
            )
            {
                return;
            }

            _formsMetadata.Clear();
            _formSections.Clear();
            _formQuestionDistribution.Clear();

            _formsMetadata = getPaperManualSectionsWithQuestionsResponseDto.FormNames.ConvertAll(x => new FormMetadataDto(x.FormId, x.FormName, x.FormCode, x.FormDescription));

            var questionsSectionsDist = new List<FormsDistributions>();

            foreach (var question in getPaperManualSectionsWithQuestionsResponseDto.QuestionSectionFormDist)
            {
                var targetSection = getPaperManualSectionsWithQuestionsResponseDto.PaperManualSections
                    .FirstOrDefault(x => x.Id == question.SectionId);

                var targetFormName = getPaperManualSectionsWithQuestionsResponseDto.FormNames
                    .Where(x => x.FormId == question.FormId)
                    .Select(x => x.FormName)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(targetFormName) && targetFormName != null)
                {
                    questionsSectionsDist.Add(new FormsDistributions(targetFormName, new QuestionDistributionInfo(question.PaperItemBankQuestionId, targetSection.Name, targetSection.SectioningIdentifier)));
                }
            }

            // NOTE: This condition applies only if we have existing distribution to restore.
            if (getPaperManualSectionsWithQuestionsResponseDto.QuestionSectionFormDist.Count > 0)
            {
                var distributionByFormName = questionsSectionsDist.GroupBy(x => x.FormName);

                foreach (var formGroup in distributionByFormName)
                {
                    var sectionList = new List<SectionRequestDto>
                    {
                        new(MiscConstants.ManualQuestionsSelectionPool, MiscConstants.ManualQuestionsSelectionPool)
                    };

                    foreach (var dist in formGroup)
                    {
                        if (dist.Distributions.SectioningIdentifier != null && !sectionList.Any(s => s.SectioningIdentifier == dist.Distributions.SectioningIdentifier))
                        {
                            var currentSection = getPaperManualSectionsWithQuestionsResponseDto.PaperManualSections.Find(x => x.SectioningIdentifier == dist.Distributions.SectioningIdentifier) ?? new();

                            sectionList.Add(new SectionRequestDto(
                                dist.Distributions.SectionName,
                                dist.Distributions.SectioningIdentifier,
                                currentSection.IsRestrictedTime,
                                currentSection.TimeInMinutes,
                                currentSection.IsRandom,
                                currentSection.InstructionSectionTemplateId,
                                currentSection.OrderId
                            ));
                        }
                    }

                    _formSections[formGroup.Key] = sectionList;
                }

                _formQuestionDistribution = distributionByFormName.ToDictionary(g => g.Key, g => g.Select(g => g.Distributions).ToList());
            }
            else // NOTE: If no existing distribution, we just create a pool section for each form.
            {
                foreach (var form in _formsMetadata)
                {
                    var sectionList = new List<SectionRequestDto>
                    {
                        new(MiscConstants.ManualQuestionsSelectionPool, MiscConstants.ManualQuestionsSelectionPool)
                    };

                    _formSections[form.Name] = sectionList;
                }
            }

            AddNewFormsInUpdateModeIfAny();
        }

        private void AddNewFormsInUpdateModeIfAny()
        {
            var formsToAdd = PaperMetadataResultedParamsDto.OutputFormsCount - _formsMetadata.Count;

            if (PaperMetadataResultedParamsDto.OutputFormsCount > 0 && formsToAdd > 0)
            {
                bool isArabicLanguage = PaperMetadataResultedParamsDto.LanguageName?.Contains("Arabic", StringComparison.OrdinalIgnoreCase) == true ||
                                        PaperMetadataResultedParamsDto.LanguageName?.Contains("عربي", StringComparison.OrdinalIgnoreCase) == true;

                string formWord = isArabicLanguage ? "نموذج" : "Form";

                var existingMaxNumber = _formsMetadata
                    .Select(f =>
                    {
                        var lastPart = f.Name?.Split(' ').LastOrDefault();
                        return int.TryParse(lastPart, out var num) ? num : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                var nextCounter = existingMaxNumber + 1;

                for (int i = 0; i < formsToAdd; i++)
                {
                    var formName = $"{formWord} {nextCounter}";
                    var formCode = $"{formWord}-{Resource.Code}-{nextCounter}";
                    var formDescription = $"{formWord}-{Resource.Description}-{nextCounter}";

                    _formsMetadata.Add(new FormMetadataDto(0, formName, formCode, formDescription));

                    _formSections[formName] =
                    [
                        new(MiscConstants.ManualQuestionsSelectionPool, MiscConstants.ManualQuestionsSelectionPool),
                    ];

                    nextCounter++;
                }

                StateHasChanged();
            }
        }

        private void OnFormMetadataSavedOnTheFly()
        {
            if (!ValidateNameAndCodeForCurrentForm()) return;

            if (_formsMetadata == null || _formsMetadata.Count == 0) return;

            // Handling Current Form:

            if (QuestionsForCurrentForm.Count > PaperMetadataResultedParamsDto.QuestionsCount)
            {
                Snackbar.Add(string.Format(Resource.FormQuestionsCountMismatch, PaperMetadataResultedParamsDto.QuestionsCount, QuestionsForCurrentForm.Count, Severity.Error));
                return;
            }

            var oldFormName = SelectedFormMetadata.Name;

            var targetFormMetadata = _formsMetadata.FirstOrDefault(f => f.Name == oldFormName);
            targetFormMetadata.Name = _formName;
            targetFormMetadata.Code = _formCode;
            targetFormMetadata.Description = _formDescribtion;

            if (_formSections.TryGetValue(oldFormName, out List<SectionRequestDto> targetFormSections))
            {
                _formSections.Remove(oldFormName);
                _formSections[targetFormMetadata.Name] = targetFormSections;
            }

            if (_formQuestionDistribution.TryGetValue(oldFormName, out List<QuestionDistributionInfo> targetFormQuestionDistributions))
            {
                _formQuestionDistribution.Remove(oldFormName);
                _formQuestionDistribution[targetFormMetadata.Name] = targetFormQuestionDistributions;
            }
            else
            {
                _formQuestionDistribution[targetFormMetadata.Name] = QuestionsForCurrentForm.ConvertAll(q => new QuestionDistributionInfo(q.Id, q.SectionName, q.SectioningIdentifier));
            }

            Snackbar.Add(Resource.FormMetadataSavedSuccessfully, Severity.Success);

            StateHasChanged();
        }

        private void MaintainCurrentFormMetadataState(FormMetadataDto selectedFormMetadataDto)
        {
            SelectedFormMetadata = selectedFormMetadataDto;

            _formName = selectedFormMetadataDto?.Name ?? string.Empty;
            _formCode = selectedFormMetadataDto?.Code ?? string.Empty;
            _formDescribtion = selectedFormMetadataDto?.Description ?? string.Empty;

            StateHasChanged();
            RecalculateCurrentFormQuestions();
        }

        private void OnQuestionUpdated(MudItemDropInfo<ManuallySelectedQuestionsResponseDto> info)
        {
            if (info.DropzoneIdentifier == MiscConstants.ManualQuestionsSelectionPool)
            {
                Snackbar.Add(Resource.UseReturnButtonToGoBack, Severity.Info);
                return;
            }

            if (info.Item.SectioningIdentifier != MiscConstants.ManualQuestionsSelectionPool)
            {
                Snackbar.Add(Resource.ReturnQuestionBackToPoolThenReassignToSection, Severity.Info);
                return;
            }

            if (info.DropzoneIdentifier != MiscConstants.ManualQuestionsSelectionPool &&
                ManualPaperBoundQuestions.Any(x => x.QuestionMetadataId == info.Item.QuestionMetadataId && x.SectioningIdentifier == info.DropzoneIdentifier))
            {
                Snackbar.Add(Resource.QuestionAlreadyAssignedToSectionBefore, Severity.Error);
                return;
            }

            if (!CanAddQuestionsToCurrentForm([info.Item], out var errorMessage))
            {
                Snackbar.Add(errorMessage, Severity.Error);

                info.Item.SectionName = MiscConstants.ManualQuestionsSelectionPool;
                info.Item.SectioningIdentifier = MiscConstants.ManualQuestionsSelectionPool;

                _dropContainerChangingKey++;

                return;
            }

            info.Item.SectionName = _formSections[SelectedFormMetadata.Name]?.Find(s => s.SectioningIdentifier == info.DropzoneIdentifier)?.Name;
            info.Item.SectioningIdentifier = info.DropzoneIdentifier;

            SaveDistributionOnTheFly(info.Item);

            StateHasChanged();

            RecalculateCurrentFormQuestions();
        }

        private void TransferAllQuestionsToFirstSection()
        {
            if (SelectedFormMetadata is null) return;

            var firstSectionOfCurrentForm = _formSections[SelectedFormMetadata.Name].FirstOrDefault(s => s.SectioningIdentifier != MiscConstants.ManualQuestionsSelectionPool);

            if (firstSectionOfCurrentForm == null) return;

            var questionsToMove = AvailableQuestionsInPool;

            if (!CanAddQuestionsToCurrentForm(questionsToMove, out var errorMessage))
            {
                Snackbar.Add(errorMessage, Severity.Error);
                return;
            }

            foreach (var question in questionsToMove)
            {
                MoveQuestionToSection(question, firstSectionOfCurrentForm.SectioningIdentifier, firstSectionOfCurrentForm.Name);
            }

            _dropContainerChangingKey++;

            RecalculateCurrentFormQuestions();
        }

        private void ReturnSectionedQuestionBackToPool(ManuallySelectedQuestionsResponseDto sectionedQuestion)
        {
            var questionToUpdate = ManualPaperBoundQuestions.FirstOrDefault(q => q.Id == sectionedQuestion.Id);

            if (questionToUpdate != null)
            {
                questionToUpdate.SectionName = MiscConstants.ManualQuestionsSelectionPool;
                questionToUpdate.SectioningIdentifier = MiscConstants.ManualQuestionsSelectionPool;

                if (SelectedFormMetadata is not null && _formQuestionDistribution.TryGetValue(SelectedFormMetadata.Name, out var distribution))
                {
                    distribution.RemoveAll(d => d.PaperItemBankQuestionId == sectionedQuestion.Id);
                }

                _dropContainerChangingKey++;

                StateHasChanged();

                RecalculateCurrentFormQuestions();
            }
        }

        private void SaveDistributionOnTheFly(ManuallySelectedQuestionsResponseDto manuallySelectedQuestionsResponseDto)
        {
            _formQuestionDistribution.TryAdd(SelectedFormMetadata.Name, []);
            _formQuestionDistribution[SelectedFormMetadata.Name].RemoveAll(d => d.PaperItemBankQuestionId == manuallySelectedQuestionsResponseDto.Id);
            _formQuestionDistribution[SelectedFormMetadata.Name].Add(new QuestionDistributionInfo(manuallySelectedQuestionsResponseDto.Id, manuallySelectedQuestionsResponseDto.SectionName, manuallySelectedQuestionsResponseDto.SectioningIdentifier));
        }


        // SECTION OPERATIONS

        private async Task OpenSectionTemplateDialogAsync(SectionRequestDto section)
        {
            var parameters = new DialogParameters<ManualSectionInstructionTemplateDialog>
            {
                { x => x.Section, section }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            await DialogService.ShowAsync<ManualSectionInstructionTemplateDialog>(string.Empty, parameters, options);
        }

        private async Task OpenSectionPropertiesDialogAsync(SectionRequestDto section)
        {
            var formSectionsCountWithoutPoolSection = _formSections[SelectedFormMetadata.Name].Count(s => s.SectioningIdentifier != MiscConstants.ManualQuestionsSelectionPool);

            var availableOrderNumbers = Enumerable.Range(1, formSectionsCountWithoutPoolSection).ToList();

            var parameters = new DialogParameters<ManualSectionPropertiesDialog>
            {
                { x => x.Section, section },
                { x => x.AvailableOrderNumbers, availableOrderNumbers }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<ManualSectionPropertiesDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (result?.Canceled == false)
            {
                _dropContainerChangingKey++;

                Snackbar.Add(
                    string.Format(Resource.SectionPropertiesUpdatedSuccessfully),
                    Severity.Success
                );
            }
        }

        private void OpenAddNewSectionForm()
        {
            _addSectionFormOpened = true;
        }

        private void CloseAddNewSectionForm()
        {
            _addSectionFormOpened = false;
        }

        private async Task DeleteSection(SectionRequestDto section)
        {
            if (section.SectioningIdentifier == MiscConstants.ManualQuestionsSelectionPool || SelectedFormMetadata is null) return;

            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, $"{Resource.ThisSectionContains} {Resource.Items}. {Resource.DeleteAndReturnQuestions}" },
                { x => x.SubmitText, Resource.Delete },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(Resource.DeleteConfirmation, parameters, options);
            var result = await dialog.Result;

            if (result.Canceled) return;

            var questionsToReturnToPool = new List<long>();

            if (_formQuestionDistribution.TryGetValue(SelectedFormMetadata.Name, out var distribution))
            {
                questionsToReturnToPool = [.. distribution
                    .Where(d => d.SectioningIdentifier == section.SectioningIdentifier)
                    .Select(d => d.PaperItemBankQuestionId)
                ];

                distribution.RemoveAll(d => d.SectioningIdentifier == section.SectioningIdentifier);
            }

            _formSections[SelectedFormMetadata.Name].Remove(section);

            foreach (var paperItemBankQuestionId in questionsToReturnToPool)
            {
                var questionObject = ManualPaperBoundQuestions.FirstOrDefault(q => q.Id == paperItemBankQuestionId);

                if (questionObject != null)
                {
                    questionObject.SectionName = MiscConstants.ManualQuestionsSelectionPool;
                    questionObject.SectioningIdentifier = MiscConstants.ManualQuestionsSelectionPool;
                }
            }

            var counter = 0;
            _formSections[SelectedFormMetadata.Name]?.ForEach(x => x.OrderId = counter++); // Reset form sections order ids

            _dropContainerChangingKey++;

            StateHasChanged();

            RecalculateCurrentFormQuestions();
        }

        private static void StartSectionRenaming(SectionRequestDto section)
        {
            if (section.SectioningIdentifier == MiscConstants.ManualQuestionsSelectionPool) return;

            section.NewName = section.Name;

            section.IsRenaming = true;
        }

        private static void CancelSectionRenaming(SectionRequestDto section)
        {
            section.NewName = section.Name;

            section.IsRenaming = false;
        }

        private void HandleEnterKeyUponSectionRenaming(KeyboardEventArgs e, SectionRequestDto section)
        {
            if (e.Key == "Enter")
            {
                SaveSectionNewName(section);
            }
        }


        // FORM SUBMIT METHODS

        public async Task<bool> OnFourthStepManualQuestionsSectioningSubmitAsync()
        {
            if (ValidateQuestionsSectioning())
            {
                var finalDistributedQuestions = ManualPaperBoundQuestions;

                var formQuestionMapping = _formQuestionDistribution
                  .ToDictionary(
                      kvp => kvp.Key,
                      kvp => kvp.Value.ConvertAll(dist => dist.PaperItemBankQuestionId)
                  );

                var requestDto = new ManualSectioningRequestDto
                {
                    FormsMetadata = SelectedFormMetadata,
                    FormSections = new()
                    {
                        [SelectedFormMetadata.Name] =
                            _formSections[SelectedFormMetadata.Name]
                    },
                    Questions = QuestionsForCurrentForm,
                    FormQuestionMap = new()
                    {
                        [SelectedFormMetadata.Name] =
                            _formQuestionDistribution[SelectedFormMetadata.Name]
                                .ConvertAll(x => x.PaperItemBankQuestionId)
                    }
                };

                var response = await BlazPaperService.AddOrUpdateManualQuestionSectioningAsync(PaperMetadataResultedParamsDto.PaperId, requestDto);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);
                    return true;
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                    return false;
                }
            }

            return false;
        }


        // VALIDATION METHODS

        private bool ValidateQuestionsSectioning()
        {
            if (!_formQuestionDistribution.TryGetValue(SelectedFormMetadata.Name, out var currentFormDist) || currentFormDist.Count == 0)
            {
                Snackbar.Add(Resource.SaveDistributionForAllFormsRequired, Severity.Error);
                return false;
            }

            if (QuestionsForCurrentForm.Any(x => x.SectioningIdentifier == MiscConstants.ManualQuestionsSelectionPool))
            {
                Snackbar.Add(
                    Resource.CannotProceedWithQuestionsLeftInPool,
                    Severity.Error);

                return false;
            }

            foreach (var formDist in _formQuestionDistribution)
            {
                var formName = SelectedFormMetadata.Name;

                var sectionsForForm =
                    _formSections[formName]
                    .Select(s => s.SectioningIdentifier)
                    .Except([MiscConstants.ManualQuestionsSelectionPool]);

                var usedSectionsInForm =
                    _formQuestionDistribution[formName]
                    .Select(d => d.SectioningIdentifier)
                    .Distinct();

                if (sectionsForForm.Except(usedSectionsInForm).Any())
                {
                    Snackbar.Add(
                        string.Format(Resource.EmptySectionsInForm, formName),
                        Severity.Error);

                    return false;
                }
            }

            foreach (var formEntry in _formSections)
            {
                var sectionsForForm = _formSections[SelectedFormMetadata.Name];

                var (isValid, message) =
                    ValidateSectionsDurationsAgainstPaperDuration(
                        sectionsForForm,
                        SelectedFormMetadata.Name);

                if (!isValid)
                {
                    Snackbar.Add(message, Severity.Error);
                    return false;
                }
            }
            return true;
        }

        public bool ValidateNameAndCodeForCurrentForm()
        {
            var invalidCurrentFormNameOrCode = string.IsNullOrWhiteSpace(_formName) || string.IsNullOrWhiteSpace(_formCode);

            if (invalidCurrentFormNameOrCode)
            {
                Snackbar.Add(string.Format(Resource.InvalidFormNameOrCode, SelectedFormMetadata.Name), Severity.Info);
                return false;
            }

            var otherFormsNames = _formsMetadata.FindAll(x => x.Name != SelectedFormMetadata.Name).ConvertAll(x => x.Name.Trim().ToLower());
            if (otherFormsNames.Contains(_formName.Trim().ToLower()))
            {
                Snackbar.Add(string.Format(Resource.FormNameAlreadyExists, _formName), Severity.Info);
                return false;
            }

            var otherFormsCodes = _formsMetadata.FindAll(x => x.Code != SelectedFormMetadata.Code).ConvertAll(x => x.Code.Trim().ToLower());
            if (otherFormsCodes.Contains(_formCode.Trim().ToLower()))
            {
                Snackbar.Add(string.Format(Resource.FormCodeAlreadyExists, _formCode), Severity.Info);
                return false;
            }

            return true;
        }

        private void OnValidSectionSubmit()
        {
            if (SelectedFormMetadata is null) return;

            if (_formSections[SelectedFormMetadata.Name].Exists(s => s.Name.Equals(_newSectionModel.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Snackbar.Add(Resource.Asectionwiththisnamealreadyexists, Severity.Error);
                return;
            }

            var newFormSectionOrderId = _formSections[SelectedFormMetadata.Name].Count; // NOTE: Here we don't say (Count + 1) because the pool section is not counted.

            _formSections[SelectedFormMetadata.Name].Add(new SectionRequestDto(_newSectionModel.Name, RandomIntegerGenerator.GenerateShortUniqueNumber().ToString(), newFormSectionOrderId));
            _newSectionModel.Name = string.Empty;
            _addSectionFormOpened = false;

            RecalculateCurrentFormQuestions();
        }

        private void SaveSectionNewName(SectionRequestDto incomingSection)
        {
            if (string.IsNullOrWhiteSpace(incomingSection.NewName) || SelectedFormMetadata is null)
            {
                Snackbar.Add(Resource.SectionNameCannotBeEmpty, Severity.Error);
                return;
            }

            if (_formSections[SelectedFormMetadata.Name].Any(s => s != incomingSection && s.Name.Equals(incomingSection.NewName, StringComparison.OrdinalIgnoreCase)))
            {
                Snackbar.Add(Resource.Asectionwiththisnamealreadyexists, Severity.Error);
                return;
            }

            string newName = incomingSection.NewName;

            incomingSection.Name = newName;
            incomingSection.IsRenaming = false;

            if (_formQuestionDistribution.TryGetValue(SelectedFormMetadata.Name, out var distribution))
            {
                var entriesToUpdate = distribution.Where(d => d.SectioningIdentifier == incomingSection.SectioningIdentifier).ToList();

                foreach (var entry in entriesToUpdate)
                {
                    distribution.Remove(entry);
                    distribution.Add(new QuestionDistributionInfo(entry.PaperItemBankQuestionId, newName, incomingSection.SectioningIdentifier));
                }
            }

            RecalculateCurrentFormQuestions();
        }

        private (bool IsValid, string Message) ValidateSectionsDurationsAgainstPaperDuration(List<SectionRequestDto> sections, string formName)
        {
            var allSectionsWithoutPool = sections.Where(s => s.SectioningIdentifier != MiscConstants.ManualQuestionsSelectionPool).ToList();

            var invalidTimedSection = allSectionsWithoutPool.FirstOrDefault(s => s.IsRestrictedTime && s.TimeInMinutes <= 0);
            if (invalidTimedSection != null)
            {
                return (false, string.Format(Resource.InvalidTimedSectionDuration, formName, invalidTimedSection.Name));
            }

            const float epsilon = 0.0001f;
            double sum = allSectionsWithoutPool.Where(s => s.IsRestrictedTime).Sum(x => x.TimeInMinutes);
            if (allSectionsWithoutPool.Count > 0 && allSectionsWithoutPool.TrueForAll(x => x.IsRestrictedTime))
            {
                var isValid = Math.Abs(sum - PaperMetadataResultedParamsDto.PaperExamDuration) < epsilon;
                var message = isValid ? "" : string.Format(Resource.TimedSectionsDurationMismatch, formName, sum, PaperMetadataResultedParamsDto.PaperExamDuration);
                return (isValid, message);
            }
            else if (allSectionsWithoutPool.TrueForAll(x => !x.IsRestrictedTime))
            {
                return (true, "");
            }
            else
            {
                return (false, Resource.SectionsTimingError);
            }
        }

        private bool GetQuestionSelectionState(long questionId) => _selectedQuestionIdsToBeMovedToSection.Contains(questionId);

        private void ToggleQuestionSelection(long questionId, bool isSelected)
        {
            if (isSelected)
                _selectedQuestionIdsToBeMovedToSection.Add(questionId);
            else
                _selectedQuestionIdsToBeMovedToSection.Remove(questionId);
        }

        private void ToggleSelectAllInPool(List<ManuallySelectedQuestionsResponseDto> poolQuestions, bool allSelected)
        {
            if (allSelected)
            {
                foreach (var question in poolQuestions)
                    _selectedQuestionIdsToBeMovedToSection.Remove(question.Id);
            }
            else
            {
                foreach (var question in poolQuestions)
                    _selectedQuestionIdsToBeMovedToSection.Add(question.Id);
            }
        }

        private void ToggleSelectAllInItemBank(List<ManuallySelectedQuestionsResponseDto> itemBankQuestions, bool allSelected)
        {
            if (allSelected)
            {
                foreach (var question in itemBankQuestions)
                    _selectedQuestionIdsToBeMovedToSection.Remove(question.Id);
            }
            else
            {
                foreach (var question in itemBankQuestions)
                    _selectedQuestionIdsToBeMovedToSection.Add(question.Id);
            }
        }

        private void ClearSelection()
        {
            _selectedQuestionIdsToBeMovedToSection.Clear();
            _bulkTargetSectionIdentifier = string.Empty;
        }

        private void BulkAssignToSection()
        {
            if (string.IsNullOrEmpty(_bulkTargetSectionIdentifier) || _selectedQuestionIdsToBeMovedToSection.Count == 0) return;

            var questionsToMove = AvailableQuestionsInPool.Where(q => _selectedQuestionIdsToBeMovedToSection.Contains(q.Id)).ToList();

            if (!CanAddQuestionsToCurrentForm(questionsToMove, out var errorMessage))
            {
                Snackbar.Add(errorMessage, Severity.Error);
                return;
            }

            var bulkSectionName = _formSections[SelectedFormMetadata.Name]?.Find(s => s.SectioningIdentifier == _bulkTargetSectionIdentifier)?.Name;

            foreach (var question in questionsToMove)
            {
                MoveQuestionToSection(question, _bulkTargetSectionIdentifier, bulkSectionName);
            }

            ClearSelection();
            _dropContainerChangingKey++;
            StateHasChanged();
            RecalculateCurrentFormQuestions();
        }

        private void AssignSelectedQuestionsToSection(string sectionIdentifier)
        {
            var questionsToMove = AvailableQuestionsInPool.Where(q => _selectedQuestionIdsToBeMovedToSection.Contains(q.Id)).ToList();

            if (!CanAddQuestionsToCurrentForm(questionsToMove, out var errorMessage))
            {
                Snackbar.Add(errorMessage, Severity.Error);
                return;
            }

            var assignSectionName = _formSections[SelectedFormMetadata.Name]?.Find(s => s.SectioningIdentifier == sectionIdentifier)?.Name;

            foreach (var question in questionsToMove)
            {
                MoveQuestionToSection(question, sectionIdentifier, assignSectionName);
            }

            ClearSelection();

            _dropContainerChangingKey++;

            RecalculateCurrentFormQuestions();
        }

        private void ReturnAllQuestionsFromSectionToPool(string sectionIdentifier)
        {
            var questionsToReturn = ManualPaperBoundQuestions
                .Where(q => q.SectioningIdentifier == sectionIdentifier)
                .ToList();

            foreach (var question in questionsToReturn)
            {
                question.SectionName = MiscConstants.ManualQuestionsSelectionPool;
                question.SectioningIdentifier = MiscConstants.ManualQuestionsSelectionPool;
            }

            if (_formQuestionDistribution.TryGetValue(SelectedFormMetadata.Name, out var distribution))
            {
                distribution.RemoveAll(d => d.SectioningIdentifier == sectionIdentifier);
            }

            _dropContainerChangingKey++;

            RecalculateCurrentFormQuestions();
        }

        private bool CanAddQuestionsToCurrentForm(List<ManuallySelectedQuestionsResponseDto> questionsToMove, out string errorMessage)
        {
            errorMessage = string.Empty;

            var (isValid, _errorMessage) = IsQuestionAssignedToFormBefore(questionsToMove);

            if (!isValid)
            {
                errorMessage = _errorMessage;
                return false;
            }

            var totalQuestions = ManualPaperBoundQuestions.Sum(x => x.SubQuestionsCount);
            var numberOfForms = PaperMetadataResultedParamsDto.OutputFormsCount;
            var questionsPerForm = PaperMetadataResultedParamsDto.QuestionsCount > 0
                ? PaperMetadataResultedParamsDto.QuestionsCount
                : ((numberOfForms > 0) ? totalQuestions / numberOfForms : totalQuestions);
            var questionsAlreadyInCurrentFormSections = QuestionsForCurrentForm.Sum(x => x.SubQuestionsCount);

            if (questionsAlreadyInCurrentFormSections + questionsToMove.Sum(x => x.SubQuestionsCount) > questionsPerForm)
            {
                errorMessage = string.Format(Resource.ActionExceedsFormQuestionLimit, questionsPerForm);
                return false;
            }

            return true;
        }

        private void ToggleQuestionBodyExpand(long questionId)
        {
            if (!_expandedQuestionIds.Remove(questionId))
                _expandedQuestionIds.Add(questionId);

            _dropContainer?.Refresh();
        }

        private (bool IsValid, string ErrorMessage) IsQuestionAssignedToFormBefore(List<ManuallySelectedQuestionsResponseDto> questionsToMove)
        {
            var seen = new HashSet<long>();

            var duplicate = questionsToMove.FirstOrDefault(q => !seen.Add(q.Id));

            if (duplicate != null)
                return (false, Resource.DuplicateQuestionInSameForm);

            var duplicateForm = _formQuestionDistribution
                .FirstOrDefault(d => d.Value.Any(v => seen.Contains(v.PaperItemBankQuestionId)));

            if (duplicateForm.Key != null)
                return (false, string.Format(Resource.QuestionAlreadyAssignedToFormBefore, duplicateForm.Key));

            return (true, string.Empty);
        }

        private List<string> GetFilteredItemBanksByQuestionBody()
        {
            var filteredBanks = ManualPaperBoundQuestions
                .Where(q => q.SectioningIdentifier == MiscConstants.ManualQuestionsSelectionPool)
                .Where(QuestionMatchesSearch)
                .Select(q => q.ItemBankName)
                .Distinct()
                .Order()
                .ToList();

            if (!string.IsNullOrWhiteSpace(_manualQuestionsSearchQuery))
            {
                _expandedItemBankNames.Clear();

                foreach (var bank in filteredBanks)
                    _expandedItemBankNames.Add(bank);
            }

            return filteredBanks;
        }

        private IEnumerable<ManuallySelectedQuestionsResponseDto> GetFilteredQuestionsForItemBank(string itemBankName)
        {
            return ManualPaperBoundQuestions
                .Where(q => q.SectioningIdentifier == MiscConstants.ManualQuestionsSelectionPool && q.ItemBankName == itemBankName)
                .Where(QuestionMatchesSearch);
        }

        private bool QuestionMatchesSearch(ManuallySelectedQuestionsResponseDto question)
        {
            if (string.IsNullOrWhiteSpace(_manualQuestionsSearchQuery))
                return true;

            var cleanBody = HtmlTagsCleaner.Clean(question.Body ?? string.Empty);

            return cleanBody.Contains(_manualQuestionsSearchQuery, StringComparison.OrdinalIgnoreCase);
        }

        private bool FilterSectionQuestions(ManuallySelectedQuestionsResponseDto question, string sectionIdentifier)
        {
            if (question.SectioningIdentifier != sectionIdentifier)
                return false;

            if (!_sectionSearchQueries.TryGetValue(sectionIdentifier, out var searchQuery) || string.IsNullOrWhiteSpace(searchQuery))
                return true;

            return question.Code?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true;
        }

        private void MoveQuestionToSection(ManuallySelectedQuestionsResponseDto question, string targetSectionIdentifier, string targetSectionName)
        {
            question.SectionName = targetSectionName;
            question.SectioningIdentifier = targetSectionIdentifier;

            SaveDistributionOnTheFly(question);
        }
    }
}