using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class ILOController : OESBaseController
    {
        private readonly IILOService _iloService;

        private readonly IPaginationSearchModel _paginationSearchModel;


        /// <summary>
        /// Initializes a new instance of the <see cref="ILOController"/> class.
        /// </summary>
        /// <param name="iloService">The ILO service.</param>
        /// <param name="paginationSearchModel">The paginated search model</param>
        public ILOController(IILOService iloService,
                             IPaginationSearchModel paginationSearchModel)
        {
            _iloService = iloService;
            _paginationSearchModel = paginationSearchModel;
        }


        /// <summary>
        /// Retrieves a specific ILO according to it's ID.
        /// </summary>
        /// <param name="id">The ILO ID</param>
        /// <returns>An API response containing the ILO.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("getById/{id}")]
        public async Task<IApiResponse> GetByIdAsync(long id)
        {
            return await _iloService.GetByIdAsync(id);
        }


        /// <summary>
        /// Retrieves a specific ILO Root according to it's ID.
        /// </summary>
        /// <param name="id">The ILO ID</param>
        /// <returns>An API response containing the root ILO.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetRootById/{id}")]
        public async Task<IApiResponse> GetRootById(long id)
        {
            return await _iloService.GetILORoot(id);
        }


        /// <summary>
        /// Retrieves paginated list of ILOs.
        /// </summary>
        /// <param name="paginationSearchModel">a search model for paginated list.</param>
        /// <returns>An API response containing a list of ILOs.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("GetPaged_ILOS")]
        public async Task<IApiResponse> GetPagedILOS(PaginationSearchModel paginationSearchModel)
        {
            return await _iloService.GetPagedILOS(paginationSearchModel);
        }


        /// <summary>
        /// Retrieves ILO items based on the parent ID and signature provided in the request DTO.
        /// </summary>
        /// <param name="iloRequestDto">The request DTO containing the parent ID and signature.</param>
        /// <returns>A list of ILO items if found, otherwise a bad request response.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("GetByParentIdAndSignature")]
        public async Task<ApiResponse> GetByParentIdAndSignatureAsync(TreeItemRequestDto iloRequestDto)
        {
            var items = await _iloService.GetByParentIdAndSignatureAsync(iloRequestDto);

            return new ApiResponse { StatusCode = System.Net.HttpStatusCode.OK, Data = items };
        }


        /// <summary>
        /// Adds a new root item to the ILO structure.
        /// </summary>
        /// <param name="iLONewRootDto">The DTO containing information about the new root item to be added.</param>
        /// <returns>An ApiResponse indicating the result of the addition operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("AddNewRoot")]
        public async Task<ApiResponse> AddNewRoot(ILONewRootDto iLONewRootDto)
        {
            return await _iloService.AddNewRoot(iLONewRootDto);
        }


        /// <summary>
        /// Inserts a new node into the ILO structure.
        /// </summary>
        /// <param name="iloRequestDto">The DTO containing information about the node to be inserted.</param>
        /// <returns>An IApiResponse indicating the result of the insertion operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("insertNode")]
        public async Task<IApiResponse> InsertNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto)
        {
            return await _iloService.InsertNodeAsync(iloRequestDto);
        }


        /// <summary>
        /// Edits the root of an ILO.
        /// </summary>
        /// <param name="IloDto">The DTO containing information about the ILO root to be edited.</param>
        /// <returns>An IApiResponse indicating the result of the edit operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("EditIloRoot")]
        public async Task<IApiResponse> EditIloRoot(IloEditDTO IloDto)
        {
            return await _iloService.EditILORoot(IloDto);
        }


        /// <summary>
        /// Edits an existing node in an ILO.
        /// </summary>
        /// <param name="iloRequestDto">The DTO containing information about the ILO node to be edited.</param>
        /// <returns>An IApiResponse indicating the result of the edit operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("editNode")]
        public async Task<IApiResponse> EditNodeAsync(ILOInsertionOrUpdateRequestDto iloRequestDto)
        {
            return await _iloService.EditNodeAsync(iloRequestDto);
        }


        /// <summary>
        /// Retrieves the all parents items of a specified ILO  node.
        /// </summary>
        /// <param name="parentId">The ID of the parent node to retrieve. If null, retrieves the top-level parent nodes.</param>
        /// <returns>An IApiResponse containing the list of parent items.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("getParents")]
        public async Task<IApiResponse> GetParentsAsync(long? parentId)
        {
            return await _iloService.GetParentsAsync(parentId);
        }


        /// <summary>
        /// Deletes the root ILO with the specified ID (only with id "node only").
        /// </summary>
        /// <param name="id">The ID of the root ILO to be deleted.</param>
        /// <returns>An IApiResponse indicating the result of the delete operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpDelete("softDeleteRoot")]
        public async Task<IApiResponse> SoftDeleteILORoot(long id)
        {
            return await _iloService.SoftDeleteRootAsync(id);
        }


        /// <summary>
        /// Deletes the root ILO and its (children) with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the root ILO to be deleted along with its children.</param>
        /// <returns>An IApiResponse indicating the result of the delete operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpDelete("SoftDeleteRootWithChildern")]
        public async Task<IApiResponse> SoftDeleteRootWithChildrenAsync(long id)
        {
            return await _iloService.SoftDeleteRootWithChildrenAsync(id);
        }


        /// <summary>
        /// Checks if the specified ILO can be deleted.
        /// </summary>
        /// <param name="itemId">The ID of the ILO to check for delete capability.</param>
        /// <returns>An IApiResponse indicating whether the ILO can be deleted.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("canSoftDeleteIlo")]
        public async Task<IApiResponse> CanSoftDeleteIloAsync(long itemId)
        {
            return await _iloService.CanSoftDeleteIloAsync(itemId);
        }


        /// <summary>
        /// Executes a delete for the specified ILO node.
        /// </summary>
        /// <param name="itemId">The ID of the ILO node to be deleted.</param>
        /// <returns>An IApiResponse indicating the result of the delete operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("executeSoftDeleteForNode")]
        public async Task<IApiResponse> ExecuteSoftDeleteForNodeAsync(long itemId)
        {
            return await _iloService.ExecuteSoftDeleteForNodeAsync(itemId);
        }


        /// <summary>
        /// Transfers the children of the specified node to another node.
        /// </summary>
        /// <param name="itemId">The ID of the ILO node whose children are to be transferred.</param>
        /// <returns>An IApiResponse indicating the result of the transfer operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("transferChildrenForNode")]
        public async Task<IApiResponse> TransferChildrenForNodeAsync(long itemId)
        {
            return await _iloService.TransferChildrenForNodeAsync(itemId);
        }


        /// <summary>
        /// Retrieves the root-level ILOs from the database.
        /// </summary>
        /// <returns>
        /// An asynchronous task that resolves to an <see cref="IApiResponse"/> containing the root node ILOs.
        /// </returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetRootILO")]
        public async Task<IApiResponse> GetRootItemBanks()
        {
            return await _iloService.GetRootNodeILO();
        }


        /// <summary>
        /// Retrieves the ILO groups associated with a specific ILO.
        /// </summary>
        /// <param name="iloId">The ID of the ILO for which to retrieve the ILO groups.</param>
        /// <returns>An IApiResponse containing the list of ILO groups associated with the specified ILO.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetIloGroups")]
        public async Task<IApiResponse> GetIloGroupsAsync(long iloId)
        {
            return await _iloService.GetIloGroupsAsync(iloId);
        }


        /// <summary>
        /// Retrieves all OESGroups that are linked to any Ilo (via IloGroups).
        /// </summary>
        /// <returns>An API response containing the linked groups.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetUserIloGroupAsync")]
        public async Task<ApiResponse> GetUserIloGroupAsync()
        {
            return await _iloService.GetUserIloGroupAsync();
        }
    }
}