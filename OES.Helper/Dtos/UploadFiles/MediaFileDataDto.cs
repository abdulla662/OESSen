
namespace OES.Helper.Dtos.UploadFiles
{
    public class MediaFileDataDto
    {
        public string Name { get; set; } = null!;

        public string Type { get; set; } = null!;

        public byte[] Content { get; set; } = null!;

        public long Size { get; set; }
    }
}
