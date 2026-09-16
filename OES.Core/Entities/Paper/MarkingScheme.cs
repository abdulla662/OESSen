using OES.Helper.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace OES.Core.Entities.Paper
{
    public class MarkingScheme : BaseEntity<long>
    {
        public string Name { get; set; }

        public ScoreSchemaType ScoreType { get; set; }

        public long PaperId { get; set; }

        // A JSON object to fill in data for each question and its score depending on the schema type
        public string Data { get; set; }
        // In case of using different scheme; what if the user changed the profile difficulty levels
        // Example: Let a profile X with the following levels {Hard, Medium, VeryHard}
        // and scheme distribution {Hard: 20, Medium: 10, VeryHard: 10}
        // then the user changed the profile to be profile X {Hard, Medium}
        // solution Profiles never changed after using it 

        [ForeignKey(nameof(PaperId))]
        public virtual PaperMetadata PaperMetadata { get; set; }
    }
}
