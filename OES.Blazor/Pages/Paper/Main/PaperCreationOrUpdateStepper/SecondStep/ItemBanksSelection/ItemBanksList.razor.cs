using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Paper.PaperItemBanks;
using OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.MainComponent;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.ItemBankPoint;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.ItemBankPoint.Requests;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.Main.PaperCreationOrUpdateStepper.SecondStep.ItemBanksSelection
{
    public partial class ItemBanksList : ComponentBase
    {
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; }
        [Inject] private IBlazItemBankPointService BlazItemBankPointService { get; set; }
        [Inject] private IBlazSessionStorageService BlazSessionStorageService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] public IDialogService DialogService { get; set; }

        [Parameter] public PaperMetadataResultedParamsDto PaperMetadataResultedParamsDto { get; set; } = new();

        public List<List<TreeItemResponseDto>> SelectedItemBankNodesLists { get; set; } = [];
        private bool IsCurrentFormsModeSingleManualForm => _paperStepperFormsMode is PaperStepperFormsMode.AddNormalSingleManualForm or PaperStepperFormsMode.AddExcelSheetSingleManualForm;
        private bool IsCurrentFormsModeSingleAutoForm => _paperStepperFormsMode == PaperStepperFormsMode.AddSingleAutoForm;
        private bool IsCurrentFormsModeContinuePendingForm => _paperStepperFormsMode == PaperStepperFormsMode.ContinuePendingForm;
        private bool IsCurrentFormsModeSingleForm => IsCurrentFormsModeSingleManualForm || IsCurrentFormsModeSingleAutoForm || IsCurrentFormsModeContinuePendingForm;
        private bool IsCurrentPaperStepperModeUpdateMode => _thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode;
        private bool ItemBankSelectionDisabledForAutoMode =>
            PaperMetadataResultedParamsDto.SelectedQuestionSelectionType == QuestionSelectionType.Auto &&
            PaperMetadataResultedParamsDto.OutputFormsCount >= 2;
        private ItemBankFilterDto ItemBankFilterDto => new(
            PaperMetadataResultedParamsDto.PaperId,
            PaperMetadataResultedParamsDto.LanguageId,
            PaperMetadataResultedParamsDto.DifficultyProfileId
        );

        private StepperOperationalMode _thisComponentCurrentOperationalMode = StepperOperationalMode.InsertionMode;
        private PaperStepperFormsMode _paperStepperFormsMode;
        private readonly Dictionary<long, TreeItemResponseDto> _selectedNodes = [];
        private readonly HashSet<long> _selectedIds = [];
        private List<long> _oldSelectedItembanks = [];
        private List<long> _initialSelectedItemBankIds = [];


        // DATA PROCESSING METHODS

        protected override async Task OnInitializedAsync()
        {
            var paperItemBanksLists = await BlazItemBankPointService.GetPaperSelectedItemBanksAsync(PaperMetadataResultedParamsDto.PaperId);

            var paperStepperFormsMode = await BlazSessionStorageService.GetValue<int>(nameof(PaperStepperFormsMode));
            _paperStepperFormsMode = (PaperStepperFormsMode)paperStepperFormsMode;

            if (paperItemBanksLists.Count > 0)
            {
                _thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                SelectedItemBankNodesLists = paperItemBanksLists;

                RebuildSelectedCache();

                _initialSelectedItemBankIds = [.. SelectedItemBankNodesLists.SelectMany(list => list).Select(x => x.Id)];

                if (IsCurrentFormsModeSingleForm)
                {
                    _oldSelectedItembanks = [.. _initialSelectedItemBankIds];
                }
            }
        }

        private async Task OpenPaperItemBankTreeViewDialogAsync()
        {
            if ((IsCurrentPaperStepperModeUpdateMode && !IsCurrentFormsModeSingleForm) || PaperMetadataResultedParamsDto.UsesExcelQuestionsImport)
            {
                Snackbar.Add(Resource.PaperStepperLockedForUpdateInItemBanks, Severity.Error);
                return;
            }

            var selectedItemBankNodeIds = _selectedIds.ToList();

            var parameters = new DialogParameters<PaperItemBankTreeViewDialog>
            {
                { x => x.SelectedItemBankNodeIds, selectedItemBankNodeIds },
                { x => x.OldSelectedItemBankNodesDisabled, IsCurrentFormsModeSingleForm }
            };

            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
            };

            var dialog = await DialogService.ShowAsync<PaperItemBankTreeViewDialog>(Resource.SelectItemBanks, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is List<TreeItemResponseDto> selectedItems)
            {
                var selectedIds = selectedItems.Select(x => x.Id).ToHashSet();

                int intersectedListIndex = SelectedItemBankNodesLists.FindIndex(list => list.Any(x => selectedIds.Contains(x.Id)));

                if (intersectedListIndex != -1)
                {
                    SelectedItemBankNodesLists[intersectedListIndex] = selectedItems;
                }
                else
                {
                    SelectedItemBankNodesLists.Add(selectedItems);
                }

                RebuildSelectedCache();
                StateHasChanged();
            }
        }

        private void RemoveSelectedItem(TreeItemResponseDto item)
        {
            foreach (var innerList in SelectedItemBankNodesLists)
            {
                innerList.RemoveAll(x => x.Id == item.Id);
            }

            _selectedNodes.Remove(item.Id);
            _selectedIds.Remove(item.Id);

            SelectedItemBankNodesLists.RemoveAll(x => x.Count == 0);
        }


        // FORM SUBMIT METHODS

        public async Task<bool> OnSecondStepItemBanksListSubmitAsync()
        {
            if (ValidateSelectedItemBanks())
            {
                var currentSelectedIds = _selectedIds.ToList();

                if (currentSelectedIds.Count == _initialSelectedItemBankIds.Count && !currentSelectedIds.Except(_initialSelectedItemBankIds).Any())
                {
                    return true;
                }

                var currentItemBankPointRequestDto = new AddOrUpdateItemBankPointRequestDto(PaperMetadataResultedParamsDto.PaperId,
                                                                                            PaperMetadataResultedParamsDto.SelectedQuestionSelectionType,
                                                                                            currentSelectedIds,
                                                                                            PaperMetadataResultedParamsDto.LanguageId,
                                                                                            PaperMetadataResultedParamsDto.DifficultyProfileId,
                                                                                            PaperMetadataResultedParamsDto.QuestionsCount,
                                                                                            PaperMetadataResultedParamsDto.OutputFormsCount,
                                                                                            PaperMetadataResultedParamsDto.AllowInstantResult,
                                                                                            PaperMetadataResultedParamsDto.QuestionDistributionTypeInForm);

                if (_thisComponentCurrentOperationalMode == StepperOperationalMode.InsertionMode)
                {
                    return await AddItemBankPointAsync(currentItemBankPointRequestDto);
                }
                else if (_thisComponentCurrentOperationalMode == StepperOperationalMode.UpdateMode)
                {
                    return await UpdateItemBankPointAsync(currentItemBankPointRequestDto);
                }
            }
            else
            {
                Snackbar.Add(Resource.PleaseSelectAtLeastOneItemBank, Severity.Error);
            }

            return false;
        }

        private async Task<bool> AddItemBankPointAsync(AddOrUpdateItemBankPointRequestDto currentItemBankPointRequestDto)
        {
            var response = await BlazItemBankPointService.AddItemBankPointAsync(currentItemBankPointRequestDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                //_thisComponentCurrentOperationalMode = StepperOperationalMode.UpdateMode;

                _initialSelectedItemBankIds = [.. currentItemBankPointRequestDto.ItemBankIds];

                Snackbar.Add(response.Message, Severity.Success);

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            return false;
        }

        private async Task<bool> UpdateItemBankPointAsync(AddOrUpdateItemBankPointRequestDto currentItemBankPointRequestDto)
        {
            var response = await BlazItemBankPointService.UpdateItemBankPointAsync(currentItemBankPointRequestDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                _initialSelectedItemBankIds = [.. currentItemBankPointRequestDto.ItemBankIds];

                Snackbar.Add(response.Message, Severity.Success);

                return true;
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            return false;
        }

        private void RebuildSelectedCache()
        {
            _selectedNodes.Clear();
            _selectedIds.Clear();

            foreach (var list in SelectedItemBankNodesLists)
            {
                foreach (var node in list)
                {
                    _selectedNodes[node.Id] = node;
                    _selectedIds.Add(node.Id);
                }
            }
        }


        // VALIDATION METHODS

        private bool ValidateSelectedItemBanks() => _selectedIds.Count > 0;
    }
}