using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Archiving
{
    public class ArchiveAllAndCleanup : IDatabaseStoredProcedure
    {
        public const string ArchiveDbName = "qiyas_archive_localoesdb";

        public const string ProcedureName = "sp_ArchiveAllAndCleanup";

        public string DropCommand => $"DROP PROCEDURE IF EXISTS `{ProcedureName}`";

        public string CreateCommand => $@"
            CREATE PROCEDURE `{ProcedureName}`()
            BEGIN
                -- 1. Declare variables to store the maximum ID for tables to be cleared
                DECLARE max_id_block INT DEFAULT 0;
                DECLARE max_id_ques  INT DEFAULT 0;
                DECLARE max_id_exam  INT DEFAULT 0;
                DECLARE max_id_track INT DEFAULT 0;
                DECLARE v_start_prev_month DATETIME;

                -- Error handling variable
                DECLARE sql_error INT DEFAULT 0;

                -- In case of any SQLEXCEPTION, set error flag and handle rollback later
                DECLARE CONTINUE HANDLER FOR SQLEXCEPTION SET sql_error = 1;

                -- Executable statements start here
                SET v_start_prev_month = DATE_FORMAT(DATE_SUB(CURDATE(), INTERVAL 1 MONTH), '%Y-%m-01 00:00:00');

                -- 2. Preparation (Disable checks for maximum speed and to avoid FK conflicts)
                SET FOREIGN_KEY_CHECKS = 0;
                SET SQL_SAFE_UPDATES   = 0;

                -- 3. Capture current Max IDs before any deletion to maintain sequence continuity
                SELECT COALESCE(MAX(Id), 0) INTO max_id_block FROM blockcandidateanswers;
                SELECT COALESCE(MAX(Id), 0) INTO max_id_ques  FROM candidatequestionsanswers;
                SELECT COALESCE(MAX(Id), 0) INTO max_id_exam  FROM candidateexamdetails;
                SELECT COALESCE(MAX(Id), 0) INTO max_id_track FROM candidatetrackinglogs;

                -- Start Transaction (Ensures data integrity across all migrations)
                START TRANSACTION;

                    -- 4. Accumulative Data Migration
                    -- Note: INSERT IGNORE ensures only new records are migrated to the Archive

                    -- Master / Configuration Tables
                    REPLACE INTO {ArchiveDbName}.languages                       SELECT * FROM languages;
                    REPLACE INTO {ArchiveDbName}.languagegroups                  SELECT * FROM languagegroups;
                    REPLACE INTO {ArchiveDbName}.subjects                        SELECT * FROM subjects;
                    REPLACE INTO {ArchiveDbName}.subjectgroups                   SELECT * FROM subjectgroups;
                    REPLACE INTO {ArchiveDbName}.difficultylevels                SELECT * FROM difficultylevels;
                    REPLACE INTO {ArchiveDbName}.difficultylevelgroups           SELECT * FROM difficultylevelgroups;
                    REPLACE INTO {ArchiveDbName}.deltatypes                      SELECT * FROM deltatypes;
                    REPLACE INTO {ArchiveDbName}.deltatypegroups                 SELECT * FROM deltatypegroups;
                    REPLACE INTO {ArchiveDbName}.difficultyprofiles              SELECT * FROM difficultyprofiles;
                    REPLACE INTO {ArchiveDbName}.difficultyprofilegroups         SELECT * FROM difficultyprofilegroups;
                    REPLACE INTO {ArchiveDbName}.questiontypes                   SELECT * FROM questiontypes;
                    REPLACE INTO {ArchiveDbName}.questioncategories              SELECT * FROM questioncategories;
                    REPLACE INTO {ArchiveDbName}.questioncategorygroups          SELECT * FROM questioncategorygroups;
                    REPLACE INTO {ArchiveDbName}.questiontemplates               SELECT * FROM questiontemplates;
                    REPLACE INTO {ArchiveDbName}.questioninstructiontemplates    SELECT * FROM questioninstructiontemplates;
                    REPLACE INTO {ArchiveDbName}.questionlayouts                 SELECT * FROM questionlayouts;
                    REPLACE INTO {ArchiveDbName}.templates                       SELECT * FROM templates;
                    REPLACE INTO {ArchiveDbName}.templatetypes                   SELECT * FROM templatetypes;
                    REPLACE INTO {ArchiveDbName}.templateattributes              SELECT * FROM templateattributes;
                    REPLACE INTO {ArchiveDbName}.attributes                      SELECT * FROM attributes;
                    REPLACE INTO {ArchiveDbName}.disabilities                    SELECT * FROM disabilities;

                    -- Organization / Roles / Security
                    REPLACE INTO {ArchiveDbName}.oesgroups                          SELECT * FROM oesgroups;
                    REPLACE INTO {ArchiveDbName}.oesroles                           SELECT * FROM oesroles;
                    REPLACE INTO {ArchiveDbName}.oesresources                       SELECT * FROM oesresources;
                    REPLACE INTO {ArchiveDbName}.oesgroupresources                  SELECT * FROM oesgroupresources;
                    REPLACE INTO {ArchiveDbName}.oesgroupresourceroles              SELECT * FROM oesgroupresourceroles;
                    REPLACE INTO {ArchiveDbName}.oesgroupsroles                     SELECT * FROM oesgroupsroles;
                    REPLACE INTO {ArchiveDbName}.pages                              SELECT * FROM pages;
                    REPLACE INTO {ArchiveDbName}.pageroles                          SELECT * FROM pageroles;
                    REPLACE INTO {ArchiveDbName}.apiendpoints                       SELECT * FROM apiendpoints;
                    REPLACE INTO {ArchiveDbName}.apiendpointroles                   SELECT * FROM apiendpointroles;
                    REPLACE INTO {ArchiveDbName}.auditlogs                          SELECT * FROM auditlogs;

                    -- Users
                    REPLACE INTO {ArchiveDbName}.appuserprofiles                SELECT * FROM appuserprofiles;
                    REPLACE INTO {ArchiveDbName}.appuserprofilegroups           SELECT * FROM appuserprofilegroups;
                    REPLACE INTO {ArchiveDbName}.appuserprofilesubjects         SELECT * FROM appuserprofilesubjects;

                    -- Question Bank / Item Bank
                    REPLACE INTO {ArchiveDbName}.itembanks                          SELECT * FROM itembanks;
                    REPLACE INTO {ArchiveDbName}.itembankgroups                     SELECT * FROM itembankgroups;
                    REPLACE INTO {ArchiveDbName}.itembanklevels                     SELECT * FROM itembanklevels;
                    REPLACE INTO {ArchiveDbName}.itembankpoints                     SELECT * FROM itembankpoints;
                    REPLACE INTO {ArchiveDbName}.questionsmetadata                  SELECT * FROM questionsmetadata;
                    REPLACE INTO {ArchiveDbName}.questionsdetails                   SELECT * FROM questionsdetails;
                    REPLACE INTO {ArchiveDbName}.questiondetailsversions            SELECT * FROM questiondetailsversions;
                    REPLACE INTO {ArchiveDbName}.questionschoices                   SELECT * FROM questionschoices;
                    REPLACE INTO {ArchiveDbName}.questionschoicesversions           SELECT * FROM questionschoicesversions;
                    REPLACE INTO {ArchiveDbName}.questiongroups                     SELECT * FROM questiongroups;
                    REPLACE INTO {ArchiveDbName}.questionreviews                    SELECT * FROM questionreviews;
                    REPLACE INTO {ArchiveDbName}.questioncommitteeassignments       SELECT * FROM questioncommitteeassignments;
                    REPLACE INTO {ArchiveDbName}.questiondoclibfiles                SELECT * FROM questiondoclibfiles;
                    REPLACE INTO {ArchiveDbName}.matchingpairquestionitems          SELECT * FROM matchingpairquestionitems;
                    REPLACE INTO {ArchiveDbName}.matchingpairquestionitemsversions  SELECT * FROM matchingpairquestionitemsversions;
                    REPLACE INTO {ArchiveDbName}.segmentquestionproperties          SELECT * FROM segmentquestionproperties;
                    REPLACE INTO {ArchiveDbName}.segmentquestionpropertiesversions  SELECT * FROM segmentquestionpropertiesversions;

                    -- Blocks / Forms / Papers
                    REPLACE INTO {ArchiveDbName}.blocks                             SELECT * FROM blocks;
                    REPLACE INTO {ArchiveDbName}.blockquestions                     SELECT * FROM blockquestions;
                    REPLACE INTO {ArchiveDbName}.blockgroups                        SELECT * FROM blockgroups;
                    REPLACE INTO {ArchiveDbName}.forms                              SELECT * FROM forms;
                    REPLACE INTO {ArchiveDbName}.formquestions                      SELECT * FROM formquestions;
                    REPLACE INTO {ArchiveDbName}.formvenuesuspensions               SELECT * FROM formvenuesuspensions;
                    REPLACE INTO {ArchiveDbName}.papermetadata                      SELECT * FROM papermetadata;
                    REPLACE INTO {ArchiveDbName}.paperformblocks                    SELECT * FROM paperformblocks;
                    REPLACE INTO {ArchiveDbName}.papergroups                        SELECT * FROM papergroups;
                    REPLACE INTO {ArchiveDbName}.papersettings                      SELECT * FROM papersettings;
                    REPLACE INTO {ArchiveDbName}.papersettingtemplates              SELECT * FROM papersettingtemplates;
                    REPLACE INTO {ArchiveDbName}.papermetadatatemplates             SELECT * FROM papermetadatatemplates;
                    REPLACE INTO {ArchiveDbName}.paperitembankequations             SELECT * FROM paperitembankequations;
                    REPLACE INTO {ArchiveDbName}.papersubjects                      SELECT * FROM papersubjects;
                    REPLACE INTO {ArchiveDbName}.papervenuesuspensions              SELECT * FROM papervenuesuspensions;

                    -- Sections / Stages / Adaptive
                    REPLACE INTO {ArchiveDbName}.sections                               SELECT * FROM sections;
                    REPLACE INTO {ArchiveDbName}.stages                                 SELECT * FROM stages;
                    REPLACE INTO {ArchiveDbName}.adaptivesection                        SELECT * FROM adaptivesection;
                    REPLACE INTO {ArchiveDbName}.transitionlevels                       SELECT * FROM transitionlevels;
                    REPLACE INTO {ArchiveDbName}.transitionprofiles                     SELECT * FROM transitionprofiles;
                    REPLACE INTO {ArchiveDbName}.transitionprofilegroups                SELECT * FROM transitionprofilegroups;
                    REPLACE INTO {ArchiveDbName}.paperstagecategorydecisionpaths        SELECT * FROM paperstagecategorydecisionpaths;

                    -- Scheduling
                    REPLACE INTO {ArchiveDbName}.schedulemetadata                           SELECT * FROM schedulemetadata;
                    REPLACE INTO {ArchiveDbName}.schedulepapers                             SELECT * FROM schedulepapers;
                    REPLACE INTO {ArchiveDbName}.schedulepaperforms                         SELECT * FROM schedulepaperforms;
                    REPLACE INTO {ArchiveDbName}.schedulepaperscandidates                   SELECT * FROM schedulepaperscandidates;
                    REPLACE INTO {ArchiveDbName}.schedulegroups                             SELECT * FROM schedulegroups;
                    REPLACE INTO {ArchiveDbName}.schedulelanguages                          SELECT * FROM schedulelanguages;
                    REPLACE INTO {ArchiveDbName}.schedulevenues                             SELECT * FROM schedulevenues;
                    REPLACE INTO {ArchiveDbName}.schedulesecurityconfigurations             SELECT * FROM schedulesecurityconfigurations;
                    REPLACE INTO {ArchiveDbName}.schedulesecurityconfigurationtemplates     SELECT * FROM schedulesecurityconfigurationtemplates;
                    REPLACE INTO {ArchiveDbName}.scheduletemplates                          SELECT * FROM scheduletemplates;

                    -- Candidates
                    REPLACE INTO {ArchiveDbName}.candidates                                 SELECT * FROM candidates;
                    REPLACE INTO {ArchiveDbName}.candidategroups                            SELECT * FROM candidategroups;
                    REPLACE INTO {ArchiveDbName}.candidatesorganizationnodelookupitems      SELECT * FROM candidatesorganizationnodelookupitems;
                    REPLACE INTO {ArchiveDbName}.candidatebatchimporthistory                SELECT * FROM candidatebatchimporthistory;

                    -- Exam Transactions (Heavy Tables)
                    REPLACE INTO {ArchiveDbName}.candidateexamdetails          SELECT * FROM candidateexamdetails;
                    REPLACE INTO {ArchiveDbName}.candidatequestionsanswers     SELECT * FROM candidatequestionsanswers;
                    REPLACE INTO {ArchiveDbName}.blockcandidateanswers         SELECT * FROM blockcandidateanswers;

                    -- Sync / Logs
                    REPLACE INTO {ArchiveDbName}.candidatetrackinglogs         SELECT * FROM candidatetrackinglogs;
                    REPLACE INTO {ArchiveDbName}.realtimesyncjobs              SELECT * FROM realtimesyncjobs;
                    REPLACE INTO {ArchiveDbName}.ctrexamsyncjobs               SELECT * FROM ctrexamsyncjobs;
                    REPLACE INTO {ArchiveDbName}.ctrresultsfilenames           SELECT * FROM ctrresultsfilenames;
                    REPLACE INTO {ArchiveDbName}.cbtcandidatessyncjobs         SELECT * FROM cbtcandidatessyncjobs;
                    REPLACE INTO {ArchiveDbName}.cbtcandidatesreceiveddata     SELECT * FROM cbtcandidatesreceiveddata;
                    REPLACE INTO {ArchiveDbName}.cbtsyncsettings               SELECT * FROM cbtsyncsettings;
                    REPLACE INTO {ArchiveDbName}.evaluationsyncjobs            SELECT * FROM evaluationsyncjobs;

                    -- Misc
                    REPLACE INTO {ArchiveDbName}.venues                                     SELECT * FROM venues;
                    REPLACE INTO {ArchiveDbName}.venuegroups                                SELECT * FROM venuegroups;
                    REPLACE INTO {ArchiveDbName}.mediasettings                              SELECT * FROM mediasettings;
                    REPLACE INTO {ArchiveDbName}.mediasettingsgroup                         SELECT * FROM mediasettingsgroup;
                    REPLACE INTO {ArchiveDbName}.notifications                              SELECT * FROM notifications;
                    REPLACE INTO {ArchiveDbName}.notificationappuserprofile                 SELECT * FROM notificationappuserprofile;
                    REPLACE INTO {ArchiveDbName}.filedetails                                SELECT * FROM filedetails;
                    REPLACE INTO {ArchiveDbName}.fileuploadresponsesettings                 SELECT * FROM fileuploadresponsesettings;
                    REPLACE INTO {ArchiveDbName}.scoringschemes                             SELECT * FROM scoringschemes;
                    REPLACE INTO {ArchiveDbName}.ilos                                       SELECT * FROM ilos;
                    REPLACE INTO {ArchiveDbName}.ilogroups                                  SELECT * FROM ilogroups;
                    REPLACE INTO {ArchiveDbName}.uploadquestiontemplates                    SELECT * FROM uploadquestiontemplates;
                    REPLACE INTO {ArchiveDbName}.autopaperitembankquestionsections          SELECT * FROM autopaperitembankquestionsections;
                    REPLACE INTO {ArchiveDbName}.manualpaperitembankquestionsections        SELECT * FROM manualpaperitembankquestionsections;
                    REPLACE INTO {ArchiveDbName}.equationcategories                         SELECT * FROM equationcategories;
                    REPLACE INTO {ArchiveDbName}.equationgroups                             SELECT * FROM equationgroups;
                    REPLACE INTO {ArchiveDbName}.equationtemplates                          SELECT * FROM equationtemplates;
                    REPLACE INTO {ArchiveDbName}.organizationnodelookupitems                SELECT * FROM organizationnodelookupitems;
                    REPLACE INTO {ArchiveDbName}.organizationsstructures                    SELECT * FROM organizationsstructures;
                    REPLACE INTO {ArchiveDbName}.qualitycheckcommittees                     SELECT * FROM qualitycheckcommittees;
                    REPLACE INTO {ArchiveDbName}.qualitycheckcommitteegroups                SELECT * FROM qualitycheckcommitteegroups;
                    REPLACE INTO {ArchiveDbName}.qualitycheckcommitteemembers               SELECT * FROM qualitycheckcommitteemembers;
                    REPLACE INTO {ArchiveDbName}.qualitycheckcommitteeitembanks             SELECT * FROM qualitycheckcommitteeitembanks;
                    REPLACE INTO {ArchiveDbName}.qccomments                                 SELECT * FROM qccomments;

                    -- 5. Cleanup Section (Clearing transactional tables)
                    -- DELETE t FROM blockcandidateanswers t JOIN {ArchiveDbName}.blockcandidateanswers a ON t.Id = a.Id WHERE t.CreationDate < v_start_prev_month;
                    -- DELETE t FROM candidatequestionsanswers t JOIN {ArchiveDbName}.candidatequestionsanswers a ON t.Id = a.Id WHERE t.CreationDate < v_start_prev_month;
                    -- DELETE t FROM candidateexamdetails t  JOIN {ArchiveDbName}.candidateexamdetails a ON t.Id = a.Id WHERE t.CreationDate < v_start_prev_month;
                    -- DELETE t FROM candidatetrackinglogs t JOIN {ArchiveDbName}.candidatetrackinglogs a ON t.Id = a.Id WHERE t.CreationDate < v_start_prev_month;

                -- 6. Reset Auto Increment to maintain continuous sequencing
                IF max_id_block > 0 THEN
                    SET @sql1 = CONCAT('ALTER TABLE blockcandidateanswers AUTO_INCREMENT = ', max_id_block + 1);
                    PREPARE stmt1 FROM @sql1;
                    EXECUTE stmt1;
                    DEALLOCATE PREPARE stmt1;
                END IF;

                IF max_id_ques > 0 THEN
                    SET @sql2 = CONCAT('ALTER TABLE candidatequestionsanswers AUTO_INCREMENT = ', max_id_ques + 1);
                    PREPARE stmt2 FROM @sql2;
                    EXECUTE stmt2;
                    DEALLOCATE PREPARE stmt2;
                END IF;

                IF max_id_exam > 0 THEN
                    SET @sql3 = CONCAT('ALTER TABLE candidateexamdetails AUTO_INCREMENT = ', max_id_exam + 1);
                    PREPARE stmt3 FROM @sql3;
                    EXECUTE stmt3;
                    DEALLOCATE PREPARE stmt3;
                END IF;

                IF max_id_track > 0 THEN
                    SET @sql4 = CONCAT('ALTER TABLE candidatetrackinglogs AUTO_INCREMENT = ', max_id_track + 1);
                    PREPARE stmt4 FROM @sql4;
                    EXECUTE stmt4;
                    DEALLOCATE PREPARE stmt4;
                END IF;

                -- Transaction Verification Check
                IF sql_error = 0 THEN
                    COMMIT;
                ELSE
                    ROLLBACK;
                    -- Ensure system constraints are restored even on failure before exiting
                    SET FOREIGN_KEY_CHECKS = 1;
                    SET SQL_SAFE_UPDATES   = 1;
                    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Migration Failed! Transaction rolled back to prevent data loss.';
                END IF;

                -- 7. Restore Foreign Key and Safe Update Checks (On Success)
                SET FOREIGN_KEY_CHECKS = 1;
                SET SQL_SAFE_UPDATES   = 1;

            END
        ";
    }
}