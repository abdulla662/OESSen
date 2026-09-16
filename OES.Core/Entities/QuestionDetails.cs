using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities
{
    public class QuestionDetails : BaseEntity<long>
    {
        [Required]
        public string Body { get; set; }

        // public string? Choices { get; set; }

        public string? Instructions { get; set; }

        public string ModelAnswer { get; set; }

        public long QuestionMetadataId { get; set; }

        public long LanguageId { get; set; }

        public bool HasShuffled { get; set; }

        public long? SegmentQuestionPropertiesId { get; set; }

        public long? MaxWords { get; set; }

        public int MaxRecordingTimeInSeconds { get; set; }

        public bool UseArabicNumbers { get; set; }

        /// <summary>
        /// This property stores the file name of the attachment associated with the question language variant, in old system.
        /// </summary>
        public string? AttachmentFileName { get; set; }


        // Navigational Properties

        [ForeignKey(nameof(QuestionMetadataId))]
        public virtual QuestionMetadata QuestionMetadata { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }

        //public virtual ICollection<QuestionModelAnswer> QuestionModelAnswers { get; set; }

        public virtual ICollection<QuestionsChoices> QuestionsChoices { get; set; } = [];

        public virtual ICollection<MatchingPairQuestionItems> MatchingPairQuestionItems { get; set; } = [];

        [ForeignKey(nameof(SegmentQuestionPropertiesId))]
        public virtual SegmentQuestionProperties? SegmentQuestionProperties { get; set; }
    }
}
