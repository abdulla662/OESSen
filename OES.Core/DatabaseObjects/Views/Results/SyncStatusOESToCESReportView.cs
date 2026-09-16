using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.Results
{
    public class SyncStatusOESToCESReportView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW vw_SyncStatusOESToCESReportView AS
            SELECT 
                COUNT(id) AS JobCount,
                VenueId,
                VenueName,
                Status,
                DATE(CreationDate) AS SyncDate,
                CASE 
                    WHEN MAX(StartedProcessingAt) IS NULL OR MAX(CompletedAt) IS NULL THEN NULL
                    ELSE CONCAT(
                        LPAD(FLOOR(TIMESTAMPDIFF(SECOND, MAX(StartedProcessingAt), MAX(CompletedAt)) / 60), 2, '0'),
                        ':',
                        LPAD(MOD(TIMESTAMPDIFF(SECOND, MAX(StartedProcessingAt), MAX(CompletedAt)), 60), 2, '0')
                    )
                END AS Duration,
                MAX(StartedProcessingAt) AS LatestRunDate
            FROM 
                realtimesyncjobs
            GROUP BY 
                VenueId,
                VenueName,
                Status,
                DATE(CreationDate);
        ";
    }
}