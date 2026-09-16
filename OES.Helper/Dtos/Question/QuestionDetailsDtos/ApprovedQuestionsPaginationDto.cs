namespace OES.Helper.Dtos.Question.QuestionDetailsDtos
{
    public class ApprovedQuestionsPaginationDto : IEquatable<ApprovedQuestionsPaginationDto>
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Subject { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string ItemBank { get; set; }
        public string DifficultyProfile { get; set; }
        public string DifficultyLevel { get; set; }
        public string Body { get; set; }
        public string Language { get; set; }

        public ApprovedQuestionsPaginationDto() { }

        public ApprovedQuestionsPaginationDto(long id,
                                              string code,
                                              string subject,
                                              string type,
                                              string category,
                                              string itemBank,
                                              string difficultyProfile,
                                              string difficultyLevel,
                                              string body,
                                              string language)
        {
            Id = id;
            Code = code;
            Subject = subject;
            Type = type;
            Category = category;
            ItemBank = itemBank;
            DifficultyProfile = difficultyProfile;
            DifficultyLevel = difficultyLevel;
            Body = body;
            Language = language;
        }

        public bool Equals(ApprovedQuestionsPaginationDto other)
            => other != null && Id == other.Id;

        public override bool Equals(object obj)
            => obj != null && Equals(obj as ApprovedQuestionsPaginationDto);

        public override int GetHashCode()
            => Id.GetHashCode();
    }
}
