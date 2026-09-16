using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Extentions.DialogHelpers;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.AIQuestionGenerator.Dialogs;

public partial class AIQuestionEditDialog
{
    [Inject] private IBlazQuestionService BlazQuestionService { get; set; } = default!;
    [Inject] private IBlazGroupService BlazGroupService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public AIQuestionMetadataDto? Question { get; set; }

    private List<GetOESGroupDto> SelectedGroups { get; set; } = [];

    private string QuestionTypeName => Question?.QuestionTypeName ?? string.Empty;

    private QuestionDetailsDto? _questionDetailsModel;

    private readonly bool _cameFromAIQuestionsGenerator = true;

    protected override void OnParametersSet()
    {
        if (Question != null && _questionDetailsModel == null)
        {
            _questionDetailsModel = MapToQuestionDetailsDto(Question);
        }
    }

    protected override void OnInitialized()
    {
        SelectedGroups = Question?.OESGroupDtos?.ToList() ?? [];
    }

    private async Task OpenGroupDialog()
    {
        SelectedGroups = await DialogInteractionService.OpenSelectionDialogAsync(
            dialogService: DialogService,
            title: Resource.SelectGroup,
            preSelectedItems: SelectedGroups.Where(g => !g.AutoCreatedForUser),
            endpointService: async pagination =>
            {
                var allGroups = await BlazQuestionService.GetAllQuestionGroupsAsync();

                allGroups = [.. allGroups
                    .Concat(SelectedGroups)
                    .GroupBy(g => g.Id)
                    .Select(g => g.First())];

                var filteredGroups = allGroups
                    .Where(g => !g.AutoCreatedForUser)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(pagination.SearchKey))
                {
                    filteredGroups = [.. filteredGroups.Where(g => g.Name.Contains(pagination.SearchKey, StringComparison.OrdinalIgnoreCase))];
                }

                return new CustomTableData<GetOESGroupDto>
                {
                    Items = filteredGroups,
                    TotalItems = filteredGroups.Count
                };
            },
            resourceType: ResourceType.Questions,
            onDelete: EventCallback.Factory.Create<Guid>(this, DeleteGroupAsync),
            isOpenedFromAI: true
        );

        SelectedGroups = [.. SelectedGroups.Where(g => !g.AutoCreatedForUser)];
    }

    private async Task DeleteGroupAsync(Guid groupId)
    {
        var response = await BlazGroupService.DeleteGroupAsync(groupId);

        if (response.CustomCodeStatus == CustomCodeStatus.Success)
            Snackbar.Add(response.Message, Severity.Success);
        else
            Snackbar.Add(response.Message, Severity.Error);
    }

    private async Task OnFormSubmittedAsync(QuestionDetailsDto submittedModel)
    {
        if (Question == null) return;

        MapBackToAIQuestionDto(submittedModel);

        Question.OESGroupDtos = [.. SelectedGroups];

        MudDialog.Close(DialogResult.Ok(true));
        await Task.CompletedTask;
    }

    private void Cancel()
    {
        MudDialog.Close(DialogResult.Cancel());
    }

    private static QuestionDetailsDto MapToQuestionDetailsDto(AIQuestionMetadataDto question)
    {
        var detail = question.Details.FirstOrDefault();

        return new QuestionDetailsDto
        {
            Id = -1, // Non-zero to indicate edit mode (prevents MCQQuestionComponent from clearing choices)
            Body = detail?.Body ?? string.Empty,
            Instructions = detail?.Instructions,
            ModelAnswer = detail?.ModelAnswer ?? string.Empty,
            LanguageId = detail.LanguageId,
            MaxWords = detail?.MaxWords,
            UseArabicNumbers = detail?.UseArabicNumbers ?? false,
            Choices = detail?.Choices?.Select((c, index) => new ChoiceDataDto
            {
                Id = index + 1,
                ChoiceText = c.Text,
                IsCorrectAnswer = c.IsCorrect,
                OrderId = c.Order
            }).ToList() ?? []
        };
    }

    private void MapBackToAIQuestionDto(QuestionDetailsDto submittedModel)
    {
        if (Question == null) return;

        var detail = Question.Details.FirstOrDefault();
        if (detail == null)
        {
            detail = new AIQuestionDetailsDto();
            Question.Details.Add(detail);
        }

        detail.Body = submittedModel.Body;
        detail.MaxWords = submittedModel.MaxWords;
        detail.UseArabicNumbers = submittedModel.UseArabicNumbers;
        detail.Instructions = submittedModel.Instructions;
        detail.ModelAnswer = submittedModel.ModelAnswer;

        detail.Choices = submittedModel.Choices?.Select((c, index) => new AIQuestionChoiceDto
        {
            Text = c.ChoiceText,
            IsCorrect = c.IsCorrectAnswer,
            Order = index
        }).ToList() ?? [];
    }
}
