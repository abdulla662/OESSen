using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Candidates
{
    public class SaveImportedCandidatesAndAssociatedLookUpsWithSchedulePaper : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `BulkInsertCandidatesAndAssociatedVenuesWithSchedulePaper`";

        public string CreateCommand => @"
            CREATE PROCEDURE `BulkInsertCandidatesAndAssociatedVenuesWithSchedulePaper`(
                    IN candidatesJson JSON,
                    IN candidateVenueLinksJson JSON,
                    IN SchedulePaperId INT,
                    IN OrgSignature VARCHAR(255),
                    IN CreationUser VARCHAR(255),
                    IN BatchId INT,
                    IN OrganizationId INT,
                    OUT status INT,
                    OUT errorMessage VARCHAR(255)
                )
                BEGIN

                    DECLARE EXIT HANDLER FOR SQLEXCEPTION
                    BEGIN
                        GET DIAGNOSTICS CONDITION 1 errorMessage = MESSAGE_TEXT;

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
                        Email VARCHAR(255),
                        PhotoURL VARCHAR(255),
                        SignatureURL VARCHAR(255),
                        Gender INT,
                        RegistrationCenterCode VARCHAR(255),
                        RegistrationDateTime DATETIME,
                        RegistrationNumber VARCHAR(255)
                    );

                    TRUNCATE TABLE TempIncomingCandidates;

                    ALTER TABLE TempIncomingCandidates
                        ADD INDEX idx_tic_email (Email),
                        ADD INDEX idx_tic_regnum (RegistrationNumber);

                    INSERT INTO TempIncomingCandidates (
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
                        RegistrationNumber
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
                        jt.registrationNumber

                    FROM JSON_TABLE(
                        candidatesJson,
                        '$[*]'
                        COLUMNS (
                            CandidateCode           VARCHAR(255) PATH '$.candidateCode',
                            Name                    VARCHAR(255) PATH '$.name',
                            UserName                VARCHAR(255) PATH '$.userName',
                            NationalId              VARCHAR(255) PATH '$.nationalId',
                            Password                VARCHAR(255) PATH '$.password',
                            Qualification           VARCHAR(255) PATH '$.qualification',
                            DateOfBirth             DATETIME     PATH '$.dateOfBirth',
                            Address                 VARCHAR(255) PATH '$.address',
                            Mobile                  VARCHAR(255) PATH '$.mobile',
                            Email                   VARCHAR(255) PATH '$.email',
                            PhotoURL                VARCHAR(255) PATH '$.photoURL',
                            SignatureURL            VARCHAR(255) PATH '$.signatureURL',
                            Gender                  INT          PATH '$.gender',
                            RegistrationCenterCode  VARCHAR(255) PATH '$.registrationCenterCode',
                            RegistrationDateTime    DATETIME     PATH '$.registrationDateTime',
                            registrationNumber      VARCHAR(255) PATH '$.registrationNumber'
                        )
                    ) AS jt;

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

                    FROM TempIncomingCandidates tc

                    ON DUPLICATE KEY UPDATE
                        CandidateCode           = tc.CandidateCode,
                        Name                    = tc.Name,
                        NationalId              = tc.NationalId,
                        Password                = tc.Password,
                        Qualification           = tc.Qualification,
                        DateOfBirth             = tc.DateOfBirth,
                        Address                 = tc.Address,
                        Mobile                  = tc.Mobile,
                        PhotoURL                = tc.PhotoURL,
                        SignatureURL            = tc.SignatureURL,
                        Gender                  = tc.Gender,
                        RegistrationCenterCode  = tc.RegistrationCenterCode,
                        RegistrationDateTime    = tc.RegistrationDateTime,
                        CreationDate            = NOW(),
                        CreationUser            = CreationUser,
                        OrganizationSignature   = OrgSignature,
                        IsActive                = 1,
                        IsDeleted               = 0;

                    CREATE TEMPORARY TABLE IF NOT EXISTS TempIncomingCandidateVenueLinks (
                        CandidateEmail VARCHAR(255),
                        VenueId INT,
                        RegistrationNumber VARCHAR(255),
                        CandidateExamDate DATETIME
                    );

                    TRUNCATE TABLE TempIncomingCandidateVenueLinks;

                    ALTER TABLE TempIncomingCandidateVenueLinks
                        ADD INDEX idx_ticvl_email (CandidateEmail),
                        ADD INDEX idx_ticvl_regnum (RegistrationNumber);

                    INSERT INTO TempIncomingCandidateVenueLinks (
                        CandidateEmail,
                        VenueId,
                        RegistrationNumber,
                        CandidateExamDate
                    )

                    SELECT
                        cvl.candidateEmail,
                        cvl.venueId,
                        cvl.registrationNumber,
                        cvl.candidateExamDate

                    FROM JSON_TABLE(
                        candidateVenueLinksJson,
                        '$[*]'
                        COLUMNS (
                            candidateEmail     VARCHAR(255) PATH '$.candidateEmail',
                            venueId            INT          PATH '$.venueId',
                            registrationNumber VARCHAR(255) PATH '$.registrationNumber',
                            candidateExamDate  DATETIME     PATH '$.candidateExamDate'
                        )
                    ) AS cvl;

                    SET @row_num := 0;

                    DROP TEMPORARY TABLE IF EXISTS TempCandidatesOrdered;

                    CREATE TEMPORARY TABLE TempCandidatesOrdered AS
                    SELECT
                        C.Id AS CandidateId,
                        SchedulePaperId,
                        TCVL.VenueId,
                        OrganizationId,
                        BatchId,

                        COALESCE(
                            TCVL.RegistrationNumber,
                            TC.RegistrationNumber,
                            ''
                        ) AS RegistrationNumber,

                        TCVL.CandidateExamDate,

                        (@row_num := @row_num + 1) AS row_num

                    FROM TempIncomingCandidateVenueLinks TCVL

                    INNER JOIN TempIncomingCandidates TC
                        ON (
                            TCVL.RegistrationNumber IS NOT NULL
                            AND TCVL.RegistrationNumber = TC.RegistrationNumber
                        )
                        OR (
                            TCVL.RegistrationNumber IS NULL
                            AND TCVL.CandidateEmail = TC.Email
                        )

                    INNER JOIN Candidates C
                        ON C.Email = TC.Email
                        AND C.OrganizationId = OrganizationId

                    ORDER BY C.CreationDate;

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
                        ORDER BY SPF.CreationDate;

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
                            NOW(),
                            CreationUser,
                            OrgSignature As OrganizationSignature,
                            1,
                            0,
                            TCO.OrganizationId,
                            TCO.BatchId,

                            CASE
                                WHEN TCO.RegistrationNumber REGEXP '^[0-9]+$'
                                    THEN CAST(TCO.RegistrationNumber AS UNSIGNED)
                                ELSE 0
                            END AS RegistrationNumber,

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

                    DROP TEMPORARY TABLE IF EXISTS TempIncomingCandidates;
                    DROP TEMPORARY TABLE IF EXISTS TempIncomingCandidateVenueLinks;
                    DROP TEMPORARY TABLE IF EXISTS TempCandidatesOrdered;
                    DROP TEMPORARY TABLE IF EXISTS TempForms;
                    DROP TEMPORARY TABLE IF EXISTS LastUsedForms;
                    DROP TEMPORARY TABLE IF EXISTS LastUsedFormsByCandidate;

                    COMMIT;

                    SET status = 1;

                    SET errorMessage = 'Process completed successfully';

                END
        ";
    }
}