namespace OES.Helper.Dtos.Question;

public sealed record StandaloneQuestionsResponseDto(
    long Id,
    string QuestionCode,
    string QuestionType,
    string ItemBankName,
    string BlockCodes,
    int TotalRecords
);