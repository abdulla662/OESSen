using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.Section;
using OES.Helper.Dtos.Section;
using OES.Helper.Enums;
using System.Net;
namespace OES.Blazor.Dialogs.Form
{
    public partial class EditFormSectionsDialog
    {
        [Inject] private ISnackbar Snackbar { get; set; }
        [Inject] private IBlazFormService BlazFormService { get; set; }
        [Inject] private IBlazSectionService BlazSectionService { get; set; }

        [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;
        [Parameter] public long FormId { get; set; }
        [Parameter] public QuestionSelectionType PaperSubType { get; set; }

        private List<SectionWithFormQuestionsDto> sections = [];

        protected override async Task OnInitializedAsync()
        {
            await LoadSectionsAsync();
        }

        private async Task LoadSectionsAsync()
        {
            sections = await BlazFormService.GetFormSectionsWithQuestionsAsync(FormId);
        }

        private async Task SaveSectionAsync(SectionWithFormQuestionsDto section)
        {
            EditSectionDto editSectionDto = new()
            {
                Id = section.SectionId,
                Name = section.SectionName,
            };

            var response = await BlazSectionService.EditSectionAsync(editSectionDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

        }
    }
}
