using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.EquationTemplate.AddEquationCategoryDialog;
using OES.Blazor.Services.Interfaces.EquationTemplate;
using OES.Blazor.Services.Interfaces.ISessionStorageService;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.EquationTemplate
{
    public partial class UpdateEquationTemplate
    {
        [Inject] IBlazEquationTemplateService BlazEquationTemplateService { get; set; }
        [Inject] IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
        [Inject] IBlazPaperService BLazPaperService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] IDialogService DialogService { get; set; }
        [Inject] IBlazSessionStorageService BlazSessionStorageService { get; set; }

        [Parameter] public long Id { get; set; }

        private AddOrUpdateEquationTemplateDto Model { get; set; } = new AddOrUpdateEquationTemplateDto();
        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(Model?.Name) ||
            (_paperType == PaperType.Standard && string.IsNullOrWhiteSpace(_totalEquation)) ||
            !string.IsNullOrEmpty(_totalEquationError) ||
            _isSubmitting;

        private string _paperName = string.Empty;
        private string _formName = string.Empty;
        private long _paperId;
        private long _formId;
        private PaperType _paperType;
        private List<ItemBanksFromItemBankPointResponseDto> _allItemBanks = [];
        private List<EquationDto> _equations = [];
        private string _totalEquation = string.Empty;
        private string _totalEquationDisplay = string.Empty;
        private string _totalEquationError = string.Empty;
        private bool _isLoading = true;
        private bool _isSubmitting = false;
        private long _templateId;
        private int _equationIdCounter = -1;
        private List<QuestionCategoryDto> _questionCategories = [];
        private Dictionary<long, string> _categoryFinalScoreNames = [];
        private static readonly List<string> _totalEquationFunctions = ["NC", "ND", "NT"];


        protected override async Task OnInitializedAsync()
        {
            var equationTemplateId = await BlazSessionStorageService.GetValue<long>(MiscConstants.PerformEditBtnClick);
            await LoadEquationTemplateAsync(equationTemplateId);
        }

        private void OnFinalScoreNameChanged(long categoryId, string name)
        {
            _categoryFinalScoreNames[categoryId] = name;
        }

        private async Task LoadEquationTemplateAsync(long id)
        {
            if (id <= 0)
            {
                Snackbar.Add(Resource.InvalidTemplateData, Severity.Error);
                NavigateBack();
                return;
            }

            var response = await BlazEquationTemplateService.GetEquationTemplateById(id);

            if (response?.StatusCode == HttpStatusCode.OK && response.Data != null)
            {
                var data = (GetEquationTemplateResponseDto)response.Data;

                _templateId = data.Id;
                Model.Name = data.Name;
                _totalEquation = data.TotalEquation ?? string.Empty;
                // Set readonly paper and form info
                _paperId = data.PaperId;
                _formId = data.FormId;
                _paperName = data.PaperName;
                _formName = data.FormName;
                _paperType = data.PaperType;

                Task<List<ItemBanksFromItemBankPointResponseDto>> itemBanksTask;

                if (_paperType == PaperType.Adaptive)
                {
                    itemBanksTask = BlazEquationTemplateService.GetAllItemBanksFromQuestionBlocksAsync(_paperId);
                }
                else
                {
                    itemBanksTask = BLazPaperService.GetAllItemBanksFromItemBankPointsAsync(_paperId);
                }

                var categoriesTask = BlazQuestionCategoryService.GetCategoriesByPaperId(_paperId);

                await Task.WhenAll(itemBanksTask, categoriesTask);

                _allItemBanks = await itemBanksTask ?? [];
                _questionCategories = await categoriesTask ?? [];

                foreach (var category in _questionCategories)
                {
                    _categoryFinalScoreNames[category.Id] = category.FinalScoreName ?? string.Empty;
                }

                // Load equations with their item banks
                _equations = data.Equations.ConvertAll(eq => new EquationDto
                {
                    Id = eq.Id,
                    Name = eq.Name,
                    Equation = eq.Equation,
                    ShowInResults = eq.ShowInResults,
                    DisplayEquation = EquationCategoryValidator.BuildDisplayEquation(eq.Equation, ibId => eq.ItemBanks?.FirstOrDefault(x => x.Id == ibId)?.Name),
                    SelectedItemBanks = eq.ItemBanks.ConvertAll(ib => new ItemBanksFromItemBankPointResponseDto
                    {
                        Id = ib.Id,
                        Name = ib.Name
                    })
                });

                if (_equations.Count == 0)
                {
                    Snackbar.Add(Resource.NoEquationCategoriesFoundForThisTemplate, Severity.Warning);
                }

                // Build display version by replacing item bank IDs with names now that item banks and equations are loaded
                _totalEquationDisplay = string.IsNullOrEmpty(_totalEquation)
                    ? string.Empty
                    : EquationCategoryValidator.BuildDisplayEquation(
                        _totalEquation,
                        ibId => _allItemBanks.FirstOrDefault(x => x.Id == ibId)?.Name ?? GetUsedItemBanks().FirstOrDefault(x => x.Id == ibId)?.Name);

                // Validate total equation if it exists
                if (!string.IsNullOrEmpty(_totalEquation))
                {
                    ValidateTotalEquation();
                }
            }
            else
            {
                Snackbar.Add(response?.Message ?? Resource.FailedToLoadEquationTemplate, Severity.Error);
                NavigateBack();
            }

            _isLoading = false;
            StateHasChanged();
        }

        private void AddCategoryToTotalEquation(string categoryName)
        {
            string placeholder = $"[{categoryName}]";
            _totalEquation += placeholder;
            ValidateTotalEquation();
        }

        private async Task OpenTotalEquationDialog()
        {
            var parameters = new DialogParameters<AddEquationCategoryDialog>
            {
                { x => x.AvailableItemBanks, _allItemBanks },
                { x => x.GloballyUsedItemBanks, GetUsedItemBanks() },
                { x => x.ExistingCategories, _equations },
                { x => x.IsEdit, false },
                { x => x.IsTotalEquationMode, true },
                { x => x.TotalEquationValue, _totalEquationDisplay }, // Display value
                { x => x.TotalEquationHiddenValue, _totalEquation } // Hidden ID value
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
            };

            var dialog = await DialogService.ShowAsync<AddEquationCategoryDialog>(
                string.IsNullOrWhiteSpace(_totalEquation) ? Resource.AddEquationCategory : Resource.EditEquationCategory,
                parameters, options);

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is EquationDto newEquation)
            {
                _totalEquation = newEquation.Equation;
                _totalEquationDisplay = newEquation.DisplayEquation;
                _totalEquationError = string.Empty;
                ValidateTotalEquation();
                Snackbar.Add(Resource.TotalEquationSetSuccessfully, Severity.Success);
                StateHasChanged();
            }
        }

        private bool IsPlaceholderInsideFunctionInTotalEquation(int placeholderIndex)
        {
            if (placeholderIndex < 3) return false;
            var beforePlaceholder = _totalEquationDisplay.Substring(0, placeholderIndex);
            var functionPattern = new Regex(@"([A-Za-z]+)\($", RegexOptions.IgnoreCase);
            return functionPattern.IsMatch(beforePlaceholder);
        }

        private void ValidateTotalEquation()
        {
            _totalEquationError = string.Empty;

            if (string.IsNullOrWhiteSpace(_totalEquationDisplay))
            {
                return;
            }

            var placeholderPattern = new Regex(@"\[([^\]]+)\]");
            var matches = placeholderPattern.Matches(_totalEquationDisplay);

            bool hasValidReference = false;

            foreach (Match match in matches)
            {
                var placeholderName = match.Groups[1].Value;
                bool isInsideFunction = IsPlaceholderInsideFunctionInTotalEquation(match.Index);

                if (isInsideFunction)
                {
                    bool isValidItemBank = _allItemBanks.Any(ib => ib.Name == placeholderName);
                    if (!isValidItemBank)
                    {
                        _totalEquationError = string.Format(Resource.InvalidItemBankUseItemBanksAbove, placeholderName);
                        return;
                    }
                    hasValidReference = true;
                }
                else
                {
                    bool isValidCategory = _equations.Any(c => c.Name == placeholderName);
                    if (!isValidCategory)
                    {
                        _totalEquationError = string.Format(Resource.InvalidCategoryDetectedPleaseUseOnlyTheCategoriesListedAbove, placeholderName);
                        return;
                    }
                    hasValidReference = true;
                }
            }

            // Check if at least one reference (category or item bank) is used
            if (!hasValidReference && matches.Count == 0)
            {
                _totalEquationError = Resource.AtLeastOneCategoryRequired;
                return;
            }

            var cleanEquation = placeholderPattern.Replace(_totalEquationDisplay, "1").Replace(" ", "");

            foreach (var function in _totalEquationFunctions)
            {
                cleanEquation = Regex.Replace(cleanEquation, $@"{function}\s*\(", "1*(", RegexOptions.IgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(cleanEquation))
            {
                return;
            }

            var allowedPattern = new Regex(@"^[0-9+\-*/().]+$");
            if (!allowedPattern.IsMatch(cleanEquation))
            {
                _totalEquationError = Resource.OnlyOperatorsAllowed;
                return;
            }

            // Check for balanced parentheses
            int openCount = 0;
            foreach (char c in cleanEquation)
            {
                if (c == '(') openCount++;
                if (c == ')') openCount--;
                if (openCount < 0)
                {
                    _totalEquationError = Resource.UnbalancedParenthesesDetected;
                    return;
                }
            }

            if (openCount != 0)
            {
                _totalEquationError = Resource.UnbalancedParenthesesDetected;
                return;
            }

            if ("*/".Contains(cleanEquation[0]))
            {
                _totalEquationError = Resource.EquationCannotStartWithMultiplyOrDivide;
                return;
            }
            if ("+-*/".Contains(cleanEquation[^1]))
            {
                _totalEquationError = Resource.EquationCannotEndWithOperator;
                return;
            }

            for (int i = 0; i < cleanEquation.Length - 1; i++)
            {
                if ("+-*/".Contains(cleanEquation[i]) && "*/".Contains(cleanEquation[i + 1]))
                {
                    _totalEquationError = Resource.InvalidOperatorSequenceDetected;
                    return;
                }
            }
        }

        private string GetExpandedTotalEquation()
        {
            if (string.IsNullOrEmpty(_totalEquationDisplay))
                return string.Empty;

            string expanded = _totalEquationDisplay;
            var placeholderPattern = new Regex(@"\[([^\]]+)\]");
            var matches = placeholderPattern.Matches(_totalEquationDisplay);

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                var match = matches[i];
                var placeholderName = match.Groups[1].Value;

                if (!IsPlaceholderInsideFunctionInTotalEquation(match.Index))
                {
                    var equation = _equations.FirstOrDefault(e => e.Name == placeholderName);
                    if (equation != null)
                    {
                        string replacement = $"<span style='color: #1976d2; font-weight: bold;'>({equation.DisplayEquation})</span>";
                        expanded = expanded.Remove(match.Index, match.Length).Insert(match.Index, replacement);
                    }
                }
            }

            return expanded;
        }

        private async Task OnSubmit()
        {
            if (_isSubmitting) return;

            if (string.IsNullOrWhiteSpace(Model.Name))
            {
                Snackbar.Add(Resource.TemplateNameIsRequired, Severity.Warning);
                return;
            }

            if (_equations.Count == 0)
            {
                Snackbar.Add(Resource.PleaseAddAtLeastOneEquationCategory, Severity.Warning);
                return;
            }

            var allItemBanksAssigned = _allItemBanks.All(ib =>
                _equations.Any(eq => eq.SelectedItemBanks.Any(sib => sib.Id == ib.Id))
            );
            if (!allItemBanksAssigned)
            {
                Snackbar.Add(Resource.AllItemBanksMustBeAssignedToEquations, Severity.Warning);
                return;
            }

            var (isValid, errorMessage) = EquationCategoryValidator.ValidateEquationCategoryReferences(_equations);
            if (!isValid)
            {
                Snackbar.Add(errorMessage, Severity.Error);
                return;
            }

            if (_paperType == PaperType.Standard)
            {
                if (string.IsNullOrWhiteSpace(_totalEquation))
                {
                    Snackbar.Add(Resource.PleaseDefineTotalQuestionsEquation, Severity.Warning);
                    return;
                }

                ValidateTotalEquation();

                if (!string.IsNullOrEmpty(_totalEquationError))
                {
                    Snackbar.Add(_totalEquationError, Severity.Error);
                    return;
                }
            }

            _isSubmitting = true;
            StateHasChanged();

            var request = new AddOrUpdateEquationTemplateDto
            {
                Id = _templateId,
                Name = Model.Name.Trim(),
                PaperId = _paperId,
                FormId = _formId,
                TotalEquation = _equations.Count == 1
                    ? $"[{_equations[0].Name.Trim()}]"
                    : _equations.Count >= 2 ? _totalEquation.Trim() : null,
                Equations = [.. _equations.Select(eq => new EquationItemDto
                {
                    Id = eq.Id,
                    Name = eq.Name.Trim(),
                    Equation = eq.Equation.Trim(),
                    ShowInResults = eq.ShowInResults,
                    ItemBankIds = eq.SelectedItemBanks.ConvertAll(ib => ib.Id)
                })]
            };

            var response = await BlazEquationTemplateService.UpdateEquationTemplateAsync(request);

            // If Adaptive paper type, update category final score names
            if (_paperType == PaperType.Adaptive)
            {
                var categoriesToUpdate = _questionCategories
                    .ConvertAll(c => new QuestionCategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        FinalScoreName = _categoryFinalScoreNames.GetValueOrDefault(c.Id, c.Name),
                    });

                var categoryUpdateResponse = await BlazQuestionCategoryService.BulkUpdateCategoryFinalScoresAsync(categoriesToUpdate);

                if (categoryUpdateResponse?.StatusCode != HttpStatusCode.OK)
                {
                    Snackbar.Add(categoryUpdateResponse?.Message, Severity.Warning);
                }
            }

            if (response?.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message ?? Resource.EquationTemplateUpdatedSuccessfully, Severity.Success);
                NavigateBack();
            }
            else
            {
                Snackbar.Add(response?.Message ?? Resource.FailedToUpdateEquationTemplate, Severity.Error);
            }

            _isSubmitting = false;
            StateHasChanged();
        }

        private async Task<bool> ConfirmChangeAsync(
            string message,
            string title,
            string submitText,
            Color submitButtonColor,
            string submitButtonStartIcon)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
               { x => x.Title, title },
               { x => x.Content, message },
               { x => x.SubmitText, submitText },
               { x => x.CancelText, Resource.Cancel },
               { x => x.SubmitButtonColor, submitButtonColor },
               { x => x.SubmitButtonStartIcon, submitButtonStartIcon }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<GenericDialog>(string.Empty, parameters, options);

            var result = await dialog.Result;

            return !result.Canceled;
        }

        private List<ItemBanksFromItemBankPointResponseDto> GetAvailableItemBanks(EquationDto excludeEquation = null)
        {
            var usedItemBankIds = GetUsedItemBankIds(excludeEquation);
            return [.. _allItemBanks.Where(ib => !usedItemBankIds.Contains(ib.Id))];
        }

        private List<ItemBanksFromItemBankPointResponseDto> GetUsedItemBanks(EquationDto excludeEquation = null)
        {
            var usedItemBankIds = GetUsedItemBankIds(excludeEquation);
            return [.. _allItemBanks.Where(ib => usedItemBankIds.Contains(ib.Id))];
        }

        private HashSet<long> GetUsedItemBankIds(EquationDto excludeEquation = null)
        {
            return [.. _equations
                .Where(eq => excludeEquation == null || eq.Id != excludeEquation.Id)
                .SelectMany(eq => eq.SelectedItemBanks)
                .Select(ib => ib.Id)
                .Distinct()];
        }

        private async Task OpenAddEquationDialog()
        {
            var availableItemBanks = GetAvailableItemBanks();

            // Check if all item banks are used
            bool allItemBanksUsed = availableItemBanks.Count == 0;

            // If no item banks available AND no categories exist, can't create anything
            if (allItemBanksUsed && _equations.Count == 0)
            {
                Snackbar.Add(Resource.NoItemBanksAvailableAndNoCategoriesExist, Severity.Warning);
                return;
            }

            // If item banks are available, user MUST use them first
            if (!allItemBanksUsed)
            {
                // User can only create categories with item banks
                var parameters = new DialogParameters<AddEquationCategoryDialog>
                {
                    { x => x.AvailableItemBanks, availableItemBanks },
                    { x => x.GloballyUsedItemBanks, GetUsedItemBanks() },
                    { x => x.ExistingCategories, _equations },
                    { x => x.IsEdit, false },
                    { x => x.IsTotalEquationMode, false }
                };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true,
                };

                var dialog = await DialogService.ShowAsync<AddEquationCategoryDialog>(Resource.AddEquationCategory, parameters, options);

                var result = await dialog.Result;

                if (!result.Canceled && result.Data is EquationDto newEquation)
                {
                    // Validate duplicate name
                    if (_equations.Any(eq => eq.Name.Equals(newEquation.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        Snackbar.Add(string.Format(Resource.CategoryWithNameAlreadyExists, newEquation.Name), Severity.Warning);
                        return;
                    }

                    newEquation.Id = _equationIdCounter--;
                    _equations.Add(newEquation);
                    Snackbar.Add(string.Format(Resource.EquationCategoryAddedSuccessfully, newEquation.Name), Severity.Success);
                    StateHasChanged();
                }
            }
            else
            {
                // All item banks used, now user can create categories using existing categories
                var parameters = new DialogParameters<AddEquationCategoryDialog>
                {
                    { x => x.AvailableItemBanks, new List<ItemBanksFromItemBankPointResponseDto>() },  // No item banks
                    { x => x.GloballyUsedItemBanks, GetUsedItemBanks() },
                    { x => x.ExistingCategories, _equations },  // Pass existing categories
                    { x => x.IsEdit, false },
                    { x => x.IsTotalEquationMode, false }
                };

                var options = new DialogOptions
                {
                    CloseButton = true,
                    MaxWidth = MaxWidth.Medium,
                    FullWidth = true,
                };

                var dialog = await DialogService.ShowAsync<AddEquationCategoryDialog>(Resource.AddEquationCategoryUsingCategories, parameters, options);
                var result = await dialog.Result;

                if (!result.Canceled && result.Data is EquationDto newEquation)
                {
                    // Validate duplicate name
                    if (_equations.Any(eq => eq.Name.Equals(newEquation.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        Snackbar.Add(string.Format(Resource.CategoryWithNameAlreadyExists, newEquation.Name), Severity.Warning);
                        return;
                    }

                    newEquation.Id = _equationIdCounter--;
                    _equations.Add(newEquation);
                    Snackbar.Add(string.Format(Resource.EquationCategoryAddedSuccessfully, newEquation.Name), Severity.Success);
                    StateHasChanged();
                }
            }
        }

        private async Task OpenEditEquationDialog(EquationDto equation)
        {
            // Check if this category is used by other categories
            var dependentCategories = _equations
                .Where(eq => eq.Id != equation.Id && eq.Equation.Contains($"[{equation.Name}]"))
                .Select(eq => eq.Name)
                .ToList();

            if (dependentCategories.Count > 0)
            {
                var dependentList = string.Join(", ", dependentCategories);
                if (!await ConfirmChangeAsync(
                    string.Format(Resource.CategoryUsedByOthersWarning, equation.Name, dependentList),
                        Resource.Confirm,
                        Resource.Proceed,
                        Color.Warning,
                        Icons.Material.Filled.Warning))
                {
                    return;
                }
            }

            // Check if total equation needs to be invalidated
            if (!string.IsNullOrEmpty(_totalEquation) && !await ConfirmChangeAsync(
                string.Format(Resource.EditingCategoryWillRemoveTotalEquation, equation.Name),
                    Resource.Confirm,
                    Resource.Proceed,
                    Color.Warning,
                    Icons.Material.Filled.Warning))
            {
                return;
            }

            // Get available item banks (this already includes the current equation's item banks)
            // GetAvailableItemBanks(equation) excludes item banks used by OTHER categories only
            var availableItemBanks = GetAvailableItemBanks(equation);

            // Check if there are any available item banks
            bool hasUnusedItemBanks = availableItemBanks.Any();

            // Determine what to show based on available resources
            List<ItemBanksFromItemBankPointResponseDto> itemBanksToPass;

            if (hasUnusedItemBanks)
            {
                // If there are unused item banks, only show item banks
                // Need to include the current equation's selected item banks too
                var combinedItemBanks = availableItemBanks.ToList();

                // Add current equation's item banks if they're not already in the list
                foreach (var selectedBank in equation.SelectedItemBanks)
                {
                    if (!combinedItemBanks.Any(ib => ib.Id == selectedBank.Id))
                    {
                        combinedItemBanks.Add(selectedBank);
                    }
                }

                itemBanksToPass = combinedItemBanks;
            }
            else
            {
                // If all item banks are used, show categories (but not the current one being edited)
                itemBanksToPass = [];
            }

            var parameters = new DialogParameters<AddEquationCategoryDialog>
            {
                { x => x.AvailableItemBanks, itemBanksToPass },
                { x => x.GloballyUsedItemBanks, GetUsedItemBanks(equation) },
                { x => x.ExistingCategories, _equations }, // Pass all equations for index-based filtering
                { x => x.ExistingEquation, equation },
                { x => x.IsEdit, true },
                { x => x.IsTotalEquationMode, false }
            };

            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
            };

            var dialog = await DialogService.ShowAsync<AddEquationCategoryDialog>(Resource.EditEquationCategory, parameters, options);
            var result = await dialog.Result;

            if (!result.Canceled && result.Data is EquationDto updatedEquation)
            {
                // Validate duplicate name (excluding the current equation being edited)
                if (_equations.Any(eq => eq.Id != equation.Id && eq.Name.Equals(updatedEquation.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    Snackbar.Add(string.Format(Resource.CategoryWithNameAlreadyExists, updatedEquation.Name), Severity.Warning);
                    return;
                }

                _totalEquation = string.Empty;
                _totalEquationError = string.Empty;

                var index = _equations.FindIndex(e => e.Id == equation.Id);
                if (index >= 0)
                {
                    updatedEquation.Id = equation.Id;
                    _equations[index] = updatedEquation;
                    Snackbar.Add(string.Format(Resource.EquationCategoryUpdatedSuccessfully, updatedEquation.Name), Severity.Success);
                    StateHasChanged();
                }
            }
        }

        private async Task DeleteEquation(EquationDto equation)
        {
            // Check if this category is used by other categories
            var dependentCategories = _equations
                .Where(eq => eq.Id != equation.Id && eq.Equation.Contains($"[{equation.Name}]"))
                .Select(eq => eq.Name)
                .ToList();

            string message = string.Format(Resource.ConfirmDeleteEquationCategory, equation.Name);

            if (dependentCategories.Count > 0)
            {
                var dependentList = string.Join(", ", dependentCategories);
                message += string.Format(Resource.CategoryUsedByOthersDeleteWarning, dependentList);
            }

            if (!string.IsNullOrEmpty(_totalEquation))
            {
                message += Resource.DeleteWillAlsoRemoveTotalEquation;
            }

            if (await ConfirmChangeAsync(
                message,
                Resource.ConfirmDelete,
                Resource.Delete,
                Color.Error,
                Icons.Material.Filled.Delete))
            {
                _equations.Remove(equation);

                if (!string.IsNullOrEmpty(_totalEquation))
                {
                    _totalEquation = string.Empty;
                    _totalEquationError = string.Empty;
                }

                Snackbar.Add(string.Format(Resource.EquationCategoryDeletedSuccessfully, equation.Name), Severity.Success);
                StateHasChanged();
            }
        }

        private void NavigateBack()
        {
            NavigationManager.NavigateTo("/EquationTemplateList");
        }
    }
}