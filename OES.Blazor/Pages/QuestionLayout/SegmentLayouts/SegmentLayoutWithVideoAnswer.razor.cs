using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Enums;
using DotNetTimer = System.Timers.Timer;

namespace OES.Blazor.Pages.QuestionLayout.SegmentLayouts
{
    public partial class SegmentLayoutWithVideoAnswer : ComponentBase, IDisposable
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;

        private int ColumnsCount { get; set; }
        private bool _isRecording;
        private bool _isLoading;
        private string _recordingTime = "00:00";
        private int _maxRecordingSeconds;
        private DotNetTimer _timer;
        private DateTime _startTime;
        private ElementReference _videoRef;

        protected override void OnParametersSet()
        {
            if (Orientation == LayoutOrientation.Horizontal)
                ColumnsCount = 6;
            else if (Orientation == LayoutOrientation.Vertical)
                ColumnsCount = 12;

            _maxRecordingSeconds = Model?.MaxRecordingTimeInSeconds > 0 ? Model.MaxRecordingTimeInSeconds : 300;
            _recordingTime = FormatTime(_maxRecordingSeconds);
        }

        private async Task StartRecordingAsync()
        {
            _isRecording = true;
            _isLoading = false;
            _startTime = DateTime.UtcNow;
            _recordingTime = FormatTime(_maxRecordingSeconds);

            _timer?.Dispose();
            _timer = new DotNetTimer(1000);
            _timer.Elapsed += async (s, e) =>
            {
                var elapsed = DateTime.UtcNow - _startTime;
                var remaining = _maxRecordingSeconds - (int)elapsed.TotalSeconds;

                if (remaining <= 0)
                {
                    _timer?.Stop();
                    await InvokeAsync(StopRecordingAsync);
                }
                else
                {
                    _recordingTime = FormatTime(remaining);
                    await InvokeAsync(StateHasChanged);
                }
            };
            _timer.Start();

            await InvokeAsync(StateHasChanged);
        }

        private async Task StopRecordingAsync()
        {
            if (!_isRecording || _isLoading) return;

            _isLoading = true;
            await InvokeAsync(StateHasChanged);

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            await Task.Delay(500);

            _isLoading = false;
            _isRecording = false;
            _recordingTime = FormatTime(_maxRecordingSeconds);

            StateHasChanged();
        }

        private static string FormatTime(int totalSeconds)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}