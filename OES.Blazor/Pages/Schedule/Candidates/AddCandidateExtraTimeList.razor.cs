using Microsoft.AspNetCore.Components;
using MudBlazor;
using OES.Blazor.Services.Interfaces.Candidate;
using OES.Blazor.Services.Interfaces.Disability;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.ResourceFiles;
using System.Net;

namespace OES.Blazor.Pages.Schedule.Candidates
{
    public partial class AddCandidateExtraTimeList
    {
        [Inject] private IBlazCandidateService BlazCandidateService { get; set; }
        [Inject] private IBlazDisabilityService BlazDisabilityService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private int Key { get; set; } = 0;
        private List<AddOrUpdateDisabilityDto> Disabilities { get; set; } = [];
        private Dictionary<long, long?> CandidateDisabilityMap { get; set; } = [];
        private AddCandidateExtraTimeDto AddCandidateExtraTimeDto { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Disabilities = await BlazDisabilityService.GetAllDisabilities() ?? [];
        }

        private async Task OnDisabilityChanged(long candidateId, long? disabilityId)
        {
            AddCandidateExtraTimeDto = default;
            CandidateDisabilityMap[candidateId] = disabilityId;
            StateHasChanged();
        }

        private async Task SaveCandidateDisability(long candidateId, long? disabilityId)
        {
            AddCandidateExtraTimeDto = new AddCandidateExtraTimeDto(candidateId, disabilityId ?? 0);

            var response = await BlazDisabilityService.AddCandidateExtraTimeAsync(AddCandidateExtraTimeDto);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Snackbar.Add(response.Message, Severity.Success);
                StateHasChanged();
            }
            else
            {
                Snackbar.Add(Resource.Failed, Severity.Error);
            }
        }

        private long? GetSelectedDisabilityForCandidate(object candidateObj)
        {
            if (candidateObj is CandidatesListResponseDto candidate)
            {
                if (CandidateDisabilityMap.TryGetValue(candidate.Id, out var mapped))
                    return mapped;

                if (!string.IsNullOrWhiteSpace(candidate.Disability))
                {
                    if (long.TryParse(candidate.Disability, out var parsedId))
                    {
                        return parsedId;
                    }

                    var match = Disabilities?.FirstOrDefault(d => string.Equals(d.Name, candidate.Disability, StringComparison.OrdinalIgnoreCase));

                    if (match != null)
                        return match.Id;
                }
            }

            return null;
        }
    }
}
