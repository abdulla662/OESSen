using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Components.Common;
using OES.Blazor.Dialogs.EquationTemplate.AddEquationCategoryDialog;
using OES.Blazor.Services.Interfaces.EquationTemplate;
using OES.Blazor.Services.Interfaces.Form;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.QuestionCataegory;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.UserPapersDto;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using SharedHelper.Enums;
using System.Net;
using System.Text.RegularExpressions;

namespace OES.Blazor.Pages.EquationTemplate
{
    public partial class AddEquationTemplate
    {
        [Inject] IBlazPaperService BLazPaperService { get; set; }
        [Inject] IBlazFormService BlazFormService { get; set; }
        [Inject] IBlazEquationTemplateService BlazEquationTemplateService { get; set; }
        [Inject] IBlazQuestionCategoryService BlazQuestionCategoryService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] private IDialogService DialogService { get; set; }

        private bool IsSubmitButtonDisabled =>
            string.IsNullOrWhiteSpace(_equationTemplateName) ||
            _selectedPaper == null ||
            _selectedForm == null ||
            (_selectedPaper.Type == PaperType.Standard && string.IsNullOrWhiteSpace(_totalEquation)) ||
            !string.IsNullOrWhiteSpace(_totalEquationError) ||
            _isSubmitting;
        public GetUserPapersDto SelectedPaper
        {
            get => _selectedPaper;
            set
            {
                if (_selectedPaper != value)
                {
                    _ = OnPaperChangedAsync(value);
                }
            }
        }

        private List<GetUserPapersDto> _papers = [];
        private GetUserPapersDto _selectedPaper;
        private List<GetFormDto> _forms = [];
        private GetFormDto _selectedForm;
        private List<ItemBanksFromItemBankPointResponseDto> _paperItemBanks = [];
        private List<EquationDto> _equations = [];
        private string _totalEquation = string.Empty;        // Storage: Item banks as IDs e.g. NC([12])
        private string _totalEquationDisplay = string.Empty; // Display: Item banks as names e.g. NC([BankName])
        private string _totalEquationError = string.Empty;
        private bool _isLoadingPaper = false;
        private bool _isSubmitting = false;
        private int _equationIdCounter = -1;
        private List<QuestionCategoryDto> _questionCategories = [];
        private string _equationTemplateName;
        private Dictionary<long, string> _categoryFinalScoreNames = [];
        private static readonly List<string> _totalEquationFunctions = ["NC", "ND", "NT"];


        protected override async Task OnInitializedAsync()
        {
            _papers = await BLazPaperService.GetPapersWithoutEquationTemplateAsync() ?? [];
        }

        private void OnFinalScoreNameChanged(long categoryId, string name)
        {
            _categoryFinalScoreNames[categoryId] = name;
        }

        private void OnEquationTemplateNameChanged(string name)
        {
            _equationTemplateName = name;
        }

        private void AddCategoryToTotalEquation(string categoryName)
        {
            string placeholder = $"[{categoryName}]";
            _totalEquation += placeholder;
            _totalEquationDisplay += placeholder;
            ValidateTotalEquation();
        }

        private async Task OpenTotalEquationDialog()
        {
            var parameters = new DialogParameters<AddEquationCategoryDialog>
            {
                { x => x.AvailableItemBanks, _paperItemBanks },
                { x => x.GloballyUsedItemBanks, GetUsedItemBanks() },
                { x => x.ExistingCategories, _equations },
                { x => x.IsEdit, false },
                { x => x.IsTotalEquationMode, true },
                { x => x.TotalEquationValue, _totalEquationDisplay },
                { x => x.TotalEquationHiddenValue, _totalEquation }
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

            // Extract placeholders
            var placeholderPattern = new Regex(@"\[([^\]]+)\]");
            var matches = placeholderPattern.Matches(_totalEquationDisplay);

            bool hasValidReference = false;

            // Validate placeholders (categories or item banks)
            foreach (Match match in matches)
            {
                var placeholderName = match.Groups[1].Value;
                bool isInsideFunction = IsPlaceholderInsideFunctionInTotalEquation(match.Index);

                if (isInsideFunction)
                {
                    // Inside a function like NC([...]) — must be a valid item bank
                    bool isValidItemBank = _paperItemBanks.Any(ib => ib.Name == placeholderName);
                    if (!isValidItemBank)
                    {
                        _totalEquationError = string.Format(Resource.InvalidCategoryDetectedUseListedCategories, placeholderName);
                        return;
                    }

                    hasValidReference = true;
                }
                else
                {
                    // Outside a function — can be a category or an item bank
                    bool isValidCategory = _equations.Any(c => c.Name == placeholderName);
                    bool isValidItemBank = _paperItemBanks.Any(ib => ib.Name == placeholderName);

                    if (!isValidCategory && !isValidItemBank)
                    {
                        _totalEquationError = string.Format(Resource.InvalidCategoryDetectedUseListedCategories, placeholderName);
                        return;
                    }

                    hasValidReference = true;
                }
            }

            // Check if at least one reference (category or item bank) is used
            if (!hasValidReference && matches.Count == 0)
            {
                _totalEquationError = Resource.AtLeastOneEquationCategoryIsRequired;
                return;
            }

            // Replace placeholders with '1' for syntax validation
            var cleanEquation = placeholderPattern.Replace(_totalEquationDisplay, "1").Replace(" ", "");

            // Replace function names with '1*(' for validation
            foreach (var function in _totalEquationFunctions)
            {
                cleanEquation = Regex.Replace(cleanEquation, $@"{function}\s*\(", "1*(", RegexOptions.IgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(cleanEquation))
            {
                return;
            }

            // Check for allowed characters
            var allowedPattern = new Regex(@"^[0-9+\-*/().]+$");
            if (!allowedPattern.IsMatch(cleanEquation))
            {
                List<string> allawedOperations = ["+", "-", "*", "/", "(", ")"];
                _totalEquationError = string.Format(Resource.InvalidCategoryDetectedPleaseUseOnlyTheCategoriesListedAbove, string.Join(",", allawedOperations));
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

            // Check if equation starts with invalid operators
            if (cleanEquation.Length > 0)
            {
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
            }

            // Check for consecutive operators
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
                        string replacement = $"<span style='color: #1976d2;'>({equation.DisplayEquation})</span>";
                        expanded = expanded.Remove(match.Index, match.Length).Insert(match.Index, replacement);
                    }
                }
            }

            return expanded;
        }

        private async Task OnSubmit()
        {
            if (_isSubmitting) return;

            if (string.IsNullOrWhiteSpace(_equationTemplateName))
            {
                Snackbar.Add(Resource.TemplateNameIsRequired, Severity.Warning);
                return;
            }

            if (_equations.Count == 0)
            {
                Snackbar.Add(Resource.PleaseAddAtLeastOneEquationCategory, Severity.Warning);
                return;
            }

            var allItemBanksAssigned = _paperItemBanks.All(ib =>
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

            if (_selectedPaper.Type == PaperType.Standard)
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
                Name = _equationTemplateName.Trim(),
                PaperId = _selectedPaper.Id,
                FormId = _selectedForm.Id,
                TotalEquation = _equations.Count == 1
                    ? $"[{_equations[0].Name.Trim()}]"
                    : _equations.Count >= 2 ? _totalEquation.Trim() : null,
                Equations = _equations.ConvertAll(eq => new EquationItemDto
                {
                    Name = eq.Name.Trim(),
                    Equation = eq.Equation.Trim(),
                    ShowInResults = eq.ShowInResults,
                    ItemBankIds = eq.SelectedItemBanks.ConvertAll(ib => ib.Id)
                })
            };

            var response = await BlazEquationTemplateService.AddEquationTemplateAsync(request);

            if (SelectedPaper.Type == PaperType.Adaptive)
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
                Snackbar.Add(response.Message ?? Resource.EquationTemplateCreatedSuccessfully, Severity.Success);
                NavigateBack();
            }
            else
            {
                Snackbar.Add(response?.Message ?? Resource.FailedToCreateEquationTemplate, Severity.Error);
            }

            _isSubmitting = false;
            StateHasChanged();
        }

        private async Task<bool> ConfirmChangeAsync(string message)
        {
            var parameters = new DialogParameters<GenericDialog>
            {
               { x => x.Title, Resource.ConfirmDelete },
               { x => x.Content, message },
               { x => x.SubmitText, Resource.Delete },
               { x => x.CancelText, Resource.Cancel },
               { x => x.SubmitButtonColor, Color.Error },
               { x => x.SubmitButtonStartIcon, Icons.Material.Filled.Delete }
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

        private async Task OnPaperChangedAsync(GetUserPapersDto selectedPaper)
        {
            if (_selectedPaper?.Id != selectedPaper?.Id && _equations.Count > 0 && !await ConfirmChangeAsync(Resource.ConfirmPaperChangeWarning))
            {
                StateHasChanged();
                return;
            }

            _selectedPaper = selectedPaper;
            _paperItemBanks.Clear();
            _forms.Clear();
            _selectedForm = null;
            _equations.Clear();
            _totalEquation = string.Empty;
            _totalEquationError = string.Empty;
            _questionCategories.Clear();
            _categoryFinalScoreNames.Clear();

            if (_selectedPaper == null)
            {
                StateHasChanged();
                return;
            }

            Task<List<ItemBanksFromItemBankPointResponseDto>> itemBanksTask;

            if (selectedPaper.Type == PaperType.Adaptive)
            {
                itemBanksTask = BlazEquationTemplateService.GetAllItemBanksFromQuestionBlocksAsync(_selectedPaper.Id);
            }
            else
            {
                itemBanksTask = BLazPaperService.GetAllItemBanksFromItemBankPointsAsync(_selectedPaper.Id);
            }

            var formsTask = BlazFormService.GetFormsWithoutEquationsByPaperIdAsync(_selectedPaper.Id);
            var categoriesTask = BlazQuestionCategoryService.GetCategoriesByPaperId(_selectedPaper.Id);

            await Task.WhenAll(itemBanksTask, formsTask, categoriesTask);

            _paperItemBanks = await itemBanksTask ?? [];
            _forms = await formsTask ?? [];
            _questionCategories = await categoriesTask ?? [];

            foreach (var category in _questionCategories)
            {
                _categoryFinalScoreNames[category.Id] = category.FinalScoreName ?? string.Empty;
            }

            if (_paperItemBanks.Count == 0)
            {
                Snackbar.Add(Resource.SelectedPaperHasNoItemBanks, Severity.Warning);
            }

            if (_forms.Count == 0)
            {
                Snackbar.Add(Resource.SelectedPaperHasNoForms, Severity.Warning);
            }

            _isLoadingPaper = false;
            StateHasChanged();
        }

        private List<ItemBanksFromItemBankPointResponseDto> GetAvailableItemBanks(EquationDto excludeEquation = null)
        {
            var usedItemBankIds = GetUsedItemBankIds(excludeEquation);
            return [.. _paperItemBanks.Where(ib => !usedItemBankIds.Contains(ib.Id))];
        }

        private List<ItemBanksFromItemBankPointResponseDto> GetUsedItemBanks(EquationDto excludeEquation = null)
        {
            var usedItemBankIds = GetUsedItemBankIds(excludeEquation);
            return [.. _paperItemBanks.Where(ib => usedItemBankIds.Contains(ib.Id))];
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
                    { x => x.ExistingCategories, _equations }, // Pass existing categories
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
                    { x => x.AvailableItemBanks, [] },  // No item banks
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

                if (!await ConfirmChangeAsync(string.Format(Resource.CategoryUsedByOthersWarning, equation.Name, dependentList)))
                {
                    return;
                }
            }

            // Check if total equation needs to be invalidated
            if (!string.IsNullOrEmpty(_totalEquation) && !await ConfirmChangeAsync(string.Format(Resource.EditingCategoryWillRemoveTotalEquation, equation.Name)))
            {
                return;
            }

            var availableItemBanks = GetAvailableItemBanks(equation);
            foreach (var selectedBank in equation.SelectedItemBanks)
            {
                if (!availableItemBanks.Any(ib => ib.Id == selectedBank.Id))
                    availableItemBanks.Add(selectedBank);
            }

            var parameters = new DialogParameters<AddEquationCategoryDialog>
            {
                { x => x.AvailableItemBanks, availableItemBanks },
                { x => x.GloballyUsedItemBanks, GetUsedItemBanks(equation) },
                { x => x.ExistingCategories, _equations },  // Pass existing categories
                { x => x.ExistingEquation, equation },
                { x => x.IsEdit, true }
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
                // Validate duplicate name
                if (_equations.Any(eq => eq.Id != equation.Id && eq.Name.Equals(updatedEquation.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    Snackbar.Add(string.Format(Resource.CategoryWithNameAlreadyExists, updatedEquation.Name), Severity.Warning);
                    return;
                }

                // Clear total equation since category was edited
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

            if (await ConfirmChangeAsync(message))
            {
                _equations.Remove(equation);

                // Clear total equation since a category was deleted
                if (!string.IsNullOrEmpty(_totalEquation))
                {
                    _totalEquation = string.Empty;
                    _totalEquationError = string.Empty;
                }

                Snackbar.Add(string.Format(Resource.ThisEquationTemplateDeletedSuccessfully, equation.Name), Severity.Success);
                StateHasChanged();
            }
        }

        private void NavigateBack()
        {
            NavigationManager.NavigateTo("/EquationTemplateList");
        }

        private static string ConvertPaperToString(GetUserPapersDto p) => p?.Name ?? string.Empty;

        private Task<IEnumerable<GetUserPapersDto>> SearchPapers(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Task.FromResult<IEnumerable<GetUserPapersDto>>(_papers ?? []);

            var result = (_papers ?? []).Where(p => p.Name != null && p.Name.Contains(value, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(result);
        }

        private static string ConvertFormToString(GetFormDto f) => f?.Name ?? string.Empty;

        private Task<IEnumerable<GetFormDto>> SearchForms(string value, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Task.FromResult<IEnumerable<GetFormDto>>(_forms ?? []);

            var result = (_forms ?? []).Where(f => f.Name != null && f.Name.Contains(value, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(result);
        }
    }
}