using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Questions
{
    public class UpdateQuestionExhaustionCount : IDatabaseStoredProcedure
    {
        public const string ArchiveDbName = "qiyas_archive_localoesdb";
        public const string ProcedureName = "sp_UpdateQuestionExhaustionCount";

        public string DropCommand => $"DROP PROCEDURE IF EXISTS `{ProcedureName}`";

        public string CreateCommand => $@"
            CREATE PROCEDURE {ProcedureName}()
            BEGIN

                -- Update CurrentExhaustionCount in the main questionsmetadata table
                UPDATE questionsmetadata qm
                LEFT JOIN
                (
                    SELECT QuestionId, SUM(QuestionCount) AS QuestionCount
                    FROM
                    (
                        -- Count active, non-deleted answers from the archive database
                        SELECT QuestionId, COUNT(*) AS QuestionCount
                        FROM {ArchiveDbName}.candidatequestionsanswers
                        WHERE IsActive = 1 AND IsDeleted = 0
                        GROUP BY QuestionId

                        UNION ALL

                        -- Count active, non-deleted answers from the current database that have not yet been archived
                        SELECT m.QuestionId, COUNT(*) AS QuestionCount
                        FROM candidatequestionsanswers m
                        WHERE m.IsActive = 1 AND m.IsDeleted = 0 
                        AND NOT EXISTS 
                        ( 
				            SELECT 1 
                            FROM {ArchiveDbName}.candidatequestionsanswers a
                            WHERE a.Id = m.Id 
			            ) GROUP BY m.QuestionId
                    ) x GROUP BY QuestionId
                ) c ON c.QuestionId = qm.Id

                -- Set count to 0 when a question has no answers
                SET qm.CurrentExhaustionCount = COALESCE(c.QuestionCount, 0);

                -- Synchronize the updated counts back to the archive questionsmetadata
                UPDATE {ArchiveDbName}.questionsmetadata a
	            JOIN questionsmetadata m ON m.Id = a.Id
                SET a.CurrentExhaustionCount = m.CurrentExhaustionCount;

            END
        ";
    }
}