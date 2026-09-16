using Microsoft.AspNetCore.Components;
using OES.Helper.Dtos.Candidate.Requests;

namespace OES.Blazor.Dialogs.Candidate
{
    public partial class CandidateViewDialog
    {
        [Parameter] public AddOrUpdateCandidateRequestDto Candidate { get; set; } = new();
    }
}