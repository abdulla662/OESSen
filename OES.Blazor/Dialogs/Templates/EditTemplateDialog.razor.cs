using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Template;
using OES.Helper;
using OES.Helper.Dtos.Template.Request;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Globalization;
using System.Net;

namespace OES.Blazor.Dialogs.Templates
{
    public partial class EditTemplateDialog : ComponentBase
    {
        [Inject] IBlazTemplateService BlazTemplateService { get; set; }
        [Inject] IBlazSessionStorageService SessoinStorage { set; get; }
        [Inject] ISnackbar Snackbar { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;

        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private TextEditorParams BodyEditorParams { get; set; }

        private TemplateDataDto template = new();
        private TemplateTypeDto _selectedTemplateType = new();
        private TemplateAttributesDto _templateAttributes = new();
        private bool _isLoadingAttributes = false;
        private List<TemplateTypeDto> _templateTypes = [];
        private readonly string _requiredErrorText = Resource.ThisFieldIsRequired;
        private int _templateContent;
        private bool _showContentError;

        protected override async Task OnInitializedAsync()
        {
            BodyEditorParams = new()
            {
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
                ErrorShown = false,
                EmbedImagesAsBase64 = true,
            };

            var id = await SessoinStorage.GetValue<long>("PerformEditBtnClick");
            template = await BlazTemplateService.GetTemplateDataById(id);

            BodyEditorParams = new()
            {
                WithMathChemPanel = true,
                WithFileManagerPanel = true,
                ErrorText = _requiredErrorText,
                RequiredAsteriskShown = true,
                InitialContent = template.Content,
                EmbedImagesAsBase64 = true,
            };

            _templateContent++;

            _templateTypes = [.. Enum.GetValues(typeof(TemplateTypeEnum))
                .Cast<TemplateTypeEnum>()
                .Select((type, index) => new TemplateTypeDto
                {
                    Id = index + 1,
                    Name = type.ToLocalizedString()
                })];

            _selectedTemplateType = _templateTypes.Find(t => t.Id == template.TemplateTypeId);

            await FetchTemplateAttributesAsync(_selectedTemplateType.Id);

            StateHasChanged();

        }

        private async Task<IEnumerable<TemplateTypeDto>> SearchTemplateTypeAsync(string value, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                _selectedTemplateType?.Name == null && string.IsNullOrWhiteSpace(value) ||
                _selectedTemplateType?.Name?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
            {
                return _templateTypes;
            }

            return _templateTypes.Where(p =>
                p.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
            );
        }

        private async Task HandleTemplateTypeChange(TemplateTypeDto selectedType)
        {
            if (selectedType != null)
            {
                _selectedTemplateType = selectedType;

                template.TemplateTypeId = _selectedTemplateType.Id;

                await FetchTemplateAttributesAsync(selectedType.Id);
            }
        }

        private async Task FetchTemplateAttributesAsync(long templateTypeId)
        {
            _isLoadingAttributes = true;

            StateHasChanged();

            try
            {
                _templateAttributes = await BlazTemplateService.GetTemplateAttributesAsync(templateTypeId);
            }
            catch
            {
                _templateAttributes = new TemplateAttributesDto();
            }

            _isLoadingAttributes = false;

            StateHasChanged();
        }

        private async Task InsertAttribute(string attribute)
        {
            if (BodyEditorParams?.TextEditor != null)
            {
                var textToInsert = $"&nbsp;[{attribute}]&nbsp;";
                await BodyEditorParams.SetTextEditorContentAsync(textToInsert);
            }
        }

        private async Task Save()
        {
            template.Content = await BodyEditorParams.GetTextEditorContentAsync();

            _showContentError = string.IsNullOrEmpty(template.Content);

            if (string.IsNullOrEmpty(template.Name) || _showContentError)
                return;

            if (string.IsNullOrEmpty(template.Name))
            {
                Snackbar.Add(Resource.NameCannotBeEmpty, Severity.Error);
                return;
            }

            if (string.IsNullOrEmpty(template.Content))
            {
                Snackbar.Add(Resource.ContentCannotBeEmpty, Severity.Error);
                return;
            }

            var response = await BlazTemplateService.UpdateTemplate(template);

            if (response.Message == Resource.TemplateWithTheSameNameAlreadyExists)
            {
                Snackbar.Add(Resource.TemplateWithTheSameNameAlreadyExists, Severity.Error);
            }
            else if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);
                await BodyEditorParams.DisposeJsEditorAsync();
                MudDialog.Close(DialogResult.Ok(true));
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }
        }

        private void Close() => MudDialog.Cancel();
    }
}
