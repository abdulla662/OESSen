using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Candidates
{
    internal class ValidateCandidateRegistration : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `ValidateCandidateRegistration`;";

        public string CreateCommand => @"
            CREATE PROCEDURE `ValidateCandidateRegistration`(
                IN candidatesJson JSON,
                IN p_OrganizationId INT,
                OUT status INT,
                OUT errorMessage VARCHAR(4000)
            )
            BEGIN
                DECLARE duplicate_info TEXT DEFAULT NULL;

                DECLARE EXIT HANDLER FOR SQLEXCEPTION
                BEGIN
                    GET DIAGNOSTICS CONDITION 1 errorMessage = MESSAGE_TEXT;
                    SET status = 0;
                    ROLLBACK;
                END;

                SET status = 1;
                SET errorMessage = 'Validation successful.';

                START TRANSACTION;

                CREATE TEMPORARY TABLE IF NOT EXISTS TempValidationCandidates (
                    Email VARCHAR(255),
                    RegistrationNumber BIGINT
                );

                TRUNCATE TABLE TempValidationCandidates;

                INSERT INTO TempValidationCandidates (Email, RegistrationNumber)
                SELECT
                    jt.Email,
                    CAST(jt.registrationNumber AS UNSIGNED)
                FROM JSON_TABLE(
                    candidatesJson,
                    '$[*]' COLUMNS (
                        Email              VARCHAR(255) PATH '$.email',
                        registrationNumber VARCHAR(255) PATH '$.registrationNumber'
                    )
                ) AS jt;

                SELECT GROUP_CONCAT(DISTINCT CONCAT(tvc.Email, '&', tvc.RegistrationNumber) SEPARATOR ', ')
                INTO duplicate_info
                FROM TempValidationCandidates tvc
                WHERE EXISTS (
                    SELECT 1
                    FROM schedulepaperscandidates spc
                    WHERE spc.RegistrationNumber = tvc.RegistrationNumber
                      AND spc.OrganizationId = p_OrganizationId
                );

                IF duplicate_info IS NOT NULL THEN
                    SET status = 0;
                    SET errorMessage = duplicate_info;
                END IF;

                DROP TEMPORARY TABLE IF EXISTS TempValidationCandidates;

                COMMIT;
            END
        ";
    }
}
