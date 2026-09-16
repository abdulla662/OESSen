using System.ComponentModel.DataAnnotations;
using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.ResourceFiles;

namespace OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos
{
    public class SubQuestionMetadataDto
    {
        [Range(1, int.MaxValue)]
        public long ParentId { get; set; }

        [Range(1, int.MaxValue)]
        public long QuestionTypeId { get; set; }

        public string? Code { get; set; }

        [Range(0.0, 1.0, ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = nameof(Resource.ValueMustBeBetween0And1))]
        public decimal Delta { get; set; }

        public FileUploadSettingsDto? FileUploadSettings { get; set; } = new();
    }
}
