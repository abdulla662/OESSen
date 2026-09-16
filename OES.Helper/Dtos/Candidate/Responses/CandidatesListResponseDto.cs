namespace OES.Helper.Dtos.Candidate.Responses
{
    public sealed record CandidatesListResponseDto(long Id,
                                                   string Name,
                                                   string Email,
                                                   string UserName,
                                                   string MobileNumber,
                                                   string CandidateCode,
                                                   string NationalId,
                                                   string Disability = "");
}
