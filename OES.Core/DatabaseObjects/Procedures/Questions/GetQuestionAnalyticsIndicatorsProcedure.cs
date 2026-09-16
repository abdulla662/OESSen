using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Questions
{
    public class GetQuestionAnalyticsIndicatorsProcedure : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `GetQuestionAnalyticsIndicators`";

        public string CreateCommand => @"
            CREATE PROCEDURE `GetQuestionAnalyticsIndicators`(
                IN IndicatorPercentage INT,
                IN TypeOfIndicator     INT,
                IN FromDate            DATETIME,
                IN ToDate              DATETIME
            )
            BEGIN
                DECLARE archive_exists INT DEFAULT 0;

                -- Check if archive database exists
                SELECT COUNT(*) INTO archive_exists
                FROM   information_schema.SCHEMATA
                WHERE  SCHEMA_NAME = 'qiyas_archive_localoesdb';

                -- ============================================================
                -- Both databases available (main + archive)
                -- ============================================================
                IF archive_exists = 1 THEN

                    SELECT *
                    FROM (
                        SELECT
                            QuestionId,
                            QuestionCode,
                            SUM(TotalAnswered) AS TotalAnswered,
                            SUM(CorrectCount)  AS CorrectCount,
                            SUM(WrongCount)    AS WrongCount,
                            CASE
                                WHEN TypeOfIndicator = 1
                                    THEN ROUND(SUM(CorrectCount) * 100 / SUM(TotalAnswered), 0)
                                WHEN TypeOfIndicator = 2
                                    THEN ROUND(SUM(WrongCount)   * 100 / SUM(TotalAnswered), 0)
                            END AS Percentage
                        FROM (
                            SELECT
                                QuestionId,
                                QuestionCode,
                                COUNT(Id) AS TotalAnswered,
                                SUM(CASE
                                        WHEN IsCorrect = 1 AND Answered = 1
                                        THEN 1 ELSE 0
                                    END) AS CorrectCount,
                                SUM(CASE
                                        WHEN (IsCorrect = 0 AND Answered = 1)
                                          OR (Answered  = 0 AND Visited  = 1)
                                        THEN 1 ELSE 0
                                    END) AS WrongCount
                            FROM   candidatequestionsanswers
                            WHERE  ExamStartDate BETWEEN FromDate AND ToDate
                            GROUP BY QuestionId, QuestionCode

                            UNION ALL

                            SELECT
                                QuestionId,
                                QuestionCode,
                                COUNT(Id) AS TotalAnswered,
                                SUM(CASE
                                        WHEN IsCorrect = 1 AND Answered = 1
                                        THEN 1 ELSE 0
                                    END) AS CorrectCount,
                                SUM(CASE
                                        WHEN (IsCorrect = 0 AND Answered = 1)
                                          OR (Answered  = 0 AND Visited  = 1)
                                        THEN 1 ELSE 0
                                    END) AS WrongCount
                            FROM   candidatequestionsanswers
                            WHERE  ExamStartDate BETWEEN FromDate AND ToDate
                            GROUP BY QuestionId, QuestionCode
                        ) AS PreAggregated
                        GROUP BY QuestionId, QuestionCode
                    ) AS FinalResult
                    WHERE Percentage >= IndicatorPercentage;

                -- ============================================================
                -- Archive database not found, query main DB only
                -- ============================================================
                ELSE

                    SELECT *
                    FROM (
                        SELECT
                            QuestionId,
                            QuestionCode,
                            SUM(TotalAnswered) AS TotalAnswered,
                            SUM(CorrectCount)  AS CorrectCount,
                            SUM(WrongCount)    AS WrongCount,
                            CASE
                                WHEN TypeOfIndicator = 1
                                    THEN ROUND(SUM(CorrectCount) * 100 / SUM(TotalAnswered), 0)
                                WHEN TypeOfIndicator = 2
                                    THEN ROUND(SUM(WrongCount)   * 100 / SUM(TotalAnswered), 0)
                            END AS Percentage
                        FROM (
                            SELECT
                                QuestionId,
                                QuestionCode,
                                COUNT(Id) AS TotalAnswered,
                                SUM(CASE
                                        WHEN IsCorrect = 1 AND Answered = 1
                                        THEN 1 ELSE 0
                                    END) AS CorrectCount,
                                SUM(CASE
                                        WHEN (IsCorrect = 0 AND Answered = 1)
                                          OR (Answered  = 0 AND Visited  = 1)
                                        THEN 1 ELSE 0
                                    END) AS WrongCount
                            FROM   candidatequestionsanswers
                            WHERE  ExamStartDate BETWEEN FromDate AND ToDate
                            GROUP BY QuestionId, QuestionCode
                        ) AS PreAggregated
                        GROUP BY QuestionId, QuestionCode
                    ) AS FinalResult
                    WHERE Percentage >= IndicatorPercentage;

                END IF;

            END
        ";
    }
}