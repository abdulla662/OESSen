using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.AuditLogs
{
    public class GetAuditLogsProcedure : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `GetAuditLogs`";

        // TODO: Change the procedure parameters to DateOnly and boolean OrderAsc (A. Ali).
        public string CreateCommand => @"
            CREATE PROCEDURE `GetAuditLogs`(
                IN p_SearchKey     TEXT,
                IN p_PathName      TEXT,
                IN p_PageName      TEXT,
                IN p_FromDate      DATETIME,
                IN p_ToDate        DATETIME,
                IN p_PageIndex     INT,
                IN p_PageSize      INT,
                IN p_PaginationOff BOOLEAN
            )
            BEGIN
                DECLARE v_Offset INT DEFAULT 0;

                SET v_Offset = p_PageIndex * p_PageSize;

                -- ============================================================
                -- Total Records
                -- ============================================================

                SELECT COUNT(*) AS TotalRecords
                FROM auditlogs
                WHERE
                    (
                        p_SearchKey IS NULL
                        OR p_SearchKey = ''
                        OR LOWER(`Action`) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%')
                        OR LOWER(CreationUser) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%')
                    )
                    AND (CreationUser <> 'superadmin@careerfirst.sa' OR ActionAt < '2026-07-12 00:00:00')
                    AND (p_PathName IS NULL OR p_PathName = '' OR LOWER(PathName) LIKE CONCAT('%', LOWER(p_PathName COLLATE utf8mb4_0900_ai_ci), '%'))
                    AND (p_PageName IS NULL OR p_PageName = '' OR LOWER(PageName) LIKE CONCAT('%', LOWER(p_PageName COLLATE utf8mb4_0900_ai_ci), '%'))
                    AND (p_FromDate IS NULL OR ActionAt >= p_FromDate)
                    AND (p_ToDate IS NULL OR ActionAt <= p_ToDate);

                -- ============================================================
                -- Audit Logs
                -- ============================================================

                IF p_PaginationOff = TRUE THEN

                    SELECT
                        Id,
                        `Action`,
                        CreationUser,
                        ActionAt,
                        Url,
                        PathName,
                        PageName,
                        IPAddress
                    FROM auditlogs
                    WHERE
                        (
                            p_SearchKey IS NULL
                            OR p_SearchKey = ''
                            OR LOWER(`Action`) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%')
                            OR LOWER(CreationUser) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%')
                        )
                        AND (CreationUser <> 'superadmin@careerfirst.sa' OR ActionAt < '2026-07-12 00:00:00')
                        AND (p_PathName IS NULL OR p_PathName = '' OR LOWER(PathName) LIKE CONCAT('%', LOWER(p_PathName COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_PageName IS NULL OR p_PageName = '' OR LOWER(PageName) LIKE CONCAT('%', LOWER(p_PageName COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_FromDate IS NULL OR ActionAt >= p_FromDate)
                        AND (p_ToDate IS NULL OR ActionAt <= p_ToDate)
                    ORDER BY ActionAt DESC;

                ELSE

                    SELECT
                        Id,
                        `Action`,
                        CreationUser,
                        ActionAt,
                        Url,
                        PathName,
                        PageName,
                        IPAddress
                    FROM auditlogs
                    WHERE
                        (
                            p_SearchKey IS NULL
                            OR p_SearchKey = ''
                            OR LOWER(`Action`) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%')
                            OR LOWER(CreationUser) LIKE CONCAT('%', LOWER(p_SearchKey COLLATE utf8mb4_0900_ai_ci), '%')
                        )
                        AND (CreationUser <> 'superadmin@careerfirst.sa' OR ActionAt < '2026-07-12 00:00:00')
                        AND (p_PathName IS NULL OR p_PathName = '' OR LOWER(PathName) LIKE CONCAT('%', LOWER(p_PathName COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_PageName IS NULL OR p_PageName = '' OR LOWER(PageName) LIKE CONCAT('%', LOWER(p_PageName COLLATE utf8mb4_0900_ai_ci), '%'))
                        AND (p_FromDate IS NULL OR ActionAt >= p_FromDate)
                        AND (p_ToDate IS NULL OR ActionAt <= p_ToDate)
                    ORDER BY ActionAt DESC
                    LIMIT v_Offset, p_PageSize;

                END IF;

            END
        ";
    }
}