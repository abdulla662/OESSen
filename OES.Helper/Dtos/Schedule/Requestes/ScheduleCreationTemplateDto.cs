using OES.Helper.Enums;
using OES.Helper.ResourceFiles;
using System.ComponentModel.DataAnnotations;

namespace OES.Helper.Dtos.Schedule.Requestes
{
    public class ScheduleCreationTemplateDto
    {
        public string TemplateName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = "Thisfieldisrequired")]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resource), ErrorMessageResourceName = "Thisfieldisrequired")]
        public string Code { get; set; }

        public string Description { get; set; }

        [Required]
        public ScheduleLocation ScheduleLocation { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public List<long> LanguageIds { get; set; } = [];

        [Required]
        public List<long> ExamVenueIds { get; set; } = [];
    }
}
