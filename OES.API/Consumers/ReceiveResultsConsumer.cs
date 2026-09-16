using MassTransit;
using OES.Helper.Dtos.CandidatesResult;
using OES.Interface.Interfaces;
using SharedHelper.Contracts.OES_CES;
using System.Net;
using System.Text.Json;

namespace OES.API.Consumers
{
    public class ReceiveResultsConsumer : IConsumer<SyncCandidateResult>
    {
        private readonly IPayloadStorageService _payloadStorageService;
        private readonly ICandidatesResultService _candidatesResultService;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ReceiveResultsConsumer(
            IPayloadStorageService payloadStorageService,
            ICandidatesResultService candidatesResultService
        )
        {
            _payloadStorageService = payloadStorageService;
            _candidatesResultService = candidatesResultService;
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task Consume(ConsumeContext<SyncCandidateResult> context)
        {
            var message = context.Message;

            var payloadJson = await _payloadStorageService.ReadPayloadAsync(message.PayloadFilePath);

            var payload = JsonSerializer.Deserialize<List<AddCandidateQuestionsAnswersDto>>(payloadJson, _jsonSerializerOptions);

            var result = await _candidatesResultService.AddCandidatesQuestionsAnswersAsync(payload);

            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(result.Message);
            }
        }
    }
}