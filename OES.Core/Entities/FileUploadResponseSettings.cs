using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class FileUploadResponseSettings : BaseEntity<long>
    {
        [Required]
        public long QuestionMetadataId { get; set; }

        public bool ShowAnswerTextArea { get; set; }

        public string SupportedFileExtensions { get; set; }

        public int UploadedFilesCount { get; set; }

        public int SingleFileMaxSizeInMB { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(QuestionMetadataId))]
        public virtual QuestionMetadata QuestionMetadata { get; set; } = null!;
    }
}
