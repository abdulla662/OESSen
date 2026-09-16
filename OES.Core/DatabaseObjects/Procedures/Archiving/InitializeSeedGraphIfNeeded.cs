using OES.Core.DatabaseObjects.CommonInterfaces;

namespace OES.Core.DatabaseObjects.Procedures.Archiving
{
    public class InitializeSeedGraphIfNeeded : IDatabaseStoredProcedure
    {
        public const string ArchiveDbName = "qiyas_archive_localoesdb";

        public const string ProcedureName = "sp_InitializeSeedGraphIfNeeded";

        public string DropCommand => $"DROP PROCEDURE IF EXISTS `{ProcedureName}`";

        public string CreateCommand => $@"CREATE PROCEDURE sp_InitializeSeedGraphIfNeeded()
			BEGIN

				SET SQL_SAFE_UPDATES = 0;				
				SET FOREIGN_KEY_CHECKS = 0;

				-- Update child tables to reference the IDs used in the main database.
			    UPDATE {ArchiveDbName}.oesgroupresources ogr
				JOIN {ArchiveDbName}.oesgroups ag ON ogr.GroupId = ag.Id
				JOIN oesgroups mg ON ag.Name = mg.Name
				SET ogr.GroupId = mg.Id
				WHERE ag.Id <> mg.Id;

				UPDATE {ArchiveDbName}.oesgroupresources ogr
				JOIN {ArchiveDbName}.oesresources ar ON ogr.ResourceId = ar.Id
				JOIN oesresources mr ON ar.Name = mr.Name
				SET ogr.ResourceId = mr.Id
				WHERE ar.Id <> mr.Id;

				UPDATE {ArchiveDbName}.oesgroupresourceroles ogrr
				JOIN {ArchiveDbName}.oesroles ar ON ogrr.RoleId = ar.Id
				JOIN oesroles mr ON ar.Name = mr.Name
				SET ogrr.RoleId = mr.Id
				WHERE ar.Id <> mr.Id;

				UPDATE {ArchiveDbName}.oesgroupresourceroles ogrr
				JOIN {ArchiveDbName}.oesgroupresources agr ON ogrr.GroupResourceId = agr.Id
				JOIN oesgroupresources mgr ON mgr.GroupId = agr.GroupId 
				AND mgr.ResourceId = agr.ResourceId 
				AND mgr.ResourceType = agr.ResourceType
				SET ogrr.GroupResourceId = mgr.Id
				WHERE agr.Id <> mgr.Id;

				UPDATE {ArchiveDbName}.oesgroupsroles ogr
				JOIN {ArchiveDbName}.oesgroups ag ON ogr.OESGroupId = ag.Id
				JOIN oesgroups mg ON ag.Name = mg.Name
				SET ogr.OESGroupId = mg.Id
				WHERE ag.Id <> mg.Id;

				UPDATE {ArchiveDbName}.oesgroupsroles ogr
				JOIN {ArchiveDbName}.oesroles ar ON ogr.OESRoleId = ar.Id
				JOIN oesroles mr ON ar.Name = mr.Name
				SET ogr.OESRoleId = mr.Id
				WHERE ar.Id <> mr.Id;

				UPDATE {ArchiveDbName}.apiendpointroles aer
				JOIN {ArchiveDbName}.apiendpoints aa ON aer.ApiId = aa.Id
				JOIN apiendpoints ma ON aa.Name = ma.Name
				SET aer.ApiId = ma.Id
				WHERE aa.Id <> ma.Id;

				UPDATE {ArchiveDbName}.apiendpointroles aer
				JOIN {ArchiveDbName}.oesroles ar ON aer.RoleId = ar.Id
				JOIN oesroles mr ON ar.Name = mr.Name
				SET aer.RoleId = mr.Id
				WHERE ar.Id <> mr.Id;

				UPDATE {ArchiveDbName}.pageroles pr
				JOIN {ArchiveDbName}.pages ap ON pr.PageId = ap.Id
				JOIN pages mp ON ap.Name = mp.Name
				SET pr.PageId = mp.Id
				WHERE ap.Id <> mp.Id;

				UPDATE {ArchiveDbName}.pageroles pr
				JOIN {ArchiveDbName}.oesroles ar ON pr.RoleId = ar.Id
				JOIN oesroles mr ON ar.Name = mr.Name
				SET pr.RoleId = mr.Id
				WHERE ar.Id <> mr.Id;

				UPDATE {ArchiveDbName}.questionlayouts ql
				JOIN {ArchiveDbName}.questiontypes aqt ON ql.QuestionTypeId = aqt.Id
				JOIN questiontypes mqt ON aqt.Name = mqt.Name
				SET ql.QuestionTypeId = mqt.Id
				WHERE aqt.Id <> mqt.Id;

				UPDATE {ArchiveDbName}.templateattributes ta
				JOIN {ArchiveDbName}.templatetypes att ON ta.TemplateTypeId = att.Id
				JOIN templatetypes mtt ON att.Type = mtt.Type
				SET ta.TemplateTypeId = mtt.Id
				WHERE att.Id <> mtt.Id;

				UPDATE {ArchiveDbName}.templateattributes ta
				JOIN {ArchiveDbName}.attributes aa ON ta.AttributeId = aa.Id
				JOIN attributes ma ON aa.Name = ma.Name
				SET ta.AttributeId = ma.Id
				WHERE aa.Id <> ma.Id;

				UPDATE {ArchiveDbName}.appuserprofilegroups aug
				JOIN {ArchiveDbName}.oesgroups ag ON aug.OESGroupId = ag.Id
				JOIN oesgroups mg ON ag.Name = mg.Name
				SET aug.OESGroupId = mg.Id
				WHERE ag.Id <> mg.Id;

				UPDATE {ArchiveDbName}.appuserprofilegroups aug
				JOIN {ArchiveDbName}.appuserprofiles aup ON aug.AppUserProfileId = aup.Id
				JOIN appuserprofiles mup ON aup.EmailAddress = mup.EmailAddress
				SET aug.AppUserProfileId = mup.Id
				WHERE aup.Id <> mup.Id;

				UPDATE {ArchiveDbName}.appuserprofilesubjects aps
				JOIN {ArchiveDbName}.subjects asu ON aps.SubjectId = asu.Id
				JOIN subjects msu ON asu.Name = msu.Name
				SET aps.SubjectId = msu.Id
				WHERE asu.Id <> msu.Id;

				UPDATE {ArchiveDbName}.appuserprofilesubjects aps
				JOIN {ArchiveDbName}.appuserprofiles aup  ON aps.AppUserProfilesId = aup.Id
				JOIN appuserprofiles mup  ON aup.EmailAddress = mup.EmailAddress
				SET aps.AppUserProfilesId = mup.Id
				WHERE aup.Id <> mup.Id;

				-- Remove archive rows whose IDs differ from their corresponding records in the main database
				DELETE a FROM {ArchiveDbName}.oesgroupresourceroles a LEFT JOIN oesgroupresourceroles m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.oesgroupsroles a LEFT JOIN oesgroupsroles m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.oesgroupresources a LEFT JOIN oesgroupresources m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.apiendpointroles a LEFT JOIN apiendpointroles m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.pageroles a LEFT JOIN pageroles m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questionlayouts a LEFT JOIN questionlayouts m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.templateattributes a LEFT JOIN templateattributes m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.appuserprofilegroups a LEFT JOIN appuserprofilegroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.appuserprofilesubjects a LEFT JOIN appuserprofilesubjects m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.pages a JOIN pages m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.apiendpoints a JOIN apiendpoints m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.questiontypes a JOIN questiontypes m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.itembanklevels a JOIN itembanklevels m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.deltatypes a JOIN deltatypes m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.languages a JOIN languages m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.subjects a JOIN subjects m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.mediasettings a JOIN mediasettings m ON a.MediaCategory = m.MediaCategory WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.appuserprofiles a JOIN appuserprofiles m ON a.EmailAddress = m.EmailAddress WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.oesroles a JOIN oesroles m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.oesresources a JOIN oesresources m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.oesgroups a JOIN oesgroups m ON a.Name = m.Name WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.templatetypes a JOIN templatetypes m ON a.Type = m.Type WHERE a.Id <> m.Id;
				DELETE a FROM {ArchiveDbName}.attributes a JOIN attributes m ON a.Name = m.Name WHERE a.Id <> m.Id;
				
				-- Remove ARCHIVED rows that no longer exist in MAIN database at all
				DELETE a FROM {ArchiveDbName}.blockgroups a LEFT JOIN blockgroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.candidategroups a LEFT JOIN candidategroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.deltatypegroups a LEFT JOIN deltatypegroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.difficultylevelgroups a LEFT JOIN difficultylevelgroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.difficultyprofilegroups a LEFT JOIN difficultyprofilegroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.equationgroups a LEFT JOIN equationgroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.ilogroups a LEFT JOIN ilogroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.itembankgroups a LEFT JOIN itembankgroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.languagegroups a LEFT JOIN languagegroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.mediasettingsgroup a LEFT JOIN mediasettingsgroup m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.notificationappuserprofile a LEFT JOIN notificationappuserprofile m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.papergroups a LEFT JOIN papergroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.papersubjects a LEFT JOIN papersubjects m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.papervenuesuspensions a LEFT JOIN papervenuesuspensions m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.formvenuesuspensions a LEFT JOIN formvenuesuspensions m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.qualitycheckcommitteegroups a LEFT JOIN qualitycheckcommitteegroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.qualitycheckcommitteeitembanks a LEFT JOIN qualitycheckcommitteeitembanks m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.qualitycheckcommitteemembers a LEFT JOIN qualitycheckcommitteemembers m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questioncategorygroups a LEFT JOIN questioncategorygroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questioncommitteeassignments a LEFT JOIN questioncommitteeassignments m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questiondetailsversions a LEFT JOIN questiondetailsversions m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questiondoclibfiles a LEFT JOIN questiondoclibfiles m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questiongroups a LEFT JOIN questiongroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questionreviews a LEFT JOIN questionreviews m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.qccomments a LEFT JOIN qccomments m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.schedulegroups a LEFT JOIN schedulegroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.schedulelanguages a LEFT JOIN schedulelanguages m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.schedulepaperforms a LEFT JOIN schedulepaperforms m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.schedulesecurityconfigurations a LEFT JOIN schedulesecurityconfigurations m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.schedulevenues a LEFT JOIN schedulevenues m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.subjectgroups a LEFT JOIN subjectgroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.transitionprofilegroups a LEFT JOIN transitionprofilegroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.venuegroups a LEFT JOIN venuegroups m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.paperformblocks a LEFT JOIN paperformblocks m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.paperitembankequations a LEFT JOIN paperitembankequations m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.paperstagecategorydecisionpaths a LEFT JOIN paperstagecategorydecisionpaths m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.formquestions a LEFT JOIN formquestions m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.blockquestions a LEFT JOIN blockquestions m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.matchingpairquestionitems a LEFT JOIN matchingpairquestionitems m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questionschoices a LEFT JOIN questionschoices m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.fileuploadresponsesettings a LEFT JOIN fileuploadresponsesettings m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.autopaperitembankquestionsections a LEFT JOIN autopaperitembankquestionsections m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.manualpaperitembankquestionsections a LEFT JOIN manualpaperitembankquestionsections m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.transitionlevels a LEFT JOIN transitionlevels m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.equationcategories a LEFT JOIN equationcategories m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.scoringschemes a LEFT JOIN scoringschemes m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.cbtcandidatesreceiveddata a LEFT JOIN cbtcandidatesreceiveddata m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.candidatesorganizationnodelookupitems a LEFT JOIN candidatesorganizationnodelookupitems m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.candidatebatchimporthistory a LEFT JOIN candidatebatchimporthistory m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.schedulepaperscandidates a LEFT JOIN schedulepaperscandidates m ON a.Id = m.Id WHERE m.Id IS NULL;

				DELETE a FROM {ArchiveDbName}.candidates a LEFT JOIN candidates m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.sections a LEFT JOIN sections m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.stages a LEFT JOIN stages m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.adaptivesection a LEFT JOIN adaptivesection m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.blocks a LEFT JOIN blocks m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questionsdetails a LEFT JOIN questionsdetails m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questionsmetadata a LEFT JOIN questionsmetadata m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.itembankpoints a LEFT JOIN itembankpoints m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.itembanks a LEFT JOIN itembanks m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.forms a LEFT JOIN forms m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.papersettings a LEFT JOIN papersettings m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.papermetadata a LEFT JOIN papermetadata m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.schedulepapers a LEFT JOIN schedulepapers m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.schedulemetadata a LEFT JOIN schedulemetadata m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.organizationnodelookupitems a LEFT JOIN organizationnodelookupitems m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.organizationsstructures a LEFT JOIN organizationsstructures m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.ilos a LEFT JOIN ilos m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.qualitycheckcommittees a LEFT JOIN qualitycheckcommittees m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.transitionprofiles a LEFT JOIN transitionprofiles m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.difficultylevels a LEFT JOIN difficultylevels m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.templates a LEFT JOIN templates m ON a.Id = m.Id WHERE m.Id IS NULL;

				DELETE a FROM {ArchiveDbName}.disabilities a LEFT JOIN disabilities m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.difficultyprofiles a LEFT JOIN difficultyprofiles m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.equationtemplates a LEFT JOIN equationtemplates m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.segmentquestionpropertiesversions a LEFT JOIN segmentquestionpropertiesversions m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.segmentquestionproperties a LEFT JOIN segmentquestionproperties m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.notifications a LEFT JOIN notifications m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.venues a LEFT JOIN venues m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.cbtcandidatessyncjobs a LEFT JOIN cbtcandidatessyncjobs m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.auditlogs a LEFT JOIN auditlogs m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.cbtsyncsettings a LEFT JOIN cbtsyncsettings m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.ctrexamsyncjobs a LEFT JOIN ctrexamsyncjobs m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.ctrresultsfilenames a LEFT JOIN ctrresultsfilenames m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.evaluationsyncjobs a LEFT JOIN evaluationsyncjobs m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.filedetails a LEFT JOIN filedetails m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.matchingpairquestionitemsversions a LEFT JOIN matchingpairquestionitemsversions m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.mediasettings a LEFT JOIN mediasettings m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.papermetadatatemplates a LEFT JOIN papermetadatatemplates m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.papersettingtemplates a LEFT JOIN papersettingtemplates m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questioncategories a LEFT JOIN questioncategories m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questioninstructiontemplates a LEFT JOIN questioninstructiontemplates m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questiontemplates a LEFT JOIN questiontemplates m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.questionschoicesversions a LEFT JOIN questionschoicesversions m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.realtimesyncjobs a LEFT JOIN realtimesyncjobs m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.scheduletemplates a LEFT JOIN scheduletemplates m ON a.Id = m.Id WHERE m.Id IS NULL;
				DELETE a FROM {ArchiveDbName}.uploadquestiontemplates a LEFT JOIN uploadquestiontemplates m ON a.Id = m.Id WHERE m.Id IS NULL;

				SET SQL_SAFE_UPDATES = 1;
				SET FOREIGN_KEY_CHECKS = 1;

			END
		";
    }
}
