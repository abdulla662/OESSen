using Microsoft.AspNetCore.Http;

namespace OES.Helper.Dtos.Candidate.Responses
{
    public class CandidateDto
    {
        public long CandidateId { get; set; }

        public string CandidateCode { get; set; }

        public string Name { get; set; }

        public string NationalId { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string Qualification { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string Address { get; set; }

        public string Mobile { get; set; }

        public string Email { get; set; }

        public int Gender { get; set; }

        public string RegistrationCenterCode { get; set; }

        public long? PaperId { get; set; }

        public DateTime? RegistrationDateTime { get; set; }

        public IFormFile PhotoFile { get; set; }

        public IFormFile SignatureFile { get; set; }

        public string SignatureURL { get; set; }

        public string PhotoURL { get; set; }

        public bool HasDisability { get; set; }

        public long? DisabilityId { get; set; }
    }
}