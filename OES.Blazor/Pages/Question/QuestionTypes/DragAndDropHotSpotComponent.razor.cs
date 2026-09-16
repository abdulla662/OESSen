using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Dialogs.FileManager;
using OES.Blazor.Dialogs.Question.InstructionTemplateDialog;
using OES.Blazor.Dialogs.Question.TemplateDialog;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.Folder.Response;
using OES.Helper.Dtos.HotSpotDtos;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Helper.Static;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.Question.QuestionTypes
{
    public partial class DragAndDropHotSpotComponent : IAsyncDisposable
    {
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguage { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazQuestionInstructionTemplate BlazInstructionTemplateService { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private IJSRuntime JSRuntime { get; set; }

        [Parameter] public QuestionDetailsDto Model { get; set; }
        [Parameter] public EventCallback<QuestionDetailsDto> FormHandler { get; set; }
        [Parameter] public EventCallback DialogCancellationCallback { get; set; }
        [Parameter] public bool IsReplaceMode { get; set; }

        public TextEditorParams BodyEditorParams { get; set; }
        public TextEditorParams InstructionsEditorParams { get; set; }
        private LanguageDto SelectedLanguage { get; set; } = new();
        private List<LanguageDto> LanguagesList { get; set; } = [];

        private HotSpotQuestionDetailsDto _internalModel = new();
        private ElementReference _canvasRef;
        private bool _isDrawing = false;
        private PointDto? _drawStartPoint;
        private string _uploadedImageUrl = string.Empty;
        private bool _needsCanvasInit = false;
        private List<DraggableItemDto> _draggableItems = [];
        private bool _processing = false;
        private bool _isSubmitButtonHit = false;
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;
        private string AvailableElementsLabel => SelectedLanguage?.Id == 1
            ? "العناصر المتاحة للسحب"
            : "Available elements to drag";

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrEmpty(Model.ModelAnswer))
            {
                var existingData = JsonSerializer.Deserialize<HotSpotQuestionDetailsDto>(Model.ModelAnswer);
                if (existingData != null)
                {
                    _internalModel = existingData;
                    _draggableItems = existingData.DraggableItems ?? [];
                }
            }

            InitializeTextEditors();

            LanguagesList = await BlazQuestionLanguage.GetAllLanguagesAsync();
            SelectedLanguage = LanguagesList.Find(l => l.Id == Model.LanguageId) ?? new();

            if (!string.IsNullOrEmpty(_internalModel.ImageUrl))
            {
                _uploadedImageUrl = _internalModel.ImageUrl;
                _needsCanvasInit = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (!string.IsNullOrEmpty(_uploadedImageUrl))
                    await InitializeCanvasAsync();
            }
            else if (_needsCanvasInit)
            {
                _needsCanvasInit = false;
                await InitializeCanvasAsync();
            }
        }

        private void InitializeTextEditors()
        {
            var cleanBody = Model.Body ?? string.Empty;

            if (!string.IsNullOrEmpty(_internalModel.ImageUrl))
            {
                var imgMatch = Regex.Match(cleanBody, HotSpotBodyParsingConstants.HotspotImageContainerPattern, RegexOptions.IgnoreCase);

                if (imgMatch.Success)
                    cleanBody = cleanBody.Substring(0, imgMatch.Index).TrimEnd();
            }

            var dragMatch = Regex.Match(cleanBody, HotSpotBodyParsingConstants.DragDropItemsSectionPattern, RegexOptions.IgnoreCase);

            if (dragMatch.Success)
                cleanBody = cleanBody.Substring(0, dragMatch.Index).TrimEnd();

            BodyEditorParams = new()
            {
                Label = Resource.QuestionBody,
                InitialContent = cleanBody,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
            };

            InstructionsEditorParams = new()
            {
                Label = Resource.QuestionInstructionsOptional,
                InitialContent = Model.Instructions,
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
            };

            StateHasChanged();
        }

        private async Task InitializeCanvasAsync()
        {
            if (string.IsNullOrEmpty(_uploadedImageUrl)) return;
            var dims = await JSRuntime.InvokeAsync<JsonElement>("hotspotCanvas.initializeCanvas", "hotspotCanvas", _uploadedImageUrl);
            _internalModel.ImageWidth = dims.GetProperty("width").GetInt32();
            _internalModel.ImageHeight = dims.GetProperty("height").GetInt32();
            if (_internalModel.HotSpotAreas.Any()) await RedrawAllDropZonesAsync();
        }

        private async Task RemoveDropZone(Guid id)
        {
            _internalModel.HotSpotAreas.RemoveAll(x => x.Id == id);
            _draggableItems.ForEach(item => item.AcceptedDropZoneIds.Remove(id));
            await RedrawAllDropZonesAsync();
            StateHasChanged();
        }

        private async Task ClearAllDropZones()
        {
            _internalModel.HotSpotAreas.Clear();
            _draggableItems.ForEach(item => item.AcceptedDropZoneIds.Clear());
            await RedrawAllDropZonesAsync();
            StateHasChanged();
        }

        private void AddDraggableItem()
        {
            var newItem = new DraggableItemDto
            {
                Id = Guid.NewGuid(),
                Label = "",
                IsDistractor = false,
                OrderIndex = _draggableItems.Any() ? _draggableItems.Max(i => i.OrderIndex) + 1 : 0,
                AcceptedDropZoneIds = []
            };

            _draggableItems.Add(newItem);
            StateHasChanged();
        }

        private void DeleteDraggableItem(DraggableItemDto item)
        {
            _draggableItems.Remove(item);
            ReorderDraggableItems();
            StateHasChanged();
        }

        private void ReorderDraggableItems()
        {
            var items = _draggableItems.OrderBy(i => i.OrderIndex).ToList();
            for (int i = 0; i < items.Count; i++)
            {
                items[i].OrderIndex = i;
            }
        }

        private void ToggleDropZone(DraggableItemDto item, Guid zoneId)
        {
            if (item.AcceptedDropZoneIds.Contains(zoneId))
            {
                item.AcceptedDropZoneIds.Remove(zoneId);
            }
            else
            {
                item.AcceptedDropZoneIds.Add(zoneId);
            }

            StateHasChanged();
        }

        private async Task<bool> ValidateFormAsync()
        {
            BodyEditorParams.ErrorShown = string.IsNullOrWhiteSpace(await BodyEditorParams.GetTextEditorContentAsync());

            var isValid = !(SelectedLanguage.Id == 0 || BodyEditorParams.ErrorShown);

            if (string.IsNullOrEmpty(_internalModel.ImageUrl))
            {
                isValid = false;
            }

            if (!_internalModel.HotSpotAreas.Any())
            {
                isValid = false;
            }

            if (!_draggableItems.Any())
            {
                isValid = false;
            }

            if (_draggableItems.Any(i => string.IsNullOrWhiteSpace(i.Label)))
            {
                isValid = false;
            }

            if (!_draggableItems.Any(i => !i.IsDistractor))
            {
                isValid = false;
            }

            var itemsWithoutZones = _draggableItems
                .Where(i => !i.IsDistractor && !i.AcceptedDropZoneIds.Any())
                .ToList();

            if (itemsWithoutZones.Any())
            {
                Snackbar.Add(Resource.NonDistractorItemAtLeastOneAcceptedDropZone, Severity.Error);
                isValid = false;
            }

            return isValid;
        }

        public async Task OnSubmitAsync()
        {
            _isSubmitButtonHit = true;
            _processing = true;
            StateHasChanged();

            var isFormValid = await ValidateFormAsync();

            if (!isFormValid)
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                _processing = false;
                return;
            }

            Model.LanguageId = SelectedLanguage.Id;
            var editorContent = await BodyEditorParams.GetTextEditorContentAsync();

            var hotspotImageHtml = $"<div class='hotspot-image-container'>" +
                                   $"<img src='{_internalModel.ImageUrl}' class='hotspot-image' alt='Hotspot Question Image' />" +
                                   $"</div>";

            var labelsHtml = GenerateLabelsHtml(_draggableItems, SelectedLanguage);

            Model.Body = $"{editorContent}{hotspotImageHtml}<div class='drag-drop-items-section'>{labelsHtml}</div>";

            Model.Instructions = await InstructionsEditorParams.GetTextEditorContentAsync();
            _internalModel.DraggableItems = _draggableItems;
            Model.ModelAnswer = JsonSerializer.Serialize(_internalModel);

            await FormHandler.InvokeAsync(Model);

            _processing = false;
            Snackbar.Add(Resource.QuestionsAddedSuccessfully, Severity.Success);
        }

        private static string GenerateLabelsHtml(List<DraggableItemDto> items, LanguageDto selectedLanguage)
        {
            var labelsJson = JsonSerializer.Serialize(items.Select(item => new
            {
                item.Id,
                item.Label,
                item.OrderIndex
            }));

            var itemsHtml = string.Join(" ", items.OrderBy(i => i.OrderIndex).Select(item => $"<span class='draggable-label' data-item-id='{item.Id}'>{System.Web.HttpUtility.HtmlEncode(item.Label)}</span>"));
            var title = selectedLanguage?.LanguageDirection == "RTL" ? "العناصر المتاحة للسحب" : "Available elements to drag";

            return $@"
                <div class='draggable-items-container' data-items='{System.Web.HttpUtility.HtmlEncode(labelsJson)}'>
                    <h4>{title}</h4>
                    <div class='items-list'>{itemsHtml}</div>
                </div>";
        }

        private async Task CancelDialogAsync()
        {
            await DialogCancellationCallback.InvokeAsync();
        }

        private async Task SaveInstructionAsTemplate()
        {
            var parameters = new DialogParameters();
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<TemplateNameDialog>(Resource.SaveAsInstructionTemplate, parameters, options);
            var result = await dialog.Result;

            if (result.Canceled) return;

            var templateName = result.Data?.ToString();

            if (string.IsNullOrWhiteSpace(templateName))
            {
                Snackbar.Add(Resource.NameRequired, Severity.Error);
                return;
            }

            var instructionsContent = await InstructionsEditorParams.GetTextEditorContentAsync();

            if (string.IsNullOrWhiteSpace(instructionsContent))
            {
                Snackbar.Add(Resource.CannotProceedWithEmptyOrInvalidMandatoryFields, Severity.Error);
                return;
            }

            var dto = new QuestionInstructionTemplateDto
            {
                Name = templateName,
                Content = instructionsContent
            };

            var response = await BlazInstructionTemplateService.SaveInstructionTemplateAsync(dto);

            if (response.CustomCodeStatus == CustomCodeStatus.Success)
            {
                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private async Task LoadInstructionTemplate()
        {
            var parameters = new DialogParameters();
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<InstructionTemplateDialog>(Resource.SelectTemplate, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is QuestionInstructionTemplateDto selectedTemplate)
            {
                Model.Instructions = selectedTemplate.Content;

                if (InstructionsEditorParams.TextEditor != null)
                {
                    await InstructionsEditorParams.TextEditor.SetTextEditorContentAsync(selectedTemplate.Content);
                }

                Snackbar.Add(Resource.TemplateFetchedSuccessfully, Severity.Success);
                StateHasChanged();
            }
        }

        private async Task ReceiveSelectedFileAsync(FolderDocumentListResponseDto insertedFile)
        {
            var extension = Path.GetExtension(insertedFile.Name)?.ToLower();

            if (!AllowedFileExtensions.Images.Contains(extension))
            {
                Snackbar.Add(Resource.PleaseUploadImageOnly, Severity.Error);
                return;
            }

            var mediaSourceUrl = $"{CentralizedUrlHelper.DocLibApiBaseUrl}/api/Document/DownloadStream?documentId={insertedFile.Id}";

            _uploadedImageUrl = mediaSourceUrl;
            _internalModel.ImageUrl = _uploadedImageUrl;

            await ClearAllDropZones();
            _needsCanvasInit = true;
            StateHasChanged();
        }

        public async ValueTask DisposeAsync()
        {
            await JSRuntime.InvokeVoidAsync("hotspotCanvas.dispose", "hotspotCanvas");
        }

        private async Task OpenFileManagerDialog()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            };

            var parameters = new DialogParameters<FileManagerDialog>
            {
                {
                    x => x.OnFileSelected,
                    EventCallback.Factory.Create<dynamic>(this, (file) => ReceiveSelectedFileAsync(file))
                }
            };

            await DialogService.ShowAsync<FileManagerDialog>(string.Empty, parameters, options);
        }

        private void RemoveBodyMedia()
        {
            Model.AttachmentFileName = null;
            StateHasChanged();
        }

        #region Mouse Moving
        private async Task OnCanvasMouseDown(MouseEventArgs e)
        {
            _isDrawing = true;
            _drawStartPoint = await JSRuntime.InvokeAsync<PointDto>("hotspotCanvas.getMousePosition", "hotspotCanvas", e.ClientX, e.ClientY);
        }

        private async Task OnCanvasMouseMove(MouseEventArgs e)
        {
            if (!_isDrawing || _drawStartPoint == null) return;
            var current = await JSRuntime.InvokeAsync<PointDto>("hotspotCanvas.getMousePosition", "hotspotCanvas", e.ClientX, e.ClientY);
            await RedrawAllDropZonesAsync();
            await DrawPreviewRectangle(_drawStartPoint, current);
        }

        private async Task OnCanvasMouseUp(MouseEventArgs e)
        {
            if (!_isDrawing || _drawStartPoint == null) return;
            _isDrawing = false;

            var end = await JSRuntime.InvokeAsync<PointDto>("hotspotCanvas.getMousePosition", "hotspotCanvas", e.ClientX, e.ClientY);
            var x = Math.Min(_drawStartPoint.X, end.X);
            var y = Math.Min(_drawStartPoint.Y, end.Y);
            var w = Math.Abs(end.X - _drawStartPoint.X);
            var h = Math.Abs(end.Y - _drawStartPoint.Y);

            if (w < 10 || h < 10)
            {
                Snackbar.Add(Resource.DropZoneTooSmall, Severity.Warning);
                return;
            }

            var dropZone = new HotSpotAreaDto
            {
                Id = Guid.NewGuid(),
                ShapeType = HotSpotShapeType.Rectangle,
                CoordinatesJson = JsonSerializer.Serialize(new { X = x, Y = y, Width = w, Height = h }),
                IsCorrectAnswer = false
            };

            _internalModel.HotSpotAreas.Add(dropZone);
            await RedrawAllDropZonesAsync();
            StateHasChanged();
        }
        #endregion

        #region Draw Rectangle
        private async Task DrawPreviewRectangle(PointDto start, PointDto end)
        {
            var x = Math.Min(start.X, end.X);
            var y = Math.Min(start.Y, end.Y);
            var w = Math.Abs(end.X - start.X);
            var h = Math.Abs(end.Y - start.Y);
            await JSRuntime.InvokeVoidAsync("hotspotCanvas.drawRectangle", "hotspotCanvas", x, y, w, h, false, false);
        }

        private async Task RedrawAllDropZonesAsync()
        {
            await JSRuntime.InvokeVoidAsync("hotspotCanvas.clearCanvas", "hotspotCanvas", _uploadedImageUrl);
            foreach (var zone in _internalModel.HotSpotAreas)
            {
                var coords = JsonSerializer.Deserialize<RectangleCoordinates>(zone.CoordinatesJson);
                if (coords != null)
                    await JSRuntime.InvokeVoidAsync("hotspotCanvas.drawRectangle", "hotspotCanvas", coords.X, coords.Y, coords.Width, coords.Height, false, true);
            }
        }
        #endregion

        #region Moving Items
        private void MoveItem(DraggableItemDto item, int direction)
        {
            var items = _draggableItems.OrderBy(i => i.OrderIndex).ToList();
            var idx = items.IndexOf(item);
            var targetIdx = idx + direction;

            if (targetIdx < 0 || targetIdx >= items.Count) return;

            (items[targetIdx].OrderIndex, items[idx].OrderIndex) = (items[idx].OrderIndex, items[targetIdx].OrderIndex);

            StateHasChanged();
        }
        #endregion
    }
}