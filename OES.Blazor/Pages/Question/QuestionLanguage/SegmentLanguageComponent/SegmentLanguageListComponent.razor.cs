using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Question.QuestionDetailsDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.Question.SegmentQuestionDtos.HelperDtos;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Question.QuestionLanguage.SegmentLanguageComponent
{
    public partial class SegmentLanguageListComponent : ComponentBase
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        [Parameter] public List<SegmentQuestionDto> Segments { get; set; } = [];
        [Parameter] public EventCallback<List<SegmentQuestionDto>> SegmentsChanged { get; set; }
        [Parameter] public List<LanguageDto> Languages { get; set; } = [];
        [Parameter] public LanguageDto SelectedLanguage { get; set; }
        [Parameter] public string RootQuestionCode { get; set; }
        [Parameter] public EventCallback<LanguageDto> SelectedLanguageChanged { get; set; }
        [Parameter] public long CreatedQuestionMetaDataId { get; set; }
        [Parameter] public string QuestionType { get; set; }
        [Parameter] public long? MainQuestionLanguageId { get; set; }
        [Parameter] public EventCallback<long?> MainQuestionLanguageIdChanged { get; set; }

        private long _previousCreatedQuestionMetaDataId = 0;
        private long? _previousMainQuestionLanguageId = null;

        protected override async Task OnInitializedAsync()
        {
            await InitializeSegmentQuestionsAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (CreatedQuestionMetaDataId != _previousCreatedQuestionMetaDataId)
            {
                Segments.Clear();

                if (CreatedQuestionMetaDataId > 0)
                {
                    await LoadSegmentsIfNeededAsync();

                    if (Segments?.Count > 0)
                    {
                        var newLanguageId = Segments[0].SegmentQuestionDetailsDto?.LanguageId;
                        if (_previousMainQuestionLanguageId != newLanguageId)
                        {
                            _previousMainQuestionLanguageId = newLanguageId;
                            await MainQuestionLanguageIdChanged.InvokeAsync(newLanguageId);
                        }
                    }
                }
                else
                {
                    if (_previousMainQuestionLanguageId != null)
                    {
                        _previousMainQuestionLanguageId = null;
                        await MainQuestionLanguageIdChanged.InvokeAsync(null);
                    }
                }

                _previousCreatedQuestionMetaDataId = CreatedQuestionMetaDataId;
                StateHasChanged();
            }
        }

        private async Task LoadSegmentsIfNeededAsync()
        {
            if (CreatedQuestionMetaDataId > 0)
            {
                long langIdToFetch = MainQuestionLanguageId ?? 0;

                var segments = await BlazQuestionService.GetSegmentQuestionByMetaDataIdAsync(
                    CreatedQuestionMetaDataId,
                    langIdToFetch
                );

                if (segments?.Count > 0)
                {
                    Segments = segments;
                    UpdateOrderNumbers();
                    await SegmentsChanged.InvokeAsync(Segments);
                }
            }
        }

        private async Task OnSegmentsChangedInternal(List<SegmentQuestionDto> updatedSegments)
        {
            Segments = updatedSegments;

            long? newLanguageId = null;

            if (Segments?.Count > 0)
            {
                newLanguageId = Segments[0].SegmentQuestionDetailsDto?.LanguageId;
            }

            if (_previousMainQuestionLanguageId != newLanguageId)
            {
                _previousMainQuestionLanguageId = newLanguageId;
                await MainQuestionLanguageIdChanged.InvokeAsync(newLanguageId);
            }

            await SegmentsChanged.InvokeAsync(Segments);
        }

        public void UpdateMainQuestionLanguageId()
        {
            long? newLanguageId = null;

            if (Segments?.Count > 0)
            {
                newLanguageId = Segments[0].SegmentQuestionDetailsDto?.LanguageId;
            }

            if (_previousMainQuestionLanguageId != newLanguageId)
            {
                _previousMainQuestionLanguageId = newLanguageId;
                MainQuestionLanguageIdChanged.InvokeAsync(newLanguageId);
            }
        }

        private async Task InitializeSegmentQuestionsAsync()
        {
            Languages = await BlazQuestionLanguage.GetAllLanguagesAsync();

            if (Segments.Count == 0 && CreatedQuestionMetaDataId > 0)
            {
                long langIdToFetch = MainQuestionLanguageId ?? 0;

                var segments = await BlazQuestionService.GetSegmentQuestionByMetaDataIdAsync(
                    CreatedQuestionMetaDataId,
                    langIdToFetch
                );

                if (segments?.Count > 0)
                {
                    Segments = segments;
                    UpdateOrderNumbers();
                    await SegmentsChanged.InvokeAsync(Segments);
                }
            }

            if (MainQuestionLanguageId > 0)
            {
                SelectedLanguage = Languages.FirstOrDefault(l => l.Id == MainQuestionLanguageId.Value);
            }
            else if (Segments?.Count > 0)
            {
                var currentLangId = Segments[0].SegmentQuestionDetailsDto.LanguageId;
                SelectedLanguage = Languages.FirstOrDefault(l => l.Id == currentLangId);
            }
            else
            {
                SelectedLanguage = null;
            }

            if (SelectedLanguage != null)
            {
                await SelectedLanguageChanged.InvokeAsync(SelectedLanguage);
            }
        }

        private void OnLanguageChanged(LanguageDto newLanguage)
        {
            SelectedLanguage = newLanguage;
            SelectedLanguageChanged.InvokeAsync(newLanguage);
        }

        private async Task OnCreateSegmentClicked()
        {
            if (SelectedLanguage == null || SelectedLanguage.Id == 0)
            {
                Snackbar.Add(Resource.pleaseSelectALanguageFirst, Severity.Error);
                return;
            }

            var newSegment = new SegmentQuestionDto();

            newSegment.SegmentMetaDataDto.Code = Segments.Count == 0
                ? RootQuestionCode
                : $"{RootQuestionCode} 1.{Segments.Count + 1}";

            var parameters = new DialogParameters<QuestionDetailsDialog>()
            {
                { x => x.ReceivedSegmentQuestionDetailsDto, newSegment },
                { x => x.QuestionTypeParameter, QuestionType },
                { x => x.CreatedQuestionMetaDataId, CreatedQuestionMetaDataId },
                { x => x.PreSelectedLanguage, SelectedLanguage }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackdropClick = false,
            };

            var dialog = await DialogService.ShowAsync<QuestionDetailsDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data != null)
            {
                await HandleQuestionCreationResult(result.Data);
            }
        }

        private async Task HandleQuestionCreationResult(object data)
        {
            if (data is SegmentQuestionDto createdSegment)
            {
                createdSegment.SegmentQuestionConfigDto ??= new SegmentQuestionConfigDto();
                createdSegment.SegmentQuestionConfigDto.OrderNumber = Segments.Count + 1;

                if (Segments.Count == 0)
                {
                    createdSegment.SegmentQuestionDetailsDto.QuestionMetadataId = CreatedQuestionMetaDataId;
                }
                else
                {
                    createdSegment.SegmentMetaDataDto.ParentId = CreatedQuestionMetaDataId;
                }

                if (string.IsNullOrWhiteSpace(createdSegment.SegmentMetaDataDto.Code))
                {
                    createdSegment.SegmentMetaDataDto.Code = Segments.Count == 0
                        ? RootQuestionCode
                        : $"{RootQuestionCode} 1.{Segments.Count + 1}";
                }

                Segments.Add(createdSegment);
                await OnSegmentsChangedInternal(Segments);
                StateHasChanged();
            }
            else if (data is bool)
            {
                Snackbar.Add(Resource.QuestionAddedSuccessfully, Severity.Success);
            }
        }

        private async Task OnEditSegment(SegmentQuestionDto segment)
        {
            var parameters = new DialogParameters<QuestionDetailsDialog>
            {
                { p => p.ReceivedSegmentQuestionDetailsDto, segment },
                { x => x.QuestionTypeParameter, QuestionType },
                { x => x.CreatedQuestionMetaDataId, CreatedQuestionMetaDataId },
                { x => x.PreSelectedLanguage, SelectedLanguage }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                BackdropClick = false,
            };

            var dialog = await DialogService.ShowAsync<QuestionDetailsDialog>(string.Empty, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is SegmentQuestionDto updatedSegment)
            {
                var index = Segments.IndexOf(segment);

                if (index >= 0)
                {
                    updatedSegment.SegmentQuestionConfigDto ??= new SegmentQuestionConfigDto();
                    updatedSegment.SegmentMetaDataDto ??= new SegmentQuestionMetaDataDto();

                    updatedSegment.SegmentQuestionConfigDto.OrderNumber = segment.SegmentQuestionConfigDto?.OrderNumber ?? (index + 1);

                    updatedSegment.SegmentMetaDataDto.ParentId = segment.SegmentMetaDataDto?.ParentId ?? 0;

                    updatedSegment.SegmentQuestionDetailsDto.QuestionMetadataId = segment.SegmentQuestionDetailsDto?.QuestionMetadataId ?? 0;

                    Segments[index] = updatedSegment;
                    await OnSegmentsChangedInternal(Segments);
                    StateHasChanged();
                }
            }
        }

        private async Task OnDeleteSegment(SegmentQuestionDto segment)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { p => p.Title, Resource.Alert },
                { p => p.Content, Resource.AreYouSureYouWantToDeleteThisQuestion },
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
                if (segment.SegmentQuestionDetailsDto.Id == 0)
                {
                    Segments.Remove(segment);
                    UpdateOrderNumbers();
                    await OnSegmentsChangedInternal(Segments);
                    Snackbar.Add(Resource.QuestionHasBeenDeletedSuccessfully, Severity.Success);
                    StateHasChanged();
                }
                else
                {
                    var response = await BlazQuestionService.DeleteSegmentQuestionLanguageVariantAsync(
                        segment.SegmentQuestionDetailsDto.QuestionMetadataId,
                        segment.SegmentQuestionDetailsDto.Id
                    );

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        Segments.Remove(segment);
                        UpdateOrderNumbers();
                        await OnSegmentsChangedInternal(Segments);
                        Snackbar.Add(response.Message, Severity.Success);
                        StateHasChanged();
                    }
                    else
                    {
                        Snackbar.Add(response.Message, Severity.Error);
                    }
                }
            }
        }

        private async Task OnMoveUp(int index)
        {
            if (index > 0 && index < Segments.Count)
            {
                var item = Segments[index];
                Segments.RemoveAt(index);
                Segments.Insert(index - 1, item);
                UpdateOrderNumbers();
                await OnSegmentsChangedInternal(Segments);
                Snackbar.Add($"{Resource.QuestionMovedSuccessfully}", Severity.Success);
            }
        }

        private async Task OnMoveDown(int index)
        {
            if (index >= 0 && index < Segments.Count - 1)
            {
                var item = Segments[index];
                Segments.RemoveAt(index);
                Segments.Insert(index + 1, item);
                UpdateOrderNumbers();
                await OnSegmentsChangedInternal(Segments);
                Snackbar.Add($"{Resource.QuestionMovedSuccessfully}", Severity.Success);
            }
        }

        private void UpdateOrderNumbers()
        {
            for (int i = 0; i < Segments.Count; i++)
            {
                if (Segments[i].SegmentQuestionConfigDto != null)
                {
                    Segments[i].SegmentQuestionConfigDto.OrderNumber = i + 1;
                }

                if (Segments[i].SegmentMetaDataDto != null && string.IsNullOrWhiteSpace(Segments[i].SegmentMetaDataDto.Code))
                {
                    Segments[i].SegmentMetaDataDto.Code = i == 0
                        ? RootQuestionCode
                        : $"{RootQuestionCode} 1.{i + 1}";
                }
            }
        }
    }
}
