using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.AIFeatures;
using OES.Helper.Dtos.AIQuestionGenerator.Request;
using OES.Helper.Dtos.AIQuestionGenerator.Response;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;
using QuestionTypeEnum = OES.Helper.Enums.QuestionType;

namespace OES.Blazor.Pages.AIQuestionGenerator.SecondStep
{
    public partial class GenerateStep : IDisposable
    {
        [Inject] private IBlazAIQuestionGenerationService BlazAIQuestionGenerationService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private ILogger<GenerateStep> Logger { get; set; } = default!;

        [Parameter] public AIQuestionGenerationStepperTransferableDto ConfigurationData { get; set; } = new();
        [Parameter] public EventCallback<AIGeneratedQuestionsResultDto> OnGenerated { get; set; }
        [Parameter] public bool IsConfigurationLocked { get; set; }
        [Parameter] public EventCallback<bool> OnProcessingStateChanged { get; set; }

        public bool IsProcessing { get; set; } = false;
        private bool GenerationComplete { get; set; } = false;
        private int TotalRequestedQuestions => ConfigurationData.QuestionTypeRequests?.Sum(q => q.Count) ?? 0;
        private int TotalGeneratedQuestions => _aiGeneratedQuestionsResultDto.Questions?.Count ?? 0;

        private readonly string _processingMessage = Resource.ProcessingTimeMayVary;
        private AIGeneratedQuestionsResultDto _aiGeneratedQuestionsResultDto = new();
        private CancellationTokenSource? _generationCts;
        private bool _isCancelledByUser = false;

        private static string GetQuestionTypeIcon(string questionTypeName)
        {
            if (!Enum.TryParse<QuestionTypeEnum>(questionTypeName, ignoreCase: true, out var questionType))
                return Icons.Material.Filled.QuestionMark;

            return questionType switch
            {
                QuestionTypeEnum.MCQ => Icons.Material.Filled.RadioButtonChecked,
                QuestionTypeEnum.TrueAndFalse => Icons.Material.Filled.ToggleOn,
                QuestionTypeEnum.Essay => Icons.Material.Filled.Edit,
                _ => Icons.Material.Filled.QuestionMark
            };
        }

        private async Task GenerateQuestions()
        {
            _generationCts?.Cancel();
            _generationCts?.Dispose();
            _generationCts = new CancellationTokenSource();

            var cancellationToken = _generationCts.Token;

            IsProcessing = true;
            _isCancelledByUser = false;
            await OnProcessingStateChanged.InvokeAsync(true);
            StateHasChanged();

            var configurationDto = new AIQuestionGenerationConfigurationsDto
            {
                QuestionTypes = ConfigurationData.QuestionTypeRequests,
                RequireAnswersFromDocumentOnly = true,
                GeneratePlausibleDistractors = true,
                ItemBank = new EntityReferenceDto(ConfigurationData.SelectedItemBank?.Id ?? 0, ConfigurationData.SelectedItemBank?.Name ?? string.Empty),
                Language = new EntityReferenceDto(ConfigurationData.SelectedLanguage?.Id ?? 0, ConfigurationData.SelectedLanguage?.Name ?? string.Empty),
                Subject = new EntityReferenceDto(ConfigurationData.SelectedSubject?.Id ?? 0, ConfigurationData.SelectedSubject?.Name ?? string.Empty),
                Category = new EntityReferenceDto(ConfigurationData.SelectedCategory?.Id ?? 0, ConfigurationData.SelectedCategory?.Name ?? string.Empty),
                DifficultyProfile = new EntityReferenceDto(ConfigurationData.SelectedDifficultyProfile?.Id ?? 0, ConfigurationData.SelectedDifficultyProfile?.Name ?? string.Empty),
                Ilo = ConfigurationData.SelectedIlo?.Id > 0
                    ? new EntityReferenceDto(ConfigurationData.SelectedIlo.Id, ConfigurationData.SelectedIlo.Name ?? string.Empty)
                    : null,
                Author = MiscConstants.AIGeneratorAuthor,
                QuestionsExhaustionCount = MiscConstants.CommonQuestionExhaustionCount,
                ScientificEditorPanelEnabled = true,
                FileManagerEditorPanelEnabled = true
            };

            try
            {
                var response = await BlazAIQuestionGenerationService.GenerateQuestionsFromFileAsync(ConfigurationData.File!, configurationDto, cancellationToken);

                if (response?.StatusCode == HttpStatusCode.OK && response.Data != null)
                {
                    GenerationComplete = true;

                    _aiGeneratedQuestionsResultDto = JsonSerializer.Deserialize<AIGeneratedQuestionsResultDto>(
                        response.Data.ToString()!,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (_aiGeneratedQuestionsResultDto != null)
                    {
                        await OnGenerated.InvokeAsync(_aiGeneratedQuestionsResultDto);
                    }
                }
                else
                {
                    Snackbar.Add(response?.Message ?? Resource.FailedLoadQuestionData, Severity.Error);
                }
            }
            catch (OperationCanceledException ex)
            {
                if (_isCancelledByUser)
                {
                    Logger.LogInformation("AI Question generation was cancelled by the user.");
                }
                else
                {
                    Logger.LogWarning(ex, "AI Question generation was cancelled for another reason. Reason: {Reason}", ex.Message);
                }
            }
            finally
            {
                IsProcessing = false;
                await OnProcessingStateChanged.InvokeAsync(false);
                StateHasChanged();
            }
        }

        private void CancelGeneration()
        {
            if (_generationCts == null || _generationCts.IsCancellationRequested)
                return;

            _isCancelledByUser = true;

            _generationCts.Cancel();
        }

        private static string GetFormattedQuestionsCount(int count)
        {
            return $"{count} {(count == 1 ? Resource.OneQuestion : Resource.Questions)}";
        }

        public void Dispose()
        {
            _generationCts?.Cancel();
            _generationCts?.Dispose();
        }
    }
}
