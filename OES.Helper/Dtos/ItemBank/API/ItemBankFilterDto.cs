namespace OES.Helper.Dtos.ItemBank.API
{
    public sealed record ItemBankFilterDto
    {
        public long PaperId { get; }
        public long LanguageId { get; }
        public long? DifficultyProfileId { get; }

        public ItemBankFilterDto(long paperId, long languageId, long? difficultyProfileId)
        {
            PaperId = paperId;
            LanguageId = languageId;
            DifficultyProfileId = difficultyProfileId;
        }
    }
}
