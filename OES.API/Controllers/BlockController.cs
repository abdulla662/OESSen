using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.Block.Requests;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class BlockController : OESBaseController
    {
        private readonly IBlockService _blockService;


        public BlockController(IBlockService blockService)
        {
            _blockService = blockService;
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllBlockPaginatedAsync")]
        public async Task<ApiResponse> GetAllBlockPaginatedAsync(long? languageId, PaginationSearchModel paginationSearchModel)
        {
            return await _blockService.GetAllBlockPaginatedAsync(languageId, paginationSearchModel);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetPaperBlocksByPaperId")]
        public async Task<ApiResponse> GetPaperBlocksByPaperIdAsync(long paperId)
        {
            return await _blockService.GetPaperBlocksByPaperIdAsync(paperId);
        }


        [HttpGet("GetBlockDataById")]
        [OESFilter(Authorize = true)]
        public async Task<ApiResponse> GetBlockDataById(long id)
        {
            return await _blockService.GetBlockDataById(id);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetStagesBlocksByPaperIdAsync")]
        public async Task<ApiResponse> GetStagesBlocksByPaperIdAsync(long paperId)
        {
            return await _blockService.GetStagesBlocksByPaperIdAsync(paperId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("CreateOrUpdatePaperBlockDistribution")]
        public async Task<ApiResponse> CreateOrUpdatePaperBlockDistribution(PaperStagesBlocksDto paperStagesBlocksDto)
        {
            return await _blockService.CreateOrUpdatePaperBlockDistributionAsync(paperStagesBlocksDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("AddOrUpdatePaperBlocks")]
        public async Task<ApiResponse> AddOrUpdatePaperBlocksAsync(PaperBlockCreationDto paperBlockCreationDto)
        {
            return await _blockService.AddOrUpdatePaperBlocksAsync(paperBlockCreationDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("AddNewBlock")]
        public async Task<ApiResponse> AddNewBlock(CreateOrUpdateBlockRequestDto blockDto)
        {
            return await _blockService.AddNewBlock(blockDto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("EditBlock")]
        public async Task<ApiResponse> EditBlock(CreateOrUpdateBlockRequestDto blockDto)
        {
            return await _blockService.EditBlock(blockDto);
        }


        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteBlockAsync")]
        public async Task<ApiResponse> DeleteBlockAsync(long id)
        {
            return await _blockService.DeleteBlockAsync(id);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetUserBlockGroupsAsync")]
        public async Task<ApiResponse> GetUserBlockGroupsAsync()
        {
            return await _blockService.GetUserBlockGroupsAsync();
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetBlockGroupsAsync")]
        public async Task<IApiResponse> GetBlockGroupsAsync(long blockId)
        {
            return await _blockService.GetBlockGroupsAsync(blockId);
        }
    }
}