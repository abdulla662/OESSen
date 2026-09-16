using System.Text.Json.Serialization;

namespace OES.Helper.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ExamDateFilter
    {
        Today,
        Tomorrow
    }
}
