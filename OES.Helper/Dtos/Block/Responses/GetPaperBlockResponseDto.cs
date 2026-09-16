namespace OES.Helper.Dtos.Block.Responses
{
    public sealed record GetPaperBlockResponseDto(long Id,
                                                  string Name,
                                                  long DifficultyLevelId,
                                                  long DeltaTypeId,
                                                  string DeltaTypeName,
                                                  long QuestionCategoryId,
                                                  List<long> QuestionsMetadataIds
    );
}
