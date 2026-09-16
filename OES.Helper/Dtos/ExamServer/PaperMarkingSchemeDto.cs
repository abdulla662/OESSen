using OES.Helper.Enums;
using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.ExamServer
{
    public class PaperMarkingSchemeDto
    {
        public long Id { get; set; }

        public string Name { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ScoreSchemaType ScoreType { get; set; }

        public string Data { get; set; }
    }
}