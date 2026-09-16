using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Dialogs.Question.QuestionViewDialog;
using OES.Blazor.Extensions;
using OES.Blazor.Services.Interfaces.AuthServices;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.General;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.Question;
public partial class QuestionsQualityCheckList : ComponentBase
{
    [Inject] IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
    [Inject] IBLazQuestionType BLazQuestionType { get; set; }
    [Inject] IBlazItemBankService BlazItemBankService { get; set; }
    [Inject] IBlazQuestionService BlazQuestionService { get; set; }
    [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }
    [Inject] IBlazAuthService AuthService { get; set; }
    [Inject] IDialogService DialogService { get; set; }
    [Inject] NavigationManager NavigationManager { get; set; }
    [Inject] ISnackbar Snackbar { get; set; }

    private List<QuestionCategoryDto> _categories;
    private List<QuestionTypeDto> _questionTypes;
    private List<RootItemBankDto> _itemBanks;
    private QuestionCategoryDto _selectedCategory;
    private QuestionTypeDto _selectedType;
    private RootItemBankDto _selectedItemBank;
    private string _selectedStatus = string.Empty;
    private List<TreeItemResponseDto> _branches = [];
    private TreeItemResponseDto _selectedBranch;

    private int ChangingKey { get; set; }

    public QuestionFilterPaginationModel QuestionFilterPaginationModel { get; set; } = new QuestionFilterPaginationModel();

    public RootItemBankDto SelectedItemBank
    {
        get => _selectedItemBank;
        set
        {
            if (_selectedItemBank != value)
            {
                _selectedItemBank = value;
                QuestionFilterPaginationModel._selectedItemBankSignature = _selectedItemBank?.ItemBankSignature;

                _selectedBranch = null;
                _branches = [];
                QuestionFilterPaginationModel._selectedBranchId = 0;

                if (_selectedItemBank != null)
                    _ = LoadBranchesAsync(_selectedItemBank.Id, _selectedItemBank.ItemBankSignature);

                ChangingKey++;
            }
        }
    }

    public TreeItemResponseDto SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            if (_selectedBranch != value)
            {
                _selectedBranch = value;
                QuestionFilterPaginationModel._selectedBranchId = _selectedBranch?.Id ?? 0;
                ChangingKey++;
            }
        }
    }

    public QuestionCategoryDto SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (_selectedCategory != value)
            {
                _selectedCategory = value;
                QuestionFilterPaginationModel._selectedCategory = _selectedCategory?.Id ?? 0;
                ChangingKey++;
            }
        }
    }

    public QuestionTypeDto SelectedType
    {
        get => _selectedType;
        set
        {
            if (_selectedType != value)
            {
                _selectedType = value;
                QuestionFilterPaginationModel._selectedType = _selectedType?.Id ?? 0;
                ChangingKey++;
            }
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (_selectedStatus != value)
            {
                _selectedStatus = value;

                if (!string.IsNullOrEmpty(_selectedStatus))
                {
                    QuestionFilterPaginationModel._selectedStatus = Enum.Parse<QuestionStatus>(_selectedStatus);
                }
                else
                {
                    QuestionFilterPaginationModel._selectedStatus = new QuestionStatus();
                }

                ChangingKey++;
            }
        }
    }


    protected override async void OnInitialized()
    {
        _categories = await BlazQuestionCategoryService.GetCategories();
        _questionTypes = await BLazQuestionType.GetAllQuestionType();
        _itemBanks = await BlazItemBankService.GetItemBanksListAsync();
    }

    public static bool CanEditQuestion(PendingQuestionPaginationDto question)
    {
        var status = Enum.TryParse(typeof(QuestionStatus), question.Status.ToString(), out var parsedStatus)
            ? (QuestionStatus)parsedStatus
            : QuestionStatus.LayoutSelectedAndPending;

        return status == QuestionStatus.LayoutSelectedAndPending ||
               status == QuestionStatus.Suspended ||
               status == QuestionStatus.Closed ||
               status == QuestionStatus.ReturnToEdit ||
               status == QuestionStatus.Rejected;
    }

    private async Task SaveRowId(long questionId, long itemBankId)
    {
        var authorized = await AuthService.IsCurrentUserAuthorizedForAnyRoleAsync<ItemBankGroupsDto, Guid>(
            itemBankId,
            [
                OesTemplateRoleConstants.QuestionQualityChecker,
                OesTemplateRoleConstants.ItemBankQuestionQualityChecker
            ],
            BlazItemBankService.GetItemBankGroupsAsync,
            dto => dto.GroupsIds,
            g => g.GroupId
        );

        if (!authorized)
        {
            Snackbar.Add(Resource.NotAuthorized, Severity.Error);
            return;
        }

        await BlazSessionStorageService.SetCrudSessionAsync(
            questionId,
            MiscConstants.PerformEditBtnClick);

        NavigationManager.NavigateTo("/QuestionQualityCheck");
    }

    private async Task ViewQuestionAsync(long questionId)
    {
        if (questionId <= 0)
        {
            Snackbar.Add(Resource.FailedToLoadQuestionDetails, Severity.Error);
            return;
        }

        var authorized = await AuthService.IsCurrentUserAuthorizedForAnyRoleAsync(
            questionId,
            [
                OesTemplateRoleConstants.Questionviewer,
                OesTemplateRoleConstants.ItemBankQuestionViewer
            ],
            BlazQuestionService.GetQuestionGroupsAsync,
            dto => dto.GroupsIds,
            g => g.GroupId
        );

        await BlazSessionStorageService.SetValue(
            MiscConstants.PerformViewBtnClick,
            questionId);

        var parameters = new DialogParameters<QuestionViewDialog>();

        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<QuestionViewDialog>(
            "Question Details",
            parameters,
            options);

        await dialog.Result;
    }

    private Task<IEnumerable<RootItemBankDto>> SearchItemBanks(string value, CancellationToken token)
    {
        IEnumerable<RootItemBankDto> result;

        if (string.IsNullOrWhiteSpace(value))
        {
            result = _itemBanks;
        }
        else
        {
            result = _itemBanks.Where(x => x.Name.Contains(value, StringComparison.InvariantCultureIgnoreCase));
        }

        return Task.FromResult(result);
    }

    private async Task LoadBranchesAsync(long itemBankId, string signature)
    {
        _branches = await BlazItemBankService.GetChildrenByParentIdAsync(itemBankId, signature);
        await InvokeAsync(StateHasChanged);
    }

    private Task<IEnumerable<TreeItemResponseDto>> SearchBranches(string searchText, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return Task.FromResult(_branches.AsEnumerable());

        return Task.FromResult(
            _branches.Where(x => x.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
        );
    }
}
