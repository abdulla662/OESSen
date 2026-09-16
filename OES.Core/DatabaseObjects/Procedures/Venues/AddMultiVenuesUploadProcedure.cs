using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Venues
{
    public class AddMultiVenuesUploadProcedure : IDatabaseStoredProcedure
    {
        public string DropCommand => "DROP PROCEDURE IF EXISTS `AddMultipleVenues`";

        public string CreateCommand => @"
            CREATE PROCEDURE `AddMultipleVenues`(
                IN VenueData JSON,
                IN OrgSignature VARCHAR(255),
                IN OrganizationId INT,
                OUT status INT,
                OUT errorMessage TEXT
            )

            proc_exit: BEGIN
            -- Declare variables
            DECLARE i INT DEFAULT 0;
            DECLARE venueCount INT DEFAULT 0;
            DECLARE successCount INT DEFAULT 0;
            DECLARE errorCount INT DEFAULT 0;
            DECLARE currentRowNumber INT DEFAULT 1;
                    
            -- Declare variables for storing JSON values
            DECLARE VenueName VARCHAR(255);
            DECLARE VenueCode VARCHAR(255);
            DECLARE VenueAddress VARCHAR(500);
            DECLARE VenueLocation VARCHAR(255);
            DECLARE VenuePinCode VARCHAR(20);
            DECLARE VenueEmail VARCHAR(255);
            DECLARE VenueMobile VARCHAR(50);
            DECLARE CoordinatorPassword VARCHAR(255);
            DECLARE CoordinatorFullName VARCHAR(255);
            DECLARE CoordinatorEmail VARCHAR(255);
            DECLARE CoordinatorMobile VARCHAR(50);
            DECLARE Url VARCHAR(50);
            DECLARE IPAddress VARCHAR(50);
                    
            -- Variables for duplicate checking
            DECLARE duplicateNameExists INT DEFAULT 0;
            DECLARE duplicateCodeExists INT DEFAULT 0;
            DECLARE canInsert BOOLEAN DEFAULT TRUE;
            DECLARE currentError VARCHAR(500);
                    
            -- Variables for building error message
            DECLARE errorDetails TEXT DEFAULT '';
            DECLARE finalMessage TEXT DEFAULT '';

            -- Initialize output parameters
            SET status = 1;
            SET errorMessage = '';
    
            IF VenueData IS NULL OR JSON_LENGTH(VenueData) = 0 THEN
                SET status = 0;
                SET errorMessage = 'There is no data. Please upload a valid file';
                LEAVE proc_exit;
            END IF;
    
            SET venueCount = JSON_LENGTH(VenueData);

            -- Create temporary table to store validation results
            CREATE TEMPORARY TABLE temp_venue_validation (
                `row_number` INT,
                `venue_name` VARCHAR(255),
                `venue_code` VARCHAR(255),
                `venue_address` VARCHAR(500),
                `venue_location` VARCHAR(255),
                `venue_pincode` VARCHAR(20),
                `venue_email` VARCHAR(255),
                `venue_mobile` VARCHAR(50),
                `coordinator_password` VARCHAR(255),
                `coordinator_fullname` VARCHAR(255),
                `coordinator_email` VARCHAR(255),
                `coordinator_mobile` VARCHAR(50),
                `venue_IPAddress` VARCHAR(50),
                `venue_Url` VARCHAR(50),
                `has_error` BOOLEAN DEFAULT FALSE,
                `error_message` VARCHAR(500)
            );
    
            -- Process each venue and validate
            WHILE i < venueCount DO
                SET currentRowNumber = i + 1;
                SET canInsert = TRUE;
                SET currentError = '';
                        
                -- Extract venue data
                SET VenueName = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].Name')));
                SET VenueCode = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].Code')));
                SET VenueAddress = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].Address')));
                SET VenueLocation = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].GeoLocation')));
                SET VenuePinCode = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].PinCode')));
                SET VenueEmail = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].VenueEmail')));
                SET VenueMobile = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].Mobile')));
                SET CoordinatorPassword = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].CoordinatorVenuePassword')));
                SET CoordinatorFullName = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].CoordinatorFullName')));
                SET CoordinatorEmail = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].CoordinatorEmail')));
                SET CoordinatorMobile = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].CoordinatorMobile')));
                SET IPAddress = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].IPAddress')));
                SET Url = JSON_UNQUOTE(JSON_EXTRACT(VenueData, CONCAT('$[', i, '].Url')));
        
                -- Handle nullable fields
                SET VenueLocation = CASE 
                    WHEN VenueLocation IS NULL OR VenueLocation = '' OR VenueLocation = 'null' 
                    THEN NULL 
                    ELSE VenueLocation 
                END;
        
                SET VenueEmail = CASE 
                    WHEN VenueEmail IS NULL OR VenueEmail = '' OR VenueEmail = 'null' 
                    THEN NULL 
                    ELSE VenueEmail 
                END;
        
                SET VenueMobile = CASE 
                    WHEN VenueMobile IS NULL OR VenueMobile = '' OR VenueMobile = 'null' 
                    THEN NULL 
                    ELSE VenueMobile 
                END;
        
                SET CoordinatorEmail = CASE 
                    WHEN CoordinatorEmail IS NULL OR CoordinatorEmail = '' OR CoordinatorEmail = 'null' 
                    THEN NULL 
                    ELSE CoordinatorEmail 
                END;
        
                SET CoordinatorMobile = CASE 
                    WHEN CoordinatorMobile IS NULL OR CoordinatorMobile = '' OR CoordinatorMobile = 'null' 
                    THEN NULL 
                    ELSE CoordinatorMobile 
                END;

                -- Check for duplicate name in existing venues
                SELECT COUNT(*) INTO duplicateNameExists
                FROM venues 
                WHERE Name = VenueName 
                    AND OrganizationId = OrganizationId 
                    AND IsDeleted = 0;
                        
                -- Check for duplicate code in existing venues
                SELECT COUNT(*) INTO duplicateCodeExists
                FROM venues 
                WHERE Code = VenueCode 
                    AND OrganizationId = OrganizationId 
                    AND IsDeleted = 0;
                        
                -- Set error message
                IF duplicateNameExists > 0 AND duplicateCodeExists > 0 THEN
                    SET canInsert = FALSE;
                    SET currentError = CONCAT('Venue name ', CHAR(34), VenueName, CHAR(34), ' and code ', CHAR(34), VenueCode, CHAR(34), ' already exist in database');
                ELSEIF duplicateNameExists > 0 THEN
                    SET canInsert = FALSE;
                    SET currentError = CONCAT('Venue name ', CHAR(34), VenueName, CHAR(34), ' already exists in database');
                ELSEIF duplicateCodeExists > 0 THEN
                    SET canInsert = FALSE;
                    SET currentError = CONCAT('Venue code ', CHAR(34), VenueCode, CHAR(34), ' already exists in database');
                END IF;
        
                -- Insert into temporary validation table
                INSERT INTO temp_venue_validation VALUES (
                    currentRowNumber, VenueName, VenueCode, VenueAddress, VenueLocation, VenuePinCode,
                    VenueEmail, VenueMobile, CoordinatorPassword, CoordinatorFullName,
                    CoordinatorEmail, CoordinatorMobile, IPAddress, Url, NOT canInsert, currentError
                );
        
                SET i = i + 1;
            END WHILE;

            -- Count successful and error records
            SELECT COUNT(*) INTO successCount FROM temp_venue_validation WHERE `has_error` = FALSE;
            SELECT COUNT(*) INTO errorCount FROM temp_venue_validation WHERE `has_error` = TRUE;

            -- Insert valid venues
            IF successCount > 0 THEN
                INSERT INTO venues (
                    Name, Code, Address, GeoLocation, PinCode, VenueEmail, Mobile,
                    CoordinatorVenuePassword, CoordinatorFullName, CoordinatorEmail, CoordinatorMobile,IPAddress,Url,
                    CreationDate, CreationUser, OrganizationSignature, IsActive, IsDeleted, OrganizationId
                )
                SELECT 
                    `venue_name`, `venue_code`, `venue_address`, `venue_location`, `venue_pincode`,
                    `venue_email`, `venue_mobile`, `coordinator_password`, `coordinator_fullname`,
                    `coordinator_email`, `coordinator_mobile`, `venue_IPAddress`, `venue_Url`,
                    NOW(), 'System', OrgSignature, 1, 0, OrganizationId
                FROM temp_venue_validation
                WHERE `has_error` = FALSE;
            END IF;

            -- Build error details for failed records
            IF errorCount > 0 THEN
                SELECT GROUP_CONCAT(
                    CONCAT('Row ', `row_number`, ': ', `error_message`) 
                    ORDER BY `row_number` 
                    SEPARATOR '; '
                ) INTO errorDetails
                FROM temp_venue_validation 
                WHERE `has_error` = TRUE;
            END IF;

            -- Build final message
            IF successCount > 0 AND errorCount > 0 THEN
                SET finalMessage = CONCAT(
                    successCount, ' venue(s) added successfully. ',
                    errorCount, ' venue(s) had errors: ', errorDetails
                );
                SET status = 2;
            ELSEIF successCount > 0 AND errorCount = 0 THEN
                SET finalMessage = CONCAT('Successfully added all ', successCount, ' venue(s) to the database.');
                SET status = 1;
            ELSEIF successCount = 0 AND errorCount > 0 THEN
                SET finalMessage = CONCAT('No venues were added. All ', errorCount, ' venue(s) had errors: ', errorDetails);
                SET status = 0;
            ELSE
                SET finalMessage = 'No venues were processed.';
                SET status = 0;
            END IF;

            SET errorMessage = finalMessage;

            DROP TEMPORARY TABLE temp_venue_validation;

            END
        ";
    }
}