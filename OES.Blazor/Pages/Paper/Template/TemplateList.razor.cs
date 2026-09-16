using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.Templates;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Template;
using OES.Helper;
using OES.Helper.Dtos.Template.Request;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Paper.Template
{
    public partial class TemplateList
    {
        [Inject] IDialogService DialogService { get; set; }

        [Inject] IBlazTemplateService BlazTemplateService { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }

        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        private List<TemplateTypeDto> _templateTypes;
        private TemplateTypeDto _selectedTemplateType;
        private int _templateKey;

        public TemplateFilterPaginationModel TemplateFilterPaginationModel { get; set; } = new();

        public TemplateTypeDto SelectedTemplateType
        {
            get => _selectedTemplateType;
            set
            {
                if (_selectedTemplateType != value)
                {
                    _selectedTemplateType = value;
                    TemplateFilterPaginationModel._selectedTemplateType = _selectedTemplateType?.Id ?? 0;
                    _templateKey += 1;
                }
            }
        }

        protected override void OnInitialized()
        {
            _templateTypes = [.. Enum.GetValues(typeof(TemplateTypeEnum))
                .Cast<TemplateTypeEnum>()
                .Select((type, index) => new TemplateTypeDto
                {
                    Id = index + 1,
                    Name = type.ToLocalizedString()
                })];
        }

        private async Task DeleteTemplateAsync(long templateId)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
                { x => x.Title, Resource.ConfirmDelete },
                { x => x.Content, Resource.AreYouSureYouWantToDeleteThisItem },
                { x => x.CancelText, Resource.Cancel },
                { x => x.SubmitText, Resource.Delete },
                { x => x.SubmitButtonColor, Color.Error },
                { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small };

            var dialog = await DialogService.ShowAsync<GenericDialog>(@Resource.Delete, parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var response = await BlazTemplateService.DeleteTemplate(templateId);

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    _templateKey++;
                    Snackbar.Add(response.Message, Severity.Success);
                }
                else
                {
                    Snackbar.Add(response.Message, Severity.Error);
                }

                StateHasChanged();
            }
        }

        private async Task OpenAddTemplateDialogAsync()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraLarge,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = true,
            };

            var dialog = await DialogService.ShowAsync<AddTemplateDialog>(string.Empty, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _templateKey++;
                StateHasChanged();
            }
        }

        private async Task OpenEditTemplateDialogAsync(long templateId)
        {
            await BlazSessionStorageService.SetCrudSessionAsync(templateId, MiscConstants.PerformEditBtnClick);

            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.ExtraLarge,
                FullWidth = true,
                CloseButton = false,
                CloseOnEscapeKey = true,
            };

            var dialog = await DialogService.ShowAsync<EditTemplateDialog>(string.Empty, options);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                _templateKey++;
                StateHasChanged();
            }
        }
    }
}
