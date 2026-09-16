using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.GenericComponents.TextEditor;
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
    public partial class AddTemplateDialog : ComponentBase
    {
        [Inject] IBlazTemplateService BlazTemplateService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }


        [CascadingParameter] MudDialogInstance MudDialog { get; set; } = default!;


        private static bool RightToLeft => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        public TextEditorParams BodyEditorParams { get; set; }


        private AddTemplateRequestDto _template = new();

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

            StateHasChanged();

            _templateTypes = [.. Enum.GetValues(typeof(TemplateTypeEnum))
                .Cast<TemplateTypeEnum>()
                .Select((type, index) => new TemplateTypeDto
                {
                    Id = index + 1,
                    Name = type.ToLocalizedString(),
                })];

            _selectedTemplateType = _templateTypes[0];

            _template.TemplateTypeId = _selectedTemplateType.Id;

            await FetchTemplateAttributesAsync(_selectedTemplateType.Id);
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

                _template.TemplateTypeId = _selectedTemplateType.Id;

                await FetchTemplateAttributesAsync(selectedType.Id);
            }
        }

        private async Task FetchTemplateAttributesAsync(long templateTypeId)
        {
            _isLoadingAttributes = true;

            StateHasChanged();

            _templateAttributes = await BlazTemplateService.GetTemplateAttributesAsync(templateTypeId);

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
            _template.Content = await BodyEditorParams.GetTextEditorContentAsync();

            _showContentError = string.IsNullOrEmpty(_template.Content);

            if (string.IsNullOrEmpty(_template.Name) || _showContentError)
                return;

            try
            {
                if (string.IsNullOrEmpty(_template.Name))
                {
                    Snackbar.Add(Resource.NameCannotBeEmpty, Severity.Error);
                    return;
                }

                if (string.IsNullOrEmpty(_template.Content))
                {
                    Snackbar.Add(Resource.ContentCannotBeEmpty, Severity.Error);
                    return;
                }

                var response = await BlazTemplateService.SaveTemplateAsync(_template);

                if (response.Message == Resource.TemplateWithTheSameNameAlreadyExists)
                {
                    Snackbar.Add(Resource.TemplateWithTheSameNameAlreadyExists, Severity.Error);
                }
                else if (response.StatusCode == HttpStatusCode.OK)
                {
                    Snackbar.Add(Resource.TemplateDataHasBeenCreatedSuccessfully, Severity.Success);
                    await BodyEditorParams.DisposeJsEditorAsync();
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add(response.Message ?? Resource.FailedToSaveTemplate, Severity.Error);
                }
            }
            catch
            {
                Snackbar.Add(Resource.FailedToSaveTemplate, Severity.Error);
            }
        }

        private void Close() => MudDialog.Cancel();
    }
}