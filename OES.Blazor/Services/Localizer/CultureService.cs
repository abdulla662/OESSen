using Blazored.LocalStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Globalization;


public class CultureService
{
    private readonly IOptions<RequestLocalizationOptions> _localizationOptions;
    private readonly NavigationManager _navigationManager;
    private readonly ILocalStorageService _localStorageService;
    private const string CultureKey = "appCulture";
    private readonly IStringLocalizer<CultureService> _localizer;

    public CultureService(IOptions<RequestLocalizationOptions> localizationOptions,
                          NavigationManager navigationManager,
                          ILocalStorageService localStorageService, IStringLocalizer<CultureService> stringLocalizer)
    {
        _localizationOptions = localizationOptions;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _localizer = stringLocalizer;
    }

    public void SetCultureAsync(string culture)
    {
        var cultureInfo = new CultureInfo(culture);

        // Save the selected culture in local storage
        _localStorageService.SetItemAsStringAsync(CultureKey, culture);

        // Update the culture for the current thread
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;
        Resource.Culture = cultureInfo;

        // Optionally reload the page to reflect the culture change
        _navigationManager.Refresh(true);

    }


    public async Task<string> GetCultureAsync()
    {
        // Retrieve the culture from local storage (default to 'en-US' if not found)
        return await _localStorageService.GetItemAsStringAsync(CultureKey) ?? LanguageCode.ENGLISH_CODE;

    }


    public string GetString(string key)
    {
        var localizedString = _localizer[key];
        return localizedString.ResourceNotFound ? $"{key}" : localizedString;
    }

}
