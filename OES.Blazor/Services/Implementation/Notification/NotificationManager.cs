using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.Notification;
using OES.Helper.Dtos.NotificationDto;
using SharedHelper.General;

namespace OES.Blazor.Services.Implementation.Notification
{
    public class NotificationManager : INotificationManager, IAsyncDisposable
    {
        private readonly IBlazAuthService _blazAuthService;
        private readonly ISnackbar _snackbar;
        private HubConnection _hubConnection;
        private bool _isInitialized;
        public event Action<NotificationDto> OnNotificationReceived;

        private bool _isConnected => _hubConnection?.State == HubConnectionState.Connected;

        public NotificationManager(NavigationManager navigationManager,
                                   ISnackbar snackbar,
                                   IBlazAuthService blazAuthService)
        {
            _snackbar = snackbar;
            _blazAuthService = blazAuthService;
            InitializeHubConnection(navigationManager);
        }

        private void InitializeHubConnection(NavigationManager navigationManager)
        {
            ArgumentNullException.ThrowIfNull(navigationManager);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(navigationManager.ToAbsoluteUri($"{CentralizedUrlHelper.OesApiBaseUrl}notificationHub"), options => options.AccessTokenProvider = async () => await GetTokenAsync())
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<NotificationDto>("ReceiveNotification", HandleNewNotification);
        }

        private async Task<string> GetTokenAsync()
        {
            var token = await _blazAuthService.GetDecryptedTokenFromLocalStorageAsync();

            if (string.IsNullOrEmpty(token))
            {
                return "";
            }

            return token;
        }

        private void HandleNewNotification(NotificationDto notification)
        {
            OnNotificationReceived?.Invoke(notification);

            var severity = notification.Type switch
            {
                NotificationTypeStatus.Success => Severity.Success,
                NotificationTypeStatus.Error => Severity.Error,
                NotificationTypeStatus.Warning => Severity.Warning,
                _ => Severity.Info
            };

            _snackbar.Add(notification.Message, severity, config =>
            {
                config.VisibleStateDuration = 5000;
                config.ShowTransitionDuration = 200;
                config.HideTransitionDuration = 200;
            });
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized || _isConnected) return;

            await _hubConnection.StartAsync();

            _isInitialized = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }
    }
}