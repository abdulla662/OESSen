using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.ItemBanksFlattenedTreeForAutoPaper
{
    public class CreatingFlattenedTreeNodeView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @$"
            CREATE OR REPLACE VIEW `vw_flattenedtreenodes` AS
            SELECT
                `ibp`.`PaperId` AS `PaperId`,
                `pm`.`AllowInstantResult` AS `AllowInstantResultPaper`,
                `ibp`.`Id` AS `ItemBankPointId`,
                `ib`.`Id` AS `ItemBankId`,
                `ib`.`Name` AS `ItemBankName`,
                `qt`.`Id` AS `QuestionTypeId`,
                `qt`.`Name` AS `QuestionTypeName`,
                `qt`.`IsAutoCorrectable` AS `IsAutoCorrectableQuestion`,
                `dl`.`Id` AS `DifficultyLevelId`,
                `dl`.`Name` AS `DifficultyLevelName`,
                COUNT(DISTINCT `qm`.`Id`) AS `QuestionCount`,
                SUM(
                    CASE
                        WHEN `qm`.`QuestionTypeId` = `qt`.`Id` THEN 1
                        ELSE 0
                    END
                ) AS `QuestionTypeCount`,
            COALESCE(`qsub`.`SubQuestionCount`, 0) AS `SubQuestionCount`
            FROM
                `itembankpoints` `ibp`
                JOIN `papermetadata` `pm` ON `ibp`.`PaperId` = `pm`.`Id`
                JOIN `itembanks` `ib` ON `ibp`.`ItemBankId` = `ib`.`Id`
                JOIN `questionsmetadata` `qm` ON `qm`.`ItemBankId` = `ib`.`Id`
                JOIN `questiontypes` `qt` ON `qm`.`QuestionTypeId` = `qt`.`Id`
                JOIN `questionsdetails` `qd` ON 
                    `qd`.`QuestionMetadataId` = `qm`.`Id` AND
                    `qd`.`LanguageId` = `pm`.`LanguageId`
                JOIN `difficultylevels` `dl` ON 
                    `qm`.`DifficultyLevelId` = `dl`.`Id` AND
                    `dl`.`DifficultyProfileId` = `pm`.`DifficultyProfileId`
                LEFT JOIN (SELECT 
                            `qm_parent`.`Id` AS `QuestionId`,
                            COUNT(`qm_child`.`Id`) AS `SubQuestionCount`
                        FROM `questionsmetadata` `qm_parent`
                        LEFT JOIN `questionsmetadata` `qm_child` ON `qm_child`.`ParentId` = `qm_parent`.`Id`
                        AND `qm_child`.`IsDeleted` = 0 AND `qm_child`.`IsActive` = 1
                WHERE `qm_parent`.`IsRoot` = 1 AND `qm_parent`.`ParentId` IS NULL
                GROUP BY `qm_parent`.`Id`) `qsub` ON `qsub`.`QuestionId` = `qm`.`Id`
            WHERE
                `qm`.`IsRoot` = 1 AND
                `qm`.`ParentId` IS NULL AND
                `qm`.`QuestionStatus` = {(long)QuestionStatus.Approved} AND
                `qm`.`IsActive` = 1 AND
                `qm`.`IsDeleted` = 0 AND
                `qd`.`IsActive` = 1 AND
                `qd`.`IsDeleted` = 0
            GROUP BY
                `ibp`.`PaperId`,
                `ibp`.`Id`,
                `qt`.`Id`,
                `dl`.`Id`,
                `ibp`.`ItemBankId`,
                `qsub`.`SubQuestionCount`;
        ";
    }
}