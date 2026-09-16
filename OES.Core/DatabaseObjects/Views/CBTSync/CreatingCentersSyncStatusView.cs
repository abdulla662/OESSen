using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.CBTSync
{
    public class CreatingCentersSyncStatusView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW vw_CentersSyncStatusView AS
            SELECT
                v.Code AS CenterCode,
                v.Name AS CenterName,
                'KSA' AS Region,
                cbt.TotalCandidates AS CBTReceived,
                cbt.AssignedToPaperCount AS Allocated,
                cbt.AssignedToPaperCount AS Pushed,
                cbt.AssignedToPaperCount AS Acknowledged,
                rsj.Status,
                cbt.LastRunTime AS LastSync,
                cbt.SyncDate,
                v.Id AS VenueId
            FROM venues v
            LEFT JOIN (
                SELECT
                    j.VenueCode,
                    COUNT(*) AS TotalCandidates,
                    SUM(CASE WHEN r.IsAssignedToPaper = 1 THEN 1 ELSE 0 END) AS AssignedToPaperCount,
                    MAX(r.CreationDate) AS LastRunTime,
                    DATE(MAX(r.CreationDate)) AS SyncDate
                FROM cbtcandidatesreceiveddata r
                INNER JOIN cbtcandidatessyncjobs j ON r.JobId = j.Id
                WHERE r.IsDeleted = 0
                GROUP BY j.VenueCode
            ) cbt ON v.Code = cbt.VenueCode
            LEFT JOIN (
                SELECT
                    VenueId,
                    Status,
                    ROW_NUMBER() OVER (PARTITION BY VenueId ORDER BY CreationDate DESC) AS rn
                FROM realtimesyncjobs
                WHERE IsDeleted = 0
            ) rsj ON v.Id = rsj.VenueId AND rsj.rn = 1
            WHERE v.IsDeleted = 0 AND v.IsActive = 1;
        ";
    }
}
