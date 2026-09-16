using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Template;
using OES.Helper.Dtos.Paper.AutoSelectedQuestionsSectioning;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.Enums;

namespace OES.Blazor.Dialogs.Paper.Sectioning.AutoSectioning
{
    public partial class AutoSectionInstructionTemplateDialog
    {
        [Inject] IBlazTemplateService BlazTemplateService { get; set; }

        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public DistributionSectionRequestDto Section { get; set; }

        private List<GetListedTemplateResponseDto> InstructionTemplate { get; set; } = [];

        private GetListedTemplateResponseDto? SelectedTemplate { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadTemplates();

            SelectedTemplate = InstructionTemplate.FirstOrDefault(t => t.Id == Section.InstructionSectionTemplateId);
        }

        private async Task LoadTemplates()
        {
            InstructionTemplate = await BlazTemplateService.GetTemplatesByTypeId((long)TemplateTypeEnum.InstructionSection);

            StateHasChanged();
        }

        private void Save()
        {
            Section.InstructionSectionTemplateId = SelectedTemplate?.Id;

            MudDialog?.Close(true);
        }

        private Task<IEnumerable<GetListedTemplateResponseDto>> SearchTemplates(string value, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(Enumerable.Empty<GetListedTemplateResponseDto>());
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult(InstructionTemplate.AsEnumerable());
            }

            var filtered = InstructionTemplate
                .Where(t => !string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .AsEnumerable();

            return Task.FromResult(filtered);
        }

        private void Cancel()
        {
            MudDialog?.Cancel();
        }
    }
}
