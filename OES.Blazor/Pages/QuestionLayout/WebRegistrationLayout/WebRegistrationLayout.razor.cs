using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.WebRegistrationQuestionDtos;
using OES.Helper.Enums;

namespace OES.Blazor.Pages.QuestionLayout.WebRegistrationLayout
{
    public partial class WebRegistrationLayout
    {
        [Parameter] public Breakpoint ScreenSize { get; set; }
        [Parameter] public string ScreenSizeClass { get; set; }
        [Parameter] public LayoutOrientation Orientation { get; set; }
        [Parameter] public QuestionDataDto Model { get; set; }
        [Parameter] public bool ShowCorrectAnswer { get; set; } = true;
        [Parameter] public bool IsReadOnly { get; set; } = false;

        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _email = string.Empty;
        private string _reEmail = string.Empty;
        private string _password = string.Empty;
        private string _rePassword = string.Empty;
        private InputType _passwordInputType = InputType.Password;
        private string _passwordIcon = Icons.Material.Filled.VisibilityOff;

        private InputType _rePasswordInputType = InputType.Password;
        private string _rePasswordIcon = Icons.Material.Filled.VisibilityOff;

        protected override void OnParametersSet()
        {
            if (ShowCorrectAnswer && !string.IsNullOrWhiteSpace(Model?.ModelAnswer))
            {
                var expectedData = System.Text.Json.JsonSerializer.Deserialize<WebRegistrationExpectedDataDto>(Model.ModelAnswer);
                if (expectedData != null)
                {
                    _firstName = expectedData.ExpectedFirstName;
                    _lastName = expectedData.ExpectedLastName;
                    _email = expectedData.ExpectedEmail;
                    _reEmail = expectedData.ExpectedEmail;
                    _password = expectedData.ExpectedPassword;
                    _rePassword = expectedData.ExpectedPassword;
                }
            }
            else
            {
                _firstName = string.Empty;
                _lastName = string.Empty;
                _email = string.Empty;
                _reEmail = string.Empty;
                _password = string.Empty;
                _rePassword = string.Empty;
            }

            StateHasChanged();
        }

        private void TogglePasswordVisibility(bool isRePassword = false)
        {
            if (isRePassword)
            {
                if (_rePasswordInputType == InputType.Password)
                {
                    _rePasswordInputType = InputType.Text;
                    _rePasswordIcon = Icons.Material.Filled.Visibility;
                }
                else
                {
                    _rePasswordInputType = InputType.Password;
                    _rePasswordIcon = Icons.Material.Filled.VisibilityOff;
                }
            }
            else
            {
                if (_passwordInputType == InputType.Password)
                {
                    _passwordInputType = InputType.Text;
                    _passwordIcon = Icons.Material.Filled.Visibility;
                }
                else
                {
                    _passwordInputType = InputType.Password;
                    _passwordIcon = Icons.Material.Filled.VisibilityOff;
                }
            }
        }
    }
}
