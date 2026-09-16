using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Candidates
{
    internal class GetTrackingAndReviewingDetails : IDatabaseStoredProcedure
    {
        // TODO - Salah: Avoid literal strings
        public string DropCommand => "DROP PROCEDURE IF EXISTS `sp_GetTrackingAndReviewingDetails`;";

        public string CreateCommand => @"
            CREATE PROCEDURE `sp_GetTrackingAndReviewingDetails`(
                 IN p_PageIndex INT,
					IN p_PageSize INT,
					IN p_PaginationOff TINYINT,
					IN p_SearchKey VARCHAR(255),
					IN p_SearchInName TINYINT,
					IN p_FromDate DATETIME,
					IN p_ToDate DATETIME,
					IN p_OrderBy VARCHAR(10),
					IN p_ReviewStatusList VARCHAR(50)
            )
            BEGIN
                DECLARE v_Offset INT;
                DECLARE v_Limit BIGINT;
                DECLARE v_SearchKey VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

                SET v_Offset = p_PageIndex * p_PageSize;
                SET v_Limit = CASE WHEN p_PageSize > 0 THEN p_PageSize ELSE 10 END;
                SET v_SearchKey = p_SearchKey;

                IF p_PaginationOff = 1 THEN

                    -- Pagination OFF: return all matching rows, no LIMIT/OFFSET
                    WITH FilteredCandidates AS (
                        SELECT
                            c.*,
                            COUNT(*) OVER() AS TotalRecords
                        FROM CandidateExamDetails c
                        WHERE 
                        (
                            p_ReviewStatusList IS NULL
                            OR p_ReviewStatusList = ''
                            OR FIND_IN_SET(c.ReviewStatus, p_ReviewStatusList) > 0
                        )
                          AND c.IsDemo = 0
                          AND c.CandidateExamDate >= '2026-05-01'
                          AND (p_FromDate IS NULL OR c.CandidateCreatedAt >= p_FromDate)
                          AND (p_ToDate IS NULL OR c.CandidateCreatedAt <= p_ToDate)
                          AND (
                              p_SearchKey IS NULL OR p_SearchKey = '' OR
                              (
                                  CASE WHEN p_SearchInName = 1 THEN
                                      LOWER(c.CandidateDisplayName) LIKE CONCAT('%', LOWER(v_SearchKey), '%')
                                  ELSE
                                      LOWER(c.CandidateDisplayName) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      LOWER(c.VenueCode) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      c.CandidateNationalId LIKE CONCAT('%', v_SearchKey, '%') OR
                                      LOWER(c.CandidatePhoneNumber) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      LOWER(c.CandidateEmail) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      LOWER(c.PaperName) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      LOWER(c.PaperCode) LIKE CONCAT('%', LOWER(v_SearchKey), '%')
                                  END
                              )
                          )
                    )
                    SELECT
                        fc.Id,
                        fc.RegistrationId,
                        fc.CandidateId,
                        fc.CandidateCode,
                        fc.CandidateNationalId,
                        fc.CandidateDisplayName,
                        fc.CandidateEmail,
                        fc.CandidatePhoneNumber,
                        fc.PaperIdActual,
                        fc.OriginalPaperId,
                        fc.PaperName,
                        fc.PaperCode,
                        fc.PaperStartDate,
                        fc.PaperEndDate,
                        fc.CandidateExamDate,
                        fc.PaperFormIdActual,
                        fc.PaperFormName,
                        fc.FinalScore,
                        fc.ScheduleId AS ScheduleIdActual,
                        fc.ScheduleName,
                        fc.ScheduleCode,
                        fc.ExamTrialId,
                        fc.TrialNumber,
                        fc.CandidateStartedExam,
                        fc.CandidateEndedExam,
                        fc.ExamTrialStartDate,
                        fc.ExamTrialEndDate,
                        fc.CandidateCurrentlyInExam,
                        fc.PaperDuration AS TotalExamDuration,
                        0 AS TotalExamSessions,
                        fc.TotalQuestionsInDatabase,
                        fc.QuestionsAnsweredCount,
                        fc.QuestionsMarkedForReview,
                        fc.QuestionsVisited,
                        fc.TotalElapsedTimeInSeconds,
                        fc.ReviewStatus,
                        fc.ReviewedAt,
                        fc.ReviewedBy,
                        fc.SentToCTR,
                        fc.SentToCTRAt,
                        fc.SentToCTRBy,
                        fc.VenueCode,
                        fc.CandidateCreatedAt,
                        fc.TrackingLogsJSON,
                        fc.TotalRecords,
                        (SELECT COUNT(*) FROM CandidateQuestionsAnswers a WHERE a.RegistrationId = fc.RegistrationId AND a.AnswerId IS NOT NULL AND a.IsCorrect = 1 AND a.QuestionType <> 'Comprehension') AS CorrectAnswers,
                        (SELECT COUNT(*) FROM CandidateQuestionsAnswers a WHERE a.RegistrationId = fc.RegistrationId AND a.AnswerId IS NOT NULL AND a.IsCorrect = 0 AND a.QuestionType <> 'Comprehension') AS IncorrectAnswers,
                        (CASE 
                            WHEN fc.PaperType = 1 THEN
                                (SELECT COUNT(*) FROM AdaptiveSection s INNER JOIN Stages st ON s.StageId = st.Id WHERE st.FormId = fc.PaperFormIdActual AND s.IsDeleted = 0)
                            ELSE
                                (SELECT COUNT(*) FROM Sections s WHERE s.PaperId = fc.PaperIdActual AND s.IsDeleted = 0)
                         END) AS TotalSections
                    FROM FilteredCandidates fc
                    ORDER BY
                        CASE WHEN p_OrderBy = 'DESC' THEN fc.CandidateCreatedAt END DESC,
                        CASE WHEN p_OrderBy <> 'DESC' OR p_OrderBy IS NULL THEN fc.CandidateCreatedAt END ASC;

                ELSE

                    -- Pagination ON: same query, with LIMIT/OFFSET
                    WITH FilteredCandidates AS (
                        SELECT
                            c.*,
                            COUNT(*) OVER() AS TotalRecords
                        FROM CandidateExamDetails c
                        WHERE 
                        (
                            p_ReviewStatusList IS NULL
                            OR p_ReviewStatusList = ''
                            OR FIND_IN_SET(c.ReviewStatus, p_ReviewStatusList) > 0
                        )
                          AND c.IsDemo = 0
                          AND c.CandidateExamDate >= '2026-05-01'
                          AND (p_FromDate IS NULL OR c.CandidateCreatedAt >= p_FromDate)
                          AND (p_ToDate IS NULL OR c.CandidateCreatedAt <= p_ToDate)
                          AND (
                              p_SearchKey IS NULL OR p_SearchKey = '' OR
                              (
                                  CASE WHEN p_SearchInName = 1 THEN
                                      LOWER(c.CandidateDisplayName) LIKE CONCAT('%', LOWER(v_SearchKey), '%')
                                  ELSE
                                      LOWER(c.CandidateDisplayName) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      LOWER(c.VenueCode) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      c.CandidateNationalId LIKE CONCAT('%', v_SearchKey, '%') OR
                                      LOWER(c.CandidatePhoneNumber) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      LOWER(c.CandidateEmail) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      LOWER(c.PaperName) LIKE CONCAT('%', LOWER(v_SearchKey), '%') OR
                                      LOWER(c.PaperCode) LIKE CONCAT('%', LOWER(v_SearchKey), '%')
                                  END
                              )
                          )
                    )
                    SELECT
                        fc.Id,
                        fc.RegistrationId,
                        fc.CandidateId,
                        fc.CandidateCode,
                        fc.CandidateNationalId,
                        fc.CandidateDisplayName,
                        fc.CandidateEmail,
                        fc.CandidatePhoneNumber,
                        fc.PaperIdActual,
                        fc.OriginalPaperId,
                        fc.PaperName,
                        fc.PaperCode,
                        fc.PaperStartDate,
                        fc.PaperEndDate,
                        fc.CandidateExamDate,
                        fc.PaperFormIdActual,
                        fc.PaperFormName,
                        fc.FinalScore,
                        fc.ScheduleId AS ScheduleIdActual,
                        fc.ScheduleName,
                        fc.ScheduleCode,
                        fc.ExamTrialId,
                        fc.TrialNumber,
                        fc.CandidateStartedExam,
                        fc.CandidateEndedExam,
                        fc.ExamTrialStartDate,
                        fc.ExamTrialEndDate,
                        fc.CandidateCurrentlyInExam,
                        fc.PaperDuration AS TotalExamDuration,
                        0 AS TotalExamSessions,
                        fc.TotalQuestionsInDatabase,
                        fc.QuestionsAnsweredCount,
                        fc.QuestionsMarkedForReview,
                        fc.QuestionsVisited,
                        fc.TotalElapsedTimeInSeconds,
                        fc.ReviewStatus,
                        fc.ReviewedAt,
                        fc.ReviewedBy,
                        fc.SentToCTR,
                        fc.SentToCTRAt,
                        fc.SentToCTRBy,
                        fc.VenueCode,
                        fc.CandidateCreatedAt,
                        fc.TrackingLogsJSON,
                        fc.TotalRecords,
                        (SELECT COUNT(*) FROM CandidateQuestionsAnswers a WHERE a.RegistrationId = fc.RegistrationId AND a.AnswerId IS NOT NULL AND a.IsCorrect = 1 AND a.QuestionType <> 'Comprehension') AS CorrectAnswers,
                        (SELECT COUNT(*) FROM CandidateQuestionsAnswers a WHERE a.RegistrationId = fc.RegistrationId AND a.AnswerId IS NOT NULL AND a.IsCorrect = 0 AND a.QuestionType <> 'Comprehension') AS IncorrectAnswers,
                        (CASE 
                            WHEN fc.PaperType = 1 THEN
                                (SELECT COUNT(*) FROM AdaptiveSection s INNER JOIN Stages st ON s.StageId = st.Id WHERE st.FormId = fc.PaperFormIdActual AND s.IsDeleted = 0)
                            ELSE
                                (SELECT COUNT(*) FROM Sections s WHERE s.PaperId = fc.PaperIdActual AND s.IsDeleted = 0)
                         END) AS TotalSections
                    FROM FilteredCandidates fc
                    ORDER BY
                        CASE WHEN p_OrderBy = 'DESC' THEN fc.CandidateCreatedAt END DESC,
                        CASE WHEN p_OrderBy <> 'DESC' OR p_OrderBy IS NULL THEN fc.CandidateCreatedAt END ASC
                    LIMIT v_Limit OFFSET v_Offset;

                END IF;
            END;
        ";
    }
}
