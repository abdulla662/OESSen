namespace OES.Helper.Dtos.ExamServer.BulkSyncReport
{
    public class BulkSyncReport
    {
        public int TotalScheduleVenuePairsToSync { get; set; }
        public int SuccessfulSyncs { get; private set; }
        public int FailedSyncs { get; private set; }
        public int TotalPapersSynced { get; private set; }
        public int TotalFormsSynced { get; private set; }
        public int TotalQuestionsSynced { get; private set; }
        public int TotalCandidatesSynced { get; private set; }
        public List<string> SuccessDetails { get; } = new();
        public List<string> FailureDetails { get; } = new();
        public List<VenueSyncPayload> Payloads { get; set; } = [];

        public void AddSuccessfulSync(VenueSyncPayload payload)
        {
            SuccessfulSyncs++;
            Payloads.Add(payload);
            TotalPapersSynced += payload.Papers.Count;
            TotalFormsSynced += payload.Forms.Count;
            TotalQuestionsSynced += payload.Questions.Count;
            TotalCandidatesSynced += payload.Users.Count;
            SuccessDetails.Add($"OK: Schedule '{payload.Schedule.Name}' (ID: {payload.Schedule.OriginalScheduleId}) -> Venue. Payload: {payload.Papers.Count} papers, {payload.Forms.Count} forms, {payload.Questions.Count} questions, {payload.Users.Count} candidates.");
        }

        public void AddFailedSync(long scheduleId, long venueId, string reason)
        {
            FailedSyncs++;
            FailureDetails.Add($"FAIL: Schedule ID: {scheduleId} -> Venue ID: {venueId}. Reason: {reason}");
        }

        public string GenerateSummaryMessage()
        {
            if (FailedSyncs == 0 && SuccessfulSyncs > 0)
            {
                return $"Bulk sync completed successfully. Synced {SuccessfulSyncs} of {TotalScheduleVenuePairsToSync} schedule-venue assignments. See report for details.";
            }
            if (SuccessfulSyncs > 0 && FailedSyncs > 0)
            {
                return $"Bulk sync completed with partial success. {SuccessfulSyncs} succeeded, but {FailedSyncs} failed. Please check the report for details.";
            }
            if (FailedSyncs > 0 && SuccessfulSyncs == 0)
            {
                return $"Bulk sync failed. All {FailedSyncs} sync operations failed. Please check the report for details.";
            }
            if (SuccessfulSyncs == 0 && FailedSyncs == 0)
            {
                return "Bulk sync finished, but no schedules were found to process.";
            }

            return "Bulk sync process finished.";
        }
    }
}