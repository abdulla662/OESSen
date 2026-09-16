using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.Questions
{
    public class CreatingQuestionsManualSelectionSummary : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE
            VIEW `vw_manualquestionsummary` AS
            SELECT 
                `pm`.`Id` AS `PaperId`,
                `pm`.`Name` AS `PaperName`,
                `sec`.`Id` AS `SectionId`,
                `sec`.`Name` AS `SectionName`,
                `sec`.`OrderId` AS `SectionOrderId`,
                `f`.`Id` AS `FormId`,
                `f`.`Name` AS `FormName`,
                `ib`.`Id` AS `ItemBankId`,
                `ib`.`Name` AS `ItemBankName`,
                `qt`.`Id` AS `QuestionTypeId`,
                `qt`.`Name` AS `QuestionTypeName`,
                `dl`.`Id` AS `DifficultyLevelId`,
                `dl`.`Name` AS `DifficultyLevelName`,
                COUNT(DISTINCT `qm`.`Id`) AS `QuestionsCount`
            FROM `papermetadata` `pm`
                JOIN `forms` `f` ON `f`.`PaperId` = `pm`.`Id`
                JOIN `sections` `sec` ON `sec`.`PaperId` = `pm`.`Id` AND `sec`.`FormId` = `f`.`Id`
                JOIN `manualpaperitembankquestionsections` `mpiqs` ON `mpiqs`.`SectionId` = `sec`.`Id`
                JOIN `itembankpoints` `ibp` ON `ibp`.`Id` = `mpiqs`.`ItemBankPointId` AND `ibp`.`PaperId` = `pm`.`Id`
                JOIN `itembanks` `ib` ON `ib`.`Id` = `ibp`.`ItemBankId`
                JOIN `questionsmetadata` `qm` ON `qm`.`ItemBankId` = `ib`.`Id` AND `qm`.`Id` = `mpiqs`.`QuestionMetaDataId`
                JOIN `questiontypes` `qt` ON `qt`.`Id` = `qm`.`QuestionTypeId`
                JOIN `difficultylevels` `dl` ON `dl`.`Id` = `qm`.`DifficultyLevelId` AND `dl`.`Id` = `mpiqs`.`DifficultyLevelId`
            GROUP BY `pm`.`Id`, `sec`.`Id`, `f`.`Id`, `ib`.`Id`, `qt`.`Id`, `dl`.`Id`
            ORDER BY `f`.`Id`;
        ";
    }
}
