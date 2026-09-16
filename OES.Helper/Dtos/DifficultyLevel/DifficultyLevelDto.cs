namespace OES.Helper.Dtos.DifficultyLevel
{
    public sealed record DifficultyLevelDto
    {
        public long Id { get; set; }

        public long questionCount { get; set; }

        public string Name { get; set; }

        public decimal FromDelta { get; set; }

        public decimal ToDelta { get; set; }

        public long? DifficultyProfileId { get; set; }

        public string DifficultyProfileName { get; set; }

        public string DifficultyProfileDescription { get; set; }

        public long DeltaTypeId { get; set; }

        public string DeltaTypeName { get; set; }

        public string CreationUser { get; set; }

        public string FromDeltaFormatted => FromDelta.ToString("F9");

        public string ToDeltaFormatted => ToDelta.ToString("F9");

        public decimal FromDeltaRounded => Math.Round(FromDelta, 2);

        public decimal ToDeltaRounded => Math.Round(ToDelta, 2);
    }
}
