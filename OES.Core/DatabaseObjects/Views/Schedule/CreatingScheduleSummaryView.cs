using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.Schedule
{
    public class CreatingScheduleSummaryView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW `vw_schedulepapers_summary` AS
            SELECT
                `sm`.`Id` AS `ScheduleMetadataId`,
                `sm`.`Name` AS `ScheduleName`,
                `sm`.`Code` AS `ScheduleCode`,
                `sm`.`Description` AS `ScheduleDescription`,
                `sm`.`StartDate` AS `ScheduleStartDate`,
                `sm`.`EndDate` AS `ScheduleEndDate`,
                `sm`.`StartTime` AS `ScheduleStartTime`,
                `sm`.`EndTime` AS `ScheduleEndTime`,
                `sm`.`ScheduleLocation` AS `ScheduleLocation`,
                `sm`.`PublishingStatus` AS `SchedulePublishingStatus`,
                `pm`.`Id` AS `PaperId`,
                `pm`.`Name` AS `PaperName`,
                `pm`.`Code` AS `PaperCode`,
                `pm`.`type` AS `PaperType`,
                `pm`.`QuestionSelectionType` AS `PaperQuestionSelectionType`,
                `pm`.`AdaptiveSubtype` AS `AdaptiveSubtype`,
                `sp`.`StartDate` AS `PaperStartDate`,
                `sp`.`EndDate` AS `PaperEndDate`,
                `sp`.`StartTime` AS `PaperStartTime`,
                `sp`.`EndTime` AS `PaperEndTime`,
                `sp`.`Description` AS `SchedulePaperDescription`
            FROM
                ((`schedulemetadata` `sm`
                LEFT JOIN `schedulepapers` `sp` ON (((`sm`.`Id` = `sp`.`ScheduleMetadataId`)
                    AND (`sp`.`IsActive` = 1)
                    AND (`sp`.`IsDeleted` = 0))))
                LEFT JOIN `papermetadata` `pm` ON (((`sp`.`PaperId` = `pm`.`Id`)
                    AND (`pm`.`IsActive` = 1)
                    AND (`pm`.`IsDeleted` = 0))))
        ";
    }
}