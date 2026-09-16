using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Views.Candidate
{
    public class CreatingSchedulePaperAllocationsView : IDatabaseView
    {
        public string CreateOrReplaceCommand => @"
            CREATE OR REPLACE VIEW vw_schedulepaperallocation AS
            SELECT 
                spc.SchedulePaperId,
                spc.VenueId,
                v.Name AS VenueName,
                COUNT(spc.CandidateId) AS CandidateCount,
                spc.OrganizationId
            FROM schedulepaperscandidates spc
            INNER JOIN venues v ON spc.VenueId = v.Id
            GROUP BY
                spc.SchedulePaperId,
                spc.VenueId,
                v.Name,
                spc.OrganizationId;
        ";
    }
}
