using Microsoft.AspNetCore.Mvc;
using OES.API.Filters;
using OES.Helper.Dtos.AIItemBankGenerator.Request;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;

namespace OES.API.Controllers
{
    public class ItemBankController(IItemBankService _itemBankService) : OESBaseController
    {
        /// <summary>
        /// Retrieves an ItemBank by its ID.
        /// </summary>
        /// <param name="id">The ID of the ItemBank.</param>
        /// <returns>An API response with the ItemBank details.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("getById/{id}")]
        public async Task<IApiResponse> GetByIdAsync(long id)
        {
            return await _itemBankService.GetByIdAsync(id);
        }


        /// <summary>
        /// Updates an existing ItemBank root.
        /// </summary>
        /// <param name="bankDTO">The DTO containing the updated details of the ItemBank.</param>
        /// <returns>An API response indicating the result of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("UpdateItemBank")]
        public async Task<IApiResponse> Update(NewItemBankDTO bankDTO)
        {
            return await _itemBankService.UpdateItemBankAsync(bankDTO);
        }


        /// <summary>
        /// Retrieves an ItemBank by its ID.
        /// </summary>
        /// <param name="Id">The ID of the ItemBank.</param>
        /// <returns>An API response with the ItemBank object details.</returns>
        ///
        [OESFilter(Authorize = true)]
        [HttpGet("GetItemBankByID")]
        public async Task<IApiResponse> GetItemBankByID(long Id)
        {
            return await _itemBankService.GetItemBankByIDAsync(Id);
        }


        /// <summary>
        /// Retrieves all ItemBanks with pagination and search options.
        /// </summary>
        /// <param name="paginationModel">The pagination and search parameters.</param>
        /// <returns>An API response with the list of ItemBanks.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("GetAllAsync")]
        public async Task<ApiResponse> GetAllAsync(PaginationSearchModel paginationModel)
        {
            return await _itemBankService.GetAllItemBankAsync(paginationModel);
        }


        /// <summary>
        /// Retrieves ItemBanks by parent ID and signature.
        /// </summary>
        /// <param name="itemBankRequestDto">The request DTO containing parent ID and signature.</param>
        /// <returns>A list of ItemBanks that match the criteria.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("getByParentIdAndSignature")]
        public async Task<ApiResponse> GetByParentIdAndSignatureAsync(TreeItemRequestDto itemBankRequestDto)
        {
            var items = await _itemBankService.GetByParentIdAndSignatureAsync(itemBankRequestDto);

            return new ApiResponse { StatusCode = System.Net.HttpStatusCode.OK, Data = items };
        }


        /// <summary>
        /// Checks if an ItemBank can be deleted.
        /// </summary>
        /// <param name="itemId">The ID of the ItemBank.</param>
        /// <returns>An API response indicating whether the ItemBank can be deleted.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("canSoftDeleteItemBank")]
        public async Task<IApiResponse> CanSoftDeleteItemBankAsync(long itemId)
        {
            return await _itemBankService.CanSoftDeleteItemBankAsync(itemId);
        }


        /// <summary>
        /// Executes a delete for an ItemBank node.
        /// </summary>
        /// <param name="itemId">The ID of the ItemBank node.</param>
        /// <returns>An API response indicating the result of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("executeSoftDeleteForNode")]
        public async Task<IApiResponse> ExecuteSoftDeleteForNodeAsync(long itemId)
        {
            return await _itemBankService.ExecuteSoftDeleteForNodeAsync(itemId);
        }


        /// <summary>
        /// Deletes a root ItemBank.
        /// </summary>
        /// <param name="itemId">The ID of the root ItemBank.</param>
        /// <returns>An API response indicating the result of the operation.</returns>
        ///
        [OESFilter(Authorize = true)]
        [HttpGet("executeSoftDeleteForRoot")]
        public async Task<IApiResponse> ExecuteSoftDeleteForRootAsync(long itemId)
        {
            return await _itemBankService.ExecuteSoftDeleteForRootAsync(itemId);
        }


        /// <summary>
        /// Edits an existing ItemBank node.
        /// </summary>
        /// <param name="dto">The DTO containing the details of the ItemBank node to edit.</param>
        /// <returns>An API response indicating the result of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("EditNode")]
        public async Task<ApiResponse> EditNode(EditItemBankNodeDto dto)
        {
            return await _itemBankService.EditItemBankNode(dto);
        }


        /// <summary>
        /// Retrieves an ItemBank node by its ID for editing.
        /// </summary>
        /// <param name="Id">The ID of the ItemBank node.</param>
        /// <returns>An API response with the ItemBank node details.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetEditNodeObject")]
        public async Task<ApiResponse> GetEditNodeObject(long Id)
        {
            return await _itemBankService.GetItemBankNodeObject(Id);
        }


        /// <summary>
        /// Retrieves parent ItemBanks for editing a node.
        /// </summary>
        /// <param name="Id">The ID of the ItemBank node.</param>
        /// <returns>An API response with the list of parent ItemBanks.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetParentsForEditNodeObject")]
        public async Task<ApiResponse> GetParentsForEditNodeObject(long Id)
        {
            return await _itemBankService.GetParentsForEditNodeObject(Id);
        }


        /// <summary>
        /// Validates an ItemBank node.
        /// </summary>
        /// <param name="parentId">The parent ID of the ItemBank.</param>
        /// <returns>An API response with the validation result.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("ValidateNode")]
        public async Task<IApiResponse> GetNodeValidation(long parentId)
        {
            return await _itemBankService.GetNodeValidation(parentId);

        }


        /// <summary>
        /// Transfers the children of an ItemBank node to its parent node.
        /// </summary>
        /// <param name="itemId">The ID of the ItemBank node.</param>
        /// <returns>An API response indicating the result of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("transferChildrenForNode")]
        public async Task<IApiResponse> TransferChildrenForNodeAsync(long itemId)
        {
            return await _itemBankService.TransferChildrenForNodeAsync(itemId);

        }


        /// <summary>
        /// Adds a new ItemBank root.
        /// </summary>
        /// <param name="itemBankDto">The DTO containing the details of the new ItemBank.</param>
        /// <returns>An API response indicating the result of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("Add")]
        public async Task<IApiResponse> AddAsync(NewItemBankDTO itemBankDto)
        {
            return await _itemBankService.AddItemBank(itemBankDto);
        }


        /// <summary>
        /// Adds a new node to an ItemBank.
        /// </summary>
        /// <param name="itemBankNode">The DTO containing the details of the new node.</param>
        /// <returns>An API response indicating the result of the operation.</returns>
        [OESFilter(Authorize = true)]
        [HttpPost("AddNode")]
        public async Task<IApiResponse> AddNode(ItemBankNode itemBankNode)
        {
            return await _itemBankService.AddNewNode(itemBankNode);
        }


        /// <summary>
        /// Retrieves the root-level item banks from the database.
        /// </summary>
        /// <returns>
        /// An asynchronous task that resolves to an <see cref="IApiResponse"/> containing the root node item banks.
        /// </returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetRootItemBanks")]
        public async Task<IApiResponse> GetRootItemBanks()
        {
            return await _itemBankService.GetRootNodeItemBank();
        }


        /// <summary>
        /// Retrieves the item bank groups for an item bank.
        /// </summary>
        /// <param name="itemBankId">The ID of the item bank.</param>
        /// <returns>An API response containing the item bank groups.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetItemBankGroups")]
        public async Task<IApiResponse> GetItemBankGroupsAsync(long itemBankId)
        {
            return await _itemBankService.GetItemBankGroupsAsync(itemBankId);
        }


        /// <summary>
        /// Retrieves the item bank groups for an item bank.
        /// </summary>
        /// <returns>An API response containing the item bank nodes.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetItemBanksList")]
        public async Task<IApiResponse> GetItemBanksListAsync()
        {
            return await _itemBankService.GetAllItemBanksListAsync();
        }


        /// <summary>
        /// Retrieves the item bank statistics for an item bank.
        /// </summary>
        /// <param name="itemBankId">The ID of the item bank.</param>
        /// <returns>An API response containing the item bank statistics.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetItemBankStatistics")]
        public async Task<IApiResponse> GetItemBankStatisticsAsync(long itemBankId)
        {
            return await _itemBankService.GetItemBankStatisticsAsync(itemBankId);
        }


        /// <summary>
        /// Retrieves all item banks for a user.
        /// </summary>
        /// <param name="itemBankId">The ID of the item bank.</param>
        /// <returns>An API response containing the item banks.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("GetAllItemBankForUserAsync")]
        public async Task<IApiResponse> GetAllItemBanksForUserAsync(long itemBankId)
        {
            return await _itemBankService.GetAllItemBankForUserAsync(itemBankId);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllItemBankRootsForPaperAsync")]
        public async Task<IApiResponse> GetAllItemBankRootsForPaperAsync(PaginationSearchModel pagination)
        {
            return await _itemBankService.GetAllItemBankRootsForPaperAsync(pagination);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("TransferQuestionsToItemBank")]
        public async Task<IApiResponse> TransferQuestionsToItemBankAsync(TransferQuestionsToItemBankDto dto)
        {
            return await _itemBankService.TransferQuestionsToItemBankAsync(dto);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetUserItemBankGroupsAsync")]
        public async Task<ApiResponse> GetUserItemBankGroupsAsync()
        {
            return await _itemBankService.GetUserItemBankGroupsAsync();
        }


        /// <summary>
        /// Checks if the current user can perform a question action on an item bank.
        /// </summary>
        /// <param name="itemBankId">The ID of the item bank.</param>
        /// <param name="requiredRole">The required role name.</param>
        /// <returns>An API response indicating whether the user is authorized.</returns>
        [OESFilter(Authorize = true)]
        [HttpGet("CanDoQuestionAction")]
        public async Task<ApiResponse> CanDoQuestionActionAsync(long itemBankId, string requiredRole)
        {
            return await _itemBankService.CanDoQuestionActionAsync(itemBankId, requiredRole);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("AddTemplateAIItemBank")]
        public async Task<ApiResponse> AddTemplateAIItemBankAsync(AIItemBankTemplateCreationDto dto)
        {
            return await _itemBankService.AddTemplateAIItemBankAsync(dto);
        }


        [OESFilter(Authorize = true)]
        [HttpPost("GetAllItemBankTemplates")]
        public async Task<IApiResponse> GetAllItemBankTemplatesAsync(PaginationSearchModel paginationSearch, bool isFromItmBankAI = true)
        {
            return await _itemBankService.GetAllItemBankTemplatesAsync(paginationSearch, isFromItmBankAI);
        }


        [OESFilter(Authorize = true)]
        [HttpGet("GetItemBankTemplateById")]
        public async Task<ApiResponse> GetItemBankTemplateByIdAsync(long templateId)
        {
            return await _itemBankService.GetItemBankTemplateByIdAsync(templateId);
        }


        [OESFilter(Authorize = true)]
        [HttpDelete("DeleteItemBankTemplate")]
        public async Task<ApiResponse> DeleteItemBankTemplateAsync(long templateId)
        {
            return await _itemBankService.DeleteItemBankTemplateAsync(templateId);
        }
    }
}
