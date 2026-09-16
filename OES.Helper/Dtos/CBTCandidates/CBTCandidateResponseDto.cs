namespace OES.Helper.Dtos.CBTCandidates
{
    public class CBTCandidateResponseDto
    {
        public string NationalId { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string ThirdName { get; set; }
        public string LastName { get; set; }
        public string FirstEnglishName { get; set; }
        public string MiddleEnglishName { get; set; }
        public string ThirdEnglishName { get; set; }
        public string LastEnglishName { get; set; }
        public string CenterArabicName { get; set; }
        public string CenterEnglishName { get; set; }
        public string CenterCode { get; set; }
        public string CityArabicName { get; set; }
        public string CityEnglishName { get; set; }
        public string RegionArabicName { get; set; }
        public string RegionEnglishName { get; set; }
        public string ExamArabicName { get; set; }
        public string ExamEnglishName { get; set; }
        public DateTime ExamDate { get; set; }
        public string ExamTypeArabicName { get; set; }
        public string ExamTypeEnglishName { get; set; }
        public long RegistrationId { get; set; }
        public string ShowStatus { get; set; }
        public string ExamSeriesCode { get; set; }
    }
}