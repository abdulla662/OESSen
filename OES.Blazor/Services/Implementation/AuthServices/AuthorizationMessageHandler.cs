using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Headers;

namespace OES.Blazor.Services.Implementation.AuthServices
{
    public class AuthorizationMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly NavigationManager _navigationManager;
        private const string token = nameof(token);
        private const string Bearer = nameof(Bearer);

        public AuthorizationMessageHandler(ILocalStorageService localStorageService, NavigationManager navigationManager)
        {
            _localStorageService = localStorageService;
            _navigationManager = navigationManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var fetchedToken = await _localStorageService.GetItemAsStringAsync(token);

            if (!string.IsNullOrWhiteSpace(fetchedToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(Bearer, fetchedToken.Replace("\"", ""));
            }

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _localStorageService.RemoveItemAsync(token);

                _navigationManager.NavigateTo("/login");
            }

            return response;
        }
    }
}
