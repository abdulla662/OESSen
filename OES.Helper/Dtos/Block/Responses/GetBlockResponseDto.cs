using OES.Helper.Dtos.Question.QuestionDetailsDtos;

namespace OES.Helper.Dtos.Block.Responses
{
    public class GetBlockResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public long DeltaTypeId { get; set; }
        public string DeltaTypeName { get; set; }
        public long LanguageId { get; set; }
        public long BlockTypeId { get; set; }
        public string BlockTypeName { get; set; }
        public long DifficultyProfileId { get; set; }
        public long DifficultyLevelId { get; set; }
        public long QuestionCategoryId { get; set; }
        public string DifficultyLevelName { get; set; }
        public List<long> QuestionsIds { get; set; } = [];
        public List<ApprovedQuestionsPaginationDto> Questions { get; set; } = [];
        public long? QuestionsCount { get; set; }
        public bool InUse { get; set; }
        public bool ConsiderDifficultyLevel { get; set; } = true;


        public GetBlockResponseDto() { }

        public GetBlockResponseDto(long id, string name)
        {
            Id = id;
            Name = name;
        }

        public GetBlockResponseDto(long id,
                                   string name,
                                   string code,
                                   string description,
                                   long deltaTypeId,
                                   long languageId,
                                   string deltaTypeName,
                                   long blockTypeId,
                                   string blockTypeName,
                                   long difficultyProfileId,
                                   long difficultyLevelId,
                                   string difficultyLevelName,
                                   List<ApprovedQuestionsPaginationDto> questions,
                                   long? questionsCount,
                                   bool inUse,
                                   bool considerDifficultyLevel
        )
        {
            Id = id;
            Name = name;
            Code = code;
            Description = description;
            DeltaTypeId = deltaTypeId;
            LanguageId = languageId;
            DeltaTypeName = deltaTypeName;
            BlockTypeId = blockTypeId;
            BlockTypeName = blockTypeName;
            DifficultyProfileId = difficultyProfileId;
            DifficultyLevelId = difficultyLevelId;
            DifficultyLevelName = difficultyLevelName;
            Questions = questions;
            QuestionsCount = questionsCount;
            InUse = inUse;
            ConsiderDifficultyLevel = considerDifficultyLevel;
        }
    }
}
