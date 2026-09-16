using OES.Helper.Dtos.EquationTemplate;

namespace OES.Helper.Dtos.Candidate
{
    public class SaveFileToCTRDto
    {
        public string FileName { get; set; }

        public string Base64Content { get; set; }

        public List<FormVenueCombination> Combinations { get; set; } = [];

        public List<long> RegistrationIds { get; set; } = [];
    }
}