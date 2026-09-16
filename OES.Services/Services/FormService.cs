using AutoMapper;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Paper;
using OES.Helper.Dtos.FileUploadResponseSettings;
using OES.Helper.Dtos.Form;
using OES.Helper.Dtos.FormQuestions;
using OES.Helper.Dtos.Paper.Responses;
using OES.Helper.Dtos.Question;
using OES.Helper.Dtos.Question.QuestionDetailsDtos;
using OES.Helper.Dtos.Question.SegmentQuestionDtos;
using OES.Helper.Dtos.QuestionChoices;
using OES.Helper.Dtos.Section;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Data;
using System.Net;

namespace OES.Services.Services
{
    public class FormService(ICommonService _commonService, IMapper _mapper, FilterParamsValues _filterParamsValues) : IFormService
    {
        // GET METHODS:

        public async Task<ApiResponse> GetAllFormsWithTheirQuestionsByPaperIdAsync(long paperId)
        {
            const string includes = $"{nameof(PaperForm.Paper)}.{nameof(PaperMetadata.Language)}," +
                                    $"{nameof(PaperForm.Paper)}.{nameof(PaperMetadata.DifficultyProfile)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.QuestionDetails)}.{nameof(QuestionDetails.QuestionsChoices)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.Subject)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.FileUploadResponseSettings)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.ItemBank)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.QuestionType)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.QuestionCategory)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.SubQuestions)}.{nameof(QuestionMetadata.QuestionDetails)}.{nameof(QuestionDetails.QuestionsChoices)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.SubQuestions)}.{nameof(QuestionMetadata.QuestionType)},";

            var validStatuses = new[] { AvailabilityStatus.Active, AvailabilityStatus.Synced, AvailabilityStatus.Suspended, AvailabilityStatus.Expired };

            var forms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAllAsync(f => f.PaperId == paperId && validStatuses.Contains(f.FormStatus), Including: includes);

            if (forms?.Any() != true)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoFormsFoundForGivenPaper
                );
            }

            var paper = forms.First().Paper;

            if (paper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoPaperMetadataFoundForGivenPaper
                );
            }

            var paperDto = new GetPaperWithFormsDto
            {
                PaperId = paper.Id,
                Name = paper.Name,
                Description = paper.Description,
                Code = paper.Code,
                LanguageDirection = paper.Language.LanguageDirection,
                Abbreviation = paper.Abbreviation,
                QuestionsCount = paper.QuestionsCount,
                Duration = paper.Duration,
                TotalMarks = paper.TotalMarks,
                Type = paper.Type.ToString(),
                Language = paper.Language.Name,
                DifficultyProfile = paper.DifficultyProfile.Name,
                Forms = [.. forms.Select(form => new FormQuestionsDto
                {
                    FormId = form.Id,
                    FormName = form.Name,
                    FormCode = form.Code,
                    FormDescription = form.Description,
                    Questions = form.FormQuestions?.Select(fq => new QuestionWithScoreDto
                    {
                        QuestionId = (long)fq.QuestionId,
                        Score = fq.Score,
                        PaperQuestionStatus = fq.FormQuestionStatus,
                        SectionName = string.Empty,
                        OrderId = 0,
                        IsTimer = false,
                        Metadata = fq.Question != null ? MapQuestionMetadata(fq.Question, paper.LanguageId) : null,
                    }).ToList() ?? [],
                })],
            };

            if (paper.QuestionSelectionType == QuestionSelectionType.Manual)
            {
                var formsIds = forms.Select(f => f.Id).ToList();

                var manualPaperItemBankQuestionSections = await _commonService
                    ._unitOfWork
                    .Repository<ManualPaperItemBankQuestionSection, long>()
                    .GetAllAsync(x => formsIds.Contains(x.Section.FormId ?? 0), Including: $"{nameof(ManualPaperItemBankQuestionSection.Section)}.{nameof(StandardSection.InstructionSectionTemplate)}");

                var manualQuestionSectionLookup = manualPaperItemBankQuestionSections.ToLookup(x => (x.Section.FormId, x.QuestionMetaDataId));

                foreach (var paperForm in paperDto.Forms)
                {
                    paperForm.Questions.ForEach(q =>
                    {
                        var manualQuestionSectionRecord = manualQuestionSectionLookup[(paperForm.FormId, q.QuestionId)].FirstOrDefault();

                        if (manualQuestionSectionRecord != null)
                        {
                            q.SectionName = manualQuestionSectionRecord.Section.Name;
                            q.OrderId = manualQuestionSectionRecord.Section.OrderId;
                            q.IsTimer = manualQuestionSectionRecord.Section.IsRestrictedTime;
                            q.SectionTime = manualQuestionSectionRecord.Section.TimeInMinutes;
                            q.SectionInstruction = manualQuestionSectionRecord.Section.InstructionSectionTemplate?.Name;
                        }
                    });
                }
            }
            else if (paper.QuestionSelectionType == QuestionSelectionType.Auto)
            {
                var autoPaperModelingSections = await _commonService
                    ._unitOfWork
                    .Repository<StandardSection, long>()
                    .GetAllAsync(s => s.PaperId == paperId, Including: $"{nameof(StandardSection.AutoQuestions)},{nameof(StandardSection.InstructionSectionTemplate)}");

                var formsById = forms.ToDictionary(f => f.Id);

                foreach (var paperForm in paperDto.Forms)
                {
                    if (!formsById.TryGetValue(paperForm.FormId, out var form))
                    {
                        continue;
                    }

                    var formQuestionsDict = (form.FormQuestions ?? [])
                        .Where(fq => fq.QuestionId.HasValue && fq.Question != null)
                        .ToDictionary(fq => fq.QuestionId!.Value, fq => fq);

                    var questionDtoById = paperForm.Questions.ToDictionary(q => q.QuestionId);
                    var assignedIds = new HashSet<long>();

                    foreach (var section in autoPaperModelingSections)
                    {
                        var sectionQuestionIds = GetAutoSectionQuestions(section, formQuestionsDict, assignedIds)
                            .Select(q => q.Id)
                            .ToHashSet();

                        foreach (var questionId in sectionQuestionIds)
                        {
                            if (!questionDtoById.TryGetValue(questionId, out var q))
                            {
                                continue;
                            }

                            q.SectionName = section.Name;
                            q.OrderId = section.OrderId;
                            q.IsTimer = section.IsRestrictedTime;
                            q.SectionTime = section.TimeInMinutes;
                            q.SectionInstruction = section.InstructionSectionTemplate?.Name;
                        }
                    }
                }
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.PaperAndFormsRetrievedSuccessfully,
                paperDto
            );
        }

        public async Task<ApiResponse> GetPaginatedFormsByPaperIdAsync(PaginationSearchModel paginationSearchModel, long paperId)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAll(f => f.PaperId == paperId)
                .AsNoTracking()
                .Include(f => f.FormQuestions)
                .ThenInclude(fq => fq.Question)
                .ThenInclude(q => q.SubQuestions)
                .Include(f => f.Paper)
                .AsNoTracking()
                .AsQueryable();

            if (!paginationSearchModel.PaginationOff)
            {
                if (!string.IsNullOrEmpty(paginationSearchModel.SearchKey) && paginationSearchModel.SearchInName)
                {
                    query = query.Where(x => x.Name.Contains(paginationSearchModel.SearchKey));
                }

                if (paginationSearchModel.FromDate is not null)
                {
                    var toDate = (paginationSearchModel.ToDate ?? DateTimeHelper.Now.Date).AddDays(1);
                    query = query.Where(o => o.CreationDate >= paginationSearchModel.FromDate && o.CreationDate < toDate);
                }

                query = paginationSearchModel.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(f => f.Name).ThenByDescending(f => f.Id)
                    : query.OrderBy(f => f.Name).ThenBy(f => f.Id);

                var totalItems = await query.CountAsync();

                var data = await query
                    .Skip(paginationSearchModel.PageIndex * paginationSearchModel.PageSize)
                    .Take(paginationSearchModel.PageSize)
                    .AsNoTracking()
                    .ToListAsync();

                var mappedData = _commonService._mapper.Map<List<FormListDto>>(data);

                await PopulateSuspendedStatusAsync(mappedData);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new CustomTableData<FormListDto>(mappedData, totalItems)
                );
            }
            else
            {
                var data = await (paginationSearchModel.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(f => f.Name).ThenByDescending(f => f.Id).ToListAsync()
                    : query.OrderBy(f => f.Name).ThenBy(f => f.Id).ToListAsync());

                var mappedData = _commonService._mapper.Map<List<FormListDto>>(data);

                await PopulateSuspendedStatusAsync(mappedData);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    null,
                    new CustomTableData<FormListDto>(mappedData, data.Count)
                );
            }
        }

        public async Task<ApiResponse> GetFormByPaperId(long paperId)
        {
            var forms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAllAsync(f => f.PaperId == paperId /* && f.FormStatus == AvailabilityStatus.Active */, asNoTracking: true);

            if (forms?.Any() != true)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoFormsFoundForPaper
                );
            }

            var formDtos = _mapper.Map<List<FormListDto>>(forms);

            await PopulateSuspendedStatusAsync(formDtos);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                formDtos
            );
        }

        private async Task PopulateSuspendedStatusAsync(List<FormListDto> formDtos)
        {
            if (formDtos == null || formDtos.Count == 0) return;

            var formIds = formDtos.ConvertAll(f => f.Id);

            var suspendedFormIds = await _commonService
                ._unitOfWork
                .Repository<FormVenueSuspension, long>()
                .Query()
                .Where(s => formIds.Contains(s.FormId) && s.Status == AvailabilityStatus.Suspended)
                .Select(s => s.FormId)
                .Distinct()
                .ToListAsync();

            var suspendedSet = suspendedFormIds.ToHashSet();

            foreach (var dto in formDtos)
            {
                dto.IsSuspendedInAnyVenue = suspendedSet.Contains(dto.Id);
            }
        }

        public async Task<ApiResponse> GetAllQuestionByFormIdAsync(long formId)
        {
            var formQuestions = await _commonService
                ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAllAsync(
                    fq => fq.FormId == formId,
                    OrderBy: q => q.OrderBy(fq => fq.QuestionId),
                    Including: $"{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.ItemBank)}," +
                    $"{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.DifficultyLevel)}," +
                    $"{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.SubQuestions)}," +
                    $"{nameof(GeneratedFormQuestion.Form)}",
                    asNoTracking: true
                );

            if (formQuestions == null || !formQuestions.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NoQuestionsFound,
                    HttpStatusCode.NotFound,
                    Resource.NoQuestionFound,
                    new List<FormQuestionDetailedDto>()
                );
            }

            var dtoList = _mapper.Map<List<FormQuestionDetailedDto>>(formQuestions);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                dtoList
            );
        }

        public async Task<ApiResponse> GetFormWithQuestionsByFormIdAsync(long formId)
        {
            const string includes = $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.QuestionDetails)}.{nameof(QuestionDetails.QuestionsChoices)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.Subject)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.QuestionType)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.QuestionDetails)}.{nameof(QuestionDetails.SegmentQuestionProperties)}," +
                                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}.{nameof(QuestionMetadata.QuestionCategory)}";

            var form = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetObjAsync(f => f.Id == formId, Including: includes);

            if (form == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoFormFoundForTheGivenID
                );
            }

            var questions = form.FormQuestions.Select(fq => new QuestionWithScoreDto
            {
                QuestionId = (long)fq.QuestionId,
                Score = fq.Score,
                PaperQuestionStatus = fq.FormQuestionStatus,
                Metadata = fq.Question != null ? new QuestionMetadataPaginationDto
                {
                    Id = fq.Question.Id,
                    Code = fq.Question.Code,
                    Subject = fq.Question.Subject?.Name,
                    Type = fq.Question.QuestionType?.Name,
                    Category = fq.Question.QuestionCategory?.Name,
                    MaximumAnswerTime = fq.Question.MaximumAnswerTime.ToString(),
                    QuestionData = [.. fq.Question.QuestionDetails.Select(d => new QuestionDataDto
                    {
                        Id = d.Id,
                        Body = d.Body,
                        Instructions = d.Instructions,
                        QuestionMetadataId = d.QuestionMetadataId,
                        Choices = [.. d.QuestionsChoices.Select(c => new ChoiceDataDto
                        {
                            Id = c.Id,
                            ChoiceText = c.ChoiceText,
                            IsCorrectAnswer = c.IsCorrectAnswer
                        })],
                        ModelAnswer = d.ModelAnswer,
                        LanguageId = d.LanguageId,
                        HasShuffled = d.HasShuffled,
                        SegmentQuestionConfig = d.SegmentQuestionProperties != null ? new SegmentQuestionConfigDto
                        {
                            ThinkingTime = d.SegmentQuestionProperties.ThinkingTime,
                            ResponseTime = d.SegmentQuestionProperties.ResponseTime,
                            WordsCount = d.SegmentQuestionProperties.WordsCount,
                            OrderNumber = d.SegmentQuestionProperties.OrderNumber,
                            HasScore = d.SegmentQuestionProperties.HasScore,
                            SegmentAudioUrl = d.SegmentQuestionProperties.SegmentAudioUrl,
                            SegmentQuestionResponseType = d.SegmentQuestionProperties.SegmentQuestionResponseType
                        } : null
                    })]
                } : null
            });

            var formDto = new FormQuestionsDto
            {
                FormId = form.Id,
                FormName = form.Name,
                Questions = questions.ToList()
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                @Resource.FormandItsquestionsRetrievedSuccessfully,
                formDto
            );
        }

        public async Task<ApiResponse> GetFormSectionsWithQuestionsAsync(long formId)
        {
            var form = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetObjAsync(f => f.Id == formId,
                    Including:
                    $"{nameof(PaperForm.Paper)}," +
                    $"{nameof(PaperForm.Paper)}.{nameof(PaperMetadata.Sections)}," +
                    $"{nameof(PaperForm.FormQuestions)}," +
                    $"{nameof(PaperForm.FormQuestions)}.{nameof(GeneratedFormQuestion.Question)}"
                );

            if (form == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.ValidationError, HttpStatusCode.NotFound, Resource.FormNotFound);

            var formQuestions = form.FormQuestions
                .Where(fq => fq.FormQuestionStatus != PaperQuestionStatus.Hanged)
                .ToDictionary(fq => fq.QuestionId ?? 0, fq => fq);

            var result = form.Paper.QuestionSelectionType == QuestionSelectionType.Manual
                ? await GetManualSectionsAsync(formId, formQuestions)
                : await GetAutoSectionsAsync(form, formQuestions);

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, null!, result);
        }

        public async Task<ApiResponse> GetAllFormsByPaperIdAsync(long paperId)
        {
            const string includes = nameof(PaperForm.Paper);

            var forms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAllAsync(f => f.PaperId == paperId, Including: includes, asNoTracking: true);

            if (!forms.Any())
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    "No forms found for the given paper"
                );
            }

            var mappedForms = forms.Select(form => new GetFormDto
            {
                Id = form.Id,
                Name = form.Name,
            }).ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                "Forms retrieved successfully",
                mappedForms
            );
        }

        public async Task<ApiResponse> GetQuestionWithFormsAsync(long paperId, long questionId)
        {
            var generatedForms = await _commonService
                ._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .GetAllAsync(
                    fq => fq.QuestionId == questionId && fq.Form != null && fq.Form.PaperId == paperId,
                    Including: $"{nameof(GeneratedFormQuestion.Form)},{nameof(GeneratedFormQuestion.Question)}",
                    asNoTracking: true
                );

            var formsInPaper = generatedForms
                .Where(fq => fq.Form != null && fq.Form.PaperId == paperId)
                .Select(fq => fq.Form)
                .ToList();

            var questionEntity = generatedForms.FirstOrDefault()?.Question;

            if (formsInPaper.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.QuestionDoesNotExistInAnyFormOfThisTest
                );
            }

            var result = new QuestionWithFormsDto
            {
                QuestionCode = questionEntity.Code,
                Forms = _mapper.Map<List<FormListDto>>(formsInPaper)
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionAndFormsRetrievedSuccessfully,
                result
            );
        }

        public async Task<ApiResponse> GetFormsWithEquationsByPaperIdsAsync(List<long> paperIds)
        {
            if (paperIds == null || paperIds.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Failure,
                    HttpStatusCode.BadRequest,
                    Resource.PaperIdIsNotCorrect
                );
            }

            var forms = await _commonService._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .Query()
                .AsNoTracking()
                .Where(pie => paperIds.Contains(pie.PaperId))
                .GroupBy(pie => new { pie.FormId, pie.Form.Name })
                .Select(g => new GetFormDto
                {
                    Id = g.Key.FormId,
                    Name = g.Key.Name,
                }).ToListAsync();

            if (forms.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoFormsFoundForTheGivenPaper
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FormsRetrievedSuccessfully,
                forms
            );
        }

        public async Task<ApiResponse> GetFormsWithoutEquationsByPaperIdAsync(long paperId)
        {
            var formsWithEquationIds = _commonService._unitOfWork
                .Repository<PaperItemBankEquation, long>()
                .Query()
                .Where(pie => pie.PaperId == paperId)
                .Select(pie => pie.FormId);

            var formsWithoutEquations = await _commonService._unitOfWork
                .Repository<PaperForm, long>()
                .Query()
                .Where(f => f.PaperId == paperId && !formsWithEquationIds.Contains(f.Id))
                .Select(f => new GetFormDto
                {
                    Id = f.Id,
                    Name = f.Name,
                })
                .ToListAsync();

            if (formsWithoutEquations.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.NoFormsFoundForTheGivenPaper
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FormsRetrievedSuccessfully,
                formsWithoutEquations
            );
        }

        // POST/PUT/DELETE METHODS:

        public async Task<ApiResponse> CreateFormsForAutoPaperAsync(long paperId, int statingCounter, long paperFinalOutputFormsCount)
        {
            if (paperFinalOutputFormsCount == 0)
            {
                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.Failure,
                                   HttpStatusCode.BadRequest,
                                   Resource.InvalidFormsCount);
            }

            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == paperId, Including: nameof(PaperMetadata.Language));

            if (paper is null)
            {
                return _commonService
                   ._apiResponse
                   .GetApiResponse(CustomCodeStatus.NotFound,
                                   HttpStatusCode.NotFound,
                                   Resource.PaperNotFound);
            }

            var forms = new List<PaperForm>();

            bool isArabicLanguage = paper.Language.Name?.Contains("Arabic", StringComparison.OrdinalIgnoreCase) == true ||
                                    paper.Language.Name?.Contains("عربي", StringComparison.OrdinalIgnoreCase) == true;

            string formWord = isArabicLanguage ? "نموذج" : "form";

            for (int i = statingCounter; i <= paperFinalOutputFormsCount; i++)
            {
                var formName = $"{formWord} {i}";
                var formCode = $"{formName}-{Resource.Code}";
                var formDescription = $"{formName}-{Resource.Description}";

                var form = new PaperForm
                {
                    Name = formName,
                    Description = formDescription,
                    Code = formCode,
                    PaperId = paper.Id,
                    FormStatus = AvailabilityStatus.InComplete
                };

                forms.Add(form);
            }

            _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .AddRangAsync(forms);

            await _commonService._unitOfWork.Complete();

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.Success,
                                HttpStatusCode.OK,
                                string.Format(Resource.ExamFormsCreatedSuccessfully, forms.Count),
                                forms);
        }

        public async Task<ApiResponse> CreateFormsForManualPaperAsync(long paperId, FormMetadataDto formMetadataDto)
        {
            var paper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(x => x.Id == paperId);

            if (paper is null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound
                );
            }

            var existingForms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAll(f => f.PaperId == paperId, OrderBy: q => q.OrderBy(e => e.Id))
                .ToListAsync();

            var formsToAdd = new List<PaperForm>();
            var formsToUpdate = new List<PaperForm>();

            if (formMetadataDto.Id > 0)
            {
                var existingForm = existingForms.FirstOrDefault(f => f.Id == formMetadataDto.Id);

                if (existingForm == null)
                {
                    return _commonService._apiResponse.GetApiResponse(
                        CustomCodeStatus.NotFound,
                        HttpStatusCode.NotFound,
                        Resource.FormNotFound);
                }

                existingForm.Name = formMetadataDto.Name;
                existingForm.Description = formMetadataDto.Description;
                existingForm.Code = formMetadataDto.Code;

                if (existingForm.FormStatus != AvailabilityStatus.Synced && existingForm.FormStatus != AvailabilityStatus.Suspended)
                {
                    existingForm.FormStatus = AvailabilityStatus.Active;
                }

                formsToUpdate.Add(existingForm);
            }
            else if (formMetadataDto.Id == 0)
            {
                var newForm = new PaperForm
                {
                    Name = formMetadataDto.Name,
                    Description = formMetadataDto.Description,
                    Code = formMetadataDto.Code,
                    PaperId = paper.Id,
                    FormStatus = AvailabilityStatus.InComplete
                };

                formsToAdd.Add(newForm);
            }

            if (formsToAdd.Count > 0)
            {
                _commonService._unitOfWork.Repository<PaperForm, long>().AddRange(formsToAdd);
            }

            if (formsToUpdate.Count > 0)
            {
                _commonService._unitOfWork.Repository<PaperForm, long>().UpdateRange(formsToUpdate);
            }

            await _commonService._unitOfWork.Complete();

            var finalForms = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetAll(f => f.PaperId == paperId)
                .ToListAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                string.Format(Resource.ExamFormsCreatedSuccessfully, finalForms.Count),
                finalForms
            );
        }

        public async Task<ApiResponse> EditFormAsync(
            EditFormDto formDto,
            CancellationToken cancellationToken = default
        )
        {
            var existingForm = await GetFormWithIncludesAsync(formDto.FormId);

            if (existingForm == null)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.FormNotFound);
            }

            UpdateFormBasicInfo(existingForm, formDto);
            await SyncFormQuestionsAsync(existingForm, formDto.Questions);

            if (existingForm.Paper.QuestionSelectionType == QuestionSelectionType.Manual)
            {
                var error = await SyncManualQuestionsAsync(
                    existingForm,
                    formDto.Questions,
                    cancellationToken
                );

                if (error != null) return error;
            }

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.FormUpdatedSuccessfully);
        }

        public async Task<ApiResponse> AssignQuestionsToFormsAsync(List<FormQuestionsDto> formQuestions)
        {
            if (formQuestions == null || formQuestions.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.Noformquestionmappingsprovided
                );
            }

            var newLinks = new List<GeneratedFormQuestion>();

            foreach (var dto in formQuestions)
            {
                if (dto.Questions == null || !dto.Questions.Any())
                    continue;

                newLinks.AddRange(dto.Questions.Select(q => new GeneratedFormQuestion
                {
                    FormId = dto.FormId,
                    QuestionId = q.QuestionId,
                    FormQuestionStatus = q.PaperQuestionStatus,
                    Score = q.Score
                }));
            }

            _commonService
               ._unitOfWork
               .Repository<GeneratedFormQuestion, long>()
               .AddRangAsync(newLinks);

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.Questionslinkedtoformswithscoressuccessfully
            );
        }

        public async Task<ApiResponse> SuspendFormAsync(long formId)
        {
            var form = await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetObjAsync(p => p.Id == formId && !p.IsDeleted);

            if (form == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.FormNotFound,
                    HttpStatusCode.NotFound,
                    Resource.FormNotFound
                );
            }

            form.FormStatus = AvailabilityStatus.Suspended;

            await _commonService._unitOfWork.Complete();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.FormSuspendedSuccessfully,
                HttpStatusCode.OK,
                Resource.FormSuspendedSuccessfully
            );
        }

        public async Task<ApiResponse> ResetPaperFormsCounterAsync(long paperId)
        {
            var targetPaper = await _commonService
                ._unitOfWork
                .Repository<PaperMetadata, long>()
                .GetObjAsync(p => p.Id == paperId, Including: $"{nameof(PaperMetadata.Forms)}.{nameof(PaperForm.FormQuestions)}");

            if (targetPaper == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.PaperNotFound
                );
            }

            var activeFormsCount = targetPaper.Forms.Count(f => f.FormStatus == AvailabilityStatus.Active);

            if (targetPaper.OutputFormsCount == activeFormsCount)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.FormCounterResetSuccessfully
                );
            }

            await DeletePendingFormAsync(targetPaper);

            targetPaper.OutputFormsCount = activeFormsCount;

            targetPaper.PaperCreationStatus = PaperCreationStatus.PaperCreated;

            await _commonService._unitOfWork.Complete();

            await _commonService
                ._unitOfWork
                .Repository<ManualPaperItemBankQuestionSection, long>()
                .Query()
                .Where(x => x.ItemBankPoint.PaperId == paperId && x.SectionId == null)
                .ExecuteDeleteAsync();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FormCounterResetSuccessfully
            );
        }

        private async Task DeletePendingFormAsync(PaperMetadata targetPaper)
        {
            if (targetPaper?.Forms is null || targetPaper.Forms.Count == 0)
            {
                return;
            }

            var currentPendingForm = targetPaper.Forms
                .SingleOrDefault(f => f.FormStatus == AvailabilityStatus.InComplete);

            if (currentPendingForm is null)
            {
                return;
            }

            var deleteDate = DateTimeHelper.Now;

            if (targetPaper.QuestionSelectionType == QuestionSelectionType.Manual)
            {
                await _commonService._unitOfWork
                    .Repository<ManualPaperItemBankQuestionSection, long>()
                    .Query()
                    .Where(x => x.Section.FormId == currentPendingForm.Id)
                    .ExecuteUpdateAsync(x =>
                        x.SetProperty(p => p.IsDeleted, true)
                         .SetProperty(p => p.IsActive, false)
                         .SetProperty(p => p.DeletedDate, deleteDate)
                    );

                await _commonService._unitOfWork
                    .Repository<StandardSection, long>()
                    .Query()
                    .Where(x => x.FormId == currentPendingForm.Id)
                    .ExecuteUpdateAsync(x =>
                        x.SetProperty(p => p.IsDeleted, true)
                         .SetProperty(p => p.IsActive, false)
                         .SetProperty(p => p.DeletedDate, deleteDate)
                    );
            }

            // Delete generated questions:
            await _commonService._unitOfWork
                .Repository<GeneratedFormQuestion, long>()
                .Query()
                .Where(x => x.FormId == currentPendingForm.Id)
                .ExecuteUpdateAsync(x =>
                    x.SetProperty(p => p.IsDeleted, true)
                     .SetProperty(p => p.IsActive, false)
                     .SetProperty(p => p.DeletedDate, deleteDate)
                );

            // Delete form:
            await _commonService._unitOfWork
                .Repository<PaperForm, long>()
                .Query()
                .Where(x => x.Id == currentPendingForm.Id)
                .ExecuteUpdateAsync(x =>
                    x.SetProperty(p => p.IsDeleted, true)
                     .SetProperty(p => p.IsActive, false)
                     .SetProperty(p => p.DeletedDate, deleteDate)
                );
        }

        public async Task<ApiResponse> SoftDeleteFormAsync(long formId)
        {
            var parameters = new List<(string Name, object Value)>
            {
                ("p_FormId", formId),
                ("p_OrganizationId", _filterParamsValues.OrganizationId),
                ("p_OrganizationSignature", _filterParamsValues.Signature ?? string.Empty)
            };

            var result = await _commonService._unitOfWork.ExecuteStoredProcedureAsync<SpStatusResultDto>(
                "sp_SoftDeleteForm",
                parameters
            );

            var spResult = result.FirstOrDefault();

            if (spResult is null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    Resource.AnErrorOccurred
                );
            }

            var errorCode = (FormErrorCode)spResult.ErrorCode;

            if (spResult.Status == 0)
            {
                string localizedError = errorCode switch
                {
                    FormErrorCode.FormNotFound => Resource.FormNotFound,
                    FormErrorCode.AlreadyDeleted => Resource.FormAlreadyDeleted,
                    FormErrorCode.AssignedToActiveSchedule => Resource.CannotDeleteFormAssignedToSchedule,
                    _ => Resource.AnErrorOccurred
                };

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.InternalServerError,
                    HttpStatusCode.InternalServerError,
                    localizedError
                );
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.FormDeletedSuccessfully
            );
        }

        #region Helper Methods

        private static QuestionMetadataPaginationDto MapQuestionMetadata(QuestionMetadata question, long languageId)
        {
            return new QuestionMetadataPaginationDto
            {
                Id = question.Id,
                Code = question.Code,
                Subject = question.Subject?.Name,
                Type = question.QuestionType?.Name,
                Category = question.QuestionCategory?.Name,
                MaximumAnswerTime = question.MaximumAnswerTime.ToString(),
                ItemBank = question.ItemBank?.Name,
                ItemBankCode = question.ItemBank?.Code,
                UseArabicNumbers = question.QuestionDetails?.FirstOrDefault(x => x.LanguageId == languageId)?.UseArabicNumbers ?? false,
                QuestionData = question.QuestionDetails?.Where(x => x.LanguageId == languageId).Select(d => new QuestionDataDto
                {
                    Id = d.Id,
                    Body = d.Body,
                    Instructions = d.Instructions,
                    QuestionMetadataId = d.QuestionMetadataId,
                    Choices = GetChoices(d),
                    ModelAnswer = d.ModelAnswer,
                    LanguageId = d.LanguageId,
                    HasShuffled = d.HasShuffled,
                    AttachmentFileName = d.AttachmentFileName,
                    HasAttachment = !string.IsNullOrWhiteSpace(d.AttachmentFileName),
                    FileUploadSettings = question.FileUploadResponseSettings != null ? new FileUploadSettingsDto
                    {
                        Id = question.FileUploadResponseSettings.Id,
                        QuestionMetadataId = question.FileUploadResponseSettings.QuestionMetadataId,
                        ShowAnswerTextArea = question.FileUploadResponseSettings.ShowAnswerTextArea,
                        SupportedFileExtensions = question.FileUploadResponseSettings.SupportedFileExtensions,
                        UploadedFilesCount = question.FileUploadResponseSettings.UploadedFilesCount,
                        SingleFileMaxSizeInMB = question.FileUploadResponseSettings.SingleFileMaxSizeInMB,
                    } : null,
                    SegmentQuestionConfig = d.SegmentQuestionProperties != null ? new SegmentQuestionConfigDto
                    {
                        ThinkingTime = d.SegmentQuestionProperties.ThinkingTime,
                        ResponseTime = d.SegmentQuestionProperties.ResponseTime,
                        WordsCount = d.SegmentQuestionProperties.WordsCount,
                        OrderNumber = d.SegmentQuestionProperties.OrderNumber,
                        HasScore = d.SegmentQuestionProperties.HasScore,
                        SegmentAudioUrl = d.SegmentQuestionProperties.SegmentAudioUrl,
                        SegmentQuestionResponseType = d.SegmentQuestionProperties.SegmentQuestionResponseType
                    } : null
                }).ToList() ?? [],
                SubQuestions = question.SubQuestions?.Select(sq => MapQuestionMetadata(sq, languageId)).ToList() ?? []
            };
        }

        private async Task<PaperForm> GetFormWithIncludesAsync(long formId)
        {
            return await _commonService
                ._unitOfWork
                .Repository<PaperForm, long>()
                .GetObjAsync(f => f.Id == formId,
                    Including:
                        $"{nameof(PaperForm.FormQuestions)}" +
                        $",{nameof(PaperForm.Paper)}" +
                        $",{nameof(PaperForm.Paper)}.{nameof(PaperMetadata.MarkingScheme)}" +
                        $",{nameof(PaperForm.Paper)}.{nameof(PaperMetadata.ItemBanksPoints)}" +
                        $",{nameof(PaperForm.Paper)}.{nameof(PaperMetadata.ItemBanksPoints)}.{nameof(PaperItemBankPoint.ManualPaperItemBankQuestionSections)}" +
                        $",{nameof(PaperForm.Paper)}.{nameof(PaperMetadata.ItemBanksPoints)}.{nameof(PaperItemBankPoint.AutoPaperItemBankQuestionSections)}"
                );
        }

        private static void UpdateFormBasicInfo(PaperForm form, EditFormDto dto)
        {
            form.Name = dto.Name;
            form.Description = dto.Description;
            form.Code = dto.Code;
        }

        private async Task SyncFormQuestionsAsync(
            PaperForm form,
            List<FormQuestionEditDto> updatedQuestions
        )
        {
            var existingQuestions = form.FormQuestions.ToList();

            var updatedIds = updatedQuestions
                .Select(q => q.QuestionId).ToHashSet();

            var newQuestions = new List<GeneratedFormQuestion>();

            foreach (var updated in updatedQuestions)
            {
                var existing = existingQuestions
                    .FirstOrDefault(eq => eq.QuestionId == updated.QuestionId);

                if (existing != null)
                {
                    existing.FormQuestionStatus = updated.FormQuestionStatus;
                    existing.Score = updated.Score;
                }
                else
                {
                    newQuestions.Add(new GeneratedFormQuestion
                    {
                        FormId = form.Id,
                        QuestionId = updated.QuestionId,
                        FormQuestionStatus = updated.FormQuestionStatus,
                        Score = updated.Score
                    });
                }
            }

            if (newQuestions.Count > 0)
            {
                await _commonService
                   ._unitOfWork
                   .Repository<GeneratedFormQuestion, long>()
                   .AddRangeAsync(newQuestions);
            }

            var toRemove = existingQuestions
                .Where(eq => !updatedIds.Contains((long)eq.QuestionId))
                .ToList();

            if (toRemove.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<GeneratedFormQuestion, long>()
                    .DeleteRange(toRemove);
            }
        }

        private async Task<ApiResponse> SyncManualQuestionsAsync(
            PaperForm paperForm,
            List<FormQuestionEditDto> updatedQuestions,
            CancellationToken cancellationToken = default
        )
        {
            var itemBankPointIds = paperForm.Paper.ItemBanksPoints?
                .Select(p => p.Id)
                .ToList();

            if (itemBankPointIds == null || itemBankPointIds.Count == 0)
            {
                return _commonService._apiResponse
                    .GetApiResponse(CustomCodeStatus.NotFoundItembank, HttpStatusCode.BadRequest, Resource.NoItemBankPointsFoundForPaper);
            }

            var existingFormManualQuestions = await _commonService
                ._unitOfWork
                .Repository<ManualPaperItemBankQuestionSection, long>()
                .GetAllAsync(
                    i => itemBankPointIds.Contains(i.ItemBankPointId) && i.Section.FormId == paperForm.Id,
                    Including: nameof(ManualPaperItemBankQuestionSection.Section));

            var updatedManualQuestionsIds = updatedQuestions
                .Select(q => q.QuestionId)
                .ToHashSet();

            var defaultSectionId = existingFormManualQuestions
                .FirstOrDefault()
                ?.SectionId;

            var newManualQuestions = new List<ManualPaperItemBankQuestionSection>();

            foreach (var question in updatedQuestions)
            {
                var existingManual = existingFormManualQuestions
                    .FirstOrDefault(eq => eq.QuestionMetaDataId == question.QuestionId);

                if (existingManual != null)
                {
                    existingManual.PaperQuestionStatus = question.FormQuestionStatus;
                }
                else
                {
                    var newManualQuestion = await AddNewManualQuestionAsync(
                        paperForm.PaperId,
                        question,
                        existingFormManualQuestions,
                        defaultSectionId,
                        cancellationToken);

                    newManualQuestions.Add(newManualQuestion);
                }
            }

            if (newManualQuestions.Count > 0)
            {
                await _commonService
                    ._unitOfWork
                    .Repository<ManualPaperItemBankQuestionSection, long>()
                    .AddRangeAsync(newManualQuestions);
            }

            var toRemoveManual = existingFormManualQuestions
                .Where(eq => !updatedManualQuestionsIds.Contains(eq.QuestionMetaDataId))
                .ToList();

            if (toRemoveManual.Count > 0)
            {
                _commonService
                    ._unitOfWork
                    .Repository<ManualPaperItemBankQuestionSection, long>()
                    .DeleteRange(toRemoveManual);
            }

            return null;
        }

        private async Task<ManualPaperItemBankQuestionSection> AddNewManualQuestionAsync(
            long? paperId,
            FormQuestionEditDto formQuestionEditDto,
            IEnumerable<ManualPaperItemBankQuestionSection> existingFormManualQuestions,
            long? defaultSectionId,
            CancellationToken cancellationToken = default)
        {
            var sectionIdToUse = existingFormManualQuestions
                .FirstOrDefault(eq => eq.PaperQuestionStatus == PaperQuestionStatus.Hanged)?.SectionId ?? defaultSectionId;

            var questionMetadata = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .Query()
                .AsNoTracking()
                .Where(q => q.Id == formQuestionEditDto.QuestionId)
                .Select(q => new
                {
                    q.Id,
                    q.ItemBankId,
                    q.DifficultyLevelId
                })
                .FirstOrDefaultAsync(cancellationToken);

            var itemBankPointId = await _commonService
                ._unitOfWork
                .Repository<PaperItemBankPoint, long>()
                .Query()
                .AsNoTracking()
                .Where(q => q.ItemBankId == questionMetadata.ItemBankId && q.PaperId == paperId)
                .Select(q => q.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return new ManualPaperItemBankQuestionSection
            {
                ItemBankPointId = itemBankPointId,
                QuestionMetaDataId = formQuestionEditDto.QuestionId,
                PaperQuestionStatus = formQuestionEditDto.FormQuestionStatus,
                SectionId = sectionIdToUse,
                DifficultyLevelId = questionMetadata.DifficultyLevelId
            };
        }

        private async Task<List<SectionWithFormQuestionsDto>> GetManualSectionsAsync(
            long formId,
            Dictionary<long, GeneratedFormQuestion> formQuestions
        )
        {
            var sections = await _commonService._unitOfWork
                .Repository<StandardSection, long>()
                .GetAllAsync(s => s.FormId == formId,
                    Including: $"{nameof(StandardSection.ManualQuestions)}.{nameof(ManualPaperItemBankQuestionSection.QuestionMetadata)}",
                    asNoTracking: true
                );

            return sections
                .Select(section => MapManualSection(section, formQuestions))
                .Where(x => x != null)
                .ToList()!;
        }

        private static SectionWithFormQuestionsDto MapManualSection(
            StandardSection section,
            Dictionary<long, GeneratedFormQuestion> formQuestions
        )
        {
            var questions = section.ManualQuestions
                .Where(m => formQuestions.ContainsKey(m.QuestionMetaDataId))
                .Select(m => new QuestionsInSectionDto
                {
                    Id = m.QuestionMetaDataId,
                    Code = formQuestions[m.QuestionMetaDataId].Question?.Code ?? m.QuestionMetadata?.Code
                })
                .ToList();

            return questions.Count == 0 ? null : new SectionWithFormQuestionsDto
            {
                SectionId = section.Id,
                SectionName = section.Name,
                SectioningIdentifier = section.SectioningIdentifier,
                Questions = questions
            };
        }

        private async Task<List<SectionWithFormQuestionsDto>> GetAutoSectionsAsync(
            PaperForm form,
            Dictionary<long, GeneratedFormQuestion> formQuestions
        )
        {
            var sections = await _commonService._unitOfWork
                .Repository<StandardSection, long>()
                .GetAllAsync(s => s.PaperId == form.Paper.Id, Including: nameof(StandardSection.AutoQuestions));

            var assignedIds = new HashSet<long>();
            var result = new List<SectionWithFormQuestionsDto>();

            foreach (var section in sections)
            {
                var questions = GetAutoSectionQuestions(section, formQuestions, assignedIds);
                result.Add(new SectionWithFormQuestionsDto
                {
                    SectionId = section.Id,
                    SectionName = section.Name,
                    SectioningIdentifier = section.SectioningIdentifier,
                    Questions = questions
                });
            }

            return result;
        }

        private static List<QuestionsInSectionDto> GetAutoSectionQuestions(
            StandardSection section,
            Dictionary<long, GeneratedFormQuestion> formQuestions,
            HashSet<long> assignedIds
        )
        {
            var questions = new List<QuestionsInSectionDto>();

            foreach (var autoQuestion in section.AutoQuestions)
            {
                var totalNeeded = (int)autoQuestion.SelectedCount;
                var assignedCount = 0;

                var parsedIds = ParseQuestionIds(autoQuestion.QuestionIds).ToList();

                if (parsedIds.Count > 0)
                {
                    var matchedIds = parsedIds
                        .Where(id => formQuestions.ContainsKey(id) && !assignedIds.Contains(id))
                        .Take(totalNeeded)
                        .ToList();

                    foreach (var id in matchedIds)
                    {
                        if (assignedIds.Add(id))
                        {
                            questions.Add(new QuestionsInSectionDto { Id = id, Code = formQuestions[id].Question?.Code });
                            assignedCount++;
                        }
                    }
                }

                var remaining = totalNeeded - assignedCount;
                if (remaining > 0)
                {
                    var matchedPairs = formQuestions
                        .Where(kvp =>
                            !assignedIds.Contains(kvp.Key) &&
                            kvp.Value.Question != null &&
                            kvp.Value.Question.DifficultyLevelId == autoQuestion.DifficultyLevelID &&
                            kvp.Value.Question.QuestionTypeId == autoQuestion.QuestionTypeID)
                        .Take(remaining)
                        .ToList();

                    foreach (var (id, fq) in matchedPairs)
                    {
                        if (assignedIds.Add(id))
                        {
                            questions.Add(new QuestionsInSectionDto { Id = id, Code = fq.Question!.Code });
                        }
                    }
                }
            }

            return questions;
        }

        private static IEnumerable<long> ParseQuestionIds(string questionIds)
        {
            var trimmed = questionIds.Trim().Trim('[', ']');
            return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => long.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0);
        }

        private static List<ChoiceDataDto> GetChoices(QuestionDetails d)
        {
            if (d.QuestionsChoices.Count == 0)
            {
                return [];
            }

            var choices = d.QuestionsChoices.Select(c => new ChoiceDataDto
            {
                Id = c.Id,
                ChoiceText = c.ChoiceText,
                IsCorrectAnswer = c.IsCorrectAnswer,
                AttachmentFileName = c.AttachmentFileName,
                HasAttachment = !string.IsNullOrWhiteSpace(c.AttachmentFileName)
            }).ToList();

            if (d.HasShuffled)
            {
                choices.Shuffle();
            }

            return choices;
        }

        #endregion Helper Methods
    }
}