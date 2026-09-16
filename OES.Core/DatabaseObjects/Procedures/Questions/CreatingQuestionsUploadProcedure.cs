using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Questions
{
    public class CreatingQuestionsUploadProcedure : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `AddFileQuestions`";

        public string CreateCommand => @"
            CREATE PROCEDURE `AddFileQuestions`(
                IN UploadQuestionData JSON,
                IN QuestionMetaData JSON,
                IN OrgSignature VARCHAR(255),
                IN CreationUser VARCHAR(255),
                IN OrganizationId INT,
                IN AesKey LONGTEXT,
                IN AesIv LONGTEXT,
                OUT status INT,
                OUT errorMessage VARCHAR(255)
            )
            BEGIN
            DECLARE MetadataId BIGINT;
            DECLARE QuestionId BIGINT;
            DECLARE i INT DEFAULT 0;
            DECLARE choice_index INT DEFAULT 0;
            DECLARE QuestionCode VARCHAR(100);
            DECLARE QuestionsExhaustionCount INT;
            DECLARE QuestionSatatus INT;
            DECLARE IloId BIGINT;
            DECLARE ItemBankId BIGINT;
            DECLARE SubjectId BIGINT;
            DECLARE QuestionCategoryId BIGINT;
            DECLARE SelectedQuestionTypeId BIGINT;
            DECLARE DifficultyProfileId BIGINT;
            DECLARE DifficultyLevelId BIGINT;
            DECLARE Delta DOUBLE;
            DECLARE IsRoot TINYINT(1);
            DECLARE MaximumAnswerTime INT;
            DECLARE Author VARCHAR(100);
            DECLARE LanguageId BIGINT;
            DECLARE Body LONGTEXT;
            DECLARE ModelAnswer LONGTEXT;
            DECLARE ChoiceText LONGTEXT;
            DECLARE IsCorrectAnswer TINYINT(1);
            DECLARE FileManagerEditorPanelEnabled TINYINT(1);
            DECLARE ScientificEditorPanelEnabled TINYINT(1);
            DECLARE hasWarning TINYINT(1);
            DECLARE DefaultQuestionLayoutId BIGINT;
            DECLARE FileUrl LONGTEXT;
            DECLARE FileId VARCHAR(100);
            DECLARE QuestionItemBankId BIGINT;
            DECLARE FinalItemBankId BIGINT;
            DECLARE UploadMode TINYINT(1);

            DECLARE EXIT HANDLER FOR SQLEXCEPTION
            BEGIN
                GET DIAGNOSTICS CONDITION 1 errorMessage = MESSAGE_TEXT;
                ROLLBACK;
                SET status = 0; 
            END;

            SET status = 1;
            SET errorMessage = '';
            
            SET @ExhCountValue = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.QuestionsExhaustionCount'));
            SET QuestionsExhaustionCount = IF(@ExhCountValue IS NULL OR @ExhCountValue = '' OR @ExhCountValue = 'null', 0, @ExhCountValue);

            SET @QuestionSatatusValue = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.QuestionStatus'));
            SET QuestionSatatus = IF(@QuestionSatatusValue IS NULL OR @QuestionSatatusValue = '' OR @QuestionSatatusValue = 'null', 0, @QuestionSatatusValue);

            SET @IloIdValue = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.IloId'));
            SET IloId = IF(@IloIdValue IS NULL OR @IloIdValue = '' OR @IloIdValue = 'null', NULL, @IloIdValue);

            SET ItemBankId = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.ItemBankId'));
            SET SubjectId = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.QuestionSubjectId'));
            SET QuestionCategoryId = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.QuestionCategoryId'));
            SET DifficultyProfileId = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.DifficultyProfileId'));
            SET DifficultyLevelId = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.DifficultyLevelId'));
            SET Delta = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.Delta'));
            SET IsRoot = IF(JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.IsRoot')) = 'true', 1, 0);
            SET MaximumAnswerTime = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.MaximumAnswerTime'));
            SET Author = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.Author'));
            SET LanguageId = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.LanguageId'));
            SET ScientificEditorPanelEnabled = IF(JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.ScientificEditorPanelEnabled')) = 'true', 1, 0);
            SET FileManagerEditorPanelEnabled = IF(JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.FileManagerEditorPanelEnabled')) = 'true', 1, 0);
            SET UploadMode = JSON_UNQUOTE(JSON_EXTRACT(QuestionMetaData, '$.UploadMode'));

            START TRANSACTION;
            
            SET @@session.block_encryption_mode = 'aes-256-cbc';
            
            WHILE i < JSON_LENGTH(UploadQuestionData) DO
                SET choice_index = 0;
                SET SelectedQuestionTypeId = JSON_UNQUOTE(JSON_EXTRACT(UploadQuestionData,  CONCAT('$[', i, '].QuestionTypeId')));
                SET QuestionCode = JSON_UNQUOTE(JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].QuestionCode')));
                SET @tmpItemBankId = JSON_UNQUOTE(JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].ItemBankId')));
                SET QuestionItemBankId = IF(@tmpItemBankId IS NULL OR @tmpItemBankId = '' OR @tmpItemBankId = 'null', NULL, @tmpItemBankId);
                SET FinalItemBankId = IF(UploadMode = 0, QuestionItemBankId, ItemBankId);

                SELECT Id
                    INTO DefaultQuestionLayoutId
                    FROM questionlayouts
                    WHERE QuestionTypeId = SelectedQuestionTypeId
                      AND Name = 'Vertical'
                    LIMIT 1;

                SELECT DefaultQuestionLayoutId, SelectedQuestionTypeId;

                INSERT INTO QuestionsMetaData (
                    Code, IloId, ItemBankId, SubjectId, QuestionCategoryId, QuestionTypeId,
                    DifficultyProfileId, DifficultyLevelId, Delta, IsRoot,
                    MaximumAnswerTime, Author, CreationDate, CreationUser, QuestionStatus,
                    OrganizationSignature, IsActive, IsDeleted, OrganizationId, QuestionLayoutId, QuestionsExhaustionCount,
                    ScientificEditorPanelEnabled, FileManagerEditorPanelEnabled, CurrentExhaustionCount
                )
                VALUES (
                    QuestionCode, IloId, FinalItemBankId, SubjectId, QuestionCategoryId, SelectedQuestionTypeId,
                    DifficultyProfileId, DifficultyLevelId, Delta, IsRoot,
                    MaximumAnswerTime, Author, NOW(), CreationUser, QuestionSatatus,
                    OrgSignature, 1, 0, OrganizationId, DefaultQuestionLayoutId, QuestionsExhaustionCount,
                    ScientificEditorPanelEnabled, FileManagerEditorPanelEnabled, 0
                );

                SET MetadataId = LAST_INSERT_ID();

                IF MetadataId = 0 THEN
                    SET status = 0;
                    SET errorMessage = 'Failed to insert Metadata. Please check the input data.';
                    ROLLBACK;
                END IF;

                SET Body = JSON_UNQUOTE(JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].Body')));
                SET hasWarning = IF(JSON_UNQUOTE(JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].HasWarning'))) = 'true', 1, 0);
                SET @ModelAnswerValue = JSON_UNQUOTE(JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].ModelAnswer')));
                SET ModelAnswer = IF(@ModelAnswerValue IS NULL OR @ModelAnswerValue = '' OR @ModelAnswerValue = 'null', NULL, @ModelAnswerValue);

                IF hasWarning = 0 THEN
                    INSERT INTO QuestionsDetails (
                        Body, QuestionMetaDataId, LanguageId, Instructions, ModelAnswer,
                        CreationDate, CreationUser, OrganizationSignature, HasShuffled, IsActive, IsDeleted, OrganizationId
                    )
                    VALUES (
                        TO_BASE64(AES_ENCRYPT(COMPRESS(Body), AesKey, AesIv)), MetadataId, LanguageId, NULL, TO_BASE64(AES_ENCRYPT(COMPRESS(ModelAnswer), AesKey, AesIv)),
                        NOW(), CreationUser, OrgSignature, false, 1, 0, OrganizationId
                    );
                ELSE
                    SELECT 'Record not inserted due to hasWarning being TRUE.' AS WarningMessage;
                END IF;

                SET QuestionId = LAST_INSERT_ID();
                SET FileUrl = NULL;
                SET FileId = NULL;

                IF Body LIKE '%<img src=%' THEN
                    SET FileURL = SUBSTRING_INDEX(SUBSTRING_INDEX(Body, 'src=""', -1), '""', 1);
                END IF;

                IF FileURL IS NOT NULL AND FileURL LIKE '%documentId=%' THEN
                    SET FileId = SUBSTRING_INDEX(FileURL, 'documentId=', -1);
                END IF;

                IF FileURL IS NOT NULL AND FileId IS NOT NULL THEN
                    INSERT INTO questiondoclibfiles (
                        QuestionId, FileURL, FileId, CreationDate, CreationUser, OrganizationSignature, IsActive, IsDeleted, OrganizationId
                    )
                    VALUES (
                        MetadataId, FileURL, FileId, NOW(), CreationUser, OrgSignature, 1, 0, OrganizationId
                    );
                END IF;

                IF JSON_LENGTH(JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].Choices'))) > 0 THEN
                    WHILE choice_index < JSON_LENGTH(JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].Choices'))) DO
                        SET ChoiceText = JSON_UNQUOTE(JSON_EXTRACT(
                            JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].Choices')),
                            CONCAT('$[', choice_index, '].ChoiceText'))
                        );
                        SET IsCorrectAnswer = IF(
                            JSON_UNQUOTE(JSON_EXTRACT(
                                JSON_EXTRACT(UploadQuestionData, CONCAT('$[', i, '].Choices')),
                                CONCAT('$[', choice_index, '].IsCorrectAnswer')
                            )) = 'true', 1, 0
                        );
                        INSERT INTO QuestionsChoices (
                            QuestionDetailsId, ChoiceText, IsCorrectAnswer,
                            CreationDate, CreationUser, OrganizationSignature, IsActive, IsDeleted, OrganizationId
                        )
                        VALUES (
                            QuestionId, TO_BASE64(AES_ENCRYPT(COMPRESS(ChoiceText), AesKey, AesIv)), IsCorrectAnswer,
                            NOW(), CreationUser, OrgSignature, 1, 0, OrganizationId
                        );
                        SET choice_index = choice_index + 1;
                    END WHILE;
                END IF;

                SET i = i + 1;
            END WHILE;

            COMMIT;
            SET status = 1;
            SET errorMessage = 'Process completed successfully.';
            END
        ";
    }
}