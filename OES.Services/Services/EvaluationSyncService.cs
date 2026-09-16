using MassTransit;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.Views.Answers;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.Contracts.OES_EVAL;
using SharedHelper.DTOs;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Net;
using System.Text.Json;

namespace OES.Services.Services
{
    public class EvaluationSyncService : IEvaluationSyncService
    {
        private readonly ICommonService _commonService;
        private readonly IPayloadStorageService _payloadStorageService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IApiResponse _apiResponse;
        public EvaluationSyncService(
            ICommonService commonService,
            IPayloadStorageService payloadStorageService,
            IPublishEndpoint publishEndpoint,
            IApiResponse apiResponse)
        {
            _commonService = commonService;
            _payloadStorageService = payloadStorageService;
            _publishEndpoint = publishEndpoint;
            _apiResponse = apiResponse;
        }

        public async Task<ApiResponse> SyncCandidateAnswersToEvaluationAsync()
        {
            var pendingAnswersToEvaluates = await _commonService
                    ._unitOfWork
                    .Repository<PendingAnswersToEvaluate, long>()
                    .Query()
                    .AsNoTracking()
                    .ToListAsync();

            if (pendingAnswersToEvaluates.Count == 0)
            {
                return _apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.NoCandidateAnswersToSync
                );
            }

            var schedulesList = pendingAnswersToEvaluates
                .GroupBy(x => new { x.EventID, x.OrganizationId })
                .Select(group =>
                {
                    var firstExam = group.First();
                    return new ExamEventDto
                    {
                        Id = group.Key.EventID,
                        Name = firstExam.EventName,
                        StartDate = firstExam.EventStartDate,
                        EndDate = firstExam.EventEndDate,
                        OrganizationId = group.Key.OrganizationId ?? 0,
                        OrganizationSignature = firstExam.OrganizationSignature,
                    };
                })
                .ToList();

            var papersList = pendingAnswersToEvaluates
                .GroupBy(x => x.PaperID)
                .Select(group => new PaperDto
                {
                    Id = group.Key,
                    Name = group.First().PaperName
                })
                .ToList();

            var venuesList = pendingAnswersToEvaluates
                .GroupBy(x => x.VenueId)
                .Select(group => new VenueDto
                {
                    Id = group.Key ?? 0,
                    Code = group.First().VenueCode ?? ""
                })
                .ToList();

            var questionTypesList = pendingAnswersToEvaluates
                .GroupBy(x => x.QuestionTypeId)
                .Select(group => new QuestionTypeDto
                {
                    Id = group.Key,
                    Name = group.First().QuestionTypeName
                })
                .ToList();

            var questionsList = pendingAnswersToEvaluates
                .GroupBy(x => x.QuestionID)
                .Select(group => new QuestionDto
                {
                    Id = group.Key,
                    ParentId = group.First().ParentQuestionID,
                    Body = group.First().QuestionText ?? "",
                    QuestionTypeId = group.First().QuestionTypeId,
                    Instruction = "",
                    ModelAnswer = group.First().ModelAnswer ?? "",
                    SegmentResponseType = group.First().SegmentResponseType,
                    SegmentAudioUrl = group.First().SegmentAudioUrl,
                    SegmentOrderNumber = group.First().SegmentOrderNumber,
                    FullMark = (double)group.First().FullMark
                })
                .ToList();

            var answersList = pendingAnswersToEvaluates
                .ConvertAll(answer => new AnswerDto
                {
                    Id = answer.CandidateAnswerID,
                    CandidateId = answer.CandidateID,
                    QuestionId = answer.QuestionID,
                    CandidateExamId = answer.CandidateExamID,
                    EventId = answer.EventID,
                    PaperId = answer.PaperID,
                    Answer = answer.TypedAnswerText ?? "",
                    RegistrationId = answer.RegistrationId,
                    VenueId = answer.VenueId ?? 0,
                    OrganizationId = answer.OrganizationId ?? 0,
                    OrganizationSignature = answer.OrganizationSignature ?? ""
                });

            var payload = new CandidateAnswersPayload
            {
                Schedules = schedulesList,
                Papers = papersList,
                Venues = venuesList,
                QuestionTypes = questionTypesList,
                Questions = questionsList,
                Answers = answersList
            };

            var jobId = Guid.NewGuid();
            var fileName = $"candidate-answers-{jobId}.json";
            var jsonContent = JsonSerializer.Serialize(payload);

            var payloadPath = await _payloadStorageService
                .WritePayloadAsync(fileName, jsonContent);

            var evaluationSyncJob = new EvaluationSyncJob
            {
                JobId = jobId,
                PayloadFilePath = payloadPath,
                TotalAnswers = answersList.Count,
                Status = SyncJobStatus.Pending,
            };

            await _commonService._unitOfWork
              .Repository<EvaluationSyncJob, long>()
              .AddAsync(evaluationSyncJob);

            await _commonService._unitOfWork
                .Repository<CandidateQuestionsAnswers, long>()
                .Query()
                .Where(x => payload.Answers.Select(a => a.Id).Contains(x.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EvaluationSyncJobId, jobId));

            await _commonService._unitOfWork.Complete();

            var message = new SyncCandidateAnswers
            {
                JobId = jobId,
                PayloadFilePath = payloadPath,
                Timestamp = DateTimeHelper.Now
            };

            await _publishEndpoint.Publish(message);

            return _apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.EvaluationSyncInitiated
            );
        }
    }
}
