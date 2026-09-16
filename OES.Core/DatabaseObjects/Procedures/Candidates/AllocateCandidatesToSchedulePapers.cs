namespace OES.Core.DatabaseObjects.Procedures.Candidates
{
    public class AllocateCandidatesToSchedulePapers
    //: IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `AllocateCandidatesToSchedulePapers`;";

        public string CreateCommand => @"
            CREATE PROCEDURE `AllocateCandidatesToSchedulePapers`(
                IN LookUpIds JSON,  -- Changed from LookUpId to LookUpIds (JSON array)
                IN OrgSignature VARCHAR(255),
                IN CreationUser VARCHAR(255),
                IN OrganizationId INT,
                IN VenueId INT,
                IN SchedulePaperId INT,
                OUT status INT,
                OUT Message VARCHAR(255)
            )
            BEGIN
                DECLARE processedCandidates INT DEFAULT 0;
                DECLARE debugInfo TEXT DEFAULT '';
                DECLARE lookupCount INT DEFAULT 0;

                DECLARE EXIT HANDLER FOR SQLEXCEPTION
                BEGIN
                    GET DIAGNOSTICS CONDITION 1
                        Message = MESSAGE_TEXT;
                    ROLLBACK;
                    SET status = 0;
                    SET Message = CONCAT('Error: ', Message);
                END;

                SET status = 1;
                SET Message = '';

                SET lookupCount = JSON_LENGTH(LookUpIds);

                START TRANSACTION;

                DELETE spc
                FROM schedulepaperscandidates spc
                WHERE spc.SchedulePaperId = SchedulePaperId
                    AND spc.OrganizationId = OrganizationId
                    AND spc.CandidateId IN (
                        SELECT cos.CandidateId
                        FROM candidatesorganizationnodelookupitems cos
                        WHERE JSON_CONTAINS(LookUpIds, CAST(cos.OrganizationNodeLookupItemId AS JSON), '$')
                            AND cos.OrganizationId = OrganizationId
                            AND cos.IsDeleted = 0
                            AND cos.IsActive = 1
                        GROUP BY cos.CandidateId
                        HAVING COUNT(DISTINCT cos.OrganizationNodeLookupItemId) = lookupCount
                    );

                INSERT INTO schedulepaperscandidates (
                    CandidateId,
                    SchedulePaperId,
                    VenueId,
                    CreationDate,
                    CreationUser,
                    OrganizationSignature,
                    IsActive,
                    IsDeleted,
                    OrganizationId
                )
                SELECT 
                    cos.CandidateId,
                    SchedulePaperId,
                    VenueId,
                    NOW(),
                    CreationUser,
                    OrgSignature,
                    1,
                    0,
                    OrganizationId
                FROM candidatesorganizationnodelookupitems cos
                WHERE JSON_CONTAINS(LookUpIds, CAST(cos.OrganizationNodeLookupItemId AS JSON), '$')
                    AND cos.OrganizationId = OrganizationId
                    AND cos.IsDeleted = 0
                    AND cos.IsActive = 1
                GROUP BY cos.CandidateId
                HAVING COUNT(DISTINCT cos.OrganizationNodeLookupItemId) = lookupCount;

                SET processedCandidates = ROW_COUNT();

                IF processedCandidates > 0 THEN
                    SET Message = CONCAT('Allocated ', processedCandidates, ' candidates successfully');
                    SET status = 1;
                ELSE
                    SELECT COUNT(DISTINCT cos.OrganizationNodeLookupItemId) INTO @lookups_exist
                    FROM candidatesorganizationnodelookupitems cos
                    WHERE JSON_CONTAINS(LookUpIds, CAST(cos.OrganizationNodeLookupItemId AS JSON), '$')
                        AND cos.OrganizationId = OrganizationId;

                    IF @lookups_exist > 0 THEN
                        -- Check if there are any candidates with at least one of the lookup IDs
                        SELECT COUNT(DISTINCT cos.CandidateId) INTO @candidates_exist
                        FROM candidatesorganizationnodelookupitems cos
                        WHERE JSON_CONTAINS(LookUpIds, CAST(cos.OrganizationNodeLookupItemId AS JSON), '$')
                            AND cos.OrganizationId = OrganizationId
                            AND cos.IsDeleted = 0
                            AND cos.IsActive = 1;

                        IF @candidates_exist > 0 THEN
                            SET Message = 'No candidates with ALL specified lookup IDs found';
                            SET status = 2; -- Lookup items exist and candidates exist, but none with all required lookups
                        ELSE
                            SET Message = 'No candidates with any of the specified lookup IDs found';
                            SET status = 3; -- Lookup items exist but no candidates with any of them
                        END IF;
                    ELSE
                        SET Message = 'None of the specified lookup IDs exist';
                        SET status = 4; -- No matching lookup items at all
                    END IF;
                END IF;

                COMMIT;
            END;
        ";
    }
}
