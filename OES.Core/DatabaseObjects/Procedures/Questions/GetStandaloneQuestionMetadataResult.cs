using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Questions
{
    public class GetStandaloneQuestionMetadataResult : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `sp_GetStandaloneQuestionMetadata`";

        public string CreateCommand => @"
            CREATE PROCEDURE sp_GetStandaloneQuestionMetadata(
                IN p_SearchKey  VARCHAR(255),
                IN p_OrderBy    VARCHAR(4),
                IN p_PageIndex  INT,
                IN p_PageSize   INT
            )
            BEGIN
                DECLARE v_Offset BIGINT UNSIGNED;
                DECLARE v_Limit  BIGINT UNSIGNED;

                SET v_Offset = LEAST(GREATEST(p_PageIndex * p_PageSize, 0), 18446744073709551615);
                SET v_Limit  = LEAST(GREATEST(p_PageSize, 0), 18446744073709551615);

                WITH StandaloneQuestions AS (
                    SELECT qm.Id
                    FROM QuestionsMetadata qm
                    WHERE qm.QuestionStatus = 8
					    AND qm.ParentId IS NULL
                        AND qm.IsRoot 
                        AND NOT EXISTS (
                            SELECT 1 FROM ManualPaperItemBankQuestionSections ms
                            WHERE ms.QuestionMetaDataId = qm.Id
                        )
                        AND NOT EXISTS (
                            SELECT 1 FROM FormQuestions fq
                            WHERE fq.QuestionId = qm.Id
                        )
                        AND NOT EXISTS (
                            SELECT 1
                            FROM BlockQuestions bq
                                INNER JOIN PaperFormBlocks pfb ON pfb.BlockId = bq.BlockId
                            WHERE bq.QuestionMetadataId = qm.Id
                        )
                        AND (p_SearchKey IS NULL OR p_SearchKey = ''
                            OR LOWER(qm.Code) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_unicode_ci), '%'))
                )
                SELECT
                    qm.Id                        AS Id,
                    qm.Code                      AS QuestionCode,
                    qt.Name                      AS QuestionType,
                    ib.Name                      AS ItemBankName,
                    COALESCE(
                        GROUP_CONCAT(DISTINCT b.Code ORDER BY b.Code SEPARATOR ','),
                        ''
                    )                            AS BlockCodes,
                    COUNT(*) OVER()              AS TotalRecords
                FROM StandaloneQuestions sq
                    INNER JOIN QuestionsMetadata qm ON qm.Id = sq.Id
                    LEFT JOIN QuestionTypes qt ON qt.Id = qm.QuestionTypeId
                    LEFT JOIN ItemBanks ib ON ib.Id = qm.ItemBankId
                    LEFT JOIN BlockQuestions bq ON bq.QuestionMetadataId = qm.Id
                    LEFT JOIN Blocks b ON b.Id = bq.BlockId
                GROUP BY qm.Id, qm.Code, qt.Name, ib.Name
                ORDER BY
                    CASE WHEN UPPER(p_OrderBy) = 'DESC' THEN qm.Code END DESC,
                    CASE WHEN UPPER(p_OrderBy) = 'ASC'  THEN qm.Code END ASC
                LIMIT v_Offset, v_Limit;
            END
        ";
    }
}