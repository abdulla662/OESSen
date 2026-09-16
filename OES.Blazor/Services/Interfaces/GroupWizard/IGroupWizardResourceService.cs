using MudBlazor;
using OES.Blazor.Models.RuleMatrix;
using OES.Helper.Enums;

namespace OES.Blazor.Services.Interfaces.GroupWizard
{
    public interface IGroupWizardResourceService
    {
        bool IsTreeResource(ResourceType resourceType);

        bool IsConfigOnlyResource(ResourceType resourceType);

        Task<TableData<ResourceInstanceItem>> FetchPageAsync(ResourceType resourceType, TableState state, string searchKey);

        Task<TableData<ResourceInstanceItem>> LoadRootsPageAsync(ResourceType resourceType, int page, int pageSize, string? searchKey = null);

        Task<List<ResourceInstanceItem>> LoadChildrenAsync(ResourceType resourceType, long parentId, string? signature = null);

        Task<List<long>> GetAssignedItemIdsAsync(ResourceType resourceType, Guid groupId);

        Task AssignGroupToItemsAsync(ResourceType resourceType, Guid groupId, List<long> selectedIds);
    }
}