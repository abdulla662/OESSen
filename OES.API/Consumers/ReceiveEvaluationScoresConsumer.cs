using MassTransit;
using OES.Interface.Interfaces;
using SharedHelper.Contracts.OES_EVAL;
using SharedHelper.Dtos;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Net;
using System.Text.Json;

namespace OES.API.Consumers
{
    public class ReceiveEvaluationScoresConsumer : IConsumer<SyncEvaluationScores>
    {
        private readonly IPayloadStorageService _payloadStorageService;
        private readonly ICandidatesResultService _candidatesResultService;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly IPublishEndpoint _publishEndpoint;

        public ReceiveEvaluationScoresConsumer(
            IPayloadStorageService payloadStorageService,
            ICandidatesResultService candidatesResultService,
            IPublishEndpoint publishEndpoint
        )
        {
            _payloadStorageService = payloadStorageService;
            _candidatesResultService = candidatesResultService;
            _publishEndpoint = publishEndpoint;
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task Consume(ConsumeContext<SyncEvaluationScores> context)
        {
            var message = context.Message;

            try
            {
                await _publishEndpoint.Publish(new JobStatusUpdated
                {
                    JobId = message.JobId,
                    Status = SyncJobStatus.InProgress
                });

                var payloadJson = await _payloadStorageService
                    .ReadPayloadAsync(message.PayloadFilePath);

                var payload = JsonSerializer.Deserialize<List<CandidateAnswersScoresDto>>(
                    payloadJson,
                    _jsonSerializerOptions
                );

                var result = await _candidatesResultService
                    .UpdateEvaluationScoresAsync(payload);

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    throw new InvalidOperationException(result.Message);
                }

                await _publishEndpoint.Publish(new JobStatusUpdated
                {
                    JobId = message.JobId,
                    Status = SyncJobStatus.Success,
                    CompletedAt = DateTimeHelper.Now
                });
            }
            catch (Exception ex)
            {
                await _publishEndpoint.Publish(new JobStatusUpdated
                {
                    JobId = message.JobId,
                    Status = SyncJobStatus.Failed,
                    ErrorMessage = ex.Message,
                    CompletedAt = DateTimeHelper.Now
                });
            }
        }
    }
}
