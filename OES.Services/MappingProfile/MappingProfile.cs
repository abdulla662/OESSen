using AutoMapper;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Core.Entities.Paper.Views;
using OES.Core.Entities.QuestionQualityCheck;
using OES.Core.Entities.Schedule;
using OES.Core.Entities.Schedule.Views;
using OES.Core.Entities.Views.Results;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Dtos.Block.Requests;
using OES.Helper.Dtos.Block.Responses;
using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.Dtos.Candidate.Responses;
using OES.Helper.Dtos.DeltaType;
using OES.Helper.Dtos.DifficultyLevel;
using OES.Helper.Dtos.DifficultyProfile;
using OES.Helper.Dtos.Disabilities;
using OES.Helper.Dtos.EquationTemplate;
using OES.Helper.Dtos.ExamServer;
using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.ItemBank.API;
using OES.Helper.Dtos.MarkingScheme;
using OES.Helper.Dtos.NotificationDto;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Paper.Requests;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Paper.TransitionDtos.Request;
using OES.Helper.Dtos.Paper.TransitionDtos.Response;
using OES.Helper.Dtos.PaperSetting.Common;
using OES.Helper.Dtos.PaperSetting.Requests;
using OES.Helper.Dtos.PaperSetting.Responses;
using OES.Helper.Dtos.QcComments;
using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.ComprehensionQuestionDtos.HelperDtos;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.QuestionInstructionDto;
using OES.Helper.Dtos.Question.QuestionMetadataDtos;
using OES.Helper.Dtos.Question.QuestionTemplateDto;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.QuestionCategory;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.Dtos.Questionlanguage;
using OES.Helper.Dtos.QuestionLayout;
using OES.Helper.Dtos.QuestionUploadTemplateDto.OES.Helper.Dtos.UploadFiles;
using OES.Helper.Dtos.QyestionType;
using OES.Helper.Dtos.Results;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Dtos.Schedule.Responses;
using OES.Helper.Dtos.ScheduleSecurityConfiguration;
using OES.Helper.Dtos.Section;
using OES.Helper.Dtos.SectionDistributionDto.Common;
using OES.Helper.Dtos.Stage;
using OES.Helper.Dtos.Subject;
using OES.Helper.Dtos.Template.Response;
using OES.Helper.Dtos.TransitionProfile;
using OES.Helper.Dtos.TreeItem;
using OES.Helper.Dtos.UploadFiles;
using OES.Helper.Dtos.UserPapersDto;
using OES.Helper.Dtos.UserRoles;
using OES.Helper.Dtos.Venue.Requests;
using OES.Helper.Dtos.Venue.Responses;
using OES.Helper.Enums;
using OES.Helper.PagesEndpointsRolesDtos;
using SharedHelper.Enums;
using SharedHelper.General;
using QuestionCategory = OES.Core.Entities.QuestionCategory;
using QuestionType = OES.Helper.Enums.QuestionType;

namespace OES.Services.MappingProfile
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ItemBank, ItemBankDto>().ReverseMap();

            CreateMap<EditItemBankNodeDto, ItemBank>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
                //.ForMember(dest => dest.Hours, opt => opt.MapFrom(src => src.Hours))
                .ForMember(p => p.ItemBankSignature, opt => opt.Ignore())
                .ForMember(p => p.OrganizationId, opt => opt.Ignore())
                .ForMember(p => p.IsActive, opt => opt.Ignore())
                .ForMember(p => p.IsDeleted, opt => opt.Ignore())
                .ForMember(p => p.CreationUser, opt => opt.Ignore())
                .ForMember(p => p.ModeficationDate, opt => opt.Ignore())
                .ForMember(p => p.DeletedDate, opt => opt.Ignore())
                .ForMember(p => p.LevelId, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<PaperFormBlock, GetChoosenBlocksDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Block.Name))
                .ForMember(dest => dest.QuestionCount, opt => opt.MapFrom(src =>
                    src.Block.Questions
                        .Where(q => q.QuestionMetadata.ParentId == null)
                        .Sum(q => q.QuestionMetadata.QuestionType.Name == QuestionType.Comprehension.ToString()
                            ? q.QuestionMetadata.SubQuestions.Count
                            : 1)))
                .ForMember(dest => dest.AdaptiveSectionName, opt => opt.MapFrom(src => src.AdaptiveSection.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Block.Description))
                .ForMember(dest => dest.DeltaType, opt => opt.MapFrom(src => src.Block.DeltaType))
                .ForMember(dest => dest.DifficultyLevel, opt => opt.MapFrom(src => src.Block.DifficultyLevel))
                .ForMember(dest => dest.BlockType, opt => opt.MapFrom(src => src.Block.QuestionCategory))
                .ReverseMap();

            CreateMap<Template, TemplateDataDto>()
               .ForMember(dest => dest.TemplateType, opt => opt.MapFrom(src => src.TemplateType.Type))
               .ReverseMap();

            CreateMap<ILOInsertionOrUpdateRequestDto, ILO>()
                .ForMember(dest => dest.ILOGroups, opt => opt.MapFrom(src => src.GroupsIds.ConvertAll(x => new ILOGroup
                {
                    GroupId = x
                })))
                .ReverseMap();

            CreateMap<NewItemBankDTO, ItemBank>()
                .ForMember(dest => dest.ItemBankGroups, opt => opt.MapFrom(src => src.OESGroupDtos.ConvertAll(x => new ItemBankGroups
                {
                    OESGroupId = x.Id
                })))
                .ReverseMap();

            CreateMap<Core.Entities.QuestionType, QuestionTypeDto>().ReverseMap();

            CreateMap<FileUploadResponseSettings, FileUploadSettingsDto>().ReverseMap();

            CreateMap<TreeItemResponseDto, ILO>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Text))
                .ForMember(dest => dest.OrganizationSignature, opt => opt.MapFrom(src => src.Signature))
                .ReverseMap();

            CreateMap<ILODto, ILO>().ReverseMap();

            CreateMap<QuestionCategory, QuestionCategoryDto>().ReverseMap();

            CreateMap<QuestionCategory, AddQuestionCategoryDto>().ReverseMap();

            CreateMap<Subject, SubjectDto>().ReverseMap();

            CreateMap<DifficultyLevel, DifficultyLevelDto>()
               .ForMember(dest => dest.DifficultyProfileName, opt => opt.MapFrom(src => src.DifficultyProfile.Name))
               .ForMember(dest => dest.DifficultyProfileDescription, opt => opt.MapFrom(src => src.DifficultyProfile.Description))
               .ForMember(dest => dest.DeltaTypeName, opt => opt.MapFrom(src => src.DeltaType.Name));

            CreateMap<QuestionLayout, LayoutDto>();

            CreateMap<DifficultyProfile, DifficultyProfileDto>().ReverseMap();

            CreateMap<DifficultyProfile, ProfileDto>().ReverseMap();

            CreateMap<ItemBank, RootItemBankDto>().ReverseMap();

            CreateMap<ILO, RootIloDto>().ReverseMap();

            CreateMap<QuestionDetails, QuestionLanguageDTO>()
                .ForMember(dest => dest.LanguageName, opt => opt.MapFrom(src => src.Language.Name))
                .ForMember(dest => dest.NumberOfChoices, opt => opt.MapFrom(src => src.QuestionsChoices.Count))
                .ReverseMap();

            CreateMap<Language, LanguageDto>().ReverseMap();

            CreateMap<Language, GetLanguageDto>().ReverseMap();

            CreateMap<LanguageCreateDto, Language>().ReverseMap();

            CreateMap<LanguageUpdateDto, Language>().ReverseMap();

            CreateMap<SegmentQuestionConfigDto, SegmentQuestionProperties>().ReverseMap();

            CreateMap<QuestionDetails, LanguageDto>().ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Language.Name))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Language.Id))
                .ReverseMap();

            CreateMap<DifficultyProfile, AddDifficultyProfileDto>().ReverseMap();

            CreateMap<SubQuestionDetailsDto, QuestionDetails>()
               .ForMember(dest => dest.QuestionsChoices, opt => opt.Ignore())
               .ForPath(dest => dest.AttachmentFileName, opt => opt.MapFrom(src => src.AttachmentFileName))
               .ForMember(dest => dest.QuestionMetadata, opt => opt.Ignore())
               .ForMember(dest => dest.CreationUser, opt => opt.Ignore())
               .ForMember(dest => dest.CreationDate, opt => opt.Ignore())
               .ForMember(dest => dest.OrganizationId, opt => opt.Ignore())
               .ForMember(dest => dest.OrganizationSignature, opt => opt.Ignore())
               .ReverseMap()
               .ForMember(dest => dest.Choices, opt => opt.MapFrom(src => src.QuestionsChoices));

            CreateMap<QuestionMetadata, LanguageDto>().ReverseMap();

            CreateMap<QuestionMetadata, QuestionMetadataPaginationDto>()
             .ForMember(dest => dest.Subject,
                           opt => opt.MapFrom(src => src.Subject.Name))
             .ForMember(dest => dest.ItemBank,
                           opt => opt.MapFrom(src => src.ItemBank.Name))
             .ForMember(dest => dest.Type,
                           opt => opt.MapFrom(src => src.QuestionType.Name))
             .ForMember(dest => dest.Category,
                           opt => opt.MapFrom(src => src.QuestionCategory.Name))
             .ForMember(dest => dest.Status,
                           opt => opt.MapFrom(src => src.QuestionStatus))
             .ForMember(dest => dest.Language,
                           opt => opt.MapFrom(src => string.Join(", ", src.QuestionDetails.Where(qd => qd.Language != null).Select(qd => qd.Language.Name))))
             .ReverseMap();

            CreateMap<BlockQuestion, BlockQuestionsDetailsPaginationDto>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.QuestionMetadata.Code))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.QuestionMetadata.Subject.Name))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.QuestionMetadata.QuestionType.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.QuestionMetadata.QuestionCategory.Name))
                .ForMember(dest => dest.ItemBank, opt => opt.MapFrom(src => src.QuestionMetadata.ItemBank.Name))
                .ForMember(dest => dest.DifficultyProfile, opt => opt.MapFrom(src => src.QuestionMetadata.DifficultyProfile.Name))
                .ForMember(dest => dest.DifficultyLevel, opt => opt.MapFrom(src => src.QuestionMetadata.DifficultyLevel.Name))
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.QuestionMetadata.QuestionDetails.FirstOrDefault().Body)).ReverseMap();

            CreateMap<QuestionMetadata, PendingQuestionPaginationDto>()
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject.Name))
                .ForMember(dest => dest.ItemBank, opt => opt.MapFrom(src => src.ItemBank.Name))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.QuestionType.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.QuestionCategory.Name))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.QuestionStatus))
                .ForMember(dest => dest.Language, opt => opt.MapFrom(src => string.Join(", ", src.QuestionDetails.Where(qd => qd.Language != null).Select(qd => qd.Language.Name))))
                .ReverseMap();

            CreateMap<QuestionMetadata, ApprovedQuestionsPaginationDto>()
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault().Body))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject.Name))
                .ForMember(dest => dest.ItemBank, opt => opt.MapFrom(src => src.ItemBank.Name))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.QuestionType.Name))
                .ForMember(dest => dest.DifficultyProfile, opt => opt.MapFrom(src => src.DifficultyProfile.Name))
                .ForMember(dest => dest.DifficultyLevel, opt => opt.MapFrom(src => src.DifficultyLevel.Name))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.QuestionCategory.Name)).ReverseMap();

            CreateMap<QuestionDetails, QuestionDetailsDto>()
                .ForMember(dest => dest.Choices, opt => opt.MapFrom(src => src.QuestionsChoices));

            CreateMap<QuestionDetailsDto, QuestionDetails>()
                .ForMember(dest => dest.QuestionsChoices, opt => opt.Ignore())
                .ForMember(dest => dest.CreationUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreationDate, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationId, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationSignature, opt => opt.Ignore());

            CreateMap<QuestionsChoices, ChoiceDataDto>()
                .ForMember(dest => dest.HasAttachment, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.AttachmentFileName)));

            CreateMap<ChoiceDataDto, QuestionsChoices>()
                .ForMember(dest => dest.CreationUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreationDate, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationId, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationSignature, opt => opt.Ignore());

            CreateMap<QuestionDetails, QuestionDataDto>()
              .ForMember(dest => dest.Choices, opt => opt.MapFrom(src => src.QuestionsChoices))
              .ForMember(dest => dest.HasAttachment, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.AttachmentFileName)))
              .ForMember(dest => dest.SegmentQuestionConfig, opt => opt.MapFrom(src => src.SegmentQuestionProperties))
              .ForMember(dest => dest.AttachmentFileName, opt => opt.MapFrom(src => src.AttachmentFileName));

            CreateMap<QuestionDataDto, QuestionDetails>()
                .ForMember(dest => dest.QuestionsChoices, opt => opt.Ignore())
                .ForMember(dest => dest.CreationUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreationDate, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationId, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationSignature, opt => opt.Ignore());

            CreateMap<QCComment, QCommentDto>().ReverseMap();

            CreateMap<QCComment, QcCommentDto>().ReverseMap();

            CreateMap<AddMultipleCandidateDto, Candidate>();

            CreateMap<QuestionMetadata, GetQuestionMetaDataForQCViewDto>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Code ?? string.Empty))
                .ForMember(dest => dest.DifficultyLevel, opt => opt.MapFrom(src =>
                    src.DifficultyProfile.DifficultyLevels.FirstOrDefault() != null
                        ? src.DifficultyProfile.DifficultyLevels.FirstOrDefault().Name
                        : string.Empty))
                .ForMember(dest => dest.FromDelta, opt => opt.MapFrom(src => src.DifficultyLevel.FromDelta))
                .ForMember(dest => dest.ToDelta, opt => opt.MapFrom(src => src.DifficultyLevel.ToDelta))
                .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.Name ?? string.Empty))
                .ForMember(dest => dest.ItemBankName, opt => opt.MapFrom(src => src.ItemBank.Name ?? string.Empty))
                .ForMember(dest => dest.ItemBankNodeId, opt => opt.MapFrom(src => src.ItemBank.Id))
                .ForMember(dest => dest.ItemBankRootId, opt => opt.MapFrom(src => src.ItemBank.ParentId))
                .ForMember(dest => dest.IloName, opt => opt.MapFrom(src => src.Ilo.Name ?? string.Empty))
                .ForMember(dest => dest.DeltaValue, opt => opt.MapFrom(src => src.Delta))
                .ForMember(dest => dest.MaximumAnswerTime, opt => opt.MapFrom(src => src.MaximumAnswerTime))
                .ForMember(dest => dest.MaxWords, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault() != null ? src.QuestionDetails.FirstOrDefault().MaxWords : null))
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author ?? string.Empty))
                .ForMember(dest => dest.QuestionTypeId, opt => opt.MapFrom(src => src.QuestionTypeId))
                .ForMember(dest => dest.MaxRecordingTimeInSeconds, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault() != null ? src.QuestionDetails.FirstOrDefault().MaxRecordingTimeInSeconds : 0))
                .ForMember(dest => dest.LayoutId, opt => opt.MapFrom(src => src.QLayout.Id))
                .ForMember(dest => dest.LayoutName, opt => opt.MapFrom(src => src.QLayout.Name))
                .ForMember(dest => dest.ProfileName, opt => opt.MapFrom(src => src.DifficultyProfile.Name ?? string.Empty));

            CreateMap<QuestionDetails, QuestionDetailsForQCDto>()
                .ForMember(dest => dest.LanguageName, opt => opt.MapFrom(src => src.Language.Name ?? string.Empty))
                .ForMember(dest => dest.Instruction, opt => opt.MapFrom(src => src.Instructions))
                .ForMember(dest => dest.AttachmentFileName, opt => opt.MapFrom(src => src.AttachmentFileName))
                .ForMember(dest => dest.MaxWords, opt => opt.MapFrom(src => src.MaxWords))
                .ForMember(dest => dest.MaxRecordingTimeInSeconds, opt => opt.MapFrom(src => src.MaxRecordingTimeInSeconds))
                .ForMember(dest => dest.SegmentQuestionConfig, opt => opt.MapFrom(src => src.SegmentQuestionProperties))
                .ForMember(dest => dest.LanguageId, opt => opt.MapFrom(src => src.Language.Id)).ReverseMap();

            CreateMap<QuestionDetails, UploadQuestionDetailsDto>().ReverseMap()
                .ForMember(dest => dest.CreationUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreationDate, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationId, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationSignature, opt => opt.Ignore());

            CreateMap<QuestionsChoices, UploadChoicesDto>().ReverseMap()
                .ForMember(dest => dest.CreationUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreationDate, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationId, opt => opt.Ignore())
                .ForMember(dest => dest.OrganizationSignature, opt => opt.Ignore());

            CreateMap<ItemBank, SelectedItemBankNodeFromDialogDto>().ReverseMap();

            CreateMap<ILO, SelectedIloNodeFromDialogDto>().ReverseMap();

            CreateMap<QuestionMetadata, QuestionMetadataRetrievalDto>()
                .ForMember(dest => dest.RootItemBankId, opt => opt.MapFrom(src => src.ItemBank.ParentId))
                .ForMember(dest => dest.ChildItemBankDto, opt => opt.MapFrom(src => src.ItemBank))
                .ForMember(dest => dest.RootIloId, opt => opt.MapFrom(src => src.Ilo.ParentId))
                .ForMember(dest => dest.ChildIloDto, opt => opt.MapFrom(src => src.Ilo))
                .ForMember(dest => dest.MaxWords, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault() != null ? src.QuestionDetails.FirstOrDefault().MaxWords : null))
                .ForMember(dest => dest.MaxRecordingTimeInSeconds, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault() != null ? src.QuestionDetails.FirstOrDefault().MaxRecordingTimeInSeconds : 0))
                .ReverseMap();

            CreateMap<AppUserProfileRetrievalDto, AppUserProfile>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ModuleUserID))
                .ReverseMap();

            CreateMap<RetrivedGroupDto, OESGroup>().ReverseMap();

            // Start copying questions, so don't touch please!
            CreateMap<QuestionMetadata, QuestionMetadata>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => 0))
               .ForMember(dest => dest.Code, opt => opt.MapFrom(src => $"{src.Code}-{DateTimeHelper.Now.Ticks}"))
               .ForMember(dest => dest.QuestionStatus, opt => opt.MapFrom(_ => QuestionStatus.LayoutSelectedAndPending))
               .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
               .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore())
               .ForMember(dest => dest.QuestionDetails, opt => opt.MapFrom(src => src.QuestionDetails));

            CreateMap<QuestionDetails, QuestionDetails>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionsChoices, opt => opt.MapFrom(src => src.QuestionsChoices))
                .ForMember(dest => dest.SegmentQuestionProperties, opt => opt.MapFrom(src => src.SegmentQuestionProperties))
                .ForMember(dest => dest.MatchingPairQuestionItems, opt => opt.MapFrom(src => src.MatchingPairQuestionItems));

            CreateMap<SegmentQuestionProperties, SegmentQuestionProperties>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<FileUploadResponseSettings, FileUploadResponseSettings>()
               .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
               .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
               .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<QuestionsChoices, QuestionsChoices>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<MatchingPairQuestionItems, MatchingPairQuestionItems>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<QuestionTemplate, QuestionTemplateDto>()
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));
            // End copying questions, so don't touch please!

            CreateMap<RoleDto, OESRole>().ReverseMap();

            CreateMap<AddDifficultyLevelDto, DifficultyLevel>().ReverseMap();

            CreateMap<UpdateDifficultyLevelDto, DifficultyLevel>().ReverseMap();

            CreateMap<OESGroup, GetOESGroupDto>().ReverseMap();

            CreateMap<OESGroupRole, RoleDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.OESRoleId.ToString()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.OESRole.Name))
                .ReverseMap();

            CreateMap<AppUserProfileGroup, UserGroupsAndRolesDto>()
                .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.OESGroup.Id))
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.OESGroup.Name))
                .ForMember(dest => dest.GroupRoles, opt => opt.MapFrom(src => src.OESGroup.GroupResources
                    .SelectMany(gr => gr.ResourceRoles)
                    .Select(rr => new RoleDto
                    {
                        Id = rr.Role.Id,
                        Name = rr.Role.Name
                    })
                    .ToList()))
                .ReverseMap();

            CreateMap<OESGroupRole, RoleDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.OESRole.Id.ToString()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.OESRole.Name));

            CreateMap<NotificationAppUserProfile, NotificationAppUserProfileDto>().ReverseMap();

            CreateMap<NotificationAppUserProfile, NotificationDto>()
                .ForMember(dest => dest.Entity, opt => opt.MapFrom(src => src.Notification.Entity))
                .ForMember(dest => dest.Operation, opt => opt.MapFrom(src => src.Notification.Operation))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Notification.Status))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Notification.Type))
                .ReverseMap();

            CreateMap<Notification, NotificationDto>().ReverseMap();

            CreateMap<DeltaType, GetDeltaTypeDto>().ReverseMap();

            CreateMap<RootItemBankDto, ItemBank>().ReverseMap();

            CreateMap<PaperDataViewResponseDto, PaperMetadata>().ReverseMap();

            CreateMap<GetPaperMetadataResponseDto, PaperMetadata>()
                .ForMember(dest => dest.Subjects, opt => opt.MapFrom(src => src.SubjectsIds.Select(id => new PaperSubject { SubjectId = id })))
                .ReverseMap()
                .ForMember(dest => dest.SubjectsIds, opt => opt.MapFrom(src => src.Subjects.Select(s => s.SubjectId)))
                .ForMember(dest => dest.CategoryFixedDPaths, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryStagePaths, opt => opt.Ignore())
                .ForMember(dest => dest.AdaptiveCategoryExecutionOrder, opt => opt.Ignore());

            CreateMap<AddScheduleMetadataRequestDto, ScheduleMetadata>()
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => DateOnly.FromDateTime(src.StartDate)))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => DateOnly.FromDateTime(src.EndDate)))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => TimeOnly.FromTimeSpan(src.StartTime)))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => TimeOnly.FromTimeSpan(src.EndTime)))
                .ForMember(dest => dest.ScheduleLocation, opt => opt.MapFrom(src => src.ScheduleLocation))
                .ForMember(dest => dest.Languages, opt => opt.Ignore())
                .ForMember(dest => dest.Venues, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<Block, GetBlockResponseDto>()
                .ForMember(dest => dest.DifficultyLevelName, opt => opt.MapFrom(src => src.DifficultyLevel.Name))
                .ForMember(dest => dest.BlockTypeName, opt => opt.MapFrom(src => src.QuestionCategory.Name))
                .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions.Select(q => q.QuestionMetadata)))
                .ForMember(dest => dest.QuestionsCount, opt => opt.MapFrom(src => src.Questions.Count))
                .ForMember(dest => dest.InUse, opt => opt.MapFrom(src => src.PaperBlocks.Count > 0));

            CreateMap<BlockQuestion, QuestionMetadataPaginationDto>()
                .ConvertUsing(src => new QuestionMetadataPaginationDto
                {
                    Id = src.QuestionMetadata.Id,
                    Code = src.QuestionMetadata.Code,
                    Subject = src.QuestionMetadata.Subject.Name,
                    ItemBank = src.QuestionMetadata.ItemBank.Name,
                    Type = src.QuestionMetadata.QuestionType.Name,
                    Category = src.QuestionMetadata.QuestionCategory.Name,
                    Status = src.QuestionMetadata.QuestionStatus
                });

            CreateMap<CreateOrUpdateBlockRequestDto, Block>()
                .ForMember(dest => dest.QuestionCategoryId, opt => opt.MapFrom(src => src.BlockTypeId))
                .ForMember(dest => dest.DeltaTypeId, opt => opt.MapFrom(src => src.DeltaTypeId))
                .ForMember(dest => dest.LanguageId, opt => opt.MapFrom(src => src.LanguageId))
                .ForMember(dest => dest.DifficultyLevelId, opt => opt.MapFrom(src => src.DifficultyLevelId));

            CreateMap<Language, LanguageDto>();

            CreateMap<ScheduleMetadata, ScheduleMetadataDto>()
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate.ToDateTime(TimeOnly.MinValue)))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate.ToDateTime(TimeOnly.MinValue)))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.ToTimeSpan()))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.ToTimeSpan()))
                .ForMember(dest => dest.ExamVenueIds, opt => opt.MapFrom(src => src.Venues.Select(v => v.Id)))
                .ForMember(dest => dest.LanguageIds, opt => opt.MapFrom(src => src.Languages.Select(l => l.Id)))
                .ReverseMap();

            CreateMap<ScheduleMetadata, ScheduleMetadataPaginationDto>()
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.ExamVenueIds, opt => opt.MapFrom(src => src.Venues.Select(v => v.Id)))
                .ForMember(dest => dest.LanguageIds, opt => opt.MapFrom(src => src.Languages.Select(l => l.Id)))
                .ReverseMap();

            CreateMap<MarkingSchemeDto, MarkingScheme>().ReverseMap();

            CreateMap<AddOrUpdatePaperMetadataRequestDto, PaperMetadata>()
                .ForMember(dest => dest.Subjects, opt => opt.MapFrom(src => src.SubjectsIds.Select(id => new PaperSubject { SubjectId = id })))
                .ReverseMap()
                .ForMember(dest => dest.SubjectsIds, opt => opt.MapFrom(src => src.Subjects.Select(s => s.SubjectId)));

            CreateMap<UpdateTemplateDto, Template>().ReverseMap();

            CreateMap<TransitionProfile, AddTransitionProfileDto>().ReverseMap();

            CreateMap<PaperMetadata, UserPapersListDto>()
                .ForMember(dest => dest.SubType, opt => opt.MapFrom(src => src.Type == PaperType.Adaptive ? "-" : src.QuestionSelectionType.ToString()))
                .ForMember(dest => dest.ActualFormsCount, opt => opt.MapFrom(src => src.Forms.Count))
                .ForMember(dest => dest.FormsStatusCounts, opt => opt.MapFrom(src => src.Forms.GroupBy(f => f.FormStatus)
                    .Select(g => new FormStatusCountDto
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToList())
                );

            CreateMap<PaperMetadata, UserPaperForSchedule>().ReverseMap();

            CreateMap<PaperMetadata, GetUserPapersDto>().ReverseMap();

            CreateMap<SectionDistributionDto, AutoPaperItemBankQuestionSection>();

            CreateMap<TransitionProfile, TransitionProfileDto>().ReverseMap();

            CreateMap<TransitionLevel, TransitionLevelDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.LowerDScore, opt => opt.MapFrom(src => src.LowerDScore))
                .ForMember(dest => dest.UpperDScore, opt => opt.MapFrom(src => src.UpperDScore))
                .ForMember(dest => dest.TransitionProfileName, opt => opt.MapFrom(src => src.TransitionProfile.Name))
                .ForMember(dest => dest.TransitionProfileDescription, opt => opt.MapFrom(src => src.TransitionProfile.Description));

            CreateMap<TransitionLevel, GetTransitionLevelsDto>()
                .ForMember(dest => dest.TransitionProfileId, opt => opt.MapFrom(src => src.TransitionProfileId))
                .ForMember(dest => dest.DifficultyLevelId, opt => opt.MapFrom(src => src.DifficultyLevelId))
                .ForMember(dest => dest.QuestionCategoryId, opt => opt.MapFrom(src => src.QuestionCategoryId))
                .ForMember(dest => dest.QuestionCategoryName, opt => opt.MapFrom(src => src.QuestionCategory.Name));

            CreateMap<AddTransitionLevelRequestDto, TransitionLevel>()
                .ForMember(dest => dest.TransitionProfile, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionCategory, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DifficultyLevelId, opt => opt.MapFrom(src => src.DifficultyLevelId));

            CreateMap<GetTransitionLevelDto, TransitionLevel>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.TransitionProfile, opt => opt.Ignore())
                .ForMember(dest => dest.QuestionCategory, opt => opt.Ignore())
                .ForMember(dest => dest.DifficultyLevel, opt => opt.Ignore());

            CreateMap<AdaptiveSection, AdaptiveSectionDto>()
                .ForMember(dest => dest.InstructionSectionAdaptiveName, opt => opt.MapFrom(src => src.InstructionSectionTemplate.Name))
                .ForMember(dest => dest.InstructionSectionAdaptiveId, opt => opt.MapFrom(src => src.InstructionSectionTemplate.Id))
                .ReverseMap();

            CreateMap<Stage, StageDto>()
                .ForMember(dest => dest.InstructionSectionAdaptiveId, opt => opt.MapFrom(src => src.InstructionSectionTemplate.Id))
                .ForMember(dest => dest.InstructionSectionAdaptiveName, opt => opt.MapFrom(src => src.InstructionSectionTemplate.Name))
                .ReverseMap();

            CreateMap<DifficultyLevel, GetDifficultyLevelDto>();

            CreateMap<DeltaType, AddDeltaTypeDto>().ReverseMap();

            CreateMap<PaperSettingTemplate, PaperSettingsTemplatePaginated>().ReverseMap();

            CreateMap<PaperSettingTemplate, GetPaperSettingsResponseDto>().ReverseMap();

            CreateMap<TransitionLevel, GetTransitionLevelDto>()
                .ForMember(dest => dest.TransitionProfileName, opt => opt.MapFrom(src => src.TransitionProfile.Name))
                .ForMember(dest => dest.TransitionProfileDescription, opt => opt.MapFrom(src => src.TransitionProfile.Description))
                .ForMember(dest => dest.DifficultyLevelName, opt => opt.MapFrom(src => src.DifficultyLevel.Name))
                .ForMember(dest => dest.DifficultyProfileId, opt => opt.MapFrom(src => src.TransitionProfile.DifficultyProfileId))
                .ForMember(dest => dest.DifficultyProfileName, opt => opt.MapFrom(src => src.TransitionProfile.DifficultyProfile.Name))
                .ForMember(dest => dest.QuestionCategoryName, opt => opt.MapFrom(src => src.QuestionCategory.Name));

            CreateMap<Template, GetListedTemplateResponseDto>().ReverseMap();

            CreateMap<PaperSettingsTemplateFlattenedDataDto, PaperSettingsTemplateRequestDto>()
                .ForMember(dest => dest.PaperSettingsResponseDto, opt => opt.MapFrom(src => src));

            CreateMap<PaperSettingsTemplateFlattenedDataDto, GetPaperSettingsResponseDto>()
                .ForMember(dest => dest.AdditionalPaperSettings, opt => opt.MapFrom(src => src));

            CreateMap<PaperSettingsTemplateFlattenedDataDto, AdditionalPaperSettings>();

            CreateMap<AdditionalPaperSettings, SchedulePaperSettings>().ReverseMap();

            CreateMap<Venue, AddVenueRequestDto>().ReverseMap();

            CreateMap<UpdateScheduleMetadataRequestDto, ScheduleMetadata>()
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate.HasValue ? DateOnly.FromDateTime(src.StartDate.Value) : default))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate.HasValue ? DateOnly.FromDateTime(src.EndDate.Value) : default))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.HasValue ? TimeOnly.FromTimeSpan(src.StartTime.Value) : default))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.HasValue ? TimeOnly.FromTimeSpan(src.EndTime.Value) : default))
                .ForMember(dest => dest.ScheduleLocation, opt => opt.MapFrom(src => src.ScheduleLocation))
                .ForMember(dest => dest.Languages, opt => opt.Ignore())
                .ForMember(dest => dest.Venues, opt => opt.Ignore());

            CreateMap<ScheduleSecurityConfigurationTemplate, SecurityConfigurationTemplateDto>().ReverseMap();

            CreateMap<Venue, EditVenueRequestDto>().ReverseMap();

            CreateMap<Candidate, CandidateDetailsDto>().ReverseMap();

            CreateMap<CandidateDto, Candidate>();

            CreateMap<Candidate, AddOrUpdateCandidateRequestDto>()
                .ForMember(dest => dest.PhotoFile, opt => opt.Ignore())
                .ForMember(dest => dest.SignatureFile, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.PhotoURL, opt => opt.MapFrom(src => src.PhotoURL))
                .ForMember(dest => dest.SignatureURL, opt => opt.MapFrom(src => src.SignatureURL));

            CreateMap<SchedulePaper, SchedulePaperPaginationDto>()
                .ForMember(dest => dest.PaperName, opt => opt.MapFrom(src => src.PaperMetadata.Name))
                .ForMember(dest => dest.PaperCode, opt => opt.MapFrom(src => src.PaperMetadata.Code))
                .ForMember(dest => dest.SessionDescription, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.PaperSettingsConfigured, opt => opt.MapFrom(src => src.PaperSettings != null))
                .ForMember(dest => dest.CandidatesCount, opt => opt.MapFrom(src => src.Candidates.LongCount()));

            CreateMap<SchedulePaper, AddOrUpdateSchedulePaperRequestDto>().ReverseMap();
            CreateMap<SchedulePaperForms, AddOrUpdateSchedulePaperFormsDto>().ReverseMap();

            CreateMap<ExamSecurityConfigurationDto, ScheduleSecurityConfiguration>().ReverseMap();

            CreateMap<AddOrUpdateSecurityConfigurationDto, ScheduleSecurityConfiguration>()
                .ForMember(dest => dest.ScheduleMetadataId, opt => opt.MapFrom(src => src.ScheduleId))
                .ReverseMap();

            CreateMap<Venue, GetPaginatedVenueResponseDto>().ReverseMap();

            CreateMap<GetSchedulePaperResponseDto, SchedulePaper>().ReverseMap();

            CreateMap<SchedulePaperSettings, GetPaperSettingsResponseDto>()
                .ForPath(dest => dest.AdditionalPaperSettings.RuleTemplateId, opt => opt.MapFrom(src => src.RuleTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.ShowNotePad, opt => opt.MapFrom(src => src.ShowNotePad))
                .ForPath(dest => dest.AdditionalPaperSettings.RequireAnswerBeforeProceeding, opt => opt.MapFrom(src => src.RequireAnswerBeforeProceeding))
                .ForPath(dest => dest.AdditionalPaperSettings.MediaPlayingCount, opt => opt.MapFrom(src => src.MediaPlayingCount))
                .ForPath(dest => dest.AdditionalPaperSettings.AskSupervisorForPlayMediaAgain, opt => opt.MapFrom(src => src.AskSupervisorForPlayMediaAgain))
                .ForPath(dest => dest.AdditionalPaperSettings.AutoStartEnabled, opt => opt.MapFrom(src => src.AutoStartEnabled))
                .ForPath(dest => dest.AdditionalPaperSettings.IdenticalItemSequenceToAllCandidate, opt => opt.MapFrom(src => src.IdenticalItemSequenceToAllCandidate))
                .ForPath(dest => dest.AdditionalPaperSettings.ResultTemplateId, opt => opt.MapFrom(src => src.ResultTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.DisclaimerTemplateId, opt => opt.MapFrom(src => src.DisclaimerTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.OtherInstructionTemplateId, opt => opt.MapFrom(src => src.OtherInstructionTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.InstructionTemplateId, opt => opt.MapFrom(src => src.InstructionTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.CertificateTemplateId, opt => opt.MapFrom(src => src.CertificateTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.IsCertificateRequired, opt => opt.MapFrom(src => src.IsCertificateRequired))
                .ForPath(dest => dest.AdditionalPaperSettings.ShowQuestionPaper, opt => opt.MapFrom(src => src.ShowQuestionPaper))
                .ForPath(dest => dest.AdditionalPaperSettings.AnsweredQuestionCanBeChanged, opt => opt.MapFrom(src => src.AnsweredQuestionCanBeChanged))
                .ForPath(dest => dest.AdditionalPaperSettings.EnableEndTestButtonAfterTimeSpent, opt => opt.MapFrom(src => src.EnableEndTestButtonAfterTimeSpent))
                .ForPath(dest => dest.AdditionalPaperSettings.IdenticalQuestionPaperToAllCandidate, opt => opt.MapFrom(src => src.IdenticalQuestionPaperToAllCandidate))
                .ForPath(dest => dest.AdditionalPaperSettings.OptionRandomization, opt => opt.MapFrom(src => src.OptionRandomization))
                .ForPath(dest => dest.AdditionalPaperSettings.SkipQuestion, opt => opt.MapFrom(src => src.SkipQuestion))
                .ForPath(dest => dest.AdditionalPaperSettings.ShowResetQuestion, opt => opt.MapFrom(src => src.ShowResetQuestion))
                .ForPath(dest => dest.AdditionalPaperSettings.ApplyTimerAlert, opt => opt.MapFrom(src => src.ApplyTimerAlert))
                .ForPath(dest => dest.AdditionalPaperSettings.TimerAlertMinutes, opt => opt.MapFrom(src => src.TimerAlertMinutes))
                .ReverseMap();

            CreateMap<SchedulePaperSettings, AddOrUpdatePaperSettingsRequestDto>()
                .ForPath(dest => dest.AdditionalPaperSettings.RuleTemplateId, opt => opt.MapFrom(src => src.RuleTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.ShowNotePad, opt => opt.MapFrom(src => src.ShowNotePad))
                .ForPath(dest => dest.AdditionalPaperSettings.RequireAnswerBeforeProceeding, opt => opt.MapFrom(src => src.RequireAnswerBeforeProceeding))
                .ForPath(dest => dest.AdditionalPaperSettings.MediaPlayingCount, opt => opt.MapFrom(src => src.MediaPlayingCount))
                .ForPath(dest => dest.AdditionalPaperSettings.AskSupervisorForPlayMediaAgain, opt => opt.MapFrom(src => src.AskSupervisorForPlayMediaAgain))
                .ForPath(dest => dest.AdditionalPaperSettings.AutoStartEnabled, opt => opt.MapFrom(src => src.AutoStartEnabled))
                .ForPath(dest => dest.AdditionalPaperSettings.IdenticalItemSequenceToAllCandidate, opt => opt.MapFrom(src => src.IdenticalItemSequenceToAllCandidate))
                .ForPath(dest => dest.AdditionalPaperSettings.ResultTemplateId, opt => opt.MapFrom(src => src.ResultTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.DisclaimerTemplateId, opt => opt.MapFrom(src => src.DisclaimerTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.OtherInstructionTemplateId, opt => opt.MapFrom(src => src.OtherInstructionTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.InstructionTemplateId, opt => opt.MapFrom(src => src.InstructionTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.CertificateTemplateId, opt => opt.MapFrom(src => src.CertificateTemplateId))
                .ForPath(dest => dest.AdditionalPaperSettings.IsCertificateRequired, opt => opt.MapFrom(src => src.IsCertificateRequired))
                .ForPath(dest => dest.AdditionalPaperSettings.ShowQuestionPaper, opt => opt.MapFrom(src => src.ShowQuestionPaper))
                .ForPath(dest => dest.AdditionalPaperSettings.AnsweredQuestionCanBeChanged, opt => opt.MapFrom(src => src.AnsweredQuestionCanBeChanged))
                .ForPath(dest => dest.AdditionalPaperSettings.EnableEndTestButtonAfterTimeSpent, opt => opt.MapFrom(src => src.EnableEndTestButtonAfterTimeSpent))
                .ForPath(dest => dest.AdditionalPaperSettings.IdenticalQuestionPaperToAllCandidate, opt => opt.MapFrom(src => src.IdenticalQuestionPaperToAllCandidate))
                .ForPath(dest => dest.AdditionalPaperSettings.OptionRandomization, opt => opt.MapFrom(src => src.OptionRandomization))
                .ForPath(dest => dest.AdditionalPaperSettings.SkipQuestion, opt => opt.MapFrom(src => src.SkipQuestion))
                .ForPath(dest => dest.AdditionalPaperSettings.ShowResetQuestion, opt => opt.MapFrom(src => src.ShowResetQuestion))
                .ForPath(dest => dest.AdditionalPaperSettings.ApplyTimerAlert, opt => opt.MapFrom(src => src.ApplyTimerAlert))
                .ForPath(dest => dest.AdditionalPaperSettings.TimerAlertMinutes, opt => opt.MapFrom(src => src.TimerAlertMinutes))
                .ReverseMap();

            CreateMap<PaperMetadata, PaperMetadata>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => $"{src.Code}-{DateTimeHelper.Now.Ticks}"))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.Name}-{DateTimeHelper.Now.Ticks}"))
                .ForMember(dest => dest.PaperCreationStatus, opt => opt.MapFrom(src => src.PaperCreationStatus))
                .ForMember(dest => dest.PaperStatus, opt => opt.MapFrom(_ => AvailabilityStatus.Active))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<PaperForm, PaperForm>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => $"{src.Code}-{DateTimeHelper.Now.Ticks}"))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.Name}-{DateTimeHelper.Now.Ticks}"))
                .ForMember(dest => dest.PaperId, opt => opt.Ignore())
                .ForMember(dest => dest.Paper, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<Block, Block>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<BlockQuestion, BlockQuestion>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.BlockId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<PaperStageCategoryDecisionPath, PaperStageCategoryDecisionPath>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.StageId, opt => opt.Ignore())
                .ForMember(dest => dest.PaperId, opt => opt.Ignore())
                .ForMember(dest => dest.Stage, opt => opt.Ignore())
                .ForMember(dest => dest.Paper, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<GeneratedFormQuestion, GeneratedFormQuestion>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.FormId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<PaperSubject, PaperSubject>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.PaperId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<PaperItemBankPoint, PaperItemBankPoint>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.PaperId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<ManualPaperItemBankQuestionSection, ManualPaperItemBankQuestionSection>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ItemBankPointId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<AutoPaperItemBankQuestionSection, AutoPaperItemBankQuestionSection>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ItemBankPointId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<StandardSection, StandardSection>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.PaperId, opt => opt.Ignore())
                .ForMember(dest => dest.FormId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<AdaptiveSection, AdaptiveSection>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<PaperFormBlock, PaperFormBlock>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.PaperId, opt => opt.Ignore())
                .ForMember(dest => dest.Paper, opt => opt.Ignore())
                .ForMember(dest => dest.FormId, opt => opt.Ignore())
                .ForMember(dest => dest.Block, opt => opt.Ignore())
                .ForMember(dest => dest.AdaptiveSectionId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<MarkingScheme, MarkingScheme>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.PaperId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<Stage, Stage>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<ScheduleMetadata, ScheduleMetadata>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => $"{src.Code}-{DateTimeHelper.Now.Ticks}"))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => $"{src.Name}-{DateTimeHelper.Now.Ticks}"))
                .ForMember(dest => dest.PublishingStatus, opt => opt.MapFrom(_ => PublishingStatus.NotPublished))
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore())
                .ForMember(dest => dest.Papers, opt => opt.MapFrom(src => src.Papers))
                .ForMember(dest => dest.SecurityConfiguration, opt => opt.MapFrom(src => src.SecurityConfiguration))
                .ForMember(dest => dest.Venues, opt => opt.MapFrom(src => src.Venues))
                .ForMember(dest => dest.Languages, opt => opt.MapFrom(src => src.Languages));

            CreateMap<SchedulePaper, SchedulePaper>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ScheduleMetadataId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore())
                .ForMember(dest => dest.PaperSettings, opt => opt.MapFrom(src => src.PaperSettings))
                .ForMember(dest => dest.Candidates, opt => opt.MapFrom(src => src.Candidates));

            CreateMap<SchedulePaperSettings, SchedulePaperSettings>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.SchedulePaperId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<SchedulePaperCandidate, SchedulePaperCandidate>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.SchedulePaperId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<ScheduleVenue, ScheduleVenue>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ScheduleId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<ScheduleLanguage, ScheduleLanguage>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ScheduleMetadataId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<ScheduleSecurityConfiguration, ScheduleSecurityConfiguration>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ScheduleMetadataId, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationUser, opt => opt.Ignore())
                .ForMember(dest => dest.ModeficationDate, opt => opt.Ignore());

            CreateMap<GetSchedulePaperAllocationResponseDto, SchedulePaperAllocationView>().ReverseMap();

            CreateMap<CandidateQuestionsAnswersDto, CandidateQuestionsWithEquationView>().ReverseMap();

            CreateMap<PaperFormAttendanceReportDto, PaperFormAttendanceReportView>().ReverseMap();

            // Exam Server Profiles

            CreateMap<ScheduleSecurityConfiguration, ScheduleSecurityConfigurationDto>();

            CreateMap<Language, LanguageDto>();

            CreateMap<SchedulePaperSettings, PaperSettingsDto>()
                .ForMember(dest => dest.InstructionTemplate, opt => opt.MapFrom(src => new PaperTemplateDto(src.InstructionTemplate.Id, src.InstructionTemplate.Name, src.InstructionTemplate.Content)))
                .ForMember(dest => dest.OtherInstructionTemplate, opt => opt.MapFrom(src => new PaperTemplateDto(src.OtherInstructionTemplate.Id, src.OtherInstructionTemplate.Name, src.OtherInstructionTemplate.Content)))
                .ForMember(dest => dest.DisclaimerTemplate, opt => opt.MapFrom(src => new PaperTemplateDto(src.DisclaimerTemplate.Id, src.DisclaimerTemplate.Name, src.DisclaimerTemplate.Content)))
                .ForMember(dest => dest.ResultTemplate, opt => opt.MapFrom(src => new PaperTemplateDto(src.ResultTemplate.Id, src.ResultTemplate.Name, src.ResultTemplate.Content)))
                .ForMember(dest => dest.CertificateTemplate, opt => opt.MapFrom(src => new PaperTemplateDto(src.CertificateTemplate.Id, src.CertificateTemplate.Name, src.CertificateTemplate.Content)))
                .ForMember(dest => dest.RuleTemplate, opt => opt.MapFrom(src => new PaperTemplateDto(src.RuleTemplate.Id, src.RuleTemplate.Name, src.RuleTemplate.Content)));

            CreateMap<Subject, PaperSubjectDto>();

            CreateMap<MarkingScheme, PaperMarkingSchemeDto>();

            CreateMap<QuestionUploadTemplateDto, QuestionUploadTemplate>()
                .ForMember(dest => dest.SelectedTypeIdsJson, opt => opt.MapFrom(src => string.Join(",", src.SelectedTypeIds)))
                .ForMember(dest => dest.DifficultyLevelId, opt => opt.MapFrom(src => src.DifficultyLevelId))
                .ReverseMap();

            CreateMap<QuestionUploadTemplate, QuestionUploadTemplateDto>()
                .AfterMap((src, dest) =>
                {
                    dest.SelectedTypeIds = string.IsNullOrEmpty(src.SelectedTypeIdsJson)
                        ? new List<long>()
                        : src.SelectedTypeIdsJson
                             .Split(',')
                             .Select(x => long.TryParse(x, out var val) ? val : 0)
                             .Where(x => x > 0)
                             .ToList();
                });

            CreateMap<QuestionsChoices, QuestionChoiceDto>();

            CreateMap<QuestionMetadata, QuestionSyncResponseDto>()
                .ForMember(dest => dest.OriginalQuestionId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PaperFormId, opt => opt.Ignore())
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
                .ForMember(dest => dest.IsRoot, opt => opt.MapFrom(src => src.IsRoot))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.QuestionType.Name))
                .ForMember(dest => dest.IsAutoCorrectable, opt => opt.MapFrom(src => src.QuestionType.IsAutoCorrectable))
                .ForMember(dest => dest.Layout, opt => opt.MapFrom(src => src.QLayout.Name))
                .ForMember(dest => dest.Subject, opt => opt.MapFrom(src => src.Subject.Name))
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault().Body))
                .ForMember(dest => dest.Instructions, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault().Instructions))
                .ForMember(dest => dest.AttachmentFileName, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault().AttachmentFileName))
                .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault().Language.Name))
                .ForMember(dest => dest.ChoicesShuffled, opt => opt.MapFrom(src => src.QuestionDetails.FirstOrDefault().HasShuffled));

            // Exam Server Profiles

            CreateMap<PaperMetadataTemplate, PaperMetadataTemplateDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            CreateMap<QuestionInstructionTemplate, QuestionInstructionTemplateDto>().ReverseMap();

            CreateMap<PaperForm, FormListDto>()
                .ForMember(dest => dest.PaperId, opt => opt.MapFrom(src => src.PaperId))
                .ForMember(dest => dest.PaperStatus, opt => opt.MapFrom(src => src.Paper.PaperStatus))
                .ForMember(dest => dest.PaperSubType, opt => opt.MapFrom(src => src.Paper.QuestionSelectionType))
                .ForMember(dest => dest.FormQuestionsCount, opt => opt.MapFrom(src => src.FormQuestions.Count))
                .ForMember(dest => dest.UsedQuestion,
                           opt => opt.MapFrom(src => src.Paper.QuestionSelectionType == QuestionSelectionType.Manual
                                                ? src.FormQuestions.Where(e => e.FormQuestionStatus == PaperQuestionStatus.Used).Sum(e => e.Question.QuestionTypeId == (int)Helper.Enums.QuestionType.Comprehension && e.Question.SubQuestions != null ? e.Question.SubQuestions.Count : 1)
                                                : src.FormQuestions.Count(e => e.FormQuestionStatus == PaperQuestionStatus.Used)))
                .ForMember(dest => dest.UnusedQuestion,
                           opt => opt.MapFrom(src => src.Paper.QuestionSelectionType == QuestionSelectionType.Manual
                                                ? src.FormQuestions.Where(e => e.FormQuestionStatus == PaperQuestionStatus.Hanged).Sum(e => e.Question.QuestionTypeId == (int)Helper.Enums.QuestionType.Comprehension && e.Question.SubQuestions != null ? e.Question.SubQuestions.Count : 1)
                                                : src.FormQuestions.Count(e => e.FormQuestionStatus == PaperQuestionStatus.Hanged)))
                .ReverseMap();

            CreateMap<GeneratedFormQuestion, FormQuestionDetailedDto>()
                .ForMember(dest => dest.FormName, opt => opt.MapFrom(src => src.Form.Name))
                .ForMember(dest => dest.FormDescription, opt => opt.MapFrom(src => src.Form.Description))
                .ForMember(dest => dest.FormCode, opt => opt.MapFrom(src => src.Form.Code))
                .ForMember(dest => dest.QuestionId, opt => opt.MapFrom(src => src.QuestionId))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Question.Code))
                .ForMember(dest => dest.QuestionTypeId, opt => opt.MapFrom(src => src.Question.QuestionTypeId))
                .ForMember(dest => dest.QuestionsExhaustionCount, opt => opt.MapFrom(src => src.Question.QuestionsExhaustionCount))
                .ForMember(dest => dest.CurrentExhaustionCount, opt => opt.MapFrom(src => src.Question.CurrentExhaustionCount))
                .ForMember(dest => dest.DifficultyProfileId, opt => opt.MapFrom(src => src.Question.DifficultyProfileId))
                .ForMember(dest => dest.DifficultyLevelId, opt => opt.MapFrom(src => src.Question.DifficultyLevelId))
                .ForMember(dest => dest.DifficultyLevelName, opt => opt.MapFrom(src => src.Question.DifficultyLevel.Name))
                .ForMember(dest => dest.ItembankId, opt => opt.MapFrom(src => src.Question.ItemBankId))
                .ForMember(dest => dest.ItemBankName, opt => opt.MapFrom(src => src.Question.ItemBank.Name))
                .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score))
                .ForMember(dest => dest.PaperQuestionStatus, opt => opt.MapFrom(src => src.FormQuestionStatus))
                .ForMember(dest => dest.SubQuestionsCount,
                           opt => opt.MapFrom(src => src.Question != null &&
                                              src.Question.QuestionTypeId == (int)Helper.Enums.QuestionType.Comprehension &&
                                              src.Question.SubQuestions != null ? src.Question.SubQuestions.Count : 1))
                .ReverseMap();

            CreateMap<FileUploadResponseSettings, FileUploadSettingsResponseDto>().ReverseMap();

            // Quality Check Committee Mappings
            CreateMap<QualityCheckCommittee, QualityCheckCommitteeDto>()
                .ForMember(dest => dest.ChiefName, opt => opt.MapFrom(src => src.Chief != null ? src.Chief.Username : null))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.ChiefEmail, opt => opt.MapFrom(src => src.Chief != null ? src.Chief.EmailAddress : null))
                .ForMember(dest => dest.Members, opt => opt.MapFrom(src => src.Members))
                .ForMember(dest => dest.ItemBanks, opt => opt.MapFrom(src => src.QualityCheckCommitteeItemBanks.Where(link => link.ItemBank != null).Select(link => new QualityCheckItemBankDto { Id = link.ItemBank.Id, Name = link.ItemBank.Name })));

            CreateMap<QualityCheckCommitteeMember, QualityCheckCommitteeMemberDto>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.User.Id))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.User.Username))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.EmailAddress))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
            // Quality Check Committee Mappings

            CreateMap<Disability, AddOrUpdateDisabilityDto>()
              .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
              .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
              .ForMember(dest => dest.ExtraTimePercentage, opt => opt.MapFrom(src => src.ExtraTimePercentage))
              .ReverseMap();
        }
    }
}