using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.QualityCheckCommittee;
using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.ResourceFiles;

namespace OES.Blazor.Dialogs.QualityCheckCommittee
{
    public partial class CommitteeDetailsDialog
    {
        [Inject] IBlazQualityCheckCommitteeService QualityCheckCommitteeService { get; set; } = default!;

        [Inject] ISnackbar Snackbar { get; set; } = default!;

        [Parameter] public QualityCheckCommitteeDto Model { get; set; } = default!;

        [Parameter] public long CommitteeId { get; set; }

        [Parameter] public bool RightToLeft { get; set; } = false;

        private List<QualityCheckCommitteeMemberDto> QcCommitteeMembers { get; set; } = [];

        private bool IsInitialized { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var committeeDetailsTask = QualityCheckCommitteeService.GetCommitteeByIdAsync(CommitteeId);
            var committeeMembersTask = QualityCheckCommitteeService.GetCommitteeMembersAsync(CommitteeId);

            await Task.WhenAll(committeeDetailsTask, committeeMembersTask);

            var committeeResponse = committeeDetailsTask.Result;
            var membersResponse = committeeMembersTask.Result;

            var committeeData = committeeResponse?.Data as QualityCheckCommitteeDto;
            var membersData = membersResponse?.Data as List<QualityCheckCommitteeMemberDto>;

            bool isDataValid = committeeData != null && membersData != null;

            if (isDataValid)
            {
                Model = committeeData;
                QcCommitteeMembers = [.. membersData.OrderByDescending(member => member.UserId == Model.ChiefId)];
                IsInitialized = true;
            }
            else
            {
                Snackbar.Add(Resource.FailedToLoadCommitteeDetails, Severity.Error);
            }
        }
    }
}
