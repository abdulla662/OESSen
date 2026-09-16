using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Pages.AIQuestionGenerator.Dialogs;
using OES.Blazor.Services.Interfaces.AIFeatures;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.AIQuestionGenerator.ThirdStep;

public partial class ReviewStep
{
    [Inject] private IBlazAIQuestionGenerationService BlazAIQuestionGenerationService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public AIGeneratedQuestionsResultDto? GeneratedQuestions { get; set; }
    [Parameter] public bool IsConfigurationLocked { get; set; }
    [Parameter] public EventCallback<bool> IsConfigurationLockedChanged { get; set; }

    private bool HasQuestions => GeneratedQuestions?.Questions != null && GeneratedQuestions.Questions.Count > 0;
    private bool _isSaving;

    private async Task SaveQuestionsAsync()
    {
        if (GeneratedQuestions == null || IsConfigurationLocked)
            return;

        _isSaving = true;
        StateHasChanged();

        try
        {
            var response = await BlazAIQuestionGenerationService.SaveGeneratedQuestionsAsync(GeneratedQuestions);

            if (response?.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.QuestionsAddedSuccessfully, Severity.Success);
                await IsConfigurationLockedChanged.InvokeAsync(true);
            }
            else
            {
                Snackbar.Add(response?.Message ?? Resource.FailedToSaveQuestions, Severity.Error);
            }
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    private async Task ViewQuestionAsync(AIQuestionMetadataDto question)
    {
        var parameters = new DialogParameters<AIQuestionViewDialog>
        {
            { x => x.Question, question }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        await DialogService.ShowAsync<AIQuestionViewDialog>(Resource.View, parameters, options);
    }

    private async Task EditQuestionAsync(AIQuestionMetadataDto question)
    {
        var parameters = new DialogParameters<AIQuestionEditDialog>
        {
            { x => x.Question, question }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = false
        };

        var dialog = await DialogService.ShowAsync<AIQuestionEditDialog>(Resource.Edit, parameters, options);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            Snackbar.Add(Resource.QuestionUpdatedSuccessfully, Severity.Success);
            StateHasChanged();
        }
    }

    private async Task OnDeleteQuestionAsync(AIQuestionMetadataDto question)
    {
        var parameters = new DialogParameters<GenericDialog>
        {
            { x => x.Title, Resource.Delete },
            { x => x.Content, Resource.AreYouSureYouWantToDeleteThisItem },
            { x => x.CancelText, Resource.Cancel },
            { x => x.SubmitText, Resource.Delete },
            { x => x.SubmitButtonColor, Color.Error }
        };

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            GeneratedQuestions?.Questions.Remove(question);
            StateHasChanged();
        }
    }

    private static string GetTruncatedBody(string? body, int maxLength = 80)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        if (body.Length <= maxLength)
            return body;

        return body[..maxLength] + "...";
    }

    private async Task EditQuestionMetaDataAsync(AIQuestionMetadataDto question)
    {
        var parameters = new DialogParameters<AIQuestionMetadataDialog>
        {
            { x => x.QuestionMetadataDto, question }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = false
        };

        var dialog = await DialogService.ShowAsync<AIQuestionMetadataDialog>(
            Resource.QuestionLayout,
            parameters,
            options
        );

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            Snackbar.Add(Resource.QuestionMetaDataUpdatedSuccessfully, Severity.Success);
            StateHasChanged();
        }
    }
}
