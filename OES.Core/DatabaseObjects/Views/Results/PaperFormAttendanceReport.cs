using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.Results
{
    public class PaperFormAttendanceReport : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW vw_PaperFormAttendanceReportView AS
                WITH demo_candidates AS (
                    SELECT c.Id AS CandidateId
                    FROM candidates c
                    WHERE c.Name LIKE '%demo%'
                       OR c.Name LIKE '%test%'
                ),
                base_attendance AS (
                    SELECT
                        ced.PaperFormIdActual,
                        v.Id                                AS VenueId,
                        DATE(ced.CandidateExamDate)         AS ExamDate,
                        SUM(CASE WHEN ced.CandidateStartedExam = 1 THEN 1 ELSE 0 END)                       AS Attended,
                        SUM(CASE WHEN ced.CandidateStartedExam = 1 AND ced.CandidateEndedExam = 1
                                 THEN 1 ELSE 0 END)                                                         AS TakenSession,
                        SUM(CASE WHEN ced.ExamEndedBySystem = 1 THEN 1 ELSE 0 END)                          AS UnfinishedSession,
                        SUM(CASE WHEN ced.ReviewStatus > 1 THEN 1 ELSE 0 END)                               AS ReviewedCount,
                        ROUND(AVG(CASE WHEN ced.PaperQuestionsCount > 0
                                       THEN (ced.QuestionsAnsweredCount / ced.PaperQuestionsCount) * 100
                                  END), 2)                                                                  AS AvgCompletionPercentage,
                        AVG(ced.TotalElapsedTimeInSeconds / 60.0)                                           AS AvgTimeSpentMinutes,
                        SUM(CASE WHEN ced.SentToCTR = 1 THEN 1 ELSE 0 END)                                  AS NumSentToCTR,
                        SUM(CASE WHEN ced.HasNoTrial = 1 OR ced.CandidateStartedExam = 0
                                 THEN 1 ELSE 0 END)                                                         AS NoTrialOrNotStarted
                    FROM candidateexamdetails ced
                    JOIN venues v ON v.Code = ced.VenueCode
                    WHERE ced.IsDeleted = 0
                      AND ced.IsDemo    = 0
                      AND ced.CandidateId NOT IN (SELECT CandidateId FROM demo_candidates)
                    GROUP BY ced.PaperFormIdActual, v.Id, DATE(ced.CandidateExamDate)
                ),
                base_registered AS (
                    SELECT
                        s.PaperFormId,
                        s.VenueId                           AS VenueId,
                        DATE(s.CandidateExamDate)           AS ExamDate,
                        sp.PaperId                          AS PaperId,
                        sp.StartDate                        AS PaperStartDate,
                        sp.EndDate                          AS PaperEndDate,
                        sp.ScheduleMetadataId               AS ScheduleId,
                        sm.Name                             AS ScheduleName,
                        sm.Code                             AS ScheduleCode,
                        COUNT(*)                            AS Allocated
                    FROM schedulepaperscandidates s
                    JOIN schedulepapers   sp ON sp.Id = s.SchedulePaperId
                    JOIN schedulemetadata sm ON sm.Id = sp.ScheduleMetadataId
                    WHERE s.IsDeleted = 0
                      AND s.CandidateId NOT IN (SELECT CandidateId FROM demo_candidates)
                    GROUP BY s.PaperFormId, s.VenueId, DATE(s.CandidateExamDate),
                             sp.PaperId, sp.StartDate, sp.EndDate,
                             sp.ScheduleMetadataId, sm.Name, sm.Code
                )
                SELECT
                    r.PaperId                                          AS PaperId,
                    p.Code                                             AS ExamSeries,
                    p.Code                                             AS PaperCode,
                    p.Name                                             AS PaperName,
                    r.PaperFormId                                      AS PaperFormId,
                    f.Name                                             AS PaperFormName,
                    v.Code                                             AS VenueCode,
                    v.DisplayName                                      AS VenueDisplayName,
                    r.ExamDate                                         AS ExamDate,
                    r.Allocated                                        AS Allocated,
                    COALESCE(a.Attended, 0)                            AS Attended,
                    (r.Allocated - COALESCE(a.Attended, 0))            AS Absent,
                    COALESCE(a.TakenSession, 0)                        AS TakenSession,
                    COALESCE(a.UnfinishedSession, 0)                   AS UnfinishedSession,
                    CONCAT(
                        COALESCE(a.ReviewedCount, 0),
                        '/',
                        COALESCE(a.UnfinishedSession, 0)
                    )                                                  AS Reviewed,
                    a.AvgCompletionPercentage                          AS AvgCompletionPercentage,
                    a.AvgTimeSpentMinutes                              AS AvgTimeSpentMinutes,
                    p.Duration                                         AS PaperDuration,
                    r.PaperStartDate                                   AS PaperStartDate,
                    r.PaperEndDate                                     AS PaperEndDate,
                    r.ScheduleId                                       AS ScheduleId,
                    r.ScheduleName                                     AS ScheduleName,
                    r.ScheduleCode                                     AS ScheduleCode,
                    COALESCE(a.NumSentToCTR, 0)                        AS NumSentToCTR,
                    CASE
                        WHEN (COALESCE(a.Attended, 0) + COALESCE(a.NoTrialOrNotStarted, 0))
                             = COALESCE(a.NumSentToCTR, 0)
                        THEN 1 ELSE 0
                    END                                                AS SentToCTR
                FROM base_registered r
                LEFT JOIN base_attendance a
                       ON a.PaperFormIdActual = r.PaperFormId
                       AND a.VenueId          = r.VenueId
                       AND a.ExamDate         = r.ExamDate
                JOIN forms          f ON f.Id = r.PaperFormId
                JOIN papermetadata  p ON p.Id = f.PaperId
                JOIN venues         v ON v.Id = r.VenueId;
        ";
    }
}