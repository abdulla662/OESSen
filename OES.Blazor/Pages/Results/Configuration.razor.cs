using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Results
{
    public partial class Configuration : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        // Auto Generate Results
        private bool _autoGenerateEnabledField = false;
        private TimeSpan? _autoGenerateTime = null;

        // Auto Send to CTR
        private bool _autoSendToCTREnabledField = false;
        private TimeSpan? _autoSendToCTRTime = null;

        // Email Notifications
        private string _emailList = "";
        private bool _notifyOnManualGeneration = false;
        private bool _notifyOnAutoGeneration = false;
        private bool _notifyOnManualSendToCTR = false;
        private bool _notifyOnAutoSendToCTR = false;
        private bool _notifyOnReviewSendToCTR = false;

        // CTR Folder Configuration
        private string _ctrFolderUrl = "";
        private string _ctrUsername = "";
        private string _ctrPassword = "";

        // Password visibility
        private bool _isPasswordVisible = false;
        private InputType _passwordInput = InputType.Password;
        private string _passwordIcon = Icons.Material.Filled.VisibilityOff;

        private void LoadConfiguration()
        {
            // TODO: Load configuration from backend
            // For now, initialize with default values
            _autoGenerateEnabledField = false;
            _autoGenerateTime = new TimeSpan(2, 0, 0); // 2:00 AM
            _autoSendToCTREnabledField = false;
            _autoSendToCTRTime = new TimeSpan(3, 0, 0); // 3:00 AM
            _emailList = "";
            _notifyOnManualGeneration = false;
            _notifyOnAutoGeneration = false;
            _notifyOnManualSendToCTR = false;
            _notifyOnAutoSendToCTR = false;
            _notifyOnReviewSendToCTR = false;
            _ctrFolderUrl = "";
            _ctrUsername = "";
            _ctrPassword = "";
        }

        private void SaveConfiguration()
        {
            // Validation
            if (_autoSendToCTREnabledField && _autoGenerateEnabledField)
            {
                if (_autoSendToCTRTime.HasValue && _autoGenerateTime.HasValue && _autoSendToCTRTime <= _autoGenerateTime)
                {
                    Snackbar.Add(Resource.AutoSendTimeMustBeAfterAutoGenerateTime, Severity.Warning);
                    return;
                }
            }

            // TODO: Save configuration to backend
            Snackbar.Add(Resource.ConfigurationSavedSuccessfully, Severity.Success);
        }

        private void ResetConfiguration()
        {
            LoadConfiguration();
            Snackbar.Add(Resource.ConfigurationReset, Severity.Info);
        }

        private void TogglePasswordVisibility()
        {
            if (_isPasswordVisible)
            {
                _isPasswordVisible = false;
                _passwordInput = InputType.Password;
                _passwordIcon = Icons.Material.Filled.VisibilityOff;
            }
            else
            {
                _isPasswordVisible = true;
                _passwordInput = InputType.Text;
                _passwordIcon = Icons.Material.Filled.Visibility;
            }
        }
    }
}
