using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OES.Blazor.Services.Interfaces.AppUser;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.OesResources;
using OES.Helper.Enums;

namespace OES.Blazor.Dialogs.User;

public partial class UserAccessDialog : ComponentBase
{
    [Inject] private IBlazUserProfileService BlazUserProfileService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [CascadingParameter] private MudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public Guid UserId { get; set; }

    private const string FlexCenterClass = "d-flex align-center gap-2";
    private const string FlexWrapClass = "d-flex flex-wrap gap-2 pa-2";

    private readonly Dictionary<ResourceType, List<UserAccessByResourceDto>> _accessData = [];
    private bool _isLoading = true;
    private long? _highlightedResourceId;
    private ResourceType? _highlightedType;
    private string _searchKey = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadAccessDataAsync();
    }

    private async Task LoadAccessDataAsync()
    {
        _isLoading = true;

        var resourceTypes = new[]
        {
            ResourceType.Questions,
            ResourceType.Papers,
            ResourceType.Schedule,
            ResourceType.ItemBank,
            ResourceType.Ilo,
            ResourceType.Block
        };

        var tasks = resourceTypes.Select(async type =>
        {
            var data = await BlazUserProfileService.GetUserAccessByResourceTypeAsync(UserId, type);
            return (type, data);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (type, data) in results)
        {
            _accessData[type] = data;
        }

        _isLoading = false;
    }

    private void OnSearchChanged(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _searchKey = string.Empty;
            StateHasChanged();
        }
    }

    private void OnSearch()
    {
        StateHasChanged();
    }

    private void OnResetSearch()
    {
        _searchKey = string.Empty;
        StateHasChanged();
    }

    private async Task NavigateToParentAsync(
        UserAccessByResourceDto currentGroup,
        ResourceAccessDto resource,
        ResourceType type)
    {
        if (resource.ParentId == null) return;

        var parentInSameGroup = currentGroup.Resources?.Any(r => r.ResourceId == resource.ParentId.Value) == true;

        if (parentInSameGroup)
        {
            _highlightedResourceId = resource.ParentId.Value;
            _highlightedType = type;
            StateHasChanged();

            await Task.Delay(50);
            await JS.InvokeVoidAsync("scrollToElement", $"resource_{type}_{resource.ParentId.Value}");
            await Task.Delay(2500);

            _highlightedResourceId = null;
            _highlightedType = null;
            StateHasChanged();
        }
    }

    private void Close() => MudDialog.Close(DialogResult.Ok(true));
}