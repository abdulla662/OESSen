using OES.Helper.Dtos.Folder.Request;
using OES.Helper.Dtos.Folder.Response;
using OES.Helper.General.DocLibPaginatedList;
using OES.Helper.General.NewApiResponse;

namespace OES.Blazor.Services.Interfaces.FolderService
{
    public interface IBlazFolderService
    {
        Task<DocLibPaginatedList<FolderDocumentListResponseDto>> GetFoldersAndDocumentsListAsync(Guid? folderId,
                                                                                                 int pageNumber,
                                                                                                 CancellationToken cancellationToken = default);

        Task<DocLibPaginatedList<FolderDocumentListResponseDto>> SearchFolderAndDocumentListAsync(string searchKey = null,
                                                                                                  int pageNumber = 1,
                                                                                                  CancellationToken cancellationToken = default);

        Task<NewApiResponse<FolderUpdateResponseDto>> UpdateFolderNameAsync(FolderUpdateRequestDto folderUpdateRequestDto, CancellationToken cancellationToken = default);

        Task<NewApiResponse<FolderCreationResponseDto>> CreateNewFolderAsync(FolderCreationRequestDto folderCreationRequestDto);

        Task<NewApiResponse<object>> DeleteFolderAsync(Guid folderId, CancellationToken cancellationToken = default);
    }
}
