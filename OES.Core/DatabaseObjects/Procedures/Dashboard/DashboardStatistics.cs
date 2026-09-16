using OES.Core.DatabaseObjects.CommonInterfaces;
namespace OES.Core.DatabaseObjects.Procedures.Dashboard
{
    public class DashboardStatisticsProcedure : IDatabaseStoredProcedure
    {
        public string DropCommand =>
            "DROP PROCEDURE IF EXISTS sp_GetDashboardStatistics";

        public string CreateCommand => @"
            CREATE PROCEDURE sp_GetDashboardStatistics (
                IN p_OrganizationId BIGINT,
                IN p_OrganizationSignature VARCHAR(255),
                IN p_CreationUser VARCHAR(300),
                IN p_IsSuperAdminOrEntityAdmin TINYINT
            )
            BEGIN
                SELECT
                    (
                        SELECT COUNT(Id)
                        FROM ItemBanks
                        WHERE OrganizationId = p_OrganizationId
                          AND OrganizationSignature = BINARY p_OrganizationSignature
                          AND IsDeleted = 0
                          AND IsActive = 1
                          AND (ParentId IS NULL OR ParentId = 0)
                    ) AS TotalItemBanks,
                    (
                        SELECT COUNT(Id)
                        FROM QuestionsMetadata
                        WHERE OrganizationId = p_OrganizationId
                          AND OrganizationSignature = BINARY p_OrganizationSignature
                          AND IsDeleted = 0
                          AND IsActive = 1
                    ) AS TotalQuestions,
                    (
                        SELECT COUNT(Id)
                        FROM PaperMetadata
                        WHERE OrganizationId = p_OrganizationId
                          AND OrganizationSignature = BINARY p_OrganizationSignature
                          AND (p_IsSuperAdminOrEntityAdmin = 1 OR CreationUser = BINARY p_CreationUser)
                          AND IsDeleted = 0
                          AND IsActive = 1
                    ) AS TotalPapers,
                    (
                        SELECT COUNT(Id)
                        FROM ScheduleMetadata
                        WHERE OrganizationId = p_OrganizationId
                          AND OrganizationSignature = BINARY p_OrganizationSignature
                          AND IsDeleted = 0
                          AND IsActive = 1
                    ) AS TotalSchedules,
                    (
                        SELECT COUNT(Id)
                        FROM Candidates
                        WHERE OrganizationId = p_OrganizationId
                          AND OrganizationSignature = BINARY p_OrganizationSignature
                          AND IsDeleted = 0
                          AND IsActive = 1
                    ) AS TotalCandidates;
            END;
        ";
    }
}