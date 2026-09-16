using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using OES.Blazor.Dialogs.DashboardPanel;
using OES.Blazor.Dialogs.Notification;
using OES.Blazor.Services.Implementation;
using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.AuditLogs;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Notification;
using OES.Blazor.Services.Interfaces.PagesService;
using OES.Helper.Dtos.AuditLogs;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Dtos.Sync;
using OES.Helper.General;
using OES.Helper.General.GlobalUserContext;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Net;
using System.Text.Json;
namespace OES.Blazor.Shared.Layout
{
    public partial class OESLayout : IDisposable
    {
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private ILocalStorageService LocalStorage { get; set; }
        [Inject] public IBlazAuthService BlazAuthService { get; set; } = default!;
        [Inject] public IBlazSessionStorageService SessionStorage { get; set; }
        [Inject] public NavigationManager NavigationManager { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] public IGetPagesService GetPagesService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        [Inject] public GlobalUserContext GlobalUserContext { get; set; } = default!;
        [Inject] private CultureService CultureService { get; set; } = default!;
        [Inject] private IBlazNotificationService BlazeNotification { get; set; }
        [Inject] public INotificationManager NotificationManager { get; set; } = default!;
        [Inject] private SyncDashboardState SyncDashboardState { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISyncToExamServer SyncToExamServerService { get; set; }
        [Inject] private ISyncStatusService SyncStatusService { get; set; }
        [Inject] private IBlazAuditLogsService UserAuditLogService { get; set; } = default!;

        [CascadingParameter] public Task<AuthenticationState> AuthenticationState { get; set; } = default!;

        private string CultureButtonLabel { get; set; } = string.Empty;
        private List<NotificationAppUserProfileDto> Notifications { get; set; }

        private bool _drawerOpen = true;
        private bool _isDefaultTheme = true;
        private MudTheme _currentTheme;
        private bool _isGlobalUserContextFilled;
        private bool _rightToLeft;
        private bool _open;
        private string orgName;
        private bool _useArabicHindi = false;
        private IDialogReference _currentDialogReference;
        private HubConnection _globalSyncHub;
        private DotNetObjectReference<OESLayout> _dotNetRef;
        private readonly SemaphoreSlim _flushLock = new(1, 1);
        private Timer? _flushTimer;
        private const int FlushThreshold = 20;
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(30);


        protected override async Task OnParametersSetAsync()
        {
            var culture = await CultureService.GetCultureAsync();

            _rightToLeft = culture?.Contains("ar", StringComparison.InvariantCultureIgnoreCase) ?? true;

            _useArabicHindi = _rightToLeft;

            await LocalStorage.SetItemAsync("OESNumberFormat", _useArabicHindi);

            SetThemeTypography();
        }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var savedTheme = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "OEStheme");

                _currentTheme = savedTheme == "Dark" ? OESDarkTheme : OESTheme;

                _isDefaultTheme = savedTheme != "Dark";

                await SetCultureButtonLabel();

                await GetSavedThemePreferenceAsync();

                await BlazAuthService.HandleAuthenticationProcessForUserAsync();

                await AuthenticationStateProvider.GetAuthenticationStateAsync();

                await GetPagesService.sendPagesListToAPI();

                if (GlobalUserContext.UserId != Guid.Empty)
                {
                    Notifications = await GetFilteredNotification(GlobalUserContext.UserId);
                }

                _isGlobalUserContextFilled = true;

                try
                {
                    await NotificationManager.InitializeAsync();
                }
                catch
                {
                    // Ignore
                }

                NotificationManager.OnNotificationReceived += HandleNotification;

                orgName = GlobalUserContext
                    .AllowedOrganizations
                    .Find(x => x.Id == GlobalUserContext.CurrentOrganizationId)?
                    .Name;

                await ToggleNumbersJavaScript(_useArabicHindi);

                SyncStatusService.StatusChanged += StateHasChanged;

                SyncDashboardState.OnSyncStarted += LaunchSyncDashboardAsync;

                SyncDashboardState.OnPreparationStarted += LaunchPreparationDashboard;

                SyncDashboardState.OnPreparationStopped += ClosePreparationDashboard;

                if (_isGlobalUserContextFilled)
                {
                    await SyncStatusService.HydrateStateFromStorageAsync();
                }

                await InitializeGlobalSyncListener();

                _dotNetRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync(MiscConstants.AuditLogsRegister, _dotNetRef);

                _ = FlushBufferAsync();
                _flushTimer = new Timer(async _ => await FlushBufferAsync(), null, FlushInterval, FlushInterval);
            }
            catch (Exception)
            {
                Console.WriteLine("An error occurred");
            }
        }

        private async Task InitializeGlobalSyncListener()
        {
            var hubUrl = $"{CentralizedUrlHelper.OesApiBaseUrl.TrimEnd('/')}/syncDashboardHub";

            _globalSyncHub = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => options.AccessTokenProvider = () => BlazAuthService.GetDecryptedTokenFromLocalStorageAsync())
                .WithAutomaticReconnect()
                .Build();

            _globalSyncHub.On<long, DateTime>("GlobalSyncStarted", (orgId, expirationTime) =>
            {
                InvokeAsync(() =>
                {
                    SyncDashboardState.SetGlobalSyncActive(true, expirationTime, orgId);
                    StateHasChanged();
                });
            });

            _globalSyncHub.On<long>("GlobalSyncFinished", (orgId) =>
            {
                InvokeAsync(() =>
                {
                    if (SyncDashboardState.LockedOrganizationId == orgId)
                    {
                        SyncDashboardState.SetGlobalSyncActive(false, null, 0);
                        StateHasChanged();
                    }
                });
            });

            await _globalSyncHub.StartAsync();

            long currentOrgId = GlobalUserContext.CurrentOrganizationId;

            await _globalSyncHub.InvokeAsync("JoinOrganizationGroup", currentOrgId);

            var activeLock = await _globalSyncHub.InvokeAsync<GlobalSyncLockDto>("CheckGlobalSyncState", currentOrgId);

            if (activeLock != null)
            {
                await InvokeAsync(() =>
                {
                    SyncDashboardState.SetGlobalSyncActive(true, activeLock.ExpirationTimeUtc, currentOrgId);

                    StateHasChanged();
                });
            }
        }

        private void ClosePreparationDashboard()
        {
            if (_currentDialogReference != null)
            {
                _currentDialogReference.Close();
                _currentDialogReference = null;
            }
        }

        private void LaunchPreparationDashboard(long scheduleId, string scheduleName)
        {
            _currentDialogReference?.Close();

            var parameters = new DialogParameters<SyncDashboardPanelDialog>
            {
                { x => x.IsPreparationMode, true },
                { x => x.PreparationScheduleId, scheduleId },
                { x => x.PreparationScheduleName, scheduleName },
                { x => x.OnRetryFailedJobs, EventCallback.Factory.Create<IEnumerable<long>>(this, RetryFailedJobsAsync) },
                { x => x.OnRetrySingleJob, EventCallback.Factory.Create<long>(this, RetrySingleJobAsync) }
            };

            var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, BackdropClick = false };

            _currentDialogReference = DialogService.Show<SyncDashboardPanelDialog>("", parameters, options);
        }

        public async void LaunchSyncDashboardAsync(List<SyncJobStatusDto> initialJobs)
        {
            SyncStatusService.AddJobs(initialJobs);

            OpenSyncDashboard();

            await Task.CompletedTask;
        }

        private void OpenSyncDashboard()
        {
            if (_currentDialogReference != null)
            {
                _currentDialogReference.Close();
            }

            var parameters = new DialogParameters<SyncDashboardPanelDialog>
            {
                { x => x.InitialJobs, SyncStatusService.GetAllJobs() },
                { x => x.OnRetryFailedJobs, EventCallback.Factory.Create<IEnumerable<long>>(this, RetryFailedJobsAsync)},
                { x => x.OnRetrySingleJob, EventCallback.Factory.Create<long>(this, RetrySingleJobAsync) }
            };

            var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, BackdropClick = false };

            _currentDialogReference = DialogService.Show<SyncDashboardPanelDialog>("", parameters, options);
        }

        private Color GetSyncIconColor()
        {
            if (SyncStatusService.FailedJobCount > 0)
                return Color.Error;
            if (SyncStatusService.ActiveJobCount > 0)
                return Color.Warning;
            if (SyncStatusService.HasAnyJobs)
                return Color.Success;
            return Color.Secondary;
        }

        private string GetSyncIcon()
        {
            if (SyncStatusService.ActiveJobCount > 0)
                return Icons.Material.Filled.Sync;
            if (SyncStatusService.FailedJobCount > 0)
                return Icons.Material.Filled.ErrorOutline;
            if (SyncStatusService.HasAnyJobs)
                return Icons.Material.Filled.CheckCircle;
            return Icons.Material.Filled.CloudSync;
        }

        private string GetSyncIconClass()
        {
            return SyncStatusService.ActiveJobCount > 0 ? "sync-fab-active" : "";
        }

        private string GetBadgeContent()
        {
            if (SyncStatusService.ActiveJobCount > 0)
                return SyncStatusService.ActiveJobCount.ToString();
            if (SyncStatusService.FailedJobCount > 0)
                return SyncStatusService.FailedJobCount.ToString();
            return "";
        }

        private Color GetBadgeColor()
        {
            if (SyncStatusService.FailedJobCount > 0)
                return Color.Error;
            return Color.Primary;
        }

        private async Task RetrySingleJobAsync(long jobId)
        {
            await RetryFailedJobsAsync(new List<long> { jobId });
        }

        private async Task RetryFailedJobsAsync(IEnumerable<long> failedJobIds)
        {
            if (_currentDialogReference != null)
            {
                ((SyncDashboardPanelDialog)_currentDialogReference.Dialog).SetRetryState(true);
            }

            var jobIdsList = failedJobIds.ToList();

            var response = await SyncToExamServerService.RetryFailedJobsAsync(jobIdsList);

            if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(Resource.RetryFailed, Severity.Success);

                List<SyncJobStatusDto> retriedJobs = null;

                if (response.Data != null)
                {
                    if (response.Data is JsonElement jsonElement)
                    {
                        retriedJobs = JsonSerializer.Deserialize<List<SyncJobStatusDto>>(
                            jsonElement.GetRawText(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    else if (response.Data is string jsonString)
                    {
                        retriedJobs = JsonSerializer.Deserialize<List<SyncJobStatusDto>>(
                            jsonString,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    else if (response.Data is IEnumerable<SyncJobStatusDto> jobsList)
                    {
                        retriedJobs = jobsList.ToList();
                    }
                    else
                    {
                        var jsonStr = JsonSerializer.Serialize(response.Data);
                        retriedJobs = JsonSerializer.Deserialize<List<SyncJobStatusDto>>(jsonStr);
                    }
                }

                if (retriedJobs != null && retriedJobs.Any())
                {
                    foreach (var retriedJob in retriedJobs)
                    {
                        SyncStatusService.UpdateJob(
                            retriedJob.Id,
                            retriedJob.Status,
                            retriedJob.ErrorMessage,
                            retriedJob.CompletedAt
                        );
                    }

                    await InvokeAsync(() =>
                    {
                        OpenSyncDashboard();
                        StateHasChanged();
                    });
                }
                else
                {
                    Snackbar.Add("Retry initiated but no jobs returned", Severity.Warning);
                }
            }
            else
            {
                Snackbar.Add(response.Message, Severity.Error);
            }

            if (_currentDialogReference != null)
            {
                ((SyncDashboardPanelDialog)_currentDialogReference.Dialog).SetRetryState(false);
            }
        }

        public async Task<List<NotificationAppUserProfileDto>> GetFilteredNotification(Guid userId)
        {
            var response = await BlazeNotification.GetFilteredNotification(userId);

            if (response != null)
                return response;

            return new List<NotificationAppUserProfileDto>();
        }

        private async void HandleNotification(NotificationDto notification)
        {
            if (notification.ParameterName == "RolesUpdated")
            {
                //Console.WriteLine("ROLES UPDATED RECEIVED");
                await BlazAuthService.RefreshUserContextAsync();
                await InvokeAsync(async () => await JSRuntime.InvokeVoidAsync("location.reload"));
                return;
            }

            Notifications = await GetFilteredNotification(GlobalUserContext.UserId);

            var severity = notification.Type switch
            {
                NotificationTypeStatus.Success => Severity.Success,
                NotificationTypeStatus.Error => Severity.Error,
                NotificationTypeStatus.Warning => Severity.Warning,
                _ => Severity.Info
            };

            Snackbar.Add(notification.Subject, severity, config =>
            {
                config.VisibleStateDuration = 5000;
                config.ShowTransitionDuration = 200;
                config.HideTransitionDuration = 200;
            });

            StateHasChanged();
        }

        private async Task OnNotificationClick(long id)
        {
            var dialogParams = new DialogParameters<NotificationDetailsDialog>
            {
                { x => x.NotificationId, id }
            };

            var dialogOptions = new DialogOptions()
            {
                FullWidth = true,
                MaxWidth = MaxWidth.Small
            };

            await DialogService.ShowAsync<NotificationDetailsDialog>(
                string.Empty,
                dialogParams,
                dialogOptions
            );

            Notifications.Remove(Notifications.First(c => c.NotificationId == id));

            StateHasChanged();
        }

        private void NavigateToNotificationList()
        {
            NavigationManager.NavigateTo("/NotificationList");
        }

        private void DrawerToggle() => _drawerOpen = !_drawerOpen;

        private async Task GetSavedThemePreferenceAsync()
        {
            try
            {
                var savedTheme = await LocalStorage.GetItemAsStringAsync("OEStheme");

                if (!string.IsNullOrEmpty(savedTheme))
                {
                    _currentTheme = savedTheme == "Dark" ? OESDarkTheme : OESTheme;
                }
                else
                {
                    _currentTheme = OESTheme;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving theme preference: {ex.Message}");
            }
        }

        private async Task SetThemePreferenceAsync()
        {
            try
            {
                var themeValue = _currentTheme == OESDarkTheme ? "Dark" : "Light";

                await LocalStorage.SetItemAsStringAsync("OEStheme", themeValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving theme preference: {ex.Message}");
            }
        }

        private async Task ToggleTheme()
        {
            _isDefaultTheme = !_isDefaultTheme;

            SwitchTheme(_isDefaultTheme);

            await SetThemePreferenceAsync();
        }

        private void SwitchTheme(bool useDefaultTheme)
        {
            _isDefaultTheme = useDefaultTheme;

            _currentTheme = _isDefaultTheme ? OESTheme : OESDarkTheme;

            StateHasChanged();
        }

        private void SetThemeTypography()
        {
            string fontFamily = "Arial";

            OESDarkTheme.Typography = new Typography()
            {
                Button = new Button { FontSize = "1rem", FontFamily = new[] { fontFamily }, TextTransform = "none" },
                Body1 = new Body1() { FontSize = "1rem", FontFamily = new[] { fontFamily } },
                H1 = new H1() { FontSize = "2rem", FontFamily = new[] { fontFamily } },
                H6 = new H6() { FontSize = "1.25rem", FontFamily = new[] { fontFamily } },
                H3 = new H3() { FontSize = "1.5rem", FontFamily = new[] { fontFamily } },
                H5 = new H5() { FontSize = "1.25rem", FontFamily = new[] { fontFamily } },
                Input = new Input() { FontSize = "1.25rem", FontFamily = new[] { fontFamily } },
                Default = new Default() { FontSize = "1rem", FontFamily = new[] { fontFamily } },
            };

            OESTheme.Typography = new Typography()
            {
                Button = new Button { FontSize = "1rem", FontFamily = new[] { fontFamily }, TextTransform = "none" },
                Body1 = new Body1() { FontSize = "1rem", FontFamily = new[] { fontFamily } },
                Body2 = new Body2() { FontSize = "1rem", FontFamily = new[] { fontFamily } },
                H1 = new H1() { FontSize = "2rem", FontFamily = new[] { fontFamily } },
                H6 = new H6() { FontSize = "1.25rem", FontFamily = new[] { fontFamily } },
                H3 = new H3() { FontSize = "1.5rem", FontFamily = new[] { fontFamily } },
                H5 = new H5() { FontSize = "1.25rem", FontFamily = new[] { fontFamily } },
                Input = new Input() { FontSize = "1.25rem", FontFamily = new[] { fontFamily } },
                Default = new Default() { FontSize = "1rem", FontFamily = new[] { fontFamily } }
            };

            StateHasChanged();
        }

        private string GetCultureClass()
        {
            return _rightToLeft ? "arabic-font" : "english-font";
        }

        private async Task ToggleLanguage()
        {
            var culture = await CultureService.GetCultureAsync();

            if (culture == LanguageCode.ARABIC_CODE)
            {
                CultureService.SetCultureAsync(LanguageCode.ENGLISH_CODE);
                _rightToLeft = false;
                _useArabicHindi = false;
            }
            else if (culture == LanguageCode.ENGLISH_CODE)
            {
                CultureService.SetCultureAsync(LanguageCode.ARABIC_CODE);
                _rightToLeft = true;
                _useArabicHindi = true;
            }

            await LocalStorage.SetItemAsync("OESNumberFormat", _useArabicHindi);

            await ToggleNumbersJavaScript(_useArabicHindi);

            await SetCultureButtonLabel();

            StateHasChanged();
        }

        private async Task SetCultureButtonLabel()
        {
            var culture = await CultureService.GetCultureAsync();

            if (culture == LanguageCode.ARABIC_CODE)
            {
                CultureButtonLabel = Resource.CultureButtonLabelEN;
            }
            else if (culture == LanguageCode.ENGLISH_CODE)
            {
                CultureButtonLabel = Resource.CultureButtonLabelAR;
            }
        }

        private async Task ToggleNumbersJavaScript(bool useArabicNumerals)
        {
            await JSRuntime.InvokeVoidAsync("toggleArabicNumerals", useArabicNumerals);
        }

        private void OpenProfileLogo() => _open = !_open;

        private void ClosePopover() => _open = false;

        [JSInvokable]
        public async Task LogAuditEvent(Dictionary<string, object> logData)
        {
            var pathName = logData.GetValueOrDefault(nameof(AuditLogsDto.PathName))?.ToString() ?? "";

            var dto = new AuditLogsDto
            {
                Action = logData.GetValueOrDefault(nameof(AuditLogsDto.Action))?.ToString() ?? "",
                Url = logData.GetValueOrDefault(nameof(AuditLogsDto.Url))?.ToString() ?? "",
                ActionAt = DateTimeHelper.Now,
                PathName = pathName,
                PageName = AuditLogPageResolver.Resolve(pathName)
            };

            var logs = await GetBufferedLogsAsync();

            logs.Add(dto);

            await LocalStorage.SetItemAsync(MiscConstants.AuditLogsStorageKey, logs);

            if (logs.Count >= FlushThreshold)
            {
                _ = FlushBufferAsync();
            }
        }

        #region Local Storage Buffer

        private async Task<List<AuditLogsDto>> GetBufferedLogsAsync()
        {
            var logs = await LocalStorage.GetItemAsync<List<AuditLogsDto>>(MiscConstants.AuditLogsStorageKey);
            return logs ?? [];
        }

        private async Task FlushBufferAsync()
        {
            if (!await _flushLock.WaitAsync(0))
                return;

            try
            {
                var authState = await AuthenticationState;
                if (authState?.User?.Identity?.IsAuthenticated != true)
                    return;

                var logs = await GetBufferedLogsAsync();

                if (logs.Count == 0) return;

                var success = await UserAuditLogService.SaveBatchAsync(logs);

                if (success)
                {
                    await LocalStorage.SetItemAsync(MiscConstants.AuditLogsStorageKey, new List<AuditLogsDto>());
                }
            }
            finally
            {
                _flushLock.Release();
            }
        }

        #endregion

        public void Dispose()
        {
            _flushTimer?.Dispose();
            _ = FlushBufferAsync();
            NotificationManager.OnNotificationReceived -= HandleNotification;
            SyncStatusService.StatusChanged -= StateHasChanged;
            SyncDashboardState.OnSyncStarted -= LaunchSyncDashboardAsync;
            SyncDashboardState.OnPreparationStarted -= LaunchPreparationDashboard;
            SyncDashboardState.OnPreparationStopped -= ClosePreparationDashboard;
            _ = _globalSyncHub?.DisposeAsync();
            _ = JSRuntime.InvokeVoidAsync(MiscConstants.AuditLogsUnRegister);
            _dotNetRef?.Dispose();
        }

        #region Theme Work

        // Dark Theme Colors:
        private static MudColor OESDarkPrimary = new MudColor("#007ecc");  // Light Blue

        private static MudColor OESDarkSecondary = new MudColor("#adb5bd");  // Soft Gray
        private static MudColor OESDarkSecondaryText = new MudColor("#CCD3DA");  // Soft Gray
        private static MudColor OESDarkSecondaryBG = new MudColor("#838f9b");  // dark Gray
        private static MudColor OESDarkAppbarBackground = new MudColor("#212529");  // Dark Charcoal
        private static MudColor OESDarkPrimaryText = new MudColor("#e9ecef");  // Off White
        private static MudColor OESDarkSuccessText = new MudColor("#37d67a");  // Soft Green
        private static MudColor OESDarkDrawerBackground = new MudColor("#1e1e2f");  // Midnight Blue
        private static MudColor OESDarkTerriratyColor = new MudColor("#065779");  // Navy Blue

        // Light Theme Colors:
        private static MudColor OESPrimary = new MudColor("#009bd9");

        private static MudColor OESSecondary = new MudColor("#3a3b3c");
        private static MudColor OESbarBackground = new MudColor("#023b6d");
        private static MudColor OESPrimaryText = new MudColor("#024077");
        private static MudColor OESSuccessText = new MudColor("#00c560");
        private static MudColor OESDrawerBackground = new MudColor("#fff");
        private static MudColor OESTerriratyColor = new MudColor("#f2f2f2");

        // Dark Theme Object without Typography (will be set later)
        private MudTheme OESDarkTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = OESDarkPrimary,
                Secondary = OESDarkSecondaryBG,
                Dark = OESDarkSecondary,
                LinesInputs = OESDarkSecondary,
                TextSecondary = OESDarkSecondaryText,
                AppbarBackground = OESDarkDrawerBackground,
                DrawerBackground = OESDarkDrawerBackground,
                DrawerText = OESDarkPrimaryText,
                TextPrimary = OESDarkPrimaryText,
                Success = OESDarkSuccessText,
                HoverOpacity = 0.08,
                Background = OESDarkAppbarBackground,
                Info = new MudColor("#99e2ff"),
                Surface = new MudColor("#1e1e2f"),  // Surface elements slightly lighter than the background
                Divider = new MudColor("#343a40"),  // Dark Gray Divider
                ActionDefault = new MudColor("#82cfff"),  // Primary action buttons
                ActionDisabled = new MudColor("#6c757d"),  // Muted Gray
                ActionDisabledBackground = new MudColor("#3c4043"),  // Slightly lighter gray for disabled backgrounds
                Tertiary = OESDarkTerriratyColor,
                TextDisabled = OESDarkPrimaryText,
            },
            LayoutProperties = new LayoutProperties()
            {
                DrawerWidthLeft = "260px",
                DrawerWidthRight = "300px"
            }
        };

        // Light Theme Object without Typography (will be set later)
        private MudTheme OESTheme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = OESPrimary,
                Secondary = Colors.Gray.Darken2,
                AppbarBackground = OESbarBackground,
                DrawerBackground = OESDrawerBackground,
                DrawerText = OESSecondary,
                TextPrimary = OESPrimaryText,
                Success = OESSuccessText,
                HoverOpacity = 0.06,
                Tertiary = OESTerriratyColor,
                Info = new MudColor("#005b80"),
            },
            PaletteDark = new PaletteDark()
            {
                Primary = Colors.Blue.Lighten1
            },
            LayoutProperties = new LayoutProperties()
            {
                DrawerWidthLeft = "260px",
                DrawerWidthRight = "300px"
            }
        };

        #endregion Theme Work
    }
}