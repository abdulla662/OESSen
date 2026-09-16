using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.CBTSync
{
    public class CreatingCBTSyncStatusView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW vw_cbtsyncstatus AS
            SELECT
                COUNT(*) AS TotalCandidates,
                SUM(CASE WHEN r.Status IN (0, 1, 2) THEN 1 ELSE 0 END) AS ImportedCount,
                SUM(CASE WHEN r.IsAssignedToPaper = 1 THEN 1 ELSE 0 END) AS AssignedToPaperCount,
                SUM(CASE WHEN r.IsAssignedToPaper = 0 THEN 1 ELSE 0 END) AS NotAssignedToPaperCount,
                MAX(r.CreationDate) AS LastRunTime,
                MAX(r.IsManualSync) AS IsManualSync,
                MAX(r.IsAutoSync) AS IsAutoSync,
                r.OrganizationId,
                v.Id AS VenueId,
                v.Code AS VenueCode,
                v.Name AS VenueName,
                (SELECT COUNT(DISTINCT j2.VenueCode)
                 FROM cbtcandidatessyncjobs j2
                 WHERE j2.IsDeleted = 0) AS TotalVenues,
                COALESCE((SELECT AVG(TIMESTAMPDIFF(MICROSECOND, j3.JobStartTime, j3.JobEndTime) / 1000)
                 FROM cbtcandidatessyncjobs j3
                 WHERE j3.JobEndTime IS NOT NULL AND j3.IsDeleted = 0), 0) AS AvgTimeMs
            FROM cbtcandidatesreceiveddata r
            INNER JOIN cbtcandidatessyncjobs j ON r.JobId = j.Id
            INNER JOIN venues v ON j.VenueCode = v.Code
            WHERE r.IsDeleted = 0
            GROUP BY r.OrganizationId, v.Id, v.Code, v.Name;
        ";
    }
}