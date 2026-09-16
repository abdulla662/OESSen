using MassTransit;
using OES.Helper.Dtos.TrackingLog;
using OES.Interface.Interfaces;
using SharedHelper.Contracts.OES_CES;
using SharedHelper.Crypto;
using System.Net;
using System.Text.Json;

namespace OES.API.Consumers
{
    public class ReceiveTrackingLogsConsumer : IConsumer<SyncTrackingLogs>
    {
        private readonly IPayloadStorageService _payloadStorageService;
        private readonly ICandidatesResultService _candidatesResultService;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ReceiveTrackingLogsConsumer(
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

        public async Task Consume(ConsumeContext<SyncTrackingLogs> context)
        {
            var payloadJson = await _payloadStorageService.ReadPayloadAsync(context.Message.PayloadFilePath);

            var payload = JsonSerializer.Deserialize<List<AddTrackingLogDto>>(payloadJson, _jsonSerializerOptions);

            foreach (var dto in payload)
            {
                if (!string.IsNullOrWhiteSpace(dto.SessionLog))
                {
                    dto.SessionLog = CompressionHelper.TryDecompressString(dto.SessionLog);
                }
            }

            var result = await _candidatesResultService.AddTrackingLogsAsync(payload);

            if (result.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(result.Message);
            }
        }
    }
}
