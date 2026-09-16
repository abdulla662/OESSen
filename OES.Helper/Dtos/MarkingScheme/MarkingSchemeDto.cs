using OES.Helper.Enums;

namespace OES.Helper.Dtos.MarkingScheme
{
    public sealed record MarkingSchemeDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public ScoreSchemaType ScoreType { get; set; } = ScoreSchemaType.EqualDistribution;
        public string Data { get; set; }
        public long PaperId { get; set; }
    }
}