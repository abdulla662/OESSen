using OES.Blazor.Services.Interfaces;
using OES.Blazor.Services.Interfaces.Block;
using OES.Blazor.Services.Interfaces.Common;
using OES.Helper.Dtos.Block.Requests;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.General;
using SharedHelper.General;
using System.Net.Http.Json;

namespace OES.Blazor.Services.Implementation.Block
{
    public class BlazBlockService(IHttpClientHelper _httpClientHelper, IBlazGetCustomTableData<GetBlockResponseDto> _blazGetQuestionPaginationDto) : IBlazBlockService
    {
        public async Task<CustomTableData<GetBlockResponseDto>> GetAllBlockPaginatedAsync(long? languageId, PaginationSearchModel paginationSearch)
        {
            return await _blazGetQuestionPaginationDto.GetCustomTableData(paginationSearch, $"api/Block/GetAllBlockPaginatedAsync?{nameof(languageId)}={languageId}");
        }

        public async Task<List<GetPaperBlockResponseDto>> GetPaperBlocksByPaperIdAsync(long paperId)
        {
            var response = await _httpClientHelper.GetAsync<List<GetPaperBlockResponseDto>>($"api/Block/GetPaperBlocksByPaperId?{nameof(paperId)}={paperId}");

            return (List<GetPaperBlockResponseDto>)response.Data ?? [];
        }

        public async Task<ApiResponse> GetStagesBlocksByPaperIdAsync(long paperId)
        {
            return await _httpClientHelper.GetAsync<PaperStagesBlocksDto>($"api/Block/GetStagesBlocksByPaperIdAsync?paperId={paperId}");
        }

        public async Task<GetBlockResponseDto> GetBlockDataById(long id)
        {
            var response = await _httpClientHelper.GetAsync<GetBlockResponseDto>($"api/Block/GetBlockDataById?id={id}");

            return (GetBlockResponseDto)response.Data;
        }

        public async Task<ApiResponse> CreateOrUpdatePaperBlockDistributionAsync(PaperStagesBlocksDto paperStagesBlocksDto)
        {
            return await _httpClientHelper.PostAsync(paperStagesBlocksDto, "api/Block/CreateOrUpdatePaperBlockDistribution");
        }

        public async Task<ApiResponse> AddOrUpdatePaperBlocksAsync(PaperBlockCreationDto paperBlockCreationDto)
        {
            var response = await _httpClientHelper.PostAsync(paperBlockCreationDto, "api/Block/AddOrUpdatePaperBlocks");

            return response;
        }

        public async Task<ApiResponse> AddNewBlock(CreateOrUpdateBlockRequestDto blockDto)
        {
            return await _httpClientHelper.PostAsync(blockDto, "api/Block/AddNewBlock");
        }

        public async Task<ApiResponse> EditBlock(CreateOrUpdateBlockRequestDto blockDto)
        {
            return await _httpClientHelper.PostAsync(blockDto, "api/Block/EditBlock");
        }

        public async Task<ApiResponse> DeleteBlockAsync(long id)
        {
            var response = await _httpClientHelper._httpClient.DeleteAsync($"{CentralizedUrlHelper.OesApiBaseUrl}api/Block/DeleteBlockAsync?{nameof(id)}={id}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse>();
            }
            else
            {
                return new ApiResponse
                {
                    StatusCode = response.StatusCode,
                    Data = response,
                    Message = $"Error: {response.ReasonPhrase}"
                };
            }
        }

        public async Task<List<GetOESGroupDto>> GetUserBlockGroupsAsync()
        {
            var response = await _httpClientHelper.GetAsync<List<GetOESGroupDto>>("api/Block/GetUserBlockGroupsAsync");

            var data = (List<GetOESGroupDto>)response.Data;

            return data ?? [];
        }

        public async Task<BlockGroupsDto> GetBlockGroupsAsync(long blockId)
        {
            var response = await _httpClientHelper.GetAsync<BlockGroupsDto>($"api/Block/GetBlockGroupsAsync?blockId={blockId}");

            return response.Data as BlockGroupsDto ?? new BlockGroupsDto();
        }
    }
}