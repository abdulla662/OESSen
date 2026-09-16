using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.Questions
{
    public class CreatingQuestionTypesCountView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW `vw_questiontypecountsbyitembank` AS
            SELECT 
                `ib`.`Id` AS `ItemBankId`,
                `qt`.`Name` AS `QuestionType`,
                `dl`.`Name` AS `DifficultyLevel`,
                COUNT(`qm`.`Id`) AS `QuestionCount`
            FROM
                ((((((`itembanks` `ib`
                JOIN `itembankpoints` `ibp` ON ((`ib`.`Id` = `ibp`.`ItemBankId`)))
                JOIN `papermetadata` `p` ON ((`ibp`.`PaperId` = `p`.`Id`)))
                JOIN `difficultyprofiles` `dp` ON ((`p`.`DifficultyProfileId` = `dp`.`Id`)))
                JOIN `difficultylevels` `dl` ON ((`dp`.`Id` = `dl`.`DifficultyProfileId`)))
                JOIN `questionsmetadata` `qm` ON (((`ib`.`Id` = `qm`.`ItemBankId`)
                    AND (`dl`.`Id` = `qm`.`DifficultyLevelId`))))
                JOIN `questiontypes` `qt` ON ((`qm`.`QuestionTypeId` = `qt`.`Id`)))
            GROUP BY `ib`.`Id` , `qt`.`Name` , `dl`.`Name`
        ";
    }
}
