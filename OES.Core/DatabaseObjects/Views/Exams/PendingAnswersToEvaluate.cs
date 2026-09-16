using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.Exams
{
    public class PendingAnswersToEvaluate : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW `vw_PendingAnswersToEvaluate` AS
            SELECT
                `ced`.`CandidatePaperId`             AS `CandidateExamID`,
                `ced`.`CandidateId`                  AS `CandidateID`,
                `ced`.`RegistrationId`               AS `RegistrationId`,
                `cqa`.`VenueId`                      AS `VenueId`,
                `cqa`.`VenueCode`                    AS `VenueCode`,
                `sm`.`Id`                            AS `EventID`,
                `sm`.`Name`                          AS `EventName`,
                `sm`.`StartDate`                     AS `EventStartDate`,
                `sm`.`EndDate`                       AS `EventEndDate`,
                `ced`.`PaperIdActual`                AS `PaperID`,
                `ced`.`PaperName`                    AS `PaperName`,
                `ced`.`ExamTrialStartDate`           AS `ExamAttemptDate`,
                `ced`.`TrialNumber`                  AS `AttemptNumber`,
                `cqa`.`MarksObtained`                AS `MarksObtained`,
                `ced`.`CandidateEndedExam`           AS `IsExamCompleted`,
                `ced`.`ExamTrialEndDate`             AS `ExamEndDate`,
                `cqa`.`Id`                           AS `CandidateAnswerID`,
                `cqa`.`QuestionId`                   AS `QuestionID`,
                `qmt`.`ParentId`                     AS `ParentQuestionID`,
                `cqa`.`SectionName`                  AS `SectionName`,
                `qd`.`Body`                          AS `QuestionText`,
                `qd`.`ModelAnswer`                   AS `ModelAnswer`,
                `sqp`.`SegmentQuestionResponseType`  AS `SegmentResponseType`,
                `sqp`.`SegmentAudioUrl`              AS `SegmentAudioUrl`,
                `sqp`.`OrderNumber`                  AS `SegmentOrderNumber`,
                `cqa`.`AnswerText`                   AS `TypedAnswerText`,
                `cqa`.`QuestionScore`                AS `FullMark`,
                `qmt`.`OrganizationSignature`        AS `OrganizationSignature`,
                `qmt`.`OrganizationId`               AS `OrganizationId`,
                `qmt`.`QuestionTypeId`               AS `QuestionTypeId`,
		        `qt`.`Name`                          AS `QuestionTypeName` -- TODO: Should be excluded here and make a table in evaluation, then seeding data to map the question types from OES and sending only the Id.
            FROM `candidatequestionsanswers` `cqa`
            JOIN `questionsdetails`    `qd`  ON `qd`.`QuestionMetadataId` = `cqa`.`QuestionId`
            JOIN `schedulemetadata`    `sm`  ON `sm`.`Id`                 = `cqa`.`ScheduleId`
            JOIN `candidateexamdetails` `ced` ON `ced`.`RegistrationId`   = `cqa`.`RegistrationId`
            JOIN `questionsmetadata` `qmt` ON `qmt`.`Id` = `cqa`.`QuestionId`
            LEFT JOIN `questiontypes` `qt` ON `qt`.`Id` = `qmt`.`QuestionTypeId`
            LEFT JOIN `segmentquestionproperties` `sqp` ON `sqp`.`Id` = `qd`.`SegmentQuestionPropertiesId`
            WHERE `ced`.`SentToCTR`             = 0
              AND `ced`.`ReviewStatus`          IN (0,2)
              AND `ced`.`HasNoTrial`            = 0
              AND `cqa`.`IsAutoCorrectable`     = 0
              AND `cqa`.`EvaluationStatus`      = 1
              AND `cqa`.`QuestionType`         <> 'Comprehension'";
    }
}