using OES.Helper.Dtos.Block.Requests;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.General;

namespace OES.Interface.Interfaces
{
    public interface IBlockService
    {
        Task<ApiResponse> GetAllBlockPaginatedAsync(long? languageId, PaginationSearchModel paginationSearchModel);

        Task<ApiResponse> GetBlockDataById(long id);

        Task<ApiResponse> GetPaperBlocksByPaperIdAsync(long paperId);

        Task<ApiResponse> GetStagesBlocksByPaperIdAsync(long paperId);

        Task<ApiResponse> AddNewBlock(CreateOrUpdateBlockRequestDto blockDto);

        Task<ApiResponse> EditBlock(CreateOrUpdateBlockRequestDto blockDto);

        Task<ApiResponse> DeleteBlockAsync(long id);

        Task<ApiResponse> AddOrUpdatePaperBlocksAsync(PaperBlockCreationDto paperBlockCreationDto);

        Task<ApiResponse> CreateOrUpdatePaperBlockDistributionAsync(PaperStagesBlocksDto paperStagesBlocksDto);

        Task<ApiResponse> GetUserBlockGroupsAsync();

        Task<ApiResponse> GetBlockGroupsAsync(long blockId);
    }
}
