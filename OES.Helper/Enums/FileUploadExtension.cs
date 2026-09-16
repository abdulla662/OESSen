using System.Text.Json.Serialization;

namespace OES.Helper.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FileUploadExtension
    {
        PDF = 1,
        DOCX = 2,
        PNG = 3,
        JPEG = 4
    }
}
