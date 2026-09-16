
using OES.Core.DatabaseObjects.CommonInterfaces;
using OES.Core.DatabaseObjects.Procedures.Questions;

namespace OES.Core.DatabaseObjects.Events
{
    public class QuestionExhaustionCountEvent : IDatabaseEvent
    {
        public string CreateOrReplaceCommand => $@"
            DROP EVENT IF EXISTS e_UpdateQuestionExhaustionCount;
            
            -- Runs daily at 12:00 AM
            CREATE EVENT e_UpdateQuestionExhaustionCount
            ON SCHEDULE EVERY 1 DAY
            STARTS DATE_FORMAT(NOW() + INTERVAL 1 DAY, '%Y-%m-%d 00:00:00')
            ON COMPLETION PRESERVE
            DO
            BEGIN
                CALL {UpdateQuestionExhaustionCount.ProcedureName}();
            END
        ";
    }
}
