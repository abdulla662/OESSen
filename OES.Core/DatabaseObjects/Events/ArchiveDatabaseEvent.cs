using OES.Core.DatabaseObjects.CommonInterfaces;
using OES.Core.DatabaseObjects.Procedures.Archiving;

namespace OES.Core.DatabaseObjects.Events
{
    public class ArchiveDatabaseEvent : IDatabaseEvent
    {
        public string CreateOrReplaceCommand => $@"
            SET GLOBAL event_scheduler = ON;

            DROP EVENT IF EXISTS e_ArchiveAndCleanupEvery28th;

            CREATE EVENT e_ArchiveAndCleanupEvery28th
            ON SCHEDULE EVERY 2 MONTH
            STARTS (
                IF(
                    NOW() <= DATE_FORMAT(NOW(), '%Y-%m-28 03:00:00'),
                    DATE_FORMAT(NOW(), '%Y-%m-28 03:00:00'),
                    DATE_FORMAT(NOW() + INTERVAL 1 MONTH, '%Y-%m-28 03:00:00')
                )
            )
            ON COMPLETION PRESERVE
            DO
            BEGIN
                CALL {InitializeSeedGraphIfNeeded.ProcedureName}();
                CALL {ArchiveAllAndCleanup.ProcedureName}();
            END;
        ";
    }
}
