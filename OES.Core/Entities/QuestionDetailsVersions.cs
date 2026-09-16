using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class QuestionDetailsVersions : BaseEntity<long>
    {
        public int VersionNumber { get; set; } = 0;

        public long QuestionDetailsId { get; set; }

        public string Body { get; set; }

        public string? Instructions { get; set; }

        public string ModelAnswer { get; set; }

        public long QuestionMetadataId { get; set; }

        public long LanguageId { get; set; }

        public bool HasShuffled { get; set; }

        public long? MaxWords { get; set; }

        public int MaxRecordingTimeInSeconds { get; set; }

        public bool UseArabicNumbers { get; set; }

        public string? AttachmentFileName { get; set; }

        public long? SegmentQuestionPropertiesVersionsId { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(SegmentQuestionPropertiesVersionsId))]
        public virtual SegmentQuestionPropertiesVersions? SegmentQuestionPropertiesVersions { get; set; }
    }
}
