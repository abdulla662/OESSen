using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.Question.SegmentQuestionDtos.HelperDtos;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using DotNetTimer = System.Timers.Timer;

namespace OES.Blazor.Pages.QuestionLayout.SegmentLayouts
{
    public partial class SegmentLayout : IDisposable
    {
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; } = null!;

        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private List<SegmentQuestionDto> _allSegments = [];
        private SegmentQuestionConfigDto? _currentSegmentProperties;
        private long _currentOrder = -1;
        private bool _isAudioPlaying = false;
        private bool _isThinkingTime = false;
        private bool _isResponseTime = false;
        private bool _isRecording = false;
        private long _countdownValue = 0;
        private DotNetTimer? _timer;

        protected override async Task OnInitializedAsync()
        {
            if (Model == null) return;

            if (Model.SegmentQuestionConfig != null)
            {
                _allSegments =
                [
                    new SegmentQuestionDto
                    {
                        SegmentMetaDataDto = new SegmentQuestionMetaDataDto
                        {
                            ParentId = Model.QuestionMetadataId
                        },
                        SegmentQuestionDetailsDto = new SegmentQuestionDetailsDto
                        {
                            Body = Model.Body,
                            Instructions = Model.Instructions,
                            ModelAnswer = Model.ModelAnswer,
                            LanguageId = Model.LanguageId,
                            QuestionMetadataId = Model.QuestionMetadataId,
                            MaxWords = Model.MaxWords,
                            MaxRecordingTimeInSeconds = Model.MaxRecordingTimeInSeconds,
                            MediaFileName = Model.AttachmentFileName,
                            Choices = Model.Choices ?? []
                        },
                        SegmentQuestionConfigDto = Model.SegmentQuestionConfig
                    }
                ];

                _currentOrder = _allSegments[0].SegmentQuestionConfigDto.OrderNumber;

                if (ShowCorrectAnswer)
                {
                    _currentOrder = _allSegments.Max(s => s.SegmentQuestionConfigDto.OrderNumber);
                    StateHasChanged();
                }
                else
                {
                    await StartSegmentSequenceAsync();
                }

                return;
            }

            var segments = await BlazQuestionService.GetSegmentQuestionByMetaDataIdAsync(Model.QuestionMetadataId, Model.LanguageId);

            if (segments?.Count > 0)
            {
                _allSegments = segments.OrderBy(s => s.SegmentQuestionConfigDto.OrderNumber).ToList();

                if (!ShowCorrectAnswer)
                {
                    _currentOrder = _allSegments[0].SegmentQuestionConfigDto.OrderNumber;
                    await StartSegmentSequenceAsync();
                }
                else
                {
                    _currentOrder = _allSegments.Max(s => s.SegmentQuestionConfigDto.OrderNumber);
                    StateHasChanged();
                }
            }
        }
        private async Task StartSegmentSequenceAsync()
        {
            var current = _allSegments.FirstOrDefault(s => s.SegmentQuestionConfigDto.OrderNumber == _currentOrder);

            if (current == null) return;

            _currentSegmentProperties = current.SegmentQuestionConfigDto;

            _isAudioPlaying = !string.IsNullOrWhiteSpace(_currentSegmentProperties.SegmentAudioUrl);
            _isThinkingTime = false;
            _isResponseTime = false;
            _isRecording = false;

            if (!_isAudioPlaying)
            {
                await StartThinkingCountdownAsync();
            }

            StateHasChanged();

            await InvokeAsync(StateHasChanged);
        }

        public async Task OnAudioFinishedAsync()
        {
            _isAudioPlaying = false;

            await InvokeAsync(StartThinkingCountdownAsync);
        }

        private async Task StartThinkingCountdownAsync()
        {
            if (ShowCorrectAnswer) return;

            if ((_currentSegmentProperties?.ThinkingTime ?? 0) <= 0)
            {
                await StartResponseCountdownAsync();
                return;
            }

            _isThinkingTime = true;

            _countdownValue = _currentSegmentProperties.ThinkingTime ?? 0;

            await SetupTimerAsync(StartResponseCountdownAsync);

            StateHasChanged();
        }

        private async Task StartResponseCountdownAsync()
        {
            _isThinkingTime = false;

            if (_currentSegmentProperties.SegmentQuestionResponseType == SegmentQuestionResponseType.None &&
                (_currentSegmentProperties.ResponseTime ?? 0) <= 0)
            {
                await MoveToNextSegmentAsync();
                return;
            }

            _isResponseTime = true;
            _countdownValue = _currentSegmentProperties.ResponseTime ?? 0;

            if (_currentSegmentProperties.SegmentQuestionResponseType == SegmentQuestionResponseType.Audio)
            {
                _isRecording = true;
            }

            if (_countdownValue > 0)
            {
                await SetupTimerAsync(async () =>
                {
                    _isRecording = false;
                    await MoveToNextSegmentAsync();
                });
            }
            else
            {
                await MoveToNextSegmentAsync();
            }

            StateHasChanged();
        }

        private async Task MoveToNextSegmentAsync()
        {
            StopTimer();

            _isRecording = false;

            var nextSegment = _allSegments.FirstOrDefault(x => x.SegmentQuestionConfigDto.OrderNumber > _currentOrder);

            if (nextSegment != null)
            {
                _currentOrder = nextSegment.SegmentQuestionConfigDto.OrderNumber;
                await StartSegmentSequenceAsync();
            }
            else
            {
                _isResponseTime = false;
                _isThinkingTime = false;
            }

            StateHasChanged();
        }

        public async Task SubmitAnswerAsync()
        {
            await MoveToNextSegmentAsync();
        }

        private async Task SetupTimerAsync(Func<Task> onTimerComplete)
        {
            StopTimer();

            if (_countdownValue <= 0)
            {
                await onTimerComplete();
                return;
            }

            _timer = new(1000);
            _timer.Elapsed += async (sender, e) =>
            {
                if (_countdownValue > 0)
                {
                    _countdownValue--;
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    StopTimer();
                    await InvokeAsync(onTimerComplete);
                }
            };

            _timer.Start();
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        private static string FormatTime(long seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);
            return timeSpan.ToString(@"mm\:ss");
        }

        private static string GetResponseTypeLabel(SegmentQuestionResponseType responseType) => responseType switch
        {
            SegmentQuestionResponseType.None => Resource.NoType,
            SegmentQuestionResponseType.Essay => Resource.EssayResponse,
            SegmentQuestionResponseType.Audio => Resource.AudioResponse,
            _ => responseType.ToString()
        };

        public void Dispose()
        {
            StopTimer();
        }
    }
}
