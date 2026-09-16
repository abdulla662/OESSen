using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Candidates
{
    public class SaveImportedCandidatesToSchedulePaperCandidates : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `BulkInsertCandidatesandSchedulePapers`";

        public string CreateCommand => @"
            CREATE PROCEDURE `BulkInsertCandidatesandSchedulePapers` (
                IN candidatesJson JSON,
                IN OrgSignature VARCHAR(255),
                IN CreationUser VARCHAR(255),
                IN OrganizationId INT,
                IN VenueId INT,
                IN SchedulePaperId INT,
                IN BatchId INT,
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

                DROP TEMPORARY TABLE IF EXISTS TempCandidates;

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
                    Email VARCHAR(255),
                    PhotoURL VARCHAR(255),
                    SignatureURL VARCHAR(255),
                    Gender INT,
                    RegistrationCenterCode VARCHAR(255),
                    RegistrationDateTime DATETIME,
                    RegistrationNumber VARCHAR(255),
                    CreationDate DATETIME,
                    CandidateExamDate DATETIME
                );

                INSERT INTO TempCandidates (
                    CandidateCode,
                    Name,
                    UserName,
                    NationalId,
                    Password,
                    Qualification,
                    DateOfBirth,
                    Address,
                    Mobile,
                    Email,
                    PhotoURL,
                    SignatureURL,
                    Gender,
                    RegistrationCenterCode,
                    RegistrationDateTime,
                    RegistrationNumber,
                    CreationDate,
                    CandidateExamDate
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
                    jt.RegistrationDateTime,
                    jt.RegistrationNumber,
                    NOW(),
                    jt.CandidateExamDate
                FROM JSON_TABLE(
                    candidatesJson,
                    '$[*]'
                    COLUMNS (
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
                        RegistrationDateTime    DATETIME     PATH '$.RegistrationDateTime',
                        RegistrationNumber      VARCHAR(255) PATH '$.RegistrationNumber',
                        CandidateExamDate       DATETIME     PATH '$.CandidateExamDate'
                    )
                ) AS jt;

                ALTER TABLE TempCandidates
                    ADD INDEX idx_tc_nationalid (NationalId),
                    ADD INDEX idx_tc_regnum (RegistrationNumber);

                INSERT INTO Candidates (
                    CandidateCode,
                    Name,
                    UserName,
                    NationalId,
                    Password,
                    Qualification,
                    DateOfBirth,
                    Address,
                    Mobile,
                    Email,
                    PhotoURL,
                    SignatureURL,
                    Gender,
                    RegistrationCenterCode,
                    RegistrationDateTime,
                    CreationDate,
                    CreationUser,
                    OrganizationSignature,
                    IsActive,
                    IsDeleted,
                    OrganizationId
                )
                SELECT * FROM (
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
                        NOW() as CreationDate,
                        CreationUser,
                        OrgSignature as OrganizationSignature,
                        1 as IsActive,
                        0 as IsDeleted,
                        OrganizationId
                    FROM TempCandidates tc
                ) AS new_candidate

                ON DUPLICATE KEY UPDATE
                    CandidateCode           = new_candidate.CandidateCode,
                    Name                    = new_candidate.Name,
                    NationalId              = new_candidate.NationalId,
                    Password                = new_candidate.Password,
                    Qualification           = new_candidate.Qualification,
                    DateOfBirth             = new_candidate.DateOfBirth,
                    Address                 = new_candidate.Address,
                    Mobile                  = new_candidate.Mobile,
                    PhotoURL                = new_candidate.PhotoURL,
                    SignatureURL            = new_candidate.SignatureURL,
                    Gender                  = new_candidate.Gender,
                    RegistrationCenterCode  = new_candidate.RegistrationCenterCode,
                    RegistrationDateTime    = new_candidate.RegistrationDateTime,
                    OrganizationSignature   = new_candidate.OrganizationSignature,
                    IsActive                = new_candidate.IsActive,
                    IsDeleted               = new_candidate.IsDeleted;

                SET @affectedCandidates = ROW_COUNT();

                SET @row_num := 0;

                DROP TEMPORARY TABLE IF EXISTS TempCandidatesOrdered;

                CREATE TEMPORARY TABLE TempCandidatesOrdered AS
                SELECT
                    c.Id AS CandidateId,
                    tc.Email,
                    OrganizationId as OrganizationId,
                    SchedulePaperId as SchedulePaperId,
                    VenueId as VenueId,
                    BatchId as BatchId,
                    tc.RegistrationNumber,
                    tc.CandidateExamDate,
                    (@row_num := @row_num + 1) AS row_num
                FROM TempCandidates tc
                INNER JOIN Candidates c ON c.NationalId = tc.NationalId
                    AND c.OrganizationId = OrganizationId
                ORDER BY tc.CreationDate;

                ALTER TABLE TempCandidatesOrdered
                    ADD INDEX idx_tco_regnum (RegistrationNumber),
                    ADD INDEX idx_tco_candidateid (CandidateId);

                SET @form_num := 0;

                DROP TEMPORARY TABLE IF EXISTS TempForms;

                SELECT COUNT(*)
                INTO @hasSpecificForms
                FROM SchedulePaperForms SPF
                INNER JOIN Forms F
                    ON F.Id = SPF.FormId
                WHERE SPF.SchedulePaperId = SchedulePaperId
                    AND SPF.IsActive = 1
                    AND SPF.IsDeleted = 0;

                IF @hasSpecificForms > 0 THEN

                    CREATE TEMPORARY TABLE TempForms AS
                    SELECT
                        F.Id AS PaperFormId,
                        (@form_num := @form_num + 1) AS form_order
                    FROM SchedulePaperForms SPF
                    INNER JOIN Forms F
                        ON F.Id = SPF.FormId
                    WHERE SPF.SchedulePaperId = SchedulePaperId
                        AND SPF.IsActive = 1
                        AND SPF.IsDeleted = 0
                    ORDER BY F.CreationDate;

                ELSE

                    CREATE TEMPORARY TABLE TempForms AS
                    SELECT
                        F.Id AS PaperFormId,
                        (@form_num := @form_num + 1) AS form_order
                    FROM Forms F
                    WHERE F.PaperId = (
                            SELECT PaperId
                            FROM SchedulePapers
                            WHERE Id = SchedulePaperId
                        )
                        AND F.IsActive = 1
                        AND F.IsDeleted = 0
                    ORDER BY F.CreationDate;

                END IF;

                ALTER TABLE TempForms
                    ADD INDEX idx_tf_paperformid (PaperFormId),
                    ADD INDEX idx_tf_formorder (form_order);

                SELECT COUNT(*) INTO @totalForms
                FROM TempForms;

                SET @totalForms = IF(@totalForms = 0, 1, @totalForms);

                DROP TEMPORARY TABLE IF EXISTS LastUsedForms;

                CREATE TEMPORARY TABLE LastUsedForms AS
                SELECT RegistrationNumber, form_order AS last_form_order
                FROM (
                    SELECT
                        TCO.RegistrationNumber,
                        tf.form_order,
                        ROW_NUMBER() OVER (
                            PARTITION BY TCO.RegistrationNumber
                            ORDER BY spc.CreationDate DESC
                        ) AS rn
                    FROM TempCandidatesOrdered TCO
                    INNER JOIN schedulepaperscandidates spc
                        ON spc.RegistrationNumber = TCO.RegistrationNumber
                        AND spc.SchedulePaperId = TCO.SchedulePaperId
                    INNER JOIN Forms f
                        ON f.Id = spc.PaperFormId
                        AND f.IsActive = 1
                        AND f.IsDeleted = 0
                    INNER JOIN TempForms tf
                        ON tf.PaperFormId = spc.PaperFormId
                    WHERE TCO.RegistrationNumber IS NOT NULL
                        AND TCO.RegistrationNumber <> ''
                ) ranked
                WHERE rn = 1;

                ALTER TABLE LastUsedForms
                    ADD INDEX idx_luf_regnum (RegistrationNumber);

                DROP TEMPORARY TABLE IF EXISTS LastUsedFormsByCandidate;

                CREATE TEMPORARY TABLE LastUsedFormsByCandidate AS
                SELECT CandidateId, form_order AS last_form_order, ShouldRotate
                FROM (
                    SELECT
                        TCO.CandidateId,
                        tf.form_order,
                        CASE
                            WHEN spc.CandidateExamDate IS NOT NULL AND spc.CandidateExamDate > NOW()
                                THEN 0
                            ELSE 1
                        END AS ShouldRotate,
                        ROW_NUMBER() OVER (
                            PARTITION BY TCO.CandidateId
                            ORDER BY spc.CreationDate DESC
                        ) AS rn
                    FROM TempCandidatesOrdered TCO
                    INNER JOIN schedulepaperscandidates spc
                        ON spc.CandidateId = TCO.CandidateId
                        AND spc.SchedulePaperId = TCO.SchedulePaperId
                    INNER JOIN Forms f
                        ON f.Id = spc.PaperFormId
                        AND f.IsActive = 1
                        AND f.IsDeleted = 0
                    INNER JOIN TempForms tf
                        ON tf.PaperFormId = spc.PaperFormId
                ) ranked
                WHERE rn = 1;

                ALTER TABLE LastUsedFormsByCandidate
                    ADD INDEX idx_lufc_candidateid (CandidateId);

                INSERT INTO schedulepaperscandidates (
                    CandidateId,
                    SchedulePaperId,
                    VenueId,
                    CreationDate,
                    CreationUser,
                    OrganizationSignature,
                    IsActive,
                    IsDeleted,
                    OrganizationId,
                    BatchId,
                    RegistrationNumber,
                    PaperFormId,
                    CandidateExamDate
                )

                 SELECT * FROM (
                    SELECT
                    TCO.CandidateId,
                    TCO.SchedulePaperId,
                    TCO.VenueId,
                    NOW() as CreationDate,
                    CreationUser,
                    OrgSignature as OrganizationSignature,
                    1 as IsActive,
                    0 as IsDeleted,
                    TCO.OrganizationId,
                    TCO.BatchId,

                    CASE
                        WHEN TCO.RegistrationNumber REGEXP '^[0-9]+$'
                            THEN CAST(TCO.RegistrationNumber AS UNSIGNED)
                        ELSE 0
                    END as RegistrationNumber,

                    (
                        SELECT PaperFormId
                        FROM TempForms tf2
                        WHERE tf2.form_order = (
                            CASE
                                WHEN LUF.last_form_order IS NOT NULL
                                    THEN LUF.last_form_order
                                WHEN LUFC.last_form_order IS NOT NULL AND LUFC.ShouldRotate = 0
                                    THEN LUFC.last_form_order
                                WHEN LUFC.last_form_order IS NOT NULL
                                    THEN ((LUFC.last_form_order MOD @totalForms) + 1)
                                ELSE ((TCO.row_num - 1) MOD @totalForms) + 1
                            END
                        )
                        LIMIT 1
                    ) AS PaperFormId,

                    TCO.CandidateExamDate

                FROM TempCandidatesOrdered TCO

                LEFT JOIN LastUsedForms LUF
                    ON LUF.RegistrationNumber = TCO.RegistrationNumber

                LEFT JOIN LastUsedFormsByCandidate LUFC
                    ON LUFC.CandidateId = TCO.CandidateId

                ) AS new_schedule

                ON DUPLICATE KEY UPDATE
                    VenueId               = new_schedule.VenueId,
                    OrganizationSignature = new_schedule.OrganizationSignature,
                    IsActive              = 1,
                    IsDeleted             = 0,
                    BatchId               = new_schedule.BatchId,
                    RegistrationNumber    = new_schedule.RegistrationNumber,
                    PaperFormId           = new_schedule.PaperFormId,
                    CandidateExamDate     = new_schedule.CandidateExamDate;

                SET @affectedSchedulePapers = ROW_COUNT();

                SET status = 1;

                SET errorMessage = CONCAT(
                    'Process completed. Affected (inserted/updated): ',
                    @affectedCandidates,
                    ' candidate rows; ',
                    @affectedSchedulePapers,
                    ' schedule-paper-candidate rows.'
                );

                COMMIT;

                DROP TEMPORARY TABLE IF EXISTS TempCandidates;
                DROP TEMPORARY TABLE IF EXISTS TempCandidatesOrdered;
                DROP TEMPORARY TABLE IF EXISTS TempForms;
                DROP TEMPORARY TABLE IF EXISTS LastUsedForms;
                DROP TEMPORARY TABLE IF EXISTS LastUsedFormsByCandidate;

            END;
        ";
    }
}