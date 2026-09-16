using OES.Blazor.Services.Implementation.DocLibHttpClientHelperService;
using OES.Blazor.Services.Interfaces.FolderService;
using OES.Helper.Dtos.Folder.Request;
using OES.Helper.Dtos.Folder.Response;
using OES.Helper.General.DocLibPaginatedList;
using OES.Helper.General.NewApiResponse;

namespace OES.Blazor.Services.Implementation.FolderService
{
    public class BlazFolderService : IBlazFolderService
    {
        private readonly DocLibHttpClientHelper _httpClientHelper;

        public BlazFolderService(DocLibHttpClientHelper httpClientHelper)
        {
            _httpClientHelper = httpClientHelper;
        }

        public async Task<DocLibPaginatedList<FolderDocumentListResponseDto>> GetFoldersAndDocumentsListAsync(Guid? folderId,
                                                                                                              int pageNumber,
                                                                                                              CancellationToken cancellationToken = default)
        {
            var apiResponse = await _httpClientHelper.GetFromJsonAsync<NewApiResponse<DocLibPaginatedList<FolderDocumentListResponseDto>>>($"Folder/GetFoldersAndDocumentsList?folderId={folderId}&pageNumber={pageNumber}");

            if (apiResponse.Success)
            {
                return apiResponse.Data;
            }
            else
            {
                return DocLibPaginatedList<FolderDocumentListResponseDto>.Empty;
            }
        }

        public async Task<DocLibPaginatedList<FolderDocumentListResponseDto>> SearchFolderAndDocumentListAsync(string searchKey = null,
                                                                                                               int pageNumber = 1,
                                                                                                               CancellationToken cancellationToken = default)
        {
            var apiResponse = await _httpClientHelper
                .GetFromJsonAsync<NewApiResponse<DocLibPaginatedList<FolderDocumentListResponseDto>>>($"Folder/Search?pageNumber={pageNumber}&searchKey={searchKey}");

            if (apiResponse.Success)
            {
                return apiResponse.Data;
            }
            else
            {
                return DocLibPaginatedList<FolderDocumentListResponseDto>.Empty;
            }
        }


        public async Task<NewApiResponse<FolderUpdateResponseDto>> UpdateFolderNameAsync(FolderUpdateRequestDto folderUpdateRequestDto, CancellationToken cancellationToken = default)
        {
            var response = await _httpClientHelper.PutAsJsonAsync<FolderUpdateRequestDto, NewApiResponse<FolderUpdateResponseDto>>($"Folder/UpdateFolderName", folderUpdateRequestDto);

            return response;
        }


        public async Task<NewApiResponse<FolderCreationResponseDto>> CreateNewFolderAsync(FolderCreationRequestDto folderCreationRequestDto)
        {
            var response = await _httpClientHelper.PostAsJsonAsync<FolderCreationRequestDto, NewApiResponse<FolderCreationResponseDto>>("Folder/CreateNewFolder", folderCreationRequestDto);

            return response;
        }


        public async Task<NewApiResponse<object>> DeleteFolderAsync(Guid folderId, CancellationToken cancellationToken = default)
        {
            return await _httpClientHelper.DeleteFromJsonAsync<NewApiResponse<object>>($"Folder/DeleteFolder?folderId={folderId}");
        }
    }
}
