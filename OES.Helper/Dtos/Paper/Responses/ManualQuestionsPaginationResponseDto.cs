using OES.Helper;
using OES.Helper.Enums;

public class ManualQuestionsPaginationResponseDto : IEquatable<ManualQuestionsPaginationResponseDto>
{
    public long QuestionMetadataId { get; set; }
    public string Body { get; set; }
    public string Code { get; set; }
    public string QuestionType { get; set; }
    public string QuestionTypeDisplay => QuestionType?.ToLocalizedString<QuestionType>();
    public long ItemBankPointId { get; set; }
    public long ItemBankId { get; set; }
    public string ItemBankName { get; set; }
    public long DifficultyLevelId { get; set; }
    public string DifficultyLevelName { get; set; }
    public PaperQuestionStatus PaperQuestionStatus { get; set; } = PaperQuestionStatus.Used;
    public long PaperItemBankQuestionId { get; set; } // Make this property in this order, because of the generic paginated list.
    public long? SectionId { get; set; } // Used to identify the questions that are not sectioned yet (e.g., null means not sectioned)
    public long? FormId { get; set; } // Used to identify the form id of a specific question (e.g., null means not bound to a form yet)
    public Guid InstanceId { get; set; } = Guid.NewGuid();
    public int UsedInFormsCount { get; set; } = 0;
    public int SubQuestionsCount { get; set; } = 1;

    public ManualQuestionsPaginationResponseDto()
    {

    }

    public ManualQuestionsPaginationResponseDto(long questionMetadataId,
                                                string body,
                                                string code,
                                                string questionType,
                                                long itemBankPointId,
                                                long itemBankId,
                                                string itemBankName,
                                                long difficultyLevelId,
                                                string difficultyLevelName,
                                                int usedInFormsCount,
                                                int subQuestionsCount)
    {
        QuestionMetadataId = questionMetadataId;
        Body = body;
        Code = code;
        QuestionType = questionType;
        ItemBankPointId = itemBankPointId;
        ItemBankId = itemBankId;
        ItemBankName = itemBankName;
        DifficultyLevelId = difficultyLevelId;
        DifficultyLevelName = difficultyLevelName;
        UsedInFormsCount = usedInFormsCount;
        SubQuestionsCount = subQuestionsCount < 1 ? 1 : subQuestionsCount;
    }

    public ManualQuestionsPaginationResponseDto(long questionMetadataId,
                                                string body,
                                                string code,
                                                string questionType,
                                                long itemBankPointId,
                                                string itemBankName,
                                                long difficultyLevelId,
                                                string difficultyLevelName,
                                                PaperQuestionStatus paperQuestionStatus,
                                                long paperItemBankQuestionId,
                                                long? sectionId,
                                                long? formId,
                                                int subQuestionsCount)
    {
        QuestionMetadataId = questionMetadataId;
        Body = body;
        Code = code;
        QuestionType = questionType;
        ItemBankPointId = itemBankPointId;
        ItemBankName = itemBankName;
        DifficultyLevelId = difficultyLevelId;
        DifficultyLevelName = difficultyLevelName;
        PaperQuestionStatus = paperQuestionStatus;
        PaperItemBankQuestionId = paperItemBankQuestionId;
        SectionId = sectionId;
        FormId = formId;
        SubQuestionsCount = subQuestionsCount < 1 ? 1 : subQuestionsCount;
    }

    public bool Equals(ManualQuestionsPaginationResponseDto other)
        => other != null && InstanceId == other.InstanceId;

    public override bool Equals(object obj)
        => obj != null && Equals(obj as ManualQuestionsPaginationResponseDto);

    public override int GetHashCode()
        => InstanceId.GetHashCode();
}