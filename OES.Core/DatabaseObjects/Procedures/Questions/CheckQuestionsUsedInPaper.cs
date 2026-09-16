using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Questions
{
    public class CheckQuestionsUsedInPaper : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `CheckQuestionsUsedInPaper`";

        public string CreateCommand => $@"
            CREATE PROCEDURE `CheckQuestionsUsedInPaper`(
                IN pQuestionIds JSON
            )
            BEGIN
                SELECT DISTINCT ids.QuestionId
                FROM JSON_TABLE
                (
                    pQuestionIds,
                    '$[*]'
                    COLUMNS
                    (
                        QuestionId BIGINT PATH '$'
                    )
                ) ids
                WHERE EXISTS
                (
                    SELECT 1 FROM formquestions fq
                    WHERE fq.QuestionId = ids.QuestionId AND fq.IsActive = 1 AND fq.IsDeleted = 0
                )
                OR EXISTS
	            (
		            SELECT 1 FROM blockquestions bq
		            JOIN paperformblocks pfb ON pfb.BlockId = bq.BlockId
		            WHERE bq.QuestionMetadataId = ids.QuestionId 
                    AND bq.IsActive = 1 AND bq.IsDeleted = 0 AND pfb.IsActive = 1 AND pfb.IsDeleted = 0
	            );
            END
        ";
    }
}