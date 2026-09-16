using DocumentFormat.OpenXml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.PagesEndpointsRolesDtos;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;
using SharedHelper.General;
using System.Net;
using System.Reflection;
using Attribute = OES.Core.Entities.Paper.Attribute;
using QuestionType = OES.Core.Entities.QuestionType;

namespace OES.Services.Services
{
    public class SeederService(ICommonService _commonService, IPathsService _pathsService, IUnitOfWork _unitOfWork, IConfiguration _configuration) : ISeederService
    {
        private const string LayoutComponentDefaultName = "Default Layout";

        public async Task<ApiResponse> SeedPages(List<PageDTO> ListOfPages)
        {
            var AllPages = await _commonService._unitOfWork.Repository<Page, long>().GetAllAsync(e => !e.IsDeleted);

            List<Page> AddedPages = [];

            try
            {
                foreach (var page in ListOfPages)
                {
                    if (!AllPages.Any(e => e.Name == page.Name))
                    {
                        AddedPages.Add(new Page()
                        {
                            Name = page.Name
                        });

                    }
                }
                if (AddedPages.Count > 0)
                {
                    _commonService._unitOfWork.Repository<Page, long>().AddRangAsync(AddedPages);

                    if (await _commonService._unitOfWork.Complete() > 0)
                    {
                        return _commonService._apiResponse.GetApiResponse(
                            CustomCodeStatus.Success,
                            HttpStatusCode.Created,
                            Resource.NewPagesAddedSuccssefully,
                            AddedPages
                        );
                    }
                    else
                    {
                        return _commonService._apiResponse.GetApiResponse(
                            CustomCodeStatus.SomethingWentWrong,
                            HttpStatusCode.NotFound,
                            Resource.SomethingWentWrong
                        );
                    }
                }

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.NoNewPagesToAdd
                );
            }
            catch (Exception)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.SomethingWentWrong,
                    HttpStatusCode.InternalServerError,
                    Resource.SomethingWentWrong
                );
            }
        }

        public async Task<bool> SeedApiEndpoints()
        {
            var apiEndpoints = _pathsService.GetControllerPaths().ToList();

            var allEndpoints = await _commonService._unitOfWork.Repository<ApiEndpoint, long>().GetAllAsync(e => !e.IsDeleted);

            List<ApiEndpoint> addedEndpoints = [];

            foreach (var endpoint in apiEndpoints)
            {
                if (!allEndpoints.Any(e => e.Name == endpoint.Name))
                {
                    addedEndpoints.Add(new ApiEndpoint()
                    {
                        Name = endpoint.Name,
                        CreationUser = DefaultSystemUser.Name
                    });
                }
            }

            _commonService._unitOfWork.Repository<ApiEndpoint, long>().AddRangAsync(addedEndpoints);

            return await _commonService._unitOfWork.Complete() > 0;
        }

        public async Task SeedPredefinedTemplatesAsync()
        {
            var roleRepo = _unitOfWork.Repository<OESRole, long>();
            var resourceRepo = _unitOfWork.Repository<OESResource, long>();
            var groupRepo = _unitOfWork.Repository<OESGroup, Guid>();

            var allRoles = typeof(OesTemplateRoleConstants)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Select(f => f.GetValue(null)?.ToString())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingRoles = await roleRepo.GetAll().ToListAsync();

            var missingRoles = allRoles
                .Where(r => !existingRoles.Any(er => er.Name.Equals(r, StringComparison.OrdinalIgnoreCase)))
                .Select(r => new OESRole
                {
                    Name = r,
                    CreationUser = DefaultSystemUser.Name
                })
                .ToList();

            if (missingRoles.Count > 0)
            {
                roleRepo.AddRangAsync(missingRoles);
                await _unitOfWork.Complete();
            }

            existingRoles = await roleRepo.GetAll().ToListAsync();

            var modules = new List<(ResourceType type, string prefix)>
            {
                (ResourceType.Questions, ResourceType.Questions.GetRolePrefix()),
                (ResourceType.Papers, ResourceType.Papers.GetRolePrefix()),
                (ResourceType.Schedule, ResourceType.Schedule.GetRolePrefix()),
                (ResourceType.Ilo, ResourceType.Ilo.GetRolePrefix()),
                (ResourceType.ItemBank, ResourceType.ItemBank.GetRolePrefix()),
                (ResourceType.Result, ResourceType.Result.GetRolePrefix()),
                (ResourceType.Candidate, ResourceType.Candidate.GetRolePrefix()),
                (ResourceType.FileManger, ResourceType.FileManger.GetRolePrefix()),
                (ResourceType.Configurations, ResourceType.Configurations.GetRolePrefix()),
                (ResourceType.Report, ResourceType.Report.GetRolePrefix()),
            };

            foreach (var (resourceType, prefix) in modules)
            {
                List<OESRole> moduleRoles;

                if (resourceType == ResourceType.Configurations)
                {
                    var configurationRoles = PageRoleMap.Map[ResourceType.Configurations]
                        .SelectMany(x => x.Roles)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    moduleRoles = [.. existingRoles.Where(r => configurationRoles.Contains(r.Name))];
                }
                else
                {
                    moduleRoles = [.. existingRoles.Where(r => r.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))];
                }

                if (moduleRoles.Count == 0)
                    continue;

                var resourceName = resourceType.ToString();
                var resource = await resourceRepo.GetObjAsync(x => x.Name == resourceName && !x.IsDeleted);

                if (resource == null)
                {
                    resource = new OESResource
                    {
                        Name = resourceName,
                        Description = $"Resource for managing {resourceName} module.",
                        CreationUser = DefaultSystemUser.Name
                    };
                }

                var templateGroupName = $"MasterGroup-{resourceType}-Admin-Group";

                var existingGroup = await groupRepo.GetObjAsync(x => x.Name == templateGroupName && !x.IsDeleted);
                if (existingGroup != null)
                    continue;

                var newGroup = new OESGroup
                {
                    Name = templateGroupName,
                    Description = $"Master Group for {resourceType}",
                    CreationUser = DefaultSystemUser.Name,
                    IsTemplate = false,
                    IsPredefined = true,

                    GroupResources =
                    [
                        new OESGroupResource
                        {
                            ResourceType = resourceType,
                            Resource = resource,
                            CreationUser = DefaultSystemUser.Name,
                            ResourceRoles = moduleRoles.ConvertAll(r => new OESGroupResourceRole
                            {
                                RoleId = r.Id,
                                CreationUser = DefaultSystemUser.Name
                            })                        }
                    ]
                };

                await groupRepo.AddAsync(newGroup);
                await _unitOfWork.Complete();
            }
        }

        public async Task<ApiResponse> SeedQuestionTypes()
        {
            var questionTypes = Enum.GetValues(typeof(Helper.Enums.QuestionType))
                                    .Cast<Helper.Enums.QuestionType>()
                                    .Select(qt => qt.ToString())
                                    .ToList();

            var existingTypes = await _commonService
                ._unitOfWork
                .Repository<QuestionType, long>()
                .Query()
                .IgnoreQueryFilters()
                .ToListAsync();

            var newQuestionTypes = questionTypes
                .Where(qt => !existingTypes.Any(et => et.Name == qt))
                .Select(qt =>
                {
                    var enumVal = Enum.Parse<Helper.Enums.QuestionType>(qt);

                    return new QuestionType
                    {
                        Id = Convert.ToInt64(enumVal),
                        Name = qt,
                        IsAutoCorrectable = enumVal == Helper.Enums.QuestionType.MCQ ||
                                            enumVal == Helper.Enums.QuestionType.TrueAndFalse ||
                                            enumVal == Helper.Enums.QuestionType.MultipleCorrectAnswers ||
                                            enumVal == Helper.Enums.QuestionType.FillInTheBlank ||
                                            enumVal == Helper.Enums.QuestionType.MatchingPairs ||
                                            enumVal == Helper.Enums.QuestionType.Ordering ||
                                            enumVal == Helper.Enums.QuestionType.HotSpotWithClicks ||
                                            enumVal == Helper.Enums.QuestionType.HotSpotWithDragDrop ||
                                            enumVal == Helper.Enums.QuestionType.WebSearch ||
                                            enumVal == Helper.Enums.QuestionType.Email ||
                                            enumVal == Helper.Enums.QuestionType.WebRegistration ||
                                            enumVal == Helper.Enums.QuestionType.MatchingPairsWithDragDrop,
                        CreationUser = DefaultSystemUser.Name
                    };
                })
                .ToList();

            if (newQuestionTypes.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionType, long>()
                    .AddRangAsync(newQuestionTypes);

                if (await _commonService._unitOfWork.Complete() > 0)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.OK,
                        Resource.QuestionTypesAddedSuccessfully
                    );
                }
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Failure,
                HttpStatusCode.InternalServerError,
                Resource.SomethingWentWrong
            );
        }

        public async Task ApplyQuestionTypeExclusionsAsync()
        {
            var questionTypesSettings = _configuration.GetSection(nameof(QuestionTypesSettings)).Get<QuestionTypesSettings>();

            var excludedTypes = questionTypesSettings?.QuestionTypeExclusions;

            var excludedList = string.IsNullOrWhiteSpace(excludedTypes)
                ? []
                : excludedTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var allExistingTypes = await _commonService
                ._unitOfWork
                .Repository<QuestionType, long>()
                .Query()
                .IgnoreQueryFilters()
                .ToListAsync();

            bool anyChangeHappened = false;

            foreach (var qt in allExistingTypes)
            {
                var excluded = excludedList.Contains(qt.Name, StringComparer.OrdinalIgnoreCase);

                if (qt.IsActive != !excluded || qt.IsDeleted != excluded)
                {
                    qt.IsActive = !excluded;
                    qt.IsDeleted = excluded;
                    anyChangeHappened = true;
                }
            }

            if (anyChangeHappened)
            {
                await _commonService._unitOfWork.Complete();
            }
        }

        public async Task<ApiResponse> SeedQuestionLayout()
        {
            var questionLayouts = Enum.GetNames<LayoutOrientation>().ToList();

            var existingLayouts = await _commonService
                ._unitOfWork
                .Repository<QuestionLayout, long>()
                .GetAllAsync(ql => !ql.IsDeleted);

            var existingTypes = await _commonService
                ._unitOfWork.Repository<QuestionType, long>()
                .GetAllAsync(ql => !ql.IsDeleted);

            List<QuestionLayout> newQuestionLayout = [];

            foreach (var existingType in existingTypes)
            {
                if (existingType.Name == Helper.Enums.QuestionType.MCQ.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.MinVertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.Essay.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                else if (existingType.Name == Helper.Enums.QuestionType.PaperAnsweredQuestion.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                else if (existingType.Name == Helper.Enums.QuestionType.FillInTheBlank.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);

                    await Task.CompletedTask;
                }
                if (existingType.Name == Helper.Enums.QuestionType.FileUploadResponse.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.HotSpotWithDragDrop.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.HotSpotWithClicks.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.SegmentWithAudioAnswer.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                     !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.SegmentWithVideoAnswer.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                     !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.MultipleCorrectAnswers.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.MinVertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.TrueAndFalse.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString()) &&
                                     !existingLayouts.Any(et => et.Name == ql))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.Comprehension.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.VerticalNext.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString() ||
                                      ql == LayoutOrientation.HorizontalNext.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.MatchingPairs.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => ql == LayoutOrientation.Vertical.ToString() &&
                                     !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.MatchingPairsWithDragDrop.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => ql == LayoutOrientation.Vertical.ToString() &&
                                     !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.Ordering.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.WebSearch.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString()) &&
                                     !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.Email.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString() ||
                                      ql == LayoutOrientation.Horizontal.ToString()) &&
                                      !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.Segment.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => (ql == LayoutOrientation.Vertical.ToString()) &&
                                     !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
                if (existingType.Name == Helper.Enums.QuestionType.WebRegistration.ToString())
                {
                    var q = questionLayouts
                        .Where(ql => ql == LayoutOrientation.Vertical.ToString() &&
                                     !existingLayouts.Any(et => et.Name == ql && et.QuestionTypeId == existingType.Id))
                        .Select(ql => new QuestionLayout
                        {
                            Name = ql,
                            CreationUser = DefaultSystemUser.Name,
                            QuestionTypeId = existingType.Id,
                            ComponentName = LayoutComponentDefaultName
                        })
                        .ToList();

                    newQuestionLayout.AddRange(q);
                }
            }

            if (newQuestionLayout.Any())
            {
                _commonService
                    ._unitOfWork
                    .Repository<QuestionLayout, long>()
                    .AddRangAsync(newQuestionLayout);

                if (await _commonService._unitOfWork.Complete() > 0)
                {
                    return _commonService
                        ._apiResponse
                        .GetApiResponse(CustomCodeStatus.Success,
                                        HttpStatusCode.OK,
                                        Resource.QuestionTypesAddedSuccessfully);
                }
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Failure,
                                HttpStatusCode.InternalServerError,
                                Resource.SomethingWentWrong);
        }

        public async Task<bool> SeedItemBankLevel()
        {
            var Levels = typeof(ItemBankLevelEnum)
             .GetFields(BindingFlags.Public | BindingFlags.Static)
             .Select(field => field.GetValue(null).ToString())
             .ToList();

            var existingLevel = await _unitOfWork
                .Repository<ItemBankLevel, long>()
                .GetAllAsync();

            List<ItemBankLevel> ItemBankLevel = new();

            foreach (var level in Levels)
            {
                if (!existingLevel.Any(c => c.Name == level))
                    ItemBankLevel.Add(new ItemBankLevel { Name = level, CreationUser = DefaultSystemUser.Name });
            }

            if (ItemBankLevel.Count > 0)
            {
                _unitOfWork
                    .Repository<ItemBankLevel, long>()
                    .AddRangAsync(ItemBankLevel);

                return await _unitOfWork.Complete() > 0;
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> SeedDeltaTypesAsync()
        {
            var predefinedDeltaTypes = typeof(DeltaTypes)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(field => field.GetValue(null).ToString())
                .ToList();

            var existingDeltaTypes = await _unitOfWork
                .Repository<DeltaType, long>()
                .GetAllAsync();

            List<DeltaType> deltaTypesToBeAdded = [];

            foreach (var predefinedDeltaTypeName in predefinedDeltaTypes)
            {
                if (!existingDeltaTypes.Any(c => c.Name == predefinedDeltaTypeName))
                {
                    deltaTypesToBeAdded.Add(new DeltaType
                    {
                        Name = predefinedDeltaTypeName,
                        CreationUser = DefaultSystemUser.Name
                    });
                }
            }

            if (deltaTypesToBeAdded.Count > 0)
            {
                _unitOfWork
                    .Repository<DeltaType, long>()
                    .AddRangAsync(deltaTypesToBeAdded);

                return await _unitOfWork.Complete() > 0;
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> SeedDifficultyProfileWithDifficultyLevels()
        {
            // Ensure Delta Types are seeded first:
            await SeedDeltaTypesAsync();

            var seededDifficultyProfile = await _unitOfWork
                .Repository<DifficultyProfile, long>()
                .GetObjAsync(x => x.Name.Trim().ToLower() == SeederStandardDifficultyProfileConstant.Name.Trim().ToLower());

            if (seededDifficultyProfile != null)
                return true;

            var deltaTypes = await _unitOfWork
                .Repository<DeltaType, long>()
                .GetAllAsync();

            var predefinedDifficultyLevels = new List<DifficultyLevel>();

            SeederStandardDifficultyProfileConstant.DifficultyLevels.ForEach(x =>
            {
                var deltaTypeId = deltaTypes.First(d => d.Name == x.DeltaType.ToString()).Id;

                predefinedDifficultyLevels.Add(new DifficultyLevel
                {
                    Name = x.Name,
                    FromDelta = x.FromDelta,
                    ToDelta = x.ToDelta,
                    DeltaTypeId = deltaTypeId,
                    CreationUser = DefaultSystemUser.Name,
                    IsDeleted = false
                });
            });

            var predefinedDifficultyProfile = new DifficultyProfile
            {
                Name = SeederStandardDifficultyProfileConstant.Name,
                Description = SeederStandardDifficultyProfileConstant.Description,
                CreationUser = DefaultSystemUser.Name,
                IsDeleted = false,
                DifficultyLevels = predefinedDifficultyLevels
            };

            await _unitOfWork.Repository<DifficultyProfile, long>().AddAsync(predefinedDifficultyProfile);

            return await _unitOfWork.Complete() > 0;
        }

        public async Task<bool> SeedLanguages()
        {
            var existingLanguages = _unitOfWork.Repository<Language, long>().GetAll().AsNoTracking();

            var newLanguagesNamesToBeAdded = SeederLanguageConstant.PredefinedLanguages.ExceptBy(existingLanguages.Select(x => x.Name), l => l.Name).ToList();

            var newLanguagesToBeAdded = new List<Language>();

            newLanguagesNamesToBeAdded.ForEach(x =>
            {
                newLanguagesToBeAdded.Add(new Language
                {
                    Name = x.Name,
                    LanguageDirection = x.Direction,
                    CreationUser = DefaultSystemUser.Name,
                    IsDeleted = false
                });
            });

            if (newLanguagesToBeAdded.Count > 0)
            {
                _unitOfWork.Repository<Language, long>().AddRangAsync(newLanguagesToBeAdded);

                return await _unitOfWork.Complete() > 0;
            }

            return true;
        }

        public async Task<bool> SeedSubjects()
        {
            var existingSubjects = _unitOfWork.Repository<Subject, long>().GetAll().AsNoTracking();

            var newSubjectsNamesToBeAdded = SeederSubjectConstant.PredefinedSubjects.ExceptBy(existingSubjects.Select(x => x.Name), s => s).ToList();

            var newSubjectsToBeAdded = new List<Subject>();

            newSubjectsNamesToBeAdded.ForEach(x =>
            {
                newSubjectsToBeAdded.Add(new Subject
                {
                    Name = x,
                    CreationUser = DefaultSystemUser.Name,
                    IsDeleted = false
                });
            });

            if (newSubjectsToBeAdded.Count > 0)
            {
                _unitOfWork.Repository<Subject, long>().AddRangAsync(newSubjectsToBeAdded);

                return await _unitOfWork.Complete() > 0;
            }

            return true;
        }

        public async Task<bool> SeedSuperAdmin()
        {
            var appUserProfiles = new AppUserProfile
            {
                Id = Guid.Parse(SuperAdminData.Id),
                Username = SuperAdminData.UserName,
                EmailAddress = SuperAdminData.Email,
                CreationUser = SuperAdminData.CreationUser,
                IsActive = SuperAdminData.IsActive,
                IsDeleted = SuperAdminData.IsDeleted,
                OrganizationSignature = SuperAdminData.OrganizationSignature,
                OrganizationId = SuperAdminData.OrganizationId
            };

            if (!await _unitOfWork.Repository<AppUserProfile, Guid>().IsExistAsync(c => c.EmailAddress == appUserProfiles.EmailAddress))
            {
                await _unitOfWork.Repository<AppUserProfile, Guid>().AddAsync(appUserProfiles);

                return await _unitOfWork.Complete() > 0;
            }
            else
            {
                return true;
            }
        }

        public async Task<bool> SeedTemplateTypesAttributesAsync()
        {
            await SeedTemplateTypesAsync();

            await SeedAttributesAsync();

            var baseTemplateTypesIds = Enum.GetValues<TemplateTypeEnum>()
                .Select(x => (long)x)
                .ToList();

            var baseAttributesIds = Enum.GetValues<AttributeEnum>()
                .Select(x => (long)x)
                .ToList();

            var baseTemplateTypesAttributes = new List<KeyValuePair<long, long>>();

            foreach (var templateTypeId in baseTemplateTypesIds)
            {
                foreach (var attributeId in baseAttributesIds)
                {
                    baseTemplateTypesAttributes.Add(new KeyValuePair<long, long>(templateTypeId, attributeId));
                }
            }

            var existedTemplateTypesAttributes = (await _unitOfWork
                .Repository<TemplateAttribute, long>()
                .GetAllAsync())
                .Select(x => new KeyValuePair<long, long>(x.TemplateTypeId, x.AttributeId));

            var templateTypesAttributesDifferenceSet = baseTemplateTypesAttributes
                .Except(existedTemplateTypesAttributes)
                .Select(pair => new TemplateAttribute
                {
                    TemplateTypeId = pair.Key,
                    AttributeId = pair.Value,
                    CreationUser = DefaultSystemUser.Name
                })
                .ToList();

            if (templateTypesAttributesDifferenceSet.Count > 0)
            {
                _unitOfWork
                    .Repository<TemplateAttribute, long>()
                    .AddRangAsync(templateTypesAttributesDifferenceSet);

                return await _unitOfWork.Complete() > 0;
            }

            return true;
        }

        private async Task<bool> SeedTemplateTypesAsync()
        {
            var baseTemplateTypes = Enum.GetNames<TemplateTypeEnum>();

            var existingTemplateTypes = await _unitOfWork
                .Repository<TemplateType, long>()
                .GetAllAsync();

            var templateTypesDifferenceSet = baseTemplateTypes
                .Except(existingTemplateTypes.Select(x => x.Type))
                .Select(x => new TemplateType
                {
                    Id = Convert.ToInt64(Enum.Parse<TemplateTypeEnum>(x)),
                    Type = x,
                    CreationUser = DefaultSystemUser.Name
                })
                .ToList();

            if (templateTypesDifferenceSet.Count > 0)
            {
                _unitOfWork
                    .Repository<TemplateType, long>()
                    .AddRangAsync(templateTypesDifferenceSet);

                return await _unitOfWork.Complete() > 0;
            }
            else
            {
                return true;
            }
        }

        private async Task<bool> SeedAttributesAsync()
        {
            var baseAttributes = Enum.GetNames<AttributeEnum>();

            var existingAttributes = await _unitOfWork
                .Repository<Attribute, long>()
                .GetAllAsync();

            var attributesDifferenceSet = baseAttributes
                .Except(existingAttributes.Select(x => x.Name))
                .Select(x => new Attribute
                {
                    Id = Convert.ToInt64(Enum.Parse<AttributeEnum>(x)),
                    Name = x,
                    CreationUser = DefaultSystemUser.Name
                })
                .ToList();

            if (attributesDifferenceSet.Count > 0)
            {
                _unitOfWork
                    .Repository<Attribute, long>()
                    .AddRangAsync(attributesDifferenceSet);

                return await _unitOfWork.Complete() > 0;
            }
            else
            {
                return true;
            }
        }

        public async Task<ApiResponse> SeedMediaSettings()
        {
            var existingSettings = await _unitOfWork.Repository<MediaSetting, long>().GetAllAsync(e => !e.IsDeleted);

            List<MediaSetting> newSettings = [];

            if (!existingSettings.Any(e => e.MediaCategory == MediaCategory.Image))
            {
                newSettings.Add(new MediaSetting(MediaCategory.Image, 5)
                {
                    CreationUser = DefaultSystemUser.Name,
                    CreationDate = DateTimeHelper.Now,
                    IsActive = true,
                    IsDeleted = false
                });
            }

            if (!existingSettings.Any(e => e.MediaCategory == MediaCategory.Audio))
            {
                newSettings.Add(new MediaSetting(MediaCategory.Audio, 10)
                {
                    CreationUser = DefaultSystemUser.Name,
                    CreationDate = DateTimeHelper.Now,
                    IsActive = true,
                    IsDeleted = false
                });
            }

            if (!existingSettings.Any(e => e.MediaCategory == MediaCategory.Video))
            {
                newSettings.Add(new MediaSetting(MediaCategory.Video, 20)
                {
                    CreationUser = DefaultSystemUser.Name,
                    CreationDate = DateTimeHelper.Now,
                    IsActive = true,
                    IsDeleted = false
                });
            }

            if (newSettings.Count > 0)
            {
                await _unitOfWork.Repository<MediaSetting, long>().AddRangeAsync(newSettings);

                if (await _unitOfWork.Complete() > 0)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.Success,
                        HttpStatusCode.Created,
                        Resource.DataAddedSuccessfully,
                        newSettings
                    );
                }
                else
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.SomethingWentWrong,
                        HttpStatusCode.InternalServerError,
                        Resource.SomethingWentWrong
                    );
                }
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.NoChangesToApply
            );
        }

        public async Task SeedCBTSyncSettingAsync()
        {
            var exists = await _unitOfWork
                .Repository<CBTSyncSetting, long>()
                .GetObjAsync(s => !s.IsDeleted);

            if (exists != null)
                return;

            var setting = new CBTSyncSetting
            {
                CBTAutoSyncEnabled = true,
                CBTSyncScheduleTime = "10:00",
                CreationUser = DefaultSystemUser.Name,
                CreationDate = DateTime.Now,
                IsActive = true
            };

            await _unitOfWork.Repository<CBTSyncSetting, long>().AddAsync(setting);

            await _unitOfWork.Complete();
        }

        public async Task SeedPageRolesAsync()
        {
            var pageRepo = _unitOfWork.Repository<Page, long>();
            var roleRepo = _unitOfWork.Repository<OESRole, long>();
            var pageRoleRepo = _unitOfWork.Repository<PageRoles, long>();

            var allRoles = await roleRepo.GetAll().Where(r => !r.IsDeleted).ToListAsync();
            var existingPageRoles = await pageRoleRepo.GetAll()
                .Where(pr => !pr.IsDeleted)
                .Select(pr => new { pr.PageId, pr.RoleId })
                .ToListAsync();

            var pageRolesMap = new Dictionary<string, string[]>
            {
                ["AddItemBank"] = [OesTemplateRoleConstants.ItemBankCreator, OesTemplateRoleConstants.ItemBankQuestionCreator],
                ["ItemBankList"] = [OesTemplateRoleConstants.ItemBankViewer, OesTemplateRoleConstants.ItemBankCreator, OesTemplateRoleConstants.ItemBankEditor, OesTemplateRoleConstants.ItemBankDeleter, OesTemplateRoleConstants.ItemBankQuestionCreator, OesTemplateRoleConstants.ItemBankQuestionDeleter, OesTemplateRoleConstants.ItemBankQuestionEditor, OesTemplateRoleConstants.ItemBankQuestionReplacer, OesTemplateRoleConstants.ItemBankQuestionViewer],
                ["ItemBankTreeView"] = [OesTemplateRoleConstants.ItemBankViewer, OesTemplateRoleConstants.ItemBankCreator, OesTemplateRoleConstants.ItemBankEditor, OesTemplateRoleConstants.ItemBankDeleter],
                ["QuestionList"] = [OesTemplateRoleConstants.Questionviewer, OesTemplateRoleConstants.QuestionCreator, OesTemplateRoleConstants.QuestionEditor, OesTemplateRoleConstants.QuestionDeleter, OesTemplateRoleConstants.QuestionCopier, OesTemplateRoleConstants.QuestionReplacer, OesTemplateRoleConstants.ItemBankQuestionCreator, OesTemplateRoleConstants.ItemBankQuestionDeleter, OesTemplateRoleConstants.ItemBankQuestionEditor, OesTemplateRoleConstants.ItemBankQuestionReplacer, OesTemplateRoleConstants.ItemBankQuestionViewer],
                ["QuestionCreation"] = [OesTemplateRoleConstants.QuestionCreator, OesTemplateRoleConstants.QuestionEditor, OesTemplateRoleConstants.QuestionReplacer, OesTemplateRoleConstants.ItemBankQuestionCreator, OesTemplateRoleConstants.ItemBankQuestionDeleter, OesTemplateRoleConstants.ItemBankQuestionEditor, OesTemplateRoleConstants.ItemBankQuestionReplacer, OesTemplateRoleConstants.ItemBankQuestionViewer],
                ["UploadFile"] = [OesTemplateRoleConstants.QuestionFileUpload],
                ["ExportFiles"] = [OesTemplateRoleConstants.QuestionExport],
                ["QuestionDeltaUpload"] = [OesTemplateRoleConstants.QuestionDeltaUpload],
                ["QuestionsQualityCheckList"] = [OesTemplateRoleConstants.QuestionQualityChecker, OesTemplateRoleConstants.ItemBankQuestionQualityChecker],
                ["QuestionsQualityPassingChecklist"] = [OesTemplateRoleConstants.QuestionQualityCheckBypass],
                ["UserPapersList"] = [OesTemplateRoleConstants.Paperviewer, OesTemplateRoleConstants.PaperCreator, OesTemplateRoleConstants.PaperEditor, OesTemplateRoleConstants.PaperDeleter],
                ["PaperStepper"] = [OesTemplateRoleConstants.PaperCreator],
                ["BlocksPaginatedList"] = [OesTemplateRoleConstants.BlockViewer, OesTemplateRoleConstants.BlockCreator, OesTemplateRoleConstants.BlockEditor, OesTemplateRoleConstants.BlockDeleter],
                ["CreateNewBlock"] = [OesTemplateRoleConstants.BlockCreator],
                ["EditBlock"] = [OesTemplateRoleConstants.BlockEditor],
                ["EquationTemplateList"] = [OesTemplateRoleConstants.EquationViewer, OesTemplateRoleConstants.EquationCreator, OesTemplateRoleConstants.EquationEditor, OesTemplateRoleConstants.EquationDeleter],
                ["AddEquationTemplate"] = [OesTemplateRoleConstants.EquationCreator],
                ["UpdateEquationTemplate"] = [OesTemplateRoleConstants.EquationEditor],
                ["ScheduleList"] = [OesTemplateRoleConstants.ScheduleViewer, OesTemplateRoleConstants.ScheduleCreator, OesTemplateRoleConstants.ScheduleEditor, OesTemplateRoleConstants.ScheduleDeleter],
                ["ScheduleCreationStepper"] = [OesTemplateRoleConstants.ScheduleCreator],
                ["SyncStatus"] = [OesTemplateRoleConstants.ScheduleSync, OesTemplateRoleConstants.ScheduleCbtSyncer, OesTemplateRoleConstants.ScheduleCbtViewer, OesTemplateRoleConstants.CandidateAnswersToEvaluationSyncer],
                ["VenueList"] = [OesTemplateRoleConstants.VenueViewer, OesTemplateRoleConstants.VenueCreator, OesTemplateRoleConstants.VenueEditor, OesTemplateRoleConstants.VenueDeleter],
                ["AddNewVenue"] = [OesTemplateRoleConstants.VenueCreator],
                ["VenueEdit"] = [OesTemplateRoleConstants.VenueEditor],
                ["ScheduleSecurityTemplatesList"] = [OesTemplateRoleConstants.ScheduleSecurityTempelate],
                ["CreateScheduleSecurityConfigurationTemplate"] = [OesTemplateRoleConstants.ScheduleSecurityTempelate],
                ["EditScheduleSecurityConfigurationTemplate"] = [OesTemplateRoleConstants.ScheduleSecurityTempelate],
                ["PaperSettingTemplateList"] = [OesTemplateRoleConstants.ScheduleExamSettingsTempelate],
                ["CreatePaperSettingTemplate"] = [OesTemplateRoleConstants.ScheduleExamSettingsTempelate],
                ["EditPaperSettingTemplate"] = [OesTemplateRoleConstants.ScheduleExamSettingsTempelate],
                ["CandidateList"] = [OesTemplateRoleConstants.CandidateList, OesTemplateRoleConstants.CandidateViewer, OesTemplateRoleConstants.CandidateEditor, OesTemplateRoleConstants.CandidateDeleter],
                ["CandidateCreation"] = [OesTemplateRoleConstants.CandidateImport],
                ["CandidateSchedules"] = [OesTemplateRoleConstants.CandidateSchedules],
                ["CandidatesResultList"] = [OesTemplateRoleConstants.CandidateResultView],
                ["AddCandidateExtraTimeList"] = [OesTemplateRoleConstants.CandidateExtraTime],
                ["VerificationCodeGenerator"] = [OesTemplateRoleConstants.CandidateVerificationCodeGenerator],
                ["ResultGeneration"] = [OesTemplateRoleConstants.ResultGenerate],
                ["ResultsStatus"] = [OesTemplateRoleConstants.ResultStatus],
                ["ReviewUnfinished"] = [OesTemplateRoleConstants.ResultReviewUnfinished],
                ["CandidateResults"] = [OesTemplateRoleConstants.ResultCandidateResults],
                ["ILORoots"] = [OesTemplateRoleConstants.IloViewer, OesTemplateRoleConstants.IloCreator, OesTemplateRoleConstants.IloEditor, OesTemplateRoleConstants.IloDeleter],
                ["FileManager"] = [OesTemplateRoleConstants.FileMangerAdmin],
                ["ItemAnalysisReport"] = [OesTemplateRoleConstants.ReportGeneratorViewer, OesTemplateRoleConstants.ReportGenerator],
                ["QuestionIndicators"] = [OesTemplateRoleConstants.ReportQuestionIndicator],
                ["QuestionCategory"] = [OesTemplateRoleConstants.QuestionCategoryViewer, OesTemplateRoleConstants.QuestionCategoryCreator, OesTemplateRoleConstants.QuestionCategoryEditor, OesTemplateRoleConstants.QuestionCategoryDeleter],
                ["DeltaTypeList"] = [OesTemplateRoleConstants.DeltaTypeViewer, OesTemplateRoleConstants.DeltaTypeDeleter],
                ["DifficultyProfile"] = [OesTemplateRoleConstants.DifficultyProfileViewer, OesTemplateRoleConstants.DifficultyProfileCreator, OesTemplateRoleConstants.DifficultyProfileEditor, OesTemplateRoleConstants.DifficultyProfileDeleter],
                ["DifficultyLevelList"] = [OesTemplateRoleConstants.DifficultyLevelViewer, OesTemplateRoleConstants.DifficultyLevelCreator, OesTemplateRoleConstants.DifficultyLevelEditor, OesTemplateRoleConstants.DifficultyLevelDeleter],
                ["Language"] = [OesTemplateRoleConstants.DefinedLanguageCreator, OesTemplateRoleConstants.DefinedLanguageEditor, OesTemplateRoleConstants.DefinedLanguageDeleter],
                ["SubjectList"] = [OesTemplateRoleConstants.DefinedMaterialViewer, OesTemplateRoleConstants.DefinedMaterialCreator, OesTemplateRoleConstants.DefinedMaterialEditor, OesTemplateRoleConstants.DefinedMaterialDeleter],
                ["MediaSettingList"] = [OesTemplateRoleConstants.MediaConfigurationViewer, OesTemplateRoleConstants.MediaConfigurationEditor, OesTemplateRoleConstants.MediaConfigurationDeleter],
                ["QualityCheckCommitteeList"] = [OesTemplateRoleConstants.QualityCheckCommitteeViewer, OesTemplateRoleConstants.QualityCheckCommitteeCreator, OesTemplateRoleConstants.QualityCheckCommitteeEditor],
                ["CBTSyncSettingList"] = [OesTemplateRoleConstants.CbtAutoSettingsViewer, OesTemplateRoleConstants.CbtAutoSettingsEditor],
                ["DisabilitiesList"] = [OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeDeleter, OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeEditor, OesTemplateRoleConstants.SpecialCasesForAddingExtraTimeCreator],
                ["TransitionProfileList"] = [OesTemplateRoleConstants.TransitionProfileViewer, OesTemplateRoleConstants.TransitionProfileCreate, OesTemplateRoleConstants.TransitionProfileEdit, OesTemplateRoleConstants.TransitionProfileDelete],
                ["TransitionLevel"] = [OesTemplateRoleConstants.TransitionLevelViewer, OesTemplateRoleConstants.TransitionLevelCreate, OesTemplateRoleConstants.TransitionLevelEdit, OesTemplateRoleConstants.TransitionLevelDelete],
                ["AuditLogs"] = [OesTemplateRoleConstants.AuditLogsViewer, OesTemplateRoleConstants.AuditLogsExporter],
                ["AddDeltaType"] = [OesTemplateRoleConstants.DeltaTypeCreator],
                ["UpdateDeltaType"] = [OesTemplateRoleConstants.DeltaTypeEditor],
                ["TrackingLogs"] = [OesTemplateRoleConstants.ResultTrackingLogs],
                ["PaperFormExamScreen"] = [OesTemplateRoleConstants.ExamScreenViewer],
                ["PaperViewDetails"] = [OesTemplateRoleConstants.PaperViewFormDetails],
                ["ExamScreenContainer"] = [OesTemplateRoleConstants.ExamScreenViewer, OesTemplateRoleConstants.QuestionExamViewer],
                ["UpdateCBTSyncSetting"] = [OesTemplateRoleConstants.CbtAutoSettingsEditor],
                ["PaperSettingTemplateView"] = [OesTemplateRoleConstants.PaperSettingTemplateViewer],
                ["ScheduleSecurityConfigurationView"] = [OesTemplateRoleConstants.SecurityConfigurationViewer],
                ["TransitionLevelDetails"] = [OesTemplateRoleConstants.TransitionLevelViewer],
                ["CandidateSchedulePapersList"] = [OesTemplateRoleConstants.CandidateSchedulePapersViewer],
            };

            var existingNames = await pageRepo.GetAll().Where(p => !p.IsDeleted).Select(p => p.Name).ToListAsync();

            var missing = pageRolesMap.Keys
                .Where(n => !existingNames.Contains(n, StringComparer.OrdinalIgnoreCase))
                .Select(n => new Page { Name = n, CreationUser = DefaultSystemUser.Name, IsActive = true, IsDeleted = false })
                .ToList();

            if (missing.Count > 0)
            {
                pageRepo.AddRangAsync(missing);
                await _unitOfWork.Complete();
            }

            var allPages = await pageRepo.GetAll().Where(p => !p.IsDeleted).ToListAsync();

            var toInsert = new List<PageRoles>();

            foreach (var (pageName, roleNames) in pageRolesMap)
            {
                var page = allPages.FirstOrDefault(p => p.Name.Equals(pageName, StringComparison.OrdinalIgnoreCase));
                if (page == null) continue;

                foreach (var roleName in roleNames)
                {
                    var role = allRoles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
                    if (role == null) continue;

                    if (existingPageRoles.Any(pr => pr.PageId == page.Id && pr.RoleId == role.Id)) continue;

                    toInsert.Add(new PageRoles
                    {
                        PageId = page.Id,
                        RoleId = role.Id,
                        CreationUser = DefaultSystemUser.Name,
                        IsActive = true,
                        IsDeleted = false,
                    });
                }
            }

            if (toInsert.Count > 0)
            {
                pageRoleRepo.AddRangAsync(toInsert);
                await _unitOfWork.Complete();
            }
        }

        public async Task SeedApiEndpointRolesAsync()
        {
            var allEndpoints = await _unitOfWork
                .Repository<ApiEndpoint, long>()
                .GetAll().Where(e => !e.IsDeleted)
                .ToListAsync();

            var pageNameToRoleIds = await _unitOfWork.Repository<PageRoles, long>()
                .GetAll()
                .Where(pr => !pr.IsDeleted && pr.IsActive)
                .Include(pr => pr.Role)
                .Include(pr => pr.Page)
                .GroupBy(pr => pr.Page.Name)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(pr => pr.RoleId).ToHashSet(),
                    StringComparer.OrdinalIgnoreCase
                );

            var existingPairs = await _unitOfWork.Repository<ApiEndpointRole, long>()
                .GetAll().Where(er => !er.IsDeleted)
                .Select(er => new { er.ApiId, er.RoleId })
                .ToListAsync();

            var existingSet = existingPairs.Select(er => (er.ApiId, er.RoleId)).ToHashSet();

            var authOnlyPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "api/AuditLogs/SaveBatch",
                "api/Dashboard/",
                "api/JobManagement/GetJobsByBatchId",
                "api/ILO/GetRootILO",
                "api/DifficultyProfile/GetProfiles",
                "api/QuestionCategory/GetCategories",
                "api/Subject/GetAllSubjectList"
            };

            var prefixToPageName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["api/Schedule/"] = "ScheduleList",
                ["api/SchedulePaper/"] = "ScheduleList",
                ["api/SecurityConfigurations/"] = "ScheduleSecurityTemplatesList",
                ["api/ExamServer/"] = "SyncStatus",
                ["api/SyncCandidates/"] = "SyncStatus",
                ["api/CBTSyncSetting/"] = "CBTSyncSettingList",
                ["api/Paper/"] = "UserPapersList",
                ["api/Form/"] = "UserPapersList",
                ["api/MarkingScheme/"] = "UserPapersList",
                ["api/Section/"] = "UserPapersList",
                ["api/SectionSummary/"] = "UserPapersList",
                ["api/PaperSettings/"] = "PaperSettingTemplateList",
                ["api/PdfGenerator/"] = "UserPapersList",
                ["api/QueueSuspend/"] = "UserPapersList",
                ["api/Block/"] = "BlocksPaginatedList",
                ["api/EquationTemplate/"] = "EquationTemplateList",
                ["api/TransitionProfile/"] = "TransitionProfileList",
                ["api/TransitionLevel/"] = "TransitionLevel",
                ["api/Question/"] = "QuestionList",
                ["api/QuestionCategory/"] = "QuestionCategory",
                ["api/QuestionType/"] = "QuestionList",
                ["api/QuestionLayout/"] = "QuestionList",
                ["api/QuestionMetadata/"] = "QuestionList",
                ["api/QuestionInstructionTemplate/"] = "QuestionList",
                ["api/QuestionUploadTemplate/"] = "UploadFile",
                ["api/QCommentController/"] = "QuestionsQualityCheckList",
                ["api/QTI/"] = "ExportFiles",
                ["api/Upload/"] = "UploadFile",
                ["api/Template/"] = "QuestionList",
                ["api/ItemBank/"] = "ItemBankList",
                ["api/ItemBankPoint/"] = "ItemBankList",
                ["api/ItemBankLevel/"] = "ItemBankList",
                ["api/ILO/"] = "ILORoots",
                ["api/Venue/"] = "VenueList",
                ["api/Candidate/"] = "CandidateList",
                ["api/CandidateBatchImportHistory/"] = "CandidateCreation",
                ["api/CandidatesResult/"] = "CandidatesResultList",
                ["api/EvaluationSync/"] = "SyncStatus",
                ["api/AnalyticalReports/"] = "ItemAnalysisReport",
                ["api/ResultsReports/"] = "ItemAnalysisReport",
                ["api/VerificationReports/"] = "ItemAnalysisReport",
                ["api/JobManagement/"] = "CandidateCreation",
                ["api/DifficultyLevel/"] = "DifficultyLevelList",
                ["api/DifficultyProfile/"] = "DifficultyProfile",
                ["api/DeltaType/"] = "DeltaTypeList",
                ["api/Subject/"] = "SubjectList",
                ["api/Language/"] = "Language",
                ["api/MediaSetting/"] = "MediaSettingList",
                ["api/QualityCheckCommittee/"] = "QualityCheckCommitteeList",
                ["api/Disability/"] = "DisabilitiesList",
                ["api/OrganizationStructure/"] = "CandidateCreation",
                ["api/Editors/"] = "FileManager",
                ["api/AuditLogs/"] = "AuditLogs",
            };

            bool IsAuthOnly(string name) => authOnlyPrefixes
                .Any(p => name.Equals(p, StringComparison.OrdinalIgnoreCase) || name.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            var toInsert = prefixToPageName
                .Where(kv => pageNameToRoleIds.ContainsKey(kv.Value))
                .SelectMany(kv => allEndpoints
                    .Where(e => e.Name.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase) && !IsAuthOnly(e.Name))
                    .SelectMany(e => pageNameToRoleIds[kv.Value]
                        .Where(roleId => !existingSet.Contains((e.Id, roleId)))
                        .Select(roleId => new ApiEndpointRole
                        {
                            ApiId = e.Id,
                            RoleId = roleId,
                            CreationUser = nameof(System),
                            IsActive = true,
                            IsDeleted = false,
                            CreationDate = DateTime.UtcNow
                        })))
                .ToList();

            if (toInsert.Count > 0)
            {
                _unitOfWork.Repository<ApiEndpointRole, long>().AddRangAsync(toInsert);
                await _unitOfWork.Complete();
            }
        }
    }
}
