using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using MudBlazor;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Helper.Dtos.User;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using SharedHelper.General;
using System.Text;
using System.Text.Json;

namespace OES.Blazor.Pages.Login
{
    public partial class Login : ComponentBase
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private IBlazAuthService BlazAuthService { get; set; } = default!;
        [Inject] private IWebAssemblyHostEnvironment WasmHostEnvironment { get; set; } = default!;

        private string PasswordInputIcon = Icons.Material.Filled.VisibilityOff;
        private readonly string token = nameof(token);
        private bool _showOrganizationsDropdown = false;
        private bool Showed = false;
        private bool _isFormBasedLoginProcessing = false;
        private bool _isSamlBasedLoginProcessing = false;
        private bool _isSamlCompletion = false;

        private List<OrganizationSelectionListDto> _organizations = [];
        private LoginDto _loginDto = new();
        private OrganizationSelectionListDto _selectedOrganization;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
        InputType PasswordInput = InputType.Password;

        // Tree-related fields
        private string _searchText = string.Empty;
        private List<OrganizationTreeNodeDto> _treeNodes = [];
        private List<OrganizationTreeNodeDto> _filteredTreeNodes = [];
        private bool _isExpanded = true;

        private bool ShowLogin => WasmHostEnvironment.IsDevelopment();


        protected override async Task OnInitializedAsync()
        {
            await InitializeFromQuery();
        }

        private async Task InitializeFromQuery()
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);

            var queryParams = QueryHelpers.ParseQuery(uri.Query);

            if (queryParams.TryGetValue("data", out var dataVal))
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(dataVal));

                _organizations = JsonSerializer.Deserialize<List<OrganizationSelectionListDto>>(json, _jsonSerializerOptions) ?? [];
            }

            if (queryParams.TryGetValue("status", out var statusVal) && Enum.TryParse<CustomCodeStatus>(statusVal!, ignoreCase: true, out var statusCode))
            {
                if (statusCode == CustomCodeStatus.Success)
                {
                    await HandleSuccess(queryParams);
                }
                else if (statusCode == CustomCodeStatus.UserHasMultipleOrganizations)
                {
                    if (_organizations.Count > 0)
                    {
                        _treeNodes = BuildTree(_organizations);
                        _filteredTreeNodes = _treeNodes;
                        _isSamlCompletion = true;
                        _showOrganizationsDropdown = true;
                    }
                    else
                    {
                        Snackbar.Add(Resource.ErrorNoOrganizationsAvailable, Severity.Error);
                    }
                }
                else
                {
                    HandleResponse(statusCode);
                }
            }
        }

        private async Task LogInAsync()
        {
            _isFormBasedLoginProcessing = true;
            _isSamlBasedLoginProcessing = false;

            if (_isSamlCompletion)
            {
                if (_selectedOrganization != null)
                {
                    BlazAuthService.CompleteSamlLogin(_selectedOrganization.Id, CentralizedUrlHelper.OesModuleName);
                }
            }
            else
            {
                if (_showOrganizationsDropdown && _selectedOrganization?.Id > 0)
                {
                    _loginDto.OrganizationId = _selectedOrganization.Id;
                }

                _loginDto.ModuleName = CentralizedUrlHelper.OesModuleName;

                var response = await BlazAuthService.LogInAsync(_loginDto);

                if (response != null)
                {
                    if (response.CustomCodeStatus == CustomCodeStatus.UserHasMultipleOrganizations)
                    {
                        HandleMultipleOrganizations(response.Data);
                    }
                    else if (response.CustomCodeStatus == CustomCodeStatus.Success && !string.IsNullOrWhiteSpace(response.Data?.ToString()))
                    {
                        if (!string.IsNullOrWhiteSpace(response.Data?.ToString()))
                        {
                            var loginResponseDto = JsonSerializer.Deserialize<LoginResponseDto>(response.Data.ToString()!, _jsonSerializerOptions);

                            if (loginResponseDto is not null)
                            {
                                var bearerToken = loginResponseDto.Token.Replace("\"", "");

                                await BlazAuthService.SaveEncryptedTokenToLocalStorageAsync(bearerToken);

                                NavigationManager.NavigateTo("/");
                            }
                            else
                            {
                                Snackbar.Add(Resource.ErrorFailedToLoginTryAgain, Severity.Error);
                            }
                        }
                        else
                        {
                            Snackbar.Add(Resource.ErrorFailedToLoginTryAgain, Severity.Error);
                        }
                    }
                    else
                    {
                        HandleResponse(response.CustomCodeStatus);
                    }
                }
            }

            _isFormBasedLoginProcessing = false;
        }

        private void LoginWithMicrosoft()
        {
            _isFormBasedLoginProcessing = false;
            _isSamlBasedLoginProcessing = true;

            StateHasChanged();

            BlazAuthService.LoginWithSaml();
        }

        private void HandleMultipleOrganizations(object data)
        {
            if (data is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                _organizations = JsonSerializer.Deserialize<List<OrganizationSelectionListDto>>(data.ToString() ?? string.Empty, _jsonSerializerOptions) ?? [];

                if (_organizations.Count > 0)
                {
                    _treeNodes = BuildTree(_organizations);
                    _filteredTreeNodes = _treeNodes;
                    _showOrganizationsDropdown = true;
                }
                else
                {
                    Snackbar.Add(Resource.ErrorNoOrganizationsAvailable, Severity.Error);
                }
            }
            else
            {
                Snackbar.Add(Resource.ErrorFormatOfPassedDataIsInvalid, Severity.Error);
            }
        }

        private void HandleResponse(CustomCodeStatus customCodeStatus)
        {
            switch (customCodeStatus)
            {
                case CustomCodeStatus.UserNameOrPasswordNotValid:
                    Snackbar.Add(Resource.ErrorUsernameOrPasswordIsNotValid, Severity.Error);
                    break;
                case CustomCodeStatus.LocalAccessUserDenied:
                    Snackbar.Add(Resource.UserLocalAccessDenied, Severity.Error);
                    break;
                case CustomCodeStatus.NoActiveOrganizations:
                    Snackbar.Add(Resource.ErrorNoActiveOrganizationsForThisUser);
                    break;
                case CustomCodeStatus.SubscriptionNotFound:
                    Snackbar.Add(Resource.ErrorThisSubscriptionIsNotAvailableForYourOrganization);
                    break;
                case CustomCodeStatus.UnableToLogin:
                    Snackbar.Add(Resource.ErrorFailedToLoginTryAgain);
                    break;
                case CustomCodeStatus.SomethingWentWrong:
                    Snackbar.Add(Resource.ErrorSomethingWentWrongTryAgain, Severity.Error);
                    break;
                case CustomCodeStatus.SamlNotAuthenticated:
                    Snackbar.Add(Resource.SamlNotAuthenticated);
                    break;
                case CustomCodeStatus.NotFound:
                    Snackbar.Add(Resource.UserNotRegistered);
                    break;
                case CustomCodeStatus.Forbidden:
                    Snackbar.Add(Resource.NotSamlUser);
                    break;
                case CustomCodeStatus.AccountExpired:
                    Snackbar.Add(Resource.AccountExpired, Severity.Error);
                    break;
                case CustomCodeStatus.NotActiveUser:
                    Snackbar.Add(Resource.UserNotActiveInThisOrganization, Severity.Error);
                    break;
                case CustomCodeStatus.UserLockedOut:
                    Snackbar.Add(Resource.UserLockedOut, Severity.Error);
                    break;
                default:
                    Snackbar.Add(Resource.AnUnExpectedErrorOccurred, Severity.Error);
                    break;
            }

            _isFormBasedLoginProcessing = false;
            _isSamlBasedLoginProcessing = false;
        }

        private async Task HandleSuccess(Dictionary<string, StringValues> query)
        {
            if (query.TryGetValue("token", out var _token))
            {
                await BlazAuthService.SaveEncryptedTokenToLocalStorageAsync(_token!);
                NavigationManager.NavigateTo("/", replace: true);
            }
        }

        void TogglePasswordVisibility()
        {
            if (Showed)
            {
                Showed = false;
                PasswordInputIcon = Icons.Material.Filled.VisibilityOff;
                PasswordInput = InputType.Password;
            }
            else
            {
                Showed = true;
                PasswordInputIcon = Icons.Material.Filled.Visibility;
                PasswordInput = InputType.Text;
            }
        }

        #region Tree Methods

        private void ToggleExpandCollapse()
        {
            _isExpanded = !_isExpanded;
            SetExpandState(_filteredTreeNodes, _isExpanded);
        }

        private List<OrganizationTreeNodeDto> BuildTree(List<OrganizationSelectionListDto> organizations)
        {
            var allNodes = organizations.ConvertAll(o => new OrganizationTreeNodeDto
            {
                Id = o.Id,
                Name = o.Name,
                ParentId = o.ParentId,
                IsExpanded = true
            });

            allNodes
                .Where(n => n.ParentId.HasValue)
                .ToList()
                .ForEach(node =>
                {
                    var parent = allNodes.FirstOrDefault(n => n.Id == node.ParentId!.Value);
                    parent?.Children.Add(node);
                });

            var rootNodes = allNodes
                .Where(n => !n.ParentId.HasValue || !allNodes.Any(p => p.Id == n.ParentId.Value))
                .OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            rootNodes.ForEach(SortChildrenRecursively);

            return rootNodes;
        }

        private void SortChildrenRecursively(OrganizationTreeNodeDto node)
        {
            if (node.Children.Count == 0) return;

            node.Children = [.. node.Children.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)];

            node.Children.ForEach(SortChildrenRecursively);
        }

        private void SelectOrganization(long id)
        {
            _selectedOrganization = _organizations.FirstOrDefault(o => o.Id == id);
            StateHasChanged();
        }

        private void ToggleNode(OrganizationTreeNodeDto node)
        {
            node.IsExpanded = !node.IsExpanded;
            StateHasChanged();
        }

        private void OnSearchInput(ChangeEventArgs e)
        {
            _searchText = e.Value?.ToString() ?? string.Empty;

            _filteredTreeNodes = string.IsNullOrWhiteSpace(_searchText)
                ? _treeNodes
                : [.. GetAllNodes(_treeNodes)
                    .Where(n => n.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    .Select(n => new OrganizationTreeNodeDto
                    {
                        Id = n.Id,
                        Name = n.Name,
                        ParentId = n.ParentId,
                        IsExpanded = false,
                        Children = []
                    })
                ];
        }

        private static IEnumerable<OrganizationTreeNodeDto> GetAllNodes(List<OrganizationTreeNodeDto> nodes) => nodes.SelectMany(n => new[] { n }.Concat(GetAllNodes(n.Children)));

        private static void SetExpandState(List<OrganizationTreeNodeDto> nodes, bool expanded) => GetAllNodes(nodes).ToList().ForEach(n => n.IsExpanded = expanded);

        #endregion
    }
}
