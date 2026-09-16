using Microsoft.AspNetCore.Components;
using OES.Blazor.Services.Interfaces.Block;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Question;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.General;
namespace OES.Blazor.Pages.Paper.Block
{
    public partial class BlockView : ComponentBase
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] IBlazQuestionService BlazQuestionService { get; set; }
        [Inject] IBlazBlockService BlazBlockService { get; set; }
        [Inject] IBlazSessionStorageService SessionStorage { get; set; }

        private bool isBlockLoaded = false;

        private GetBlockResponseDto block = new();

        private int numberOfQuestions;


        protected override async Task OnInitializedAsync()
        {
            var blockId = await SessionStorage.GetValue<long>("PerformViewBtnClick");

            block = await BlazBlockService.GetBlockDataById(blockId);

            numberOfQuestions = block?.Questions?.Count ?? 0;

            isBlockLoaded = true;
        }

        private async Task<CustomTableData<BlockQuestionsDetailsPaginationDto>> GetAllQuestions(PaginationSearchModel paginationSearchModel)
        {
            return await BlazQuestionService.GetAllQuestionByBlockId(block.Id, paginationSearchModel);
        }

        private void Close() => NavigationManager.NavigateTo("/BlocksList");
    }
}
