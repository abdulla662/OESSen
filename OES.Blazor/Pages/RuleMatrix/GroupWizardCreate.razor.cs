using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.RuleMatrix
{
    public partial class GroupWizardCreate
    {
        [Inject] NavigationManager Navigation { get; set; } = default!;

        [Inject] ISnackbar Snackbar { get; set; } = default!;

        private void OnSaved(string groupName)
        {
            Snackbar.Add(string.Format(Resource.GroupCreatedSuccessfully, groupName), Severity.Success);

            Navigation.NavigateTo("/GroupWizardList");
        }

        private void OnCanceled()
        {
            Navigation.NavigateTo("/GroupWizardList");
        }
    }
}