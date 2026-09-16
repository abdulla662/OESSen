namespace OES.Helper.Dtos.Results
{
    public sealed record VenueAttendanceVerificationRequest
    {
        public DateOnly ExamDate { get; set; }
    }
}
