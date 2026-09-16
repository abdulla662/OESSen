using System.Text.Json.Serialization;

namespace OES.Helper.Dtos.Folder.Response
{
    public sealed class FolderDocumentListResponseDto
    {
        public Guid Id { get; init; }
        public string Name { get; set; }
        public string Type { get; init; }
        public long? Size { get; init; }
        public bool IsFolder { get; init; }

        [JsonConstructor]
        public FolderDocumentListResponseDto(Guid id, string name, string type, long? size, bool isFolder)
        {
            Id = id;
            Name = name;
            Type = type;
            Size = size;
            IsFolder = isFolder;
        }
    }
}
