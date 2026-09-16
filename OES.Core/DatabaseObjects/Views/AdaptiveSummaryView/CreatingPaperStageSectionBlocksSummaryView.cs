using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.AdaptiveSummaryView
{
    public class CreatingPaperStageSectionBlocksSummaryView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW `vw_paperstagesectionblockssummary` AS
            SELECT 
                `p`.`Id` AS `PaperId`,
                `f`.`Id` AS `FormId`,
                `p`.`Name` AS `PaperName`,
                `f`.`Name` AS `FormName`,
                `s`.`Id` AS `StageId`,
                `s`.`Name` AS `StageName`,
                `s`.`Order` AS `StageOrder`,
                `s`.`TimeInMinutes` AS `StageTime`,
                `a`.`Id` AS `SectionId`,
                `a`.`Name` AS `SectionName`,
                `a`.`Order` AS `SectionOrder`,
                `b`.`Id` AS `BlockId`,
                `b`.`Name` AS `BlockName`,
                `b`.`Code` AS `BlockCode`,
                 COUNT(
                        CASE 
                            WHEN qm_sub.Id IS NOT NULL THEN qm_sub.Id 
                            ELSE bq.QuestionMetadataId  
                        END
                      ) AS QuestionCount 
            FROM `papermetadata` `p`
                JOIN `forms` `f` ON `f`.`PaperId` = `p`.`Id`
                JOIN `stages` `s` ON `s`.`FormId` = `f`.`Id`
                JOIN `adaptivesection` `a` ON `a`.`StageId` = `s`.`Id`
                LEFT JOIN `paperformblocks` `pb` ON `pb`.`AdaptiveSectionId` = `a`.`Id` 
                    AND `pb`.`IsActive` = 1 
                    AND `pb`.`IsDeleted` = 0
                LEFT JOIN `blocks` `b` ON `pb`.`BlockId` = `b`.`Id` 
                    AND `b`.`IsActive` = 1 
                    AND `b`.`IsDeleted` = 0
                LEFT JOIN `blockquestions` `bq` ON `bq`.`BlockId` = `b`.`Id` 
                    AND `bq`.`IsActive` = 1 
                    AND `bq`.`IsDeleted` = 0               
                LEFT JOIN questionsmetadata qm_sub  ON qm_sub.ParentId = bq.QuestionMetadataId   
                    AND qm_sub.IsActive = 1 AND qm_sub.IsDeleted = 0
            WHERE 
                `p`.`IsActive` = 1 AND `p`.`IsDeleted` = 0
                AND `f`.`IsActive` = 1 AND `f`.`IsDeleted` = 0
                AND `s`.`IsActive` = 1 AND `s`.`IsDeleted` = 0
                AND `a`.`IsActive` = 1 AND `a`.`IsDeleted` = 0
            GROUP BY 
                `p`.`Id`, `f`.`Id`, `s`.`Id`, `s`.`Name`, `s`.`Order`, 
                `a`.`Id`, `a`.`Name`, `a`.`Order`, 
                `b`.`Id`, `b`.`Name`, `b`.`Code`
        ";
    }
}
