using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Helper.General;

namespace OES.Blazor.Pages.TokenHandler
{
    public partial class TokenHandler : ComponentBase
    {
        [Inject] public IBlazAuthService BlazAuthService { get; set; } = default!;

        [Inject] public NavigationManager NavigationManager { get; set; } = default!;

        [Inject] public ILocalStorageService LocalStorageService { get; set; } = default!;

        private StringValues Token { get; set; }

        private StringValues Culture { get; set; }

        protected override async Task OnInitializedAsync()
        {
            HandleQueryStrings();

            await BlazAuthService.SaveEncryptedTokenToLocalStorageAsync(Token.ToString());

            // Apply culture from SSO if provided
            if (!string.IsNullOrEmpty(Culture.ToString()))
            {
                await LocalStorageService.SetItemAsStringAsync(MiscConstants.AppCulture, Culture.ToString());
                NavigationManager.NavigateTo("/");
                return;
            }

            NavigationManager.NavigateTo("/");
        }

        private void HandleQueryStrings()
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

            var queryStrings = QueryHelpers.ParseQuery(uri.Query);

            if (queryStrings.TryGetValue("token", out var token))
            {
                Token = token;
            }

            if (queryStrings.TryGetValue("culture", out var culture))
            {
                Culture = culture;
            }
        }
    }
}
