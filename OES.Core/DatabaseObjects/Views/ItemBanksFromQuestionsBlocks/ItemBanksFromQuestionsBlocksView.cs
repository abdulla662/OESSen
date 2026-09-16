using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.ItemBanksFromQuestionsBlocks
{
    public class ItemBanksFromQuestionsBlocksView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW `vw_ItemBanksFromQuestionsBlocksView` AS
            SELECT DISTINCT
                `pfb`.`PaperId` AS `PaperId`,
                `q`.`ItemBankId` AS `ItemBankId`,
                `ib`.`name` AS `ItemBankName`
            FROM
                `paperformblocks` `pfb`
                JOIN `blocks` `b` ON `b`.`id` = `pfb`.`BlockId`
                JOIN `blockquestions` `bq` ON `bq`.`BlockId` = `b`.`id`
                JOIN `questionsmetadata` `q` ON `q`.`id` = `bq`.`QuestionMetadataId`
                JOIN `itembanks` `ib` ON `ib`.`id` = `q`.`ItemBankId`;
        ";
    }
}