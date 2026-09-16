using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Question.ItemBankMultiSelectionDialog;
using OES.Blazor.Services.Interfaces.FileExportService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Questionlanguage;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.Enums;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.ExportFiles
{
    public partial class ExportFiles
    {
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IBlazItemBankService BlazItemBankService { get; set; } = default!;
        [Inject] private IBlazQuestionLanguageService BlazQuestionLanguageService { get; set; } = default!;
        [Inject] private IBlazFileExportService BlazFileExportService { get; set; } = default!;
        [Inject] private IBlazQuestionService BlazQuestionService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;


        private List<LanguageDto> _languages = [];
        private LanguageDto _selectedLanguage = new();

        private List<RootItemBankDto> RootItemBankDtos { get; set; } = [];
        private RootItemBankDto? _selectedItemBankRoot;

        private FileExportTypes? _selectedDocType;
        private List<TreeItemResponseDto> _selectedItemBankNodesFromDialog = [];

        private ExportQuestionInItemBankDto ExportQuestionsOfItemBankDto { get; set; } = new();

        private bool _processing;
        private bool _contentWithHtmlTag;


        protected override async Task OnInitializedAsync()
        {
            RootItemBankDtos = await BlazItemBankService.GetRootItemBanksNodesAsync();

            _languages = await BlazQuestionLanguageService.GetAllLanguagesAsync();

            StateHasChanged();
        }


        private async Task<IEnumerable<LanguageDto>> SearchLanguageAsync(string value, CancellationToken cancellationToken)
        {
            return await FilterListAsync(_languages, s => s.Name, value);
        }

        private async Task<IEnumerable<RootItemBankDto>> SearchItemBankRootsAsync(string value, CancellationToken cancellationToken)
        {
            return await FilterListAsync(RootItemBankDtos, ib => ib.Name, value);
        }

        private async Task<IEnumerable<T>> FilterListAsync<T>(IEnumerable<T> list, Func<T, string> selector, string value)
        {
            await Task.Delay(250);

            if (string.IsNullOrEmpty(value))
                return list;

            return list.Where(item => selector(item).Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        private void CaptureSelectedItemBankRoot(RootItemBankDto rootItemBankDto)
        {
            _selectedItemBankRoot = rootItemBankDto;

            _selectedItemBankNodesFromDialog = [];
        }

        private void RemoveSelectedNode(TreeItemResponseDto node)
        {
            _selectedItemBankNodesFromDialog.Remove(node);
        }

        private async Task OpenItemBankRootTreeDialogAsync()
        {
            var parameters = new DialogParameters<ItemBankMultiSelectionDialog>
            {
                { p => p.ItemBankRootId,  _selectedItemBankRoot.Id },
                { p => p.SelectedItemBankNodesIds, _selectedItemBankNodesFromDialog.ConvertAll(static x => x.Id)}
            };

            var options = new DialogOptions()
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true
            };

            var result = await (await DialogService.ShowAsync<ItemBankMultiSelectionDialog>(string.Empty, parameters, options)).Result;

            if (!result.Canceled)
            {
                _selectedItemBankNodesFromDialog = (List<TreeItemResponseDto>)result.Data;
                StateHasChanged();
            }
        }

        private bool ValidateForm()
        {
            return !(_selectedDocType is null ||
                     _selectedItemBankNodesFromDialog == null ||
                     _selectedItemBankNodesFromDialog.Count == 0);
        }

        public async Task OnSubmit()
        {
            _processing = true;

            StateHasChanged();

            var isFormValid = ValidateForm();

            var shouldSelectLanguage = _selectedDocType != FileExportTypes.QTI && _selectedLanguage.Id == 0;

            if (!isFormValid || shouldSelectLanguage)
            {
                Snackbar.Add(Resource.PleaseFillInAllRequiredFields, Severity.Error);
                _processing = false;
                StateHasChanged();
                return;
            }

            ExportQuestionsOfItemBankDto.LanguageId = _selectedLanguage.Id;
            ExportQuestionsOfItemBankDto.ItemBanksIds = _selectedItemBankNodesFromDialog.ConvertAll(x => x.Id);
            ExportQuestionsOfItemBankDto.WithTags = _contentWithHtmlTag;

            switch (_selectedDocType)
            {
                case FileExportTypes.Excel:
                    await BlazFileExportService.ExportItemBankQuestionsAsExcelFileAsync(ExportQuestionsOfItemBankDto);
                    break;
                case FileExportTypes.Word:
                    await BlazFileExportService.ExportItemBankQuestionsAsDocxFileAsync(ExportQuestionsOfItemBankDto);
                    break;
                case FileExportTypes.QTI:
                    await BlazFileExportService.ExportItemBankQuestionsAsQtiXmlFileAsync(ExportQuestionsOfItemBankDto);
                    break;
                default:
                    break;
            }

            _processing = false;

            StateHasChanged();
        }


        Func<RootItemBankDto, string> ItemBankDtoToStringConverter = p => p.Name;
    }
}
