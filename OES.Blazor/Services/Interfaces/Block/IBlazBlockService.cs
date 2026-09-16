using OES.Helper.Dtos.Block.Requests;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.General;

namespace OES.Blazor.Services.Interfaces.Block
{
    public interface IBlazBlockService
    {
        Task<CustomTableData<GetBlockResponseDto>> GetAllBlockPaginatedAsync(long? languageId, PaginationSearchModel paginationSearch);

        Task<List<GetPaperBlockResponseDto>> GetPaperBlocksByPaperIdAsync(long paperId);

        Task<GetBlockResponseDto> GetBlockDataById(long id);

        Task<ApiResponse> GetStagesBlocksByPaperIdAsync(long paperId);

        Task<ApiResponse> CreateOrUpdatePaperBlockDistributionAsync(PaperStagesBlocksDto paperStagesBlocksDto);

        Task<ApiResponse> AddOrUpdatePaperBlocksAsync(PaperBlockCreationDto paperBlockCreationDto);

        Task<ApiResponse> AddNewBlock(CreateOrUpdateBlockRequestDto blockDto);

        Task<ApiResponse> EditBlock(CreateOrUpdateBlockRequestDto blockDto);

        Task<ApiResponse> DeleteBlockAsync(long id);

        Task<List<GetOESGroupDto>> GetUserBlockGroupsAsync();

        Task<BlockGroupsDto> GetBlockGroupsAsync(long blockId);
    }
}
