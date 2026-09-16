using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.CandidateQuestionsWithEquationView
{
    /// <summary>
    /// Creates the CandidateQuestionsWithEquationView database view.
    /// Metrics are calculated in memory from this view data for better performance.
    /// </summary>
    public class CandidateQuestionsWithEquationView : IDatabaseView
    {
        // OPTIMIZED VERSION - Removed expensive correlated subqueries (CleanAnswers, ModelAnswer)
        // that were not used anywhere in the codebase. Also simplified GROUP BY handling.
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW `vw_CandidateQuestionsWithEquationView` AS
            SELECT
                `cqa`.`Id` AS `Id`,

                -- candidate fields from candidateexamdetails (ced)
                `ced`.`CandidateId` AS `CandidateID`,
                `ced`.`CandidateCode` AS `CandidateCode`,
                `ced`.`CandidateNationalId` AS `ClientCandidateID`,
                `ced`.`CandidateDisplayName` AS `FirstName`,
                '' AS `MiddleName`,
                '.' AS `LastName`,
                `ced`.`CandidateEmail` AS `CandidateEmail`,
                `ced`.`CandidatePhoneNumber` AS `CandidatePhoneNumber`,

                `cqa`.`Gender` AS `Gender`,
                `cqa`.`QuestionId` AS `QuestionId`,
                `cqa`.`ParentQuestionId` AS `ParentQuestionId`,
                `cqa`.`IsRootQuestion` AS `IsRootQuestion`,
                `cqa`.`SectionId` AS `SectionId`,
                `cqa`.`SectionName` AS `SectionName`,
                CASE
                    WHEN `pm`.`Type` = 1 THEN `asec`.`UnScored`
                    WHEN `pm`.`Type` = 0 THEN `q`.`UnScored`
                    ELSE 0
                END AS `UnScored`,
                CAST(COALESCE(`s`.`TimeInMinutes`, 0) AS SIGNED) AS `SectionTimeInMinutes`,
                `cqa`.`QuestionCode` AS `QuestionCode`,
                `cqa`.`QuestionType` AS `QuestionType`,
                `cqa`.`QuestionSubject` AS `QuestionSubject`,
                `cqa`.`QuestionScore` AS `QuestionScore`,
                `cqa`.`IsAutoCorrectable` AS `IsAutoCorrectable`,
                `cqa`.`AnswerId` AS `AnswerId`,
                `cqa`.`AnswerText` AS `AnswerText`,
                `cqa`.`AnswerIds` AS `AnswerIds`,
                `cqa`.`Visited` AS `Visited`,
                `cqa`.`Answered` AS `Answered`,
                `cqa`.`MarkedForReview` AS `MarkedForReview`,
                `cqa`.`ElapsedTimeInSeconds` AS `ElapsedTimeInSeconds`,
                `cqa`.`PaperFormId` AS `FormId`,
                `cqa`.`PaperFormName` AS `FormName`,
                `cqa`.`PaperId` AS `PaperId`,
                `cqa`.`PaperName` AS `PaperName`,
                `pm`.`Code` AS `PaperCode`,
                `cqa`.`AnswerCreatedAt` AS `AnswerCreatedAt`,
                `cqa`.`AnswerLastModifiedAt` AS `AnswerLastModifiedAt`,
                `cqa`.`CreatedAt` AS `CreatedAt`,
                `cqa`.`ExamStartDate` AS `ExamStartDate`,
                `cqa`.`ExamEndDate` AS `ExamEndDate`,
                `cqa`.`VenueId` AS `VenueId`,
                `cqa`.`VenueCode` AS `VenueCode`,
                `cqa`.`TrialNumber` AS `TrialNumber`,
                `cqa`.`RegistrationId` AS `RegistrationId`,
                `cqa`.`TCID` AS `TCID`,
                `cqa`.`CreationUser` AS `CreationUser`,
                `cqa`.`ModeficationUser` AS `ModeficationUser`,
                `cqa`.`CreationDate` AS `CreationDate`,
                `cqa`.`ModeficationDate` AS `ModeficationDate`,
                `cqa`.`IsDeleted` AS `IsDeleted`,
                `cqa`.`DeletedDate` AS `DeletedDate`,
                `cqa`.`IsActive` AS `IsActive`,
                `cqa`.`OrganizationSignature` AS `OrganizationSignature`,
                `cqa`.`OrganizationId` AS `OrganizationId`,
                `cqa`.`MarksObtained` AS `MarksObtained`,
                `cqa`.`ModelAnswerIds` AS `ModelAnswerIds`,
                `cqa`.`IsCorrect` AS `IsCorrect`,
                `ced`.`SentToCTR` AS `SentToCTR`,
                `ced`.`CandidateExamDate` AS `CandidateExamDate`,
                `cqa`.`EvaluationStatus` AS `EvaluationStatus`,

                CONCAT(`pm`.`Code`, '-', IF(`cqa`.`Gender` = 1, 'F', 'M')) AS `ExamSeriesCode`,
                `pm`.`LanguageId` AS `LanguageId`,
                CASE `pm`.`LanguageId`
                    WHEN 1 THEN 'ARA'
                    WHEN 2 THEN 'ENU'
                    ELSE 'UNDEFINED'
                END AS `ExamLanguage`,

                1 AS `Attempt`,

                COALESCE(`ps_agg`.`PassingScore`, 0) AS `PassingScore`,

                `cqa`.`SchedulePaperId` AS `SchedulePaperId`,
                `cqa`.`ScheduleId` AS `ScheduleId`,

                -- REMOVED: CleanAnswers correlated subquery (not used anywhere)
                NULL AS `CleanAnswers`,

                -- REMOVED: ModelAnswer correlated subquery (not used anywhere)
                NULL AS `ModelAnswer`,

           CASE
                    WHEN LOWER(TRIM(`qd`.`ModelAnswer`)) IN ('true', '1') THEN 1
                    ELSE 0
                END AS `ModelAnswerNumericValue`,

                CASE
                    WHEN LOWER(TRIM(`cqa`.`AnswerText`)) = LOWER(TRIM(`qd`.`ModelAnswer`)) THEN 1
                    ELSE 0
                END AS `CorrectAnswer`,
                `cqa`.`KeyAnswer` AS `KeyAnswer`,
                `cqa`.`Response` AS `Response`,
                CAST(`pie`.`ItemBankId` AS SIGNED) AS `ItemBankId`,
                `ib`.`Name` AS `ItemBankName`,
                CAST(`pie`.`EquationTemplateId` AS SIGNED) AS `EquationTemplateId`,
                CAST(`pie`.`EquationCategoryId` AS SIGNED) AS `EquationCategoryId`,
                `et`.`Name` AS `EquationTemplateName`,
                `et`.`TotalEquation` AS `TotalEquation`,
                `ec`.`CategoryName` AS `EquationCategoryName`,
                `ec`.`Equation` AS `OriginalEquation`,
                `ec`.`CreationDate` AS `EquationCategoryCreationDate`,

                NULL AS `DifficultyProfileName`,
                NULL AS `DifficultyLevelName`,

                CASE
                    WHEN `cqa`.`IsCorrect` = 1 THEN `q`.`Delta`
                    ELSE 0
                END AS `FinalDeltaValue`
            FROM
                `candidatequestionsanswers` `cqa`
                INNER JOIN `questionsmetadata` `q`
                    ON `q`.`Id` = `cqa`.`QuestionId`
                    AND `q`.`IsDeleted` = 0
                INNER JOIN `candidateexamdetails` `ced`
                    ON `ced`.`UserId` = `cqa`.`CandidateId`
                    AND `ced`.`PaperFormIdActual` = `cqa`.`PaperFormId`
                    AND `ced`.`TrialNumber` = `cqa`.`TrialNumber`
                    AND `ced`.`RegistrationId` = `cqa`.`RegistrationId`
                LEFT JOIN `papermetadata` `pm`
                    ON `pm`.`Id` = `cqa`.`PaperId`
                LEFT JOIN `questionsdetails` `qd`
                    ON `qd`.`QuestionMetadataId` = `q`.`Id`
                    AND `qd`.`LanguageId` = `pm`.`LanguageId`
                    AND `qd`.`IsDeleted` = 0
                LEFT JOIN (
                    SELECT
                        `FormId`,
                        `ItemBankId`,
                        MIN(`EquationTemplateId`) AS `EquationTemplateId`,
                        MIN(`EquationCategoryId`) AS `EquationCategoryId`
                    FROM
                        `paperitembankequations`
                    GROUP BY
                        `FormId`,
                        `ItemBankId`
                ) `pie`
                    ON `pie`.`FormId` = `cqa`.`PaperFormId`
                    AND `pie`.`ItemBankId` = `q`.`ItemBankId`

                LEFT JOIN (
                    SELECT
                        `sps`.`PaperId`,
                        `sps`.`ScheduleMetadataId`,
                        MAX(`pss`.`MinimumPassingMarks`) AS `PassingScore`
                    FROM `schedulepapers` `sps`
                    INNER JOIN `papersettings` `pss`
                        ON `pss`.`SchedulePaperId` = `sps`.`Id`
                    GROUP BY `sps`.`PaperId`, `sps`.`ScheduleMetadataId`
                ) `ps_agg`
                    ON `ps_agg`.`PaperId` = `cqa`.`PaperId`
                    AND `ps_agg`.`ScheduleMetadataId` = `cqa`.`ScheduleId`

                LEFT JOIN `equationtemplates` `et`
                    ON `et`.`Id` = `pie`.`EquationTemplateId`
                LEFT JOIN `equationcategories` `ec`
                    ON `ec`.`Id` = `pie`.`EquationCategoryId`
                LEFT JOIN `itembanks` `ib`
                    ON `ib`.`Id` = `pie`.`ItemBankId`
                LEFT JOIN `sections` `s`
                    ON `s`.`Id` = `cqa`.`SectionId`
                LEFT JOIN `adaptivesection` `asec`
                    ON `asec`.`Id` = `cqa`.`SectionId`
                LEFT JOIN (
                    SELECT DISTINCT `RegistrationId`
                    FROM `candidatequestionsanswers`
                    WHERE `EvaluationStatus` IN (1, 2) 
                    AND `QuestionType` <> 'Comprehension'
                    AND `IsDeleted` = 0
                ) `incomplete_regs`
                    ON `incomplete_regs`.`RegistrationId` = `cqa`.`RegistrationId`
               WHERE
                    `ced`.`CandidateId` IS NOT NULL
                    AND `ced`.`ReviewStatus` <> 1
                    AND `cqa`.`QuestionType` <> 'Comprehension'
                    AND `ced`.`IsDemo` = 0
                    AND `cqa`.`IsDeleted` = 0
                    AND `incomplete_regs`.`RegistrationId` IS NULL;
        ";
    }
}
