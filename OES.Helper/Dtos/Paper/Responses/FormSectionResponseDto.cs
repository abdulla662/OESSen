namespace OES.Helper.Dtos.Paper.Responses
{
    public sealed record FormSectionResponseDto(
        long Id,
        string Name,
        long? FormId
    );
}
