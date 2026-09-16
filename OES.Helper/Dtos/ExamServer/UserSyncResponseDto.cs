namespace OES.Helper.Dtos.ExamServer
{
    public class UserSyncResponseDto
    {
        public long OriginalUserId { get; set; }

        public string DisplayName { get; set; }

        public string Email { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string PhoneNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string CreatedBy { get; set; }
    }
}