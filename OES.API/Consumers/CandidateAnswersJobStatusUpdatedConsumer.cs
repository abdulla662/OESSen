using MassTransit;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Enums;
using OES.Interface.Interfaces;
using SharedHelper.Contracts.OES_EVAL;
using SharedHelper.Enums;
using SharedHelper.General;

namespace OES.API.Consumers
{
    public class CandidateAnswersJobStatusUpdatedConsumer : IConsumer<JobStatusUpdated>
    {
        private readonly ICommonService _commonService;

        public CandidateAnswersJobStatusUpdatedConsumer(ICommonService commonService)
        {
            _commonService = commonService;
        }

        public async Task Consume(ConsumeContext<JobStatusUpdated> context)
        {
            var request = context.Message;

            var job = await _commonService._unitOfWork
                .Repository<EvaluationSyncJob, long>()
                .Query()
                .FirstOrDefaultAsync(j => j.JobId == request.JobId);

            if (job == null) return;

            job.Status = request.Status;
            job.ErrorMessage = request.ErrorMessage;
            job.CompletedAt = request.CompletedAt;

            if (request.Status == SyncJobStatus.InProgress)
            {
                job.StartedProcessingAt = DateTimeHelper.Now;
            }

            if (request.Status == SyncJobStatus.Success)
            {
                await _commonService._unitOfWork
                    .Repository<CandidateQuestionsAnswers, long>()
                    .Query()
                    .Where(e => e.EvaluationSyncJobId == job.JobId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.EvaluationStatus, EvaluationStatus.PendingEvaluation)
                        .SetProperty(x => x.EvaluationSyncDate, DateTimeHelper.Now));
            }

            await _commonService._unitOfWork.Complete();
        }
    }
}
