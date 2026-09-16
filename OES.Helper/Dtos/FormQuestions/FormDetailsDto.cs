namespace OES.Helper.Dtos.FormQuestions
{
    public record FormDetailsDto(
        long FormId,
        string FormName,
        string FormCode,
        string FormDescription
    );
}
