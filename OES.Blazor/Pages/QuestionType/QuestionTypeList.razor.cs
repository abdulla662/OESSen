using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.QuestionType;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Pages.QuestionType
{
    public partial class QuestionTypeList : ComponentBase
    {
        [Inject] private IBLazQuestionType _BLazQuestionType { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private long Id;
        private bool isDialogOpen;
        private int key;

        private void OpenDeleteDialog(object id)
        {
            if (id is long parsedId)
            {
                Id = parsedId;

                isDialogOpen = true;
            }
            else
            {
                Snackbar.Add(Resource.FailedToFetchQuestionTypedetails, Severity.Error);
            }
        }

        private async Task ConfirmDelete()
        {
            isDialogOpen = false;

            if (Id != 0)
            {
                var response = await _BLazQuestionType.DeleteQuestionType(Id);

                if (response != null)
                {
                    key++;

                    Snackbar.Add(Resource.QuestionTypeDeletedSuccessfully, Severity.Success);
                }
                else
                {
                    Snackbar.Add(Resource.FailedToDeleteQuestionType, Severity.Error);
                }
            }
        }
    }
}
