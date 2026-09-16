using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Question.QuestionDetailsDialog;
using OES.Blazor.Dialogs.Question.QuestionLanguageSelectionDialog;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.SubQuestion;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.ComprehensionSubQuestionDtos;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos;
using OES.Helper.Dtos.Question.MatchingPairsQuestion;
using OES.Helper.Dtos.Question.MatchingPairsWithDragDropQuestion;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.Question.QuestionLanguage
{
    public partial class QuestionLanguageList : ComponentBase
    {
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private IBlazSubQuestionService BlazSubQuestionService { get; set; }
        [Inject] private IBlazAuthService AuthService { get; set; }
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; }

        [Parameter] public string QuestionType { get; set; } // In case of comprehension: This is the root comprehension question type.
        [Parameter] public long CreatedQuestionMetaDataId { get; set; } // In case of comprehension: This is the root comprehension question metadata id.
        [Parameter] public string RootQuestionCode { get; set; }
        [Parameter] public EventCallback<List<SegmentQuestionDto>> SegmentQuestionChanged { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        private List<SegmentQuestionDto> SegmentQuestions { get; set; } = [];
        private CustomTableData<QuestionLanguageDTO> QuestionsTableData { get; set; } = new([], 0);
        private CustomTableData<PaginatedListSubQuestionDto> SubQuestionsTableData { get; set; } = new([], 0);

        private bool _questionLanguageVariantDeletionButtonShown = true;
        private bool _subQuestionLanguageVariantsListCreationButtonShown = true;
        private Guid _questionLanguageVariantsListKey = Guid.NewGuid();
        private Guid _subQuestionLanguageVariantsListKey = Guid.NewGuid();
        private long? _mainQuestionLanguageId;
        private long _previousCreatedQuestionMetaDataId = 0;
        private long _itemBankId = 0;

        protected override async Task OnParametersSetAsync()
        {
            if (CreatedQuestionMetaDataId != _previousCreatedQuestionMetaDataId)
            {
                if (QuestionType != nameof(Helper.Enums.QuestionType.Segment) && CreatedQuestionMetaDataId == 0)
                {
                    _mainQuestionLanguageId = null;
                }

                if (CreatedQuestionMetaDataId > 0)
                {
                    var metaResponse = await BlazQuestionService.GetQuestionMetadataByIdAsync(CreatedQuestionMetaDataId);

                    if (metaResponse?.Data is QuestionMetadataRetrievalDto md)
                    {
                        _itemBankId = (long)md.RootItemBankId;
                    }
                    else if (metaResponse?.Data != null)
                    {
                        var dto = JsonSerializer.Deserialize<QuestionMetadataRetrievalDto>(
                            JsonSerializer.Serialize(metaResponse.Data),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                        _itemBankId = dto?.RootItemBankId ?? 0;
                    }
                }

                _previousCreatedQuestionMetaDataId = CreatedQuestionMetaDataId;
                _questionLanguageVariantsListKey = Guid.NewGuid();
                _subQuestionLanguageVariantsListKey = Guid.NewGuid();
            }

            StateHasChanged();
        }

        private async Task OnSegmentsChanged(List<SegmentQuestionDto> updatedSegments)
        {
            SegmentQuestions = updatedSegments;

            if (SegmentQuestions?.Count > 0)
            {
                _mainQuestionLanguageId = SegmentQuestions[0].SegmentQuestionDetailsDto?.LanguageId;
            }
            else
            {
                _mainQuestionLanguageId = null;
            }

            await SegmentQuestionChanged.InvokeAsync(SegmentQuestions);
        }

        private Task OnMainQuestionLanguageIdChanged(long? newLanguageId)
        {
            _mainQuestionLanguageId = newLanguageId;
            StateHasChanged();
            return Task.CompletedTask;
        }

        public async Task<CustomTableData<QuestionLanguageDTO>> GetAllQuestionLanguageVariantsAsync(PaginationSearchModel paginationSearchModel)
        {
            var data = await BlazQuestionService.GetAllQuestionLanguage(paginationSearchModel, CreatedQuestionMetaDataId);

            _subQuestionLanguageVariantsListCreationButtonShown = data.TotalItems > 0;

            QuestionsTableData = data;

            HandleMainQuestionLanguageForNonSegment(data);

            RefreshQuestionChildStatus();

            _subQuestionLanguageVariantsListKey = Guid.NewGuid();

            return QuestionsTableData;
        }

        public void RefreshQuestionChildStatus()
        {
            if (QuestionsTableData?.Items != null && SubQuestionsTableData?.Items != null)
            {
                var subQuestionLanguageIds = SubQuestionsTableData
                    .Items
                    .Where(q => q?.LanguagesIds != null)
                    .SelectMany(q => q.LanguagesIds)
                    .ToHashSet();

                foreach (var item in QuestionsTableData.Items)
                {
                    item.HasChild = subQuestionLanguageIds.Contains(item.LanguageId);
                }

                StateHasChanged();
            }
        }

        public async Task<CustomTableData<PaginatedListSubQuestionDto>> GetAllSubQuestionLanguageVariantsGroupsAsync(PaginationSearchModel paginationSearchModel)
        {
            var data = await BlazSubQuestionService.PaginationSubQuestions(CreatedQuestionMetaDataId, paginationSearchModel);

            SubQuestionsTableData = data;

            RefreshQuestionChildStatus();

            return SubQuestionsTableData;
        }

        private async Task CreateQuestionAsync()
        {
            if (!await IsAuthorizedAsync(OesTemplateRoleConstants.QuestionCreator, OesTemplateRoleConstants.ItemBankQuestionCreator))
            {
                Snackbar.Add(Resource.NotAuthorizedToCreateQuestion, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<QuestionDetailsDialog>()
            {
                { x => x.QuestionTypeParameter, QuestionType },
                { x => x.CreatedQuestionMetaDataId, CreatedQuestionMetaDataId },
                { x => x.IsReplaceMode, IsReplaceMode }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraExtraLarge,
                FullWidth = true,
                BackdropClick = false,
            };

            var dialog = await DialogService.ShowAsync<QuestionDetailsDialog>(
                string.Empty,
                parameters,
                options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data != null)
            {
                HandleQuestionCreationResult();
            }
        }

        private void HandleQuestionCreationResult()
        {
            _questionLanguageVariantsListKey = Guid.NewGuid();
            _subQuestionLanguageVariantsListKey = Guid.NewGuid();
        }

        private async Task UpdateQuestionAsync(object questionDetailsId)
        {
            if (!await IsAuthorizedAsync(OesTemplateRoleConstants.QuestionEditor, OesTemplateRoleConstants.ItemBankQuestionEditor))
            {
                Snackbar.Add(Resource.NotAuthorizedToUpdateQuestion, Severity.Error);
                return;
            }

            long _questionDetailsId = ToLong(questionDetailsId);

            var response = await BlazQuestionService.GetQuestionDetailsByIdAsync(_questionDetailsId);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Snackbar.Add(
                    Resource.CannotFindQuestionDetailsWithTheGivenId,
                    Severity.Error);

                return;
            }

            var retrievedQuestion = (QuestionDetailsDto)response.Data;

            // Matching Pairs Question Part
            GetMatchingPairsResponseDto matchingPairsData = null;

            if (QuestionType == nameof(Helper.Enums.QuestionType.MatchingPairs))
            {
                var matchingResponse =
                    await BlazQuestionService.GetMatchingPairsQuestionAsync(
                        CreatedQuestionMetaDataId,
                        retrievedQuestion.LanguageId);

                if (matchingResponse.StatusCode == HttpStatusCode.OK)
                {
                    matchingPairsData = (GetMatchingPairsResponseDto)matchingResponse.Data;
                }
                else
                {
                    Snackbar.Add(
                        matchingResponse.Message,
                        Severity.Error);

                    return;
                }
            }
            // Matching Pairs Question Part

            // Matching Pairs With Drag Drop Question Part
            GetMatchingPairsWithDragDropResponseDto matchingPairsWithDragDropData = null;

            if (QuestionType == nameof(Helper.Enums.QuestionType.MatchingPairsWithDragDrop))
            {
                var matchingResponse =
                    await BlazQuestionService.GetMatchingPairsWithDragDropQuestionAsync(
                        CreatedQuestionMetaDataId,
                        retrievedQuestion.LanguageId);

                if (matchingResponse.StatusCode == HttpStatusCode.OK)
                {
                    matchingPairsWithDragDropData = (GetMatchingPairsWithDragDropResponseDto)matchingResponse.Data;
                }
                else
                {
                    Snackbar.Add(
                        matchingResponse.Message,
                        Severity.Error);

                    return;
                }
            }
            // Matching Pairs With Drag Drop Question Part

            var parameters = new DialogParameters<QuestionDetailsDialog>
            {
                { p => p.ReceivedQuestionDetailsDto, retrievedQuestion },
                { x => x.QuestionTypeParameter, QuestionType },
                { x => x.CreatedQuestionMetaDataId, CreatedQuestionMetaDataId },
                { x => x.AllMatchingPairsData, matchingPairsData },
                {
                    x => x.AllMatchingPairsWithDragDropData,
                    matchingPairsWithDragDropData
                },
                { x => x.IsReplaceMode, IsReplaceMode }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraExtraLarge,
                FullWidth = true,
                BackdropClick = false,
            };

            var dialog = await DialogService.ShowAsync<QuestionDetailsDialog>(
                string.Empty,
                parameters,
                options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _questionLanguageVariantsListKey = Guid.NewGuid();
            }
        }

        private async Task DeleteQuestionAsync(object questionDetailsId)
        {
            if (!await IsAuthorizedAsync(OesTemplateRoleConstants.QuestionDeleter, OesTemplateRoleConstants.ItemBankQuestionDeleter))
            {
                Snackbar.Add(Resource.NotAuthorizedToDeleteQuestion, Severity.Error);
                return;
            }

            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.ConfirmDelete },
                {
                    p => p.Content,
                    Resource.AreYouSureYouWantToDeleteThisQuestion
                },
                { p => p.SubmitText, Resource.Delete },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonColor, Color.Error },
                {
                    p => p.SubmitButtonStartIcon,
                    Icons.Material.Filled.Delete
                },
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(
                string.Empty,
                parameters,
                options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                long _questionDetailsId = ToLong(questionDetailsId);

                var response =
                    await BlazQuestionService.DeleteQuestionDetailsByIdAsync(_questionDetailsId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(
                        Resource.QuestionHasBeenDeletedSuccessfully,
                        Severity.Success);
                }
                else
                {
                    Snackbar.Add(
                        Resource.SomethingWentWrongwhileDeletingQuestionDtails,
                        Severity.Error);
                }

                _questionLanguageVariantsListKey = Guid.NewGuid();
                _subQuestionLanguageVariantsListKey = Guid.NewGuid();
            }
        }

        private async Task CreateSubQuestionAsync()
        {
            var subQuestionCount = SubQuestionsTableData?.TotalItems ?? 0;

            var defaultCode = !string.IsNullOrWhiteSpace(RootQuestionCode)
                ? $"{RootQuestionCode} 1.{subQuestionCount + 1}"
                : string.Empty;

            var parameters = new DialogParameters<ComprehensionSubQuestionDetailsDialog>()
            {
                { x => x.ParentMetadataId, CreatedQuestionMetaDataId },
                { x => x.DefaultCode, defaultCode },
                { x => x.IsReplaceMode, IsReplaceMode }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraExtraLarge,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await DialogService.ShowAsync<ComprehensionSubQuestionDetailsDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _subQuestionLanguageVariantsListKey = Guid.NewGuid();
            }
        }

        private async Task AddSubQuestionLanguageVariantAsync(object subQuestionMetadataId)
        {
            long _subQuestionMetadataId = ToLong(subQuestionMetadataId);

            var response = await BlazQuestionService.GetQuestionMetadataByIdAsync(_subQuestionMetadataId);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Snackbar.Add(Resource.CannotFindQuestionMetadataWithTheGivenId, Severity.Error);
                return;
            }

            var retrievedSubQuestionMetadataDto = (QuestionMetadataRetrievalDto)response.Data;

            var parameters = new DialogParameters<ComprehensionSubQuestionDetailsDialog>()
            {
                { x => x.ParentMetadataId, CreatedQuestionMetaDataId },
                { x => x.SubQuestionMetadataRetrievalDto, retrievedSubQuestionMetadataDto },
                { x => x.IsReplaceMode, IsReplaceMode }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraExtraLarge,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await DialogService.ShowAsync<ComprehensionSubQuestionDetailsDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _subQuestionLanguageVariantsListKey = Guid.NewGuid();
            }
        }

        private async Task UpdateSubQuestionAsync(object subQuestionMetadataId)
        {
            long _subQuestionMetadataId = ToLong(subQuestionMetadataId);

            var parameters = new DialogParameters<QuestionLanguageSelectionDialog>
            {
                { p => p.RootComprehensionQuestionMetadataId, CreatedQuestionMetaDataId },
                { p => p.ComprehensionSubQuestionMetadataId, _subQuestionMetadataId },
                { p => p.ConfirmationButtonText, Resource.UpdateQuestion}
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<QuestionLanguageSelectionDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var receivedQuestionDetailsDto = (QuestionDataDto)result.Data;

                var comprehensionSubQuestionDto = new ComprehensionSubQuestionDto
                {
                    SubQuestionDetailsDto = new SubQuestionDetailsDto
                    {
                        Id = receivedQuestionDetailsDto.Id,
                        Body = receivedQuestionDetailsDto.Body,
                        Choices = receivedQuestionDetailsDto.Choices,
                        Instructions = receivedQuestionDetailsDto.Instructions,
                        LanguageId = receivedQuestionDetailsDto.LanguageId,
                        ModelAnswer = receivedQuestionDetailsDto.ModelAnswer,
                        QuestionMetadataId = receivedQuestionDetailsDto.QuestionMetadataId,
                        HasShuffled = receivedQuestionDetailsDto.HasShuffled,
                    }
                };

                await OpenActualUpdateDialogAsync(comprehensionSubQuestionDto, _subQuestionMetadataId);
            }
        }

        private async Task DeleteSubQuestionAsync(object subQuestionMetadataId)
        {
            long _subQuestionMetadataId = ToLong(subQuestionMetadataId);

            var parameters = new DialogParameters<QuestionLanguageSelectionDialog>
            {
                { p => p.RootComprehensionQuestionMetadataId, CreatedQuestionMetaDataId },
                { p => p.ComprehensionSubQuestionMetadataId, _subQuestionMetadataId },
                { p => p.ConfirmationButtonText, Resource.DeleteQuestion }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<QuestionLanguageSelectionDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var receivedQuestionDetailsDto = (QuestionDataDto)result.Data;

                await OpenActualDeletionDialogAsync(_subQuestionMetadataId, receivedQuestionDetailsDto.Id);
            }
        }

        private async Task OpenActualUpdateDialogAsync(ComprehensionSubQuestionDto comprehensionSubQuestionDto, long subQuestionMetadataId)
        {
            var response = await BlazQuestionService.GetQuestionMetadataByIdAsync(subQuestionMetadataId);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Snackbar.Add(Resource.CannotFindQuestionMetadataWithTheGivenId, Severity.Error);
                return;
            }

            var retrievedSubQuestionMetadataDto = (QuestionMetadataRetrievalDto)response.Data;

            var parameters = new DialogParameters<ComprehensionSubQuestionDetailsDialog>()
            {
                { x => x.ParentMetadataId, CreatedQuestionMetaDataId },
                { x => x.SubQuestionMetadataRetrievalDto, retrievedSubQuestionMetadataDto },
                { x => x.ReceivedComprehensionSubQuestionDto, comprehensionSubQuestionDto },
                { x => x.IsReplaceMode, IsReplaceMode }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.ExtraExtraLarge,
                FullWidth = true,
                BackdropClick = false
            };

            var dialog = await DialogService.ShowAsync<ComprehensionSubQuestionDetailsDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _subQuestionLanguageVariantsListKey = Guid.NewGuid();
            }
        }

        private async Task OpenActualDeletionDialogAsync(long subQuestionMetadataId, long subQuestionDetailsId)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisQuestionLanguage },
                { p => p.SubmitText, Resource.Delete },
                { p => p.CancelText, Resource.Cancel },
                { p => p.SubmitButtonColor, Color.Error },
                { p => p.SubmitButtonStartIcon, Icons.Material.Filled.Delete },
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazQuestionService.DeleteComprehensionSubQuestionLanguageVariantAsync(subQuestionMetadataId, subQuestionDetailsId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                _subQuestionLanguageVariantsListKey = Guid.NewGuid();
            }
        }

        private bool ValidateCountEqualityOfComprehensionQuestionsVariantsWithSubQuestionsVariants()
        {
            if (!SubQuestionsTableData.Items.Any())
            {
                Snackbar.Add(Resource.InCaseOfComprehensionCannotProceedWithNoEnteredsubquestionDetails, Severity.Error);
                return false;
            }

            var questionLanguagesIdsList = QuestionsTableData.Items.Select(i => i.LanguageId).ToList();

            foreach (var subQuestion in SubQuestionsTableData.Items)
            {
                var exceptionResultList = questionLanguagesIdsList.Except(subQuestion.LanguagesIds).ToList();

                if (exceptionResultList.Count > 0)
                {
                    Snackbar.Add(Resource.ComprehensionSubquestionsVariantsMatchRootQuestionVariants, Severity.Error);
                    return false;
                }
            }

            return true;
        }

        private bool ValidateIfAllQuestionsChoicesCountsAreEqual()
        {
            var choicesCountsList = QuestionsTableData.Items.Select(x => x.NumberOfChoices).ToList();

            var allCountsAreEqual = choicesCountsList.TrueForAll(x => x == choicesCountsList[0]);

            if (!allCountsAreEqual)
            {
                Snackbar.Add(Resource.UnmatchedChoicesCountsInQuestionLanguageVariants, Severity.Error);
                return false;
            }

            return true;
        }

        private bool ValidateIfAllComprehensionSubQuestionLanguageVariantsChoicesCountsAreEqual()
        {
            var choicesCountsLists = SubQuestionsTableData.Items.Select(x => x.ChoicesCountsList).ToList();

            foreach (var choicesCountsList in choicesCountsLists)
            {
                var allCountsAreEqual = choicesCountsList.TrueForAll(x => x == choicesCountsList[0]);

                if (!allCountsAreEqual)
                {
                    Snackbar.Add(Resource.UnmatchedChoicesCountsInsubquestioLanguageVariants, Severity.Error);
                    return false;
                }
            }

            return true;
        }

        public bool ValidateStepTwo()
        {
            if (QuestionType == nameof(Helper.Enums.QuestionType.Segment) && (SegmentQuestions.Count == 0))
            {
                Snackbar.Add(Resource.AtLeastOneSegmentIsRequired, Severity.Error);
                return false;
            }

            if (!QuestionsTableData.Items.Any() && QuestionType != nameof(Helper.Enums.QuestionType.Segment))
            {
                Snackbar.Add(Resource.CannotProceedwithNoEnteredQuestionDetails, Severity.Error);
                return false;
            }

            var countsMatch = ValidateIfAllQuestionsChoicesCountsAreEqual();

            if (!countsMatch)
            {
                return false;
            }

            var isComprehensionKitValid = true;

            if (Enum.TryParse<Helper.Enums.QuestionType>(QuestionType?.ToString(), out var questionType) && questionType == Helper.Enums.QuestionType.Comprehension)
            {
                isComprehensionKitValid = ValidateCountEqualityOfComprehensionQuestionsVariantsWithSubQuestionsVariants() &&
                                          ValidateIfAllComprehensionSubQuestionLanguageVariantsChoicesCountsAreEqual();
            }

            return isComprehensionKitValid;
        }

        public void ResetForAnotherNewQuestion()
        {
            CreatedQuestionMetaDataId = 0;
            SegmentQuestions.Clear();
            _mainQuestionLanguageId = null;
            QuestionsTableData = new([], 0);
            SubQuestionsTableData = new([], 0);

            StateHasChanged();
        }

        public List<SegmentQuestionDto> GetSegmentQuestions()
        {
            return SegmentQuestions;
        }

        private static long ToLong(object o) => long.TryParse(o?.ToString(), out var x) ? x : 0;

        #region Helper Methods For Segment
        public void ReassignAllSegmentsMetadata()
        {
            for (int i = 0; i < SegmentQuestions.Count; i++)
            {
                var segment = SegmentQuestions[i];

                if (i == 0)
                {
                    segment.SegmentMetaDataDto.ParentId = 0;
                    segment.SegmentQuestionDetailsDto.QuestionMetadataId = CreatedQuestionMetaDataId;
                }
                else
                {
                    segment.SegmentMetaDataDto.ParentId = CreatedQuestionMetaDataId;
                }

                if (string.IsNullOrWhiteSpace(segment.SegmentMetaDataDto.Code))
                {
                    segment.SegmentMetaDataDto.Code = i == 0
                        ? RootQuestionCode
                        : $"{RootQuestionCode} 1.{i + 1}";
                }
            }
        }

        private void HandleMainQuestionLanguageForNonSegment(CustomTableData<QuestionLanguageDTO> data)
        {
            if (QuestionType == nameof(Helper.Enums.QuestionType.Segment))
                return;

            if (_mainQuestionLanguageId == null && data.Items?.Any() == true)
            {
                _mainQuestionLanguageId = data.Items.First().LanguageId;
            }

            _previousCreatedQuestionMetaDataId = CreatedQuestionMetaDataId;
        }

        private async Task<bool> IsAuthorizedAsync(string questionRole, string itemBankRole)
        {
            var viaQuestion = await AuthService.IsCurrentUserAuthorizedAsync(
                CreatedQuestionMetaDataId,
                questionRole,
                BlazQuestionService.GetQuestionGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );

            if (viaQuestion) return true;

            return await AuthService.IsCurrentUserAuthorizedAsync<ItemBankGroupsDto, Guid>(
                _itemBankId,
                itemBankRole,
                BlazItemBankService.GetItemBankGroupsAsync,
                dto => dto.GroupsIds,
                g => g.GroupId
            );
        }
        #endregion
    }
}