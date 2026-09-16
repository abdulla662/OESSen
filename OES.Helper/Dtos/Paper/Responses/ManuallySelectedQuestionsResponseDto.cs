using OES.Helper.Enums;

namespace OES.Helper.Dtos.Paper.Responses
{
    public class ManuallySelectedQuestionsResponseDto
    {
        public long Id { get; set; }
        public long QuestionMetadataId { get; set; }
        public string Body { get; set; }
        public string Code { get; set; }
        public long LanguageId { get; set; }
        public string SectionName { get; set; }
        public string SectioningIdentifier { get; set; }
        public long ItemBankId { get; set; }
        public string ItemBankName { get; set; }
        public long DifficultyLevelId { get; set; }
        public string DifficultyLevelName { get; set; }
        public double? Mark { get; set; } = 0.0;
        public decimal? DeltaValue { get; set; } = 0.0m;
        public PaperQuestionStatus PaperQuestionStatus { get; set; }
        public long? SectionId { get; set; } // NOTE: You may find this property null in some paper stepper steps; as it is used mainly in the 'Marking Scheme' step.
        public long? FormId { get; set; } // NOTE: You may find this property null in some paper stepper steps; as it is used mainly in the 'Marking Scheme' step.
        public int SubQuestionsCount { get; set; }
        public QuestionType QuestionType { get; set; }
        public List<ManuallySelectedQuestionsResponseDto> SubQuestions { get; set; } = [];


        public ManuallySelectedQuestionsResponseDto()
        {

        }

        public ManuallySelectedQuestionsResponseDto(long id,
                                                    long questionMetadataId,
                                                    string body,
                                                    string code,
                                                    long languageId,
                                                    string sectioningIdentifier,
                                                    string sectionName,
                                                    long itemBankId,
                                                    string itemBankName,
                                                    long difficultyLevelId,
                                                    string difficultyLevelName,
                                                    double? mark,
                                                    decimal? deltaValue,
                                                    PaperQuestionStatus paperQuestionStatus,
                                                    long? sectionId,
                                                    long? formId,
                                                    int subQuestionsCount,
                                                    QuestionType questionType,
                                                    List<ManuallySelectedQuestionsResponseDto> subQuestions)
        {
            Id = id;
            QuestionMetadataId = questionMetadataId;
            Body = body;
            Code = code;
            LanguageId = languageId;
            SectioningIdentifier = sectioningIdentifier;
            SectionName = sectionName;
            ItemBankId = itemBankId;
            ItemBankName = itemBankName;
            DifficultyLevelId = difficultyLevelId;
            DifficultyLevelName = difficultyLevelName;
            Mark = mark;
            DeltaValue = deltaValue;
            PaperQuestionStatus = paperQuestionStatus;
            SectionId = sectionId;
            FormId = formId;
            SubQuestionsCount = subQuestionsCount;
            QuestionType = questionType;
            SubQuestions = subQuestions ?? [];
        }
    }
}