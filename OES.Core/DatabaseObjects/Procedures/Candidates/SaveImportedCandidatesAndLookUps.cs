using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Candidates
{
    public class SaveImportedCandidatesAndLookUps : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `BulkInsertCandidatesAndLookUps`";

        public string CreateCommand => @"
            CREATE PROCEDURE `BulkInsertCandidatesAndLookUps`(
                IN candidatesJson JSON,
                IN OrgSignature VARCHAR(255),
                IN CreationUser VARCHAR(255),
                IN OrganizationId INT,
                OUT status INT,
                OUT errorMessage VARCHAR(255)
            )
            BEGIN
                DECLARE EXIT HANDLER FOR SQLEXCEPTION
                BEGIN
                    GET DIAGNOSTICS CONDITION 1 @p1 = RETURNED_SQLSTATE, @p2 = MESSAGE_TEXT;
                    SET status = 0;
                    SET errorMessage = CONCAT('SQL Error: ', @p2);
                    ROLLBACK;
                END;

                START TRANSACTION;

                CREATE TEMPORARY TABLE TempCandidates (
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
                    Email VARCHAR(255) UNIQUE,
                    PhotoURL VARCHAR(255),
                    SignatureURL VARCHAR(255),
                    Gender INT,
                    RegistrationCenterCode VARCHAR(255),
                    RegistrationDateTime DATETIME
                );

                INSERT INTO TempCandidates (
                    CandidateCode, Name, UserName, NationalId, Password, Qualification, DateOfBirth,
                    Address, Mobile, Email, PhotoURL, SignatureURL, Gender,
                    RegistrationCenterCode, RegistrationDateTime
                )
                SELECT
                    jt.CandidateCode,
                    jt.Name,
                    jt.UserName,
                    jt.NationalId,
                    jt.Password,
                    jt.Qualification,
                    jt.DateOfBirth,
                    jt.Address,
                    jt.Mobile,
                    jt.Email,
                    jt.PhotoURL,
                    jt.SignatureURL,
                    jt.Gender,
                    jt.RegistrationCenterCode,
                    jt.RegistrationDateTime
                FROM JSON_TABLE(candidatesJson, '$[*]' COLUMNS (
                    CandidateCode           VARCHAR(255) PATH '$.CandidateCode',
                    Name                    VARCHAR(255) PATH '$.Name',
                    UserName                VARCHAR(255) PATH '$.UserName',
                    NationalId              VARCHAR(255) PATH '$.NationalId',
                    Password                VARCHAR(255) PATH '$.Password',
                    Qualification           VARCHAR(255) PATH '$.Qualification',
                    DateOfBirth             DATETIME     PATH '$.DateOfBirth',
                    Address                 VARCHAR(255) PATH '$.Address',
                    Mobile                  VARCHAR(255) PATH '$.Mobile',
                    Email                   VARCHAR(255) PATH '$.Email',
                    PhotoURL                VARCHAR(255) PATH '$.PhotoURL',
                    SignatureURL            VARCHAR(255) PATH '$.SignatureURL',
                    Gender                  INT          PATH '$.Gender',
                    RegistrationCenterCode  VARCHAR(255) PATH '$.RegistrationCenterCode',
                    RegistrationDateTime    DATETIME     PATH '$.RegistrationDateTime'
                )) AS jt;

                INSERT INTO Candidates (
                    CandidateCode, Name, UserName, NationalId, Password, Qualification, DateOfBirth,
                    Address, Mobile, Email, PhotoURL, SignatureURL, Gender, RegistrationCenterCode,
                    RegistrationDateTime, CreationDate, CreationUser, OrganizationSignature,
                    IsActive, IsDeleted, OrganizationId
                )
                SELECT
                    tc.CandidateCode,
                    tc.Name,
                    tc.UserName,
                    tc.NationalId,
                    tc.Password,
                    tc.Qualification,
                    tc.DateOfBirth,
                    tc.Address,
                    tc.Mobile,
                    tc.Email,
                    tc.PhotoURL,
                    tc.SignatureURL,
                    tc.Gender,
                    tc.RegistrationCenterCode,
                    tc.RegistrationDateTime,
                    NOW(),
                    CreationUser,
                    OrgSignature,
                    1,
                    0,
                    OrganizationId
                FROM TempCandidates tc
                ON DUPLICATE KEY UPDATE
                    CandidateCode           = VALUES(CandidateCode),
                    Name                    = VALUES(Name),
                    NationalId              = VALUES(NationalId),
                    Password                = VALUES(Password),
                    Qualification           = VALUES(Qualification),
                    DateOfBirth             = VALUES(DateOfBirth),
                    Address                 = VALUES(Address),
                    Mobile                  = VALUES(Mobile),
                    PhotoURL                = VALUES(PhotoURL),
                    SignatureURL            = VALUES(SignatureURL),
                    Gender                  = VALUES(Gender),
                    RegistrationCenterCode  = VALUES(RegistrationCenterCode),
                    RegistrationDateTime    = VALUES(RegistrationDateTime),
                    OrganizationSignature   = VALUES(OrganizationSignature),
                    IsActive                = VALUES(IsActive),
                    IsDeleted               = VALUES(IsDeleted);

                SET @affectedCandidates = ROW_COUNT(); 

                SET status = 1;
                SET errorMessage = CONCAT(
                    'Process completed. Affected (inserted/updated): ',
                    @affectedCandidates,
                    ' candidate rows; '
                );

                COMMIT;

                DROP TEMPORARY TABLE IF EXISTS TempCandidates;
            END
        ";
    }
}