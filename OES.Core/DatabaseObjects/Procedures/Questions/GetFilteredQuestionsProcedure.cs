using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Questions
{
    public class GetFilteredQuestionsProcedure : IDatabaseStoredProcedure
    {
        public string DropCommand => $"DROP PROCEDURE IF EXISTS `{nameof(GetFilteredQuestionsProcedure)}`";

        public string CreateCommand => @$"
            CREATE PROCEDURE `{nameof(GetFilteredQuestionsProcedure)}`(
                IN p_Signature          VARCHAR(255),
                IN p_SearchKey          TEXT,
                IN p_CategoryId         BIGINT,
                IN p_QuestionTypeId     BIGINT,
                IN p_CreatedByUserName  TEXT,
                IN p_ModifiedByUserName TEXT,
                IN p_FromDate           DATETIME,
                IN p_ToDate             DATETIME,
                IN p_ModifiedFromDate   DATETIME,
                IN p_ModifiedToDate     DATETIME,
                IN p_PageIndex          INT,
                IN p_PageSize           INT,
                IN p_PaginationOff      BOOLEAN
            )
            BEGIN
                DECLARE v_Offset INT DEFAULT 0;

                SET v_Offset = p_PageIndex * p_PageSize;

                -- ============================================================
                -- Total Records
                -- ============================================================

                SELECT COUNT(*) AS TotalRecords
                FROM QuestionsMetaData qm
                WHERE
                    qm.IsDeleted = FALSE
                    AND qm.IsActive = TRUE
                    AND qm.OrganizationSignature = p_Signature COLLATE utf8mb4_0900_ai_ci
                    AND (qm.ParentId IS NULL OR qm.ParentId = 0)
                    AND (p_SearchKey IS NULL OR p_SearchKey = '' OR LOWER(qm.Code) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%'))
                    AND (p_CategoryId IS NULL OR qm.QuestionCategoryId = p_CategoryId)
                    AND (p_QuestionTypeId IS NULL OR qm.QuestionTypeId = p_QuestionTypeId)
                    AND (p_CreatedByUserName IS NULL OR p_CreatedByUserName = '' OR LOWER(qm.CreationUser) LIKE CONCAT('%', LOWER(p_CreatedByUserName COLLATE utf8mb4_0900_ai_ci), '%'))
                    AND (p_ModifiedByUserName IS NULL OR p_ModifiedByUserName = '' OR LOWER(qm.ModeficationUser) LIKE CONCAT('%', LOWER(p_ModifiedByUserName COLLATE utf8mb4_0900_ai_ci), '%'))
                    AND (p_FromDate IS NULL OR qm.CreationDate >= p_FromDate)
                    AND (p_ToDate IS NULL OR qm.CreationDate <= p_ToDate)
                    AND (p_ModifiedFromDate IS NULL OR qm.ModeficationDate >= p_ModifiedFromDate)
                    AND (p_ModifiedToDate IS NULL OR qm.ModeficationDate <= p_ModifiedToDate);

                -- ============================================================
                -- Filtered Questions
                -- ============================================================

                IF p_PaginationOff = TRUE THEN

                    SELECT
                        qm.Id,
                        qm.Code,
                        qc.Name             AS Category,
                        qt.Name             AS QuestionTypeName,
                        qm.CreationDate     AS CreatedDate,
                        CASE WHEN LOWER(qm.CreationUser) LIKE '%superadmin%' COLLATE utf8mb4_0900_ai_ci THEN 'system' ELSE qm.CreationUser END AS CreatedBy,
                        qm.ModeficationDate AS ModifiedDate,
                        CASE WHEN LOWER(qm.ModeficationUser) LIKE '%superadmin%' COLLATE utf8mb4_0900_ai_ci THEN 'system' ELSE qm.ModeficationUser END AS ModifiedBy
                    FROM QuestionsMetaData qm
                    LEFT JOIN QuestionCategories qc ON qc.Id = qm.QuestionCategoryId
                    LEFT JOIN QuestionTypes qt ON qt.Id = qm.QuestionTypeId
                    WHERE
                        qm.IsDeleted = FALSE
                        AND qm.IsActive = TRUE
                        AND qm.OrganizationSignature = p_Signature COLLATE utf8mb4_0900_ai_ci
                        AND (qm.ParentId IS NULL OR qm.ParentId = 0)
                        AND (p_SearchKey IS NULL OR p_SearchKey = '' OR LOWER(qm.Code) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_CategoryId IS NULL OR qm.QuestionCategoryId = p_CategoryId)
                        AND (p_QuestionTypeId IS NULL OR qm.QuestionTypeId = p_QuestionTypeId)
                        AND (p_CreatedByUserName IS NULL OR p_CreatedByUserName = '' OR LOWER(qm.CreationUser) LIKE CONCAT('%', LOWER(p_CreatedByUserName COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_ModifiedByUserName IS NULL OR p_ModifiedByUserName = '' OR LOWER(qm.ModeficationUser) LIKE CONCAT('%', LOWER(p_ModifiedByUserName COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_FromDate IS NULL OR qm.CreationDate >= p_FromDate)
                        AND (p_ToDate IS NULL OR qm.CreationDate <= p_ToDate)
                        AND (p_ModifiedFromDate IS NULL OR qm.ModeficationDate >= p_ModifiedFromDate)
                        AND (p_ModifiedToDate IS NULL OR qm.ModeficationDate <= p_ModifiedToDate)
                    ORDER BY qm.CreationDate DESC;

                ELSE

                    SELECT
                        qm.Id,
                        qm.Code,
                        qc.Name             AS Category,
                        qt.Name             AS QuestionTypeName,
                        qm.CreationDate     AS CreatedDate,
                        CASE WHEN LOWER(qm.CreationUser) LIKE '%superadmin%' COLLATE utf8mb4_0900_ai_ci THEN 'system' ELSE qm.CreationUser END AS CreatedBy,
                        qm.ModeficationDate AS ModifiedDate,
                        CASE WHEN LOWER(qm.ModeficationUser) LIKE '%superadmin%' COLLATE utf8mb4_0900_ai_ci THEN 'system' ELSE qm.ModeficationUser END AS ModifiedBy
                    FROM QuestionsMetaData qm
                    LEFT JOIN QuestionCategories qc ON qc.Id = qm.QuestionCategoryId
                    LEFT JOIN QuestionTypes qt ON qt.Id = qm.QuestionTypeId
                    WHERE
                        qm.IsDeleted = FALSE
                        AND qm.IsActive = TRUE
                        AND qm.OrganizationSignature = p_Signature COLLATE utf8mb4_0900_ai_ci
                        AND (qm.ParentId IS NULL OR qm.ParentId = 0)
                        AND (p_SearchKey IS NULL OR p_SearchKey = '' OR LOWER(qm.Code) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_CategoryId IS NULL OR qm.QuestionCategoryId = p_CategoryId)
                        AND (p_QuestionTypeId IS NULL OR qm.QuestionTypeId = p_QuestionTypeId)
                        AND (p_CreatedByUserName IS NULL OR p_CreatedByUserName = '' OR LOWER(qm.CreationUser) LIKE CONCAT('%', LOWER(p_CreatedByUserName COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_ModifiedByUserName IS NULL OR p_ModifiedByUserName = '' OR LOWER(qm.ModeficationUser) LIKE CONCAT('%', LOWER(p_ModifiedByUserName COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_FromDate IS NULL OR qm.CreationDate >= p_FromDate)
                        AND (p_ToDate IS NULL OR qm.CreationDate <= p_ToDate)
                        AND (p_ModifiedFromDate IS NULL OR qm.ModeficationDate >= p_ModifiedFromDate)
                        AND (p_ModifiedToDate IS NULL OR qm.ModeficationDate <= p_ModifiedToDate)
                    ORDER BY qm.CreationDate DESC
                    LIMIT v_Offset, p_PageSize;

                END IF;

            END
        ";
    }
}
