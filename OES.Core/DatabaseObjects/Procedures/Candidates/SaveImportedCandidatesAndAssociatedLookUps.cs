using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Candidates
{
    public class SaveImportedCandidatesAndAssociatedLookUps : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `BulkInsertCandidatesAndAssociatedLookups`";

        public string CreateCommand => @"
            CREATE PROCEDURE `BulkInsertCandidatesAndAssociatedLookups`(
                IN candidatesJson JSON,
			    IN candidatesLookupLinksJson JSON,
			    IN OrgSignature VARCHAR(255),
			    IN CreationUser VARCHAR(255),
			    IN OrganizationId INT,
			    OUT status INT,
			    OUT errorMessage VARCHAR(255)
            )
            BEGIN
                DECLARE EXIT HANDLER FOR SQLEXCEPTION
                BEGIN
                    GET DIAGNOSTICS CONDITION 1
                    errorMessage = MESSAGE_TEXT;
                    SET status = 0;
                    ROLLBACK;
                END;

                START TRANSACTION;

                CREATE TEMPORARY TABLE IF NOT EXISTS TempIncomingCandidates (
	                Id INT AUTO_INCREMENT PRIMARY KEY,
                    CandidateCode VARCHAR(255),
                    Name VARCHAR(255),
                    UserName VARCHAR(255),
                    NationalId VARCHAR(255),
                    Password VARCHAR(255),
                    Qualification VARCHAR(255),
                    DateOfBirth DATETIME,
                    Address VARCHAR(255),
                    Mobile VARCHAR(255),
                    Email VARCHAR(255) ,
                    PhotoURL VARCHAR(255),
                    SignatureURL VARCHAR(255),
                    Gender INT,
                    RegistrationCenterCode VARCHAR(255),
                    RegistrationDateTime DATETIME
                );

                INSERT INTO TempIncomingCandidates (
                    CandidateCode, Name, UserName, NationalId, Password, Qualification, DateOfBirth,
                    Address, Mobile, Email, PhotoURL, SignatureURL, Gender, RegistrationCenterCode, RegistrationDateTime
                )
                SELECT
                    jt.CandidateCode, jt.Name, jt.UserName, jt.NationalId, jt.Password, jt.Qualification, jt.DateOfBirth,
	                jt.Address, jt.Mobile, jt.Email, jt.PhotoURL, jt.SignatureURL, jt.Gender, jt.RegistrationCenterCode, jt.RegistrationDateTime
                FROM
                    JSON_TABLE(candidatesJson, '$[*]' COLUMNS (
                        CandidateCode VARCHAR(255) PATH '$.candidateCode',
                        Name VARCHAR(255) PATH '$.name',
                        UserName VARCHAR(255) PATH '$.userName',
                        NationalId VARCHAR(255) PATH '$.nationalId',
                        Password VARCHAR(255) PATH '$.password',
                        Qualification VARCHAR(255) PATH '$.qualification',
                        DateOfBirth datetime PATH '$.dateOfBirth',
                        Address VARCHAR(255) PATH '$.address',
                        Mobile VARCHAR(255) PATH '$.mobile',
                        Email VARCHAR(255) PATH '$.email',
                        PhotoURL VARCHAR(255) PATH '$.photoURL',
                        SignatureURL VARCHAR(255) PATH '$.signatureURL',
                        Gender INT PATH '$.gender',
                        RegistrationCenterCode VARCHAR(255) PATH '$.registrationCenterCode',
                        RegistrationDateTime VARCHAR(255) PATH '$.registrationDateTime'
                    )) AS jt;

                INSERT IGNORE INTO Candidates (
                    CandidateCode, Name, UserName, NationalId, Password, Qualification, DateOfBirth, Address, Mobile, Email,
                    PhotoURL, SignatureURL, Gender, RegistrationCenterCode, RegistrationDateTime, CreationDate, CreationUser,
                    OrganizationSignature, IsActive, IsDeleted, OrganizationId
                )
                SELECT
                    tc.CandidateCode, tc.Name, tc.UserName, tc.NationalId, tc.Password, tc.Qualification, tc.DateOfBirth,
                    tc.Address, tc.Mobile, tc.Email, tc.PhotoURL, tc.SignatureURL, tc.Gender, tc.RegistrationCenterCode,
                    tc.RegistrationDateTime, NOW(), CreationUser, OrgSignature, 1, 0, OrganizationId
                FROM TempIncomingCandidates tc;

                CREATE TEMPORARY TABLE IF NOT EXISTS TempIncomingCandidateLookupLinks (
                    CandidateEmail VARCHAR(255) ,
                    LookupId BIGINT
                );
                     
                INSERT INTO TempIncomingCandidateLookupLinks (CandidateEmail, LookupId)
                SELECT
                    link.CandidateEmail , link.LookupId
                FROM
                    JSON_TABLE(candidatesLookupLinksJson, '$[*]' COLUMNS (
                        CandidateEmail VARCHAR(255) PATH '$.candidateEmail',
                        LookupId BIGINT PATH '$.lookupId'
                    )) AS link;

                DELETE FROM candidatesorganizationnodelookupitems 
				WHERE CandidateId IN (
					SELECT C.Id AS CandidateId
					FROM TempIncomingCandidateLookupLinks TCL
					INNER JOIN Candidates C
						ON TCL.CandidateEmail = C.Email AND C.OrganizationId = OrganizationId
				);

                INSERT IGNORE INTO candidatesorganizationnodelookupitems (
                    CandidateId, OrganizationNodeLookupItemId, CreationDate, CreationUser, OrganizationSignature,
                    IsActive, IsDeleted, OrganizationId
                )
                SELECT
                    C.Id AS CandidateId,
                    TCL.LookupId AS OrganizationNodeLookupItemId,
                    NOW() AS CreationDate,
                    CreationUser,
                    OrgSignature,
                    1,
                    0,
                    OrganizationId
                FROM
                    TempIncomingCandidateLookupLinks TCL
                INNER JOIN
                    Candidates C
                    ON TCL.CandidateEmail = C.Email AND C.OrganizationId = OrganizationId;

                -- 5. Clean up temporary tables
                DROP TEMPORARY TABLE IF EXISTS TempIncomingCandidates;
                DROP TEMPORARY TABLE IF EXISTS TempIncomingCandidateLookupLinks;

                COMMIT;
                SET status = 1; -- Indicate success
                SET errorMessage = 'Process completed successfully.';

            END
        ";
    }
}