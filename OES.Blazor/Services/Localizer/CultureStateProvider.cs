using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using SharedHelper.General;

namespace OES.Blazor.Services.Localizer
{
    public class CultureStateProvider : ComponentBase
    {
        [Inject] private ILocalStorageService _localStorageService { get; set; } = default;
        private string _currentCulture = LanguageCode.ENGLISH_CODE;

        public string CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    NotifyStateChanged();
                }
            }
        }
        public event Action OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
