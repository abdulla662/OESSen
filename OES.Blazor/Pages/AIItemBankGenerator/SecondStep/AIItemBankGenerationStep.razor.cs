using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.AIFeatures;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.Net;
using System.Text.Json;

namespace OES.Blazor.Pages.AIItemBankGenerator.SecondStep
{
    public partial class AIItemBankGenerationStep : IDisposable
    {
        [Inject] private IBlazAIItemBankGenerationService BlazAIItemBankGenerationService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Parameter] public AIItemBankStepperTransferableDto ConfigurationData { get; set; } = new();
        [Parameter] public EventCallback<AIItemBankNodeDto> OnGenerated { get; set; }
        [Parameter] public bool IsConfigurationLocked { get; set; }
        [Parameter] public EventCallback<bool> OnProcessingStateChanged { get; set; }

        public bool IsProcessing { get; set; } = false;
        private bool GenerationComplete { get; set; } = false;

        private readonly string _processingMessage = Resource.ProcessingTimeMayVary;
        private AIItemBankNodeDto? _generatedTree;
        private CancellationTokenSource? _generationCts;

        private static string GetLevelsCreationName(ItemBankLevelsCreation value)
        {
            return value switch
            {
                ItemBankLevelsCreation.UseExisting => Resource.UseExistingLevels,
                ItemBankLevelsCreation.CreateNew => Resource.CreateNewLevels,
                ItemBankLevelsCreation.Mix => Resource.MixLevels,
                _ => value.ToString()
            };
        }

        private async Task GenerateItemBank()
        {
            if (ConfigurationData.File == null)
            {
                Snackbar.Add(Resource.FileRequired, Severity.Error);
                return;
            }

            _generationCts?.Cancel();
            _generationCts?.Dispose();
            _generationCts = new CancellationTokenSource();

            var cancellationToken = _generationCts.Token;

            IsProcessing = true;
            GenerationComplete = false;
            await OnProcessingStateChanged.InvokeAsync(true);
            StateHasChanged();

            var configurationDto = new AIItemBankGenerationConfigurationsDto
            {
                ItemBankLevelsCreation = ConfigurationData.ItemBankLevelsCreation,
                MaxLevelsCount = ConfigurationData.MaxLevelsCount,
                MaxChildrenPerNode = ConfigurationData.MaxChildrenPerNode,
                LanguageDto = ConfigurationData.SelectedLanguageDto
            };

            try
            {
                var response = await BlazAIItemBankGenerationService.GenerateItemBankFromFileAsync(
                    ConfigurationData.File,
                    configurationDto,
                    cancellationToken
                );

                if (response?.StatusCode == HttpStatusCode.OK && response.Data != null)
                {
                    GenerationComplete = true;

                    if (response.Data is AIItemBankNodeDto treeData)
                    {
                        _generatedTree = treeData;
                    }
                    else
                    {
                        var jsonString = response.Data.ToString();
                        if (!string.IsNullOrWhiteSpace(jsonString))
                        {
                            _generatedTree = JsonSerializer.Deserialize<AIItemBankNodeDto>(
                                jsonString,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                            );
                        }
                    }

                    if (_generatedTree != null)
                    {
                        await OnGenerated.InvokeAsync(_generatedTree);
                    }
                    else
                    {
                        Snackbar.Add(Resource.FailedLoadQuestionData, Severity.Error);
                    }
                }
                else
                {
                    Snackbar.Add(response?.Message ?? Resource.FailedLoadQuestionData, Severity.Error);
                }
            }
            catch (OperationCanceledException)
            {
                Snackbar.Add(Resource.OperationCancelled, Severity.Info);
            }
            catch (Exception ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
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
            if (_generationCts != null && !_generationCts.IsCancellationRequested)
            {
                _generationCts.Cancel();
            }
        }

        public void Dispose()
        {
            _generationCts?.Cancel();
            _generationCts?.Dispose();
        }
    }
}
