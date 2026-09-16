using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Forms
{
    public class SoftDeleteFormProcedure : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `sp_SoftDeleteForm`";

        public string CreateCommand => @"
            CREATE PROCEDURE `sp_SoftDeleteForm`(
                IN  p_FormId                BIGINT,
                IN  p_OrganizationId        BIGINT,
                IN  p_OrganizationSignature VARCHAR(255)
            )
            BEGIN
                DECLARE v_PaperId                BIGINT;
                DECLARE v_QuestionSelectionType  INT;
                DECLARE v_FormExists             BOOLEAN     DEFAULT FALSE;
                DECLARE v_IsDeleted              TINYINT(1)  DEFAULT 0;
                DECLARE v_IsAssignedToSchedule   BOOLEAN     DEFAULT FALSE;

                -- Setup Error Handler
                DECLARE EXIT HANDLER FOR SQLEXCEPTION
                BEGIN
                    ROLLBACK;
                    SELECT 0 AS Status, -1 AS ErrorCode;
                END;

                -- 1. Validate the form belongs to the current Organization and get required Metadata
                SELECT
                    TRUE,
                    pf.PaperId,
                    pm.QuestionSelectionType,
                    pf.IsDeleted
                INTO
                    v_FormExists,
                    v_PaperId,
                    v_QuestionSelectionType,
                    v_IsDeleted
                FROM        Forms         pf
                LEFT JOIN   PaperMetadata pm ON pf.PaperId = pm.Id
                WHERE pf.Id = p_FormId
                  AND (pf.OrganizationId        = p_OrganizationId        OR p_OrganizationId        = 0)
                  AND (pf.OrganizationSignature  = p_OrganizationSignature OR p_OrganizationSignature IS NULL)
                LIMIT 1;

                -- 2. Check if the Paper is assigned to an active Schedule
                IF v_PaperId IS NOT NULL THEN
                    SELECT EXISTS(
                        SELECT 1
                        FROM  SchedulePapers
                        WHERE PaperId    = v_PaperId
                          AND IsDeleted  = 0
                          AND IsActive   = 1
                    ) INTO v_IsAssignedToSchedule;
                END IF;

                -- 3. Guard Clauses
                IF v_FormExists = FALSE THEN
                    SELECT 0 AS Status, 1 AS ErrorCode;

                ELSEIF v_IsDeleted = 1 THEN
                    SELECT 0 AS Status, 2 AS ErrorCode;

                ELSEIF v_IsAssignedToSchedule = 1 THEN
                    SELECT 0 AS Status, 3 AS ErrorCode;

                ELSE
                    START TRANSACTION;

                    -- 4. Soft Delete Questions based on QuestionSelectionType
                    IF v_QuestionSelectionType = 0 THEN
                        UPDATE ManualPaperItemBankQuestionSections mq
                        INNER JOIN Sections ss ON mq.SectionId = ss.Id
                        SET mq.IsDeleted    = 1,
                            mq.IsActive     = 0,
                            mq.DeletedDate  = NOW()
                        WHERE ss.FormId = p_FormId
                          AND (mq.OrganizationId = p_OrganizationId OR p_OrganizationId = 0);

                        -- 5. Soft delete Sections (Manual only)
                        UPDATE Sections
                        SET IsDeleted   = 1,
                            IsActive    = 0,
                            DeletedDate = NOW()
                        WHERE FormId = p_FormId
                          AND (OrganizationId = p_OrganizationId OR p_OrganizationId = 0);
                    END IF;

                    -- 6. Soft delete FormQuestions (always)
                    UPDATE FormQuestions
                    SET IsDeleted   = 1,
                        IsActive    = 0,
                        DeletedDate = NOW()
                    WHERE FormId = p_FormId
                      AND (OrganizationId = p_OrganizationId OR p_OrganizationId = 0);

                    -- 7. Soft delete the Form itself (always)
                    UPDATE Forms
                    SET IsDeleted   = 1,
                        IsActive    = 0,
                        DeletedDate = NOW()
                    WHERE Id = p_FormId
                      AND (OrganizationId = p_OrganizationId OR p_OrganizationId = 0);

                    -- 8. Decrement the OutputFormsCount in PaperMetadata
                    IF v_PaperId IS NOT NULL THEN
                        UPDATE PaperMetadata
                        SET OutputFormsCount = GREATEST(0, OutputFormsCount - 1)
                        WHERE Id = v_PaperId
                          AND (OrganizationId = p_OrganizationId OR p_OrganizationId = 0);
                    END IF;

                    COMMIT;
                    SELECT 1 AS Status, 0 AS ErrorCode;
                END IF;
            END
        ";
    }
}