using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using System.Globalization;
using System.Text.RegularExpressions;

namespace OES.Blazor.Dialogs.EquationTemplate.AddEquationCategoryDialog
{
    public partial class AddEquationCategoryDialog
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }
        [Parameter] public List<ItemBanksFromItemBankPointResponseDto> AvailableItemBanks { get; set; } = [];
        [Parameter] public List<ItemBanksFromItemBankPointResponseDto> GloballyUsedItemBanks { get; set; } = [];
        [Parameter] public List<EquationDto> ExistingCategories { get; set; } = [];
        [Parameter] public EquationDto ExistingEquation { get; set; }
        [Parameter] public bool IsEdit { get; set; }
        [Parameter] public bool IsTotalEquationMode { get; set; }
        [Parameter] public string TotalEquationValue { get; set; }
        [Parameter] public string TotalEquationHiddenValue { get; set; }

        private static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        private string _equationName = string.Empty;
        private bool _isTotalEquation = false;
        private string _rawEquation => BuildDisplayEquation(_hiddenEquation);
        private string _equationError = string.Empty;
        private List<ItemBanksFromItemBankPointResponseDto> _selectedItemBanks = [];
        private string _validationMessage = string.Empty;
        private Severity _validationSeverity = Severity.Normal;
        private readonly List<string> _operators = ["+", "-", "*", "/", "(", ")", "."];
        private readonly List<string> _numbers = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0"];
        private readonly List<string> _functions = ["NC", "ND", "NT"];
        private List<EquationDto> _availableCategories = [];
        private string _hiddenEquation = string.Empty;
        private ElementReference _focusDiv;
        private bool _isInputFocused = false;
        private bool _showInResults = true;

        private IEnumerable<ItemBanksFromItemBankPointResponseDto> AvailableItemBanksPool =>
            AvailableItemBanks?.Where(ib => !_selectedItemBanks.Any(sib => sib.Id == ib.Id) && !ActiveItemBanksPool.Any(sib => sib.Id == ib.Id)) ?? [];
        private IEnumerable<ItemBanksFromItemBankPointResponseDto> ActiveItemBanksPool =>
            GloballyUsedItemBanks.Concat(_selectedItemBanks.Where(sib => !GloballyUsedItemBanks.Any(gib => gib.Id == sib.Id)));
        private IEnumerable<ItemBanksFromItemBankPointResponseDto> DistinctAvailableItemBanks => AvailableItemBanksPool?.Distinct(new ItemBankByIdComparer()) ?? [];
        private IEnumerable<ItemBanksFromItemBankPointResponseDto> DistinctActiveItemBanks => ActiveItemBanksPool?.Distinct(new ItemBankByIdComparer()) ?? [];


        protected override void OnInitialized()
        {
            // Filter out current equation if editing to prevent self-reference
            _availableCategories = [.. ExistingCategories.Where(c => !IsEdit || c.Id != ExistingEquation?.Id)];

            if (IsTotalEquationMode)
            {
                _isTotalEquation = true;
                _hiddenEquation = TotalEquationHiddenValue ?? string.Empty;
                _equationName = "Total";

                // Restore selected item banks from the hidden equation IDs
                if (!string.IsNullOrEmpty(_hiddenEquation))
                {
                    var idPattern = new Regex(@"\[(\d+)\]");
                    foreach (Match m in idPattern.Matches(_hiddenEquation))
                    {
                        if (long.TryParse(m.Groups[1].Value, out long id) && !_selectedItemBanks.Any(x => x.Id == id))
                        {
                            var ib = AvailableItemBanks?.FirstOrDefault(x => x.Id == id)
                                  ?? GloballyUsedItemBanks?.FirstOrDefault(x => x.Id == id);
                            if (ib != null)
                                _selectedItemBanks.Add(ib);
                        }
                    }
                }
            }
            else if (IsEdit && ExistingEquation != null)
            {
                _equationName = ExistingEquation.Name;
                _hiddenEquation = ExistingEquation.Equation;
                _selectedItemBanks = [.. ExistingEquation.SelectedItemBanks];
                _showInResults = ExistingEquation.ShowInResults;
                // Show only categories that appear BEFORE this one in the list
                var currentIndex = ExistingCategories.FindIndex(c => c.Id == ExistingEquation.Id);
                if (currentIndex >= 0)
                {
                    _availableCategories = [.. ExistingCategories.Take(currentIndex)];
                }
                else
                {
                    _availableCategories = [.. ExistingCategories];
                }
            }
            else
            {
                // For new categories, all existing categories are "previous"
                _availableCategories = [.. ExistingCategories];
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender) await _focusDiv.FocusAsync();
        }

        private void AddToEquation(string value)
        {
            _hiddenEquation += value;
            ValidateEquation();
        }

        private void AddFunctionToEquation(string function)
        {
            _hiddenEquation += $"{function}(";
            ValidateEquation();
        }

        private void AddItemBankToEquation(ItemBanksFromItemBankPointResponseDto itemBank)
        {
            if (!_selectedItemBanks.Any(ib => ib.Id == itemBank.Id))
            {
                _selectedItemBanks.Add(itemBank);
            }

            _hiddenEquation += $"[{itemBank.Id}]";
            ValidateEquation();
        }

        private void AddCategoryToEquation(EquationDto category)
        {
            string placeholder = $"[{category.Name}]";
            _hiddenEquation += placeholder;
            ValidateEquation();
        }

        private void ClearEquation()
        {
            _hiddenEquation = string.Empty;
            _selectedItemBanks.Clear();
            _equationError = string.Empty;
            _validationMessage = string.Empty;
        }

        private void Backspace()
        {
            if (string.IsNullOrEmpty(_hiddenEquation)) return;

            if (_hiddenEquation.EndsWith(']'))
            {
                int hiddenStart = _hiddenEquation.LastIndexOf('[');

                if (hiddenStart >= 0)
                {
                    string hiddenPlaceholder = _hiddenEquation.Substring(hiddenStart);
                    _hiddenEquation = _hiddenEquation.Substring(0, hiddenStart);

                    if (int.TryParse(hiddenPlaceholder.Trim('[', ']'), out int ibId) && !_hiddenEquation.Contains($"[{ibId}]"))
                    {
                        var toRemove = _selectedItemBanks.FirstOrDefault(ib => ib.Id == ibId);
                        if (toRemove != null)
                            _selectedItemBanks.Remove(toRemove);
                    }
                }
            }
            else if (_hiddenEquation.Length >= 3)
            {
                bool isFunctionRemoved = false;
                foreach (var function in _functions)
                {
                    string functionWithParen = $"{function}(";
                    if (_hiddenEquation.EndsWith(functionWithParen))
                    {
                        _hiddenEquation = _hiddenEquation.Substring(0, _hiddenEquation.Length - functionWithParen.Length);
                        isFunctionRemoved = true;
                        break;
                    }
                }

                if (!isFunctionRemoved)
                {
                    _hiddenEquation = _hiddenEquation.Substring(0, _hiddenEquation.Length - 1);
                }
            }
            else
            {
                _hiddenEquation = _hiddenEquation.Substring(0, _hiddenEquation.Length - 1);
            }

            ValidateEquation();
        }

        private void ValidateEquation()
        {
            _validationMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(_hiddenEquation))
            {
                _equationError = string.Empty;
                return;
            }

            // Extract placeholders
            var placeholderPattern = new Regex(@"\[([^\]]+)\]");

            var matches = placeholderPattern.Matches(_hiddenEquation);

            // Validate placeholders (item banks OR categories)
            foreach (Match match in matches)
            {
                var placeholderValue = match.Groups[1].Value;

                // Check if it's inside a function like NC([ItemBankId])
                bool isInsideFunction = IsPlaceholderInsideFunction(match.Index);
                bool isNumericId = long.TryParse(placeholderValue, out long ibId);

                if (isInsideFunction)
                {
                    if (!isNumericId)
                    {
                        // They tried to put a non-numeric category placeholder inside a function
                        _equationError = string.Format(Resource.CategoryCannotBeUsedInsideFunction, placeholderValue, GetFunctionNameAtIndex(match.Index));
                        return;
                    }

                    // Must be a valid item bank
                    bool isValidItemBank = _selectedItemBanks.Any(ib => ib.Id == ibId) || AvailableItemBanks.Any(ib => ib.Id == ibId);
                    if (!isValidItemBank)
                    {
                        _equationError = string.Format(Resource.InvalidItemBankDetectedInFunction, placeholderValue);
                        return;
                    }
                }
                else
                {
                    if (isNumericId)
                    {
                        // They put an item bank ID outside a function
                        _equationError = Resource.ItemBanksMustBeWrappedInFunction;
                        return;
                    }

                    // Must be a valid category
                    bool isValidCategory = _availableCategories.Any(c => c.Name == placeholderValue);
                    if (!isValidCategory)
                    {
                        _equationError = string.Format(Resource.InvalidPlaceholderMustBeValidItemBankOrCategory, placeholderValue);
                        return;
                    }
                }
            }

            if (IsEdit && DetectCircularReference(_equationName, _hiddenEquation))
            {
                _equationError = Resource.CircularReferenceDetected;
                return;
            }

            // Replace placeholders with '1' for validation
            var cleanEquation = placeholderPattern.Replace(_hiddenEquation, "1").Replace(" ", "");

            // Replace function names with '1' for validation
            foreach (var function in _functions)
            {
                cleanEquation = Regex.Replace(cleanEquation, $@"{function}\s*\(", "1*(", RegexOptions.IgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(cleanEquation))
            {
                _equationError = string.Empty;
                return;
            }

            // Check for allowed characters (numbers, operators)
            var allowedPattern = new Regex(@"^[0-9+\-*/().]+$");
            if (!allowedPattern.IsMatch(cleanEquation))
            {
                _equationError = Resource.InvalidCharactersDetectedInEquation;
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
                    _equationError = Resource.UnbalancedParenthesesDetected;
                    return;
                }
            }

            if (openCount != 0)
            {
                _equationError = Resource.UnbalancedParenthesesDetected;
                return;
            }

            // Check if equation starts with invalid operators
            if (cleanEquation.Length > 0)
            {
                if ("*/".Contains(cleanEquation[0]))
                {
                    _equationError = Resource.EquationCannotStartWithMultiplyOrDivide;
                    return;
                }
                if ("+-*/".Contains(cleanEquation[^1]))
                {
                    _equationError = Resource.EquationCannotEndWithOperator;
                    return;
                }
            }

            // Check for consecutive operators
            for (int i = 0; i < cleanEquation.Length - 1; i++)
            {
                if ("+-*/".Contains(cleanEquation[i]) && "*/".Contains(cleanEquation[i + 1]))
                {
                    _equationError = Resource.InvalidOperatorSequenceDetected;
                    return;
                }
            }

            _equationError = string.Empty;
        }

        private bool IsPlaceholderInsideFunction(int placeholderIndex)
        {
            if (placeholderIndex < 3) return false; // Not enough space for function name

            // Look back to find if there's a function pattern: FunctionName([
            var beforePlaceholder = _hiddenEquation.Substring(0, placeholderIndex);

            // Check if the last few characters match a function pattern
            var functionPattern = new Regex(@"([A-Za-z]+)\($", RegexOptions.IgnoreCase);
            return functionPattern.IsMatch(beforePlaceholder);
        }

        private bool DetectCircularReference(string currentCategoryName, string equation)
        {
            var placeholderPattern = new Regex(@"\[([^\]]+)\]");
            var matches = placeholderPattern.Matches(equation);

            var visitedCategories = new HashSet<string> { currentCategoryName };
            var toCheck = new Queue<string>();

            // Add all referenced categories to check
            foreach (Match match in matches)
            {
                var placeholderName = match.Groups[1].Value;
                if (_availableCategories.Any(c => c.Name == placeholderName))
                {
                    toCheck.Enqueue(placeholderName);
                }
            }

            // BFS to detect circular reference
            while (toCheck.Count > 0)
            {
                var categoryName = toCheck.Dequeue();

                if (visitedCategories.Contains(categoryName))
                {
                    return true; // Circular reference detected
                }

                visitedCategories.Add(categoryName);

                // Find the category and check its dependencies
                var category = _availableCategories.FirstOrDefault(c => c.Name == categoryName);
                if (category != null && !string.IsNullOrEmpty(category.Equation))
                {
                    var categoryMatches = placeholderPattern.Matches(category.Equation);
                    foreach (Match match in categoryMatches)
                    {
                        var refName = match.Groups[1].Value;
                        if (_availableCategories.Any(c => c.Name == refName))
                        {
                            toCheck.Enqueue(refName);
                        }
                    }
                }
            }

            return false;
        }

        private bool IsValid()
        {
            ValidateEquation();

            if (!string.IsNullOrEmpty(_equationError))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_rawEquation?.Trim()))
            {
                _validationMessage = Resource.EquationIsRequired;
                _validationSeverity = Severity.Error;
                return false;
            }

            if (!_isTotalEquation)
            {
                if (string.IsNullOrWhiteSpace(_equationName?.Trim()))
                {
                    _validationMessage = Resource.CategoryNameIsRequired;
                    _validationSeverity = Severity.Error;
                    return false;
                }

                // Check for duplicate category name
                var trimmedName = _equationName.Trim();
                bool isDuplicateName = ExistingCategories.Any(eq =>
                    (!IsEdit || eq.Id != ExistingEquation?.Id) &&
                    eq.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)
                );

                if (isDuplicateName)
                {
                    _validationMessage = string.Format(Resource.CategoryWithNameAlreadyExists, trimmedName);
                    _validationSeverity = Severity.Error;
                    return false;
                }
            }

            // Extract all placeholders from the equation
            var placeholderPattern = new Regex(@"\[([^\]]+)\]");
            var matches = placeholderPattern.Matches(_hiddenEquation);

            bool hasDirectItemBank = false;
            bool hasReferencedCategory = false;

            foreach (Match match in matches)
            {
                var placeholderValue = match.Groups[1].Value;

                // Check if it's inside a function (item bank reference)
                bool isInsideFunction = IsPlaceholderInsideFunction(match.Index);

                if (isInsideFunction)
                {
                    // This is an item bank reference (we know it has to be a numeric ID from Validation)
                    hasDirectItemBank = true;
                }
                else
                {
                    // Outside a function -> Category reference
                    if (_availableCategories.Any(c => c.Name == placeholderValue))
                    {
                        hasReferencedCategory = true;
                    }
                }
            }

            // Equation must reference at least one item bank (directly or through categories)
            if (!hasDirectItemBank && !hasReferencedCategory)
            {
                _validationMessage = Resource.EquationMustReferenceAtLeastOneItemBank;
                _validationSeverity = Severity.Error;
                return false;
            }

            return true;
        }

        private string GetDisplayMessage()
        {
            if (!string.IsNullOrEmpty(_equationError))
                return _equationError;

            if (!string.IsNullOrEmpty(_validationMessage))
                return _validationMessage;

            return string.Empty;
        }

        private Severity GetMessageSeverity()
        {
            if (!string.IsNullOrEmpty(_equationError))
                return Severity.Error;

            if (!string.IsNullOrEmpty(_validationMessage))
                return _validationSeverity;

            return Severity.Normal;
        }

        private void Submit()
        {
            if (!IsValid())
            {
                return;
            }

            var equation = new EquationDto
            {
                Id = IsEdit ? ExistingEquation.Id : 0,
                Name = _isTotalEquation ? "Total" : _equationName.Trim(),
                Equation = _hiddenEquation.Trim(),
                DisplayEquation = _rawEquation.Trim(),
                SelectedItemBanks = [.. _selectedItemBanks],
                IsTotalEquation = _isTotalEquation,
                ShowInResults = _isTotalEquation ? true : _showInResults
            };

            MudDialog.Close(DialogResult.Ok(equation));
        }

        private string BuildDisplayEquation(string hiddenEquation)
        {
            return EquationCategoryValidator.BuildDisplayEquation(hiddenEquation, id =>
            {
                var ib = _selectedItemBanks.FirstOrDefault(ib => ib.Id == id)
                      ?? AvailableItemBanks?.FirstOrDefault(ib => ib.Id == id)
                      ?? GloballyUsedItemBanks?.FirstOrDefault(ib => ib.Id == id);
                return ib?.Name;
            });
        }

        private string GetFunctionNameAtIndex(int placeholderIndex)
        {
            if (placeholderIndex < 3) return string.Empty;

            var beforePlaceholder = _hiddenEquation.Substring(0, placeholderIndex);
            var functionPattern = new Regex(@"([A-Za-z]+)\($", RegexOptions.IgnoreCase);
            var match = functionPattern.Match(beforePlaceholder);

            return match.Success ? match.Groups[1].Value.ToUpper() : string.Empty;
        }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void OnKeyDown(KeyboardEventArgs e)
        {
            if (_isInputFocused) return;

            var allowed = new HashSet<string> { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+", "-", "*", "/", "(", ")", ".", "Space" };

            if (e.Key == "Backspace")
            {
                Backspace();
                return;
            }

            if (!allowed.Contains(e.Key)) return;

            var ch = e.Key == "Space" ? " " : e.Key;
            _hiddenEquation += ch;
            ValidateEquation();
            StateHasChanged();
        }

        public class ItemBankByIdComparer : IEqualityComparer<ItemBanksFromItemBankPointResponseDto>
        {
            public bool Equals(ItemBanksFromItemBankPointResponseDto x, ItemBanksFromItemBankPointResponseDto y)
                => x?.Id == y?.Id;

            public int GetHashCode(ItemBanksFromItemBankPointResponseDto obj)
                => obj.Id.GetHashCode();
        }
    }
}