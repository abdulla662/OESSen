using MassTransit;
using OES.API.Consumers;
using OES.Helper.General;
using RabbitMQ.Client;
using SharedHelper.Contracts.OES_CES;
using SharedHelper.Contracts.OES_EVAL;
using SharedHelper.General;

namespace OES.API.Extensions
{
    public static class RabbitMQConfiguration
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddMassTransit(busConfigurator =>
            {
                busConfigurator.AddConsumer<JobStatusUpdatedConsumer>();

                busConfigurator.AddConsumer<CandidateAnswersJobStatusUpdatedConsumer>();

                busConfigurator.AddConsumer<ReceiveResultsConsumer>();

                busConfigurator.AddConsumer<ReceiveExamDetailsConsumer>();

                busConfigurator.AddConsumer<ReceiveBlockCandidateAnswerConsumer>();

                busConfigurator.AddConsumer<ReceiveTrackingLogsConsumer>();

                busConfigurator.AddConsumer<ReceiveEvaluationScoresConsumer>();

                busConfigurator.SetKebabCaseEndpointNameFormatter();

                busConfigurator.UsingRabbitMq((context, mqConfigurator) =>
                {
                    var rabbitMQSettings = builder.Configuration.GetSection(nameof(RabbitMQSettings)).Get<RabbitMQSettings>();

                    string virtualHost = rabbitMQSettings.VirtualHost ?? "/";

                    if (builder.Environment.IsDevelopment())
                    {
                        virtualHost = Environment.MachineName.ToUpper();
                    }

                    mqConfigurator.Host(rabbitMQSettings.Host, rabbitMQSettings.Port, virtualHost, h =>
                    {
                        h.Username(rabbitMQSettings.Username);
                        h.Password(rabbitMQSettings.Password);
                        h.ConnectionName("OES-Publisher");
                    });

                    mqConfigurator.Message<SyncScheduleToVenue>(x => x.SetEntityName(SharedConstants.ScheduleExchangeName));
                    mqConfigurator.Publish<SyncScheduleToVenue>(x =>
                    {
                        x.ExchangeType = ExchangeType.Topic;
                        x.Exclude = true;
                    });

                    mqConfigurator.Message<SuspendPaperToVenue>(x => x.SetEntityName(SharedConstants.PaperExchangeName));
                    mqConfigurator.Publish<SuspendPaperToVenue>(x =>
                    {
                        x.ExchangeType = ExchangeType.Topic;
                        x.Exclude = true;
                    });

                    mqConfigurator.Message<SuspendFormToVenue>(x => x.SetEntityName(SharedConstants.PaperFormExchangeName));
                    mqConfigurator.Publish<SuspendFormToVenue>(x =>
                    {
                        x.ExchangeType = ExchangeType.Topic;
                        x.Exclude = true;
                    });

                    mqConfigurator.Message<SyncEvaluationScores>(x => x.SetEntityName(SharedConstants.AnswerScoreExchangeName));
                    mqConfigurator.Publish<SyncEvaluationScores>(x => x.ExchangeType = ExchangeType.Direct);

                    mqConfigurator.Message<SyncCandidateAnswers>(x => x.SetEntityName(SharedConstants.CandidateAnswersExchangeName));
                    mqConfigurator.Publish<SyncCandidateAnswers>(x => x.ExchangeType = ExchangeType.Direct);

                    const string OesStatusQueue = MessageQueues.OesStatusQueue;

                    mqConfigurator.ReceiveEndpoint(OesStatusQueue, e =>
                    {
                        e.ConcurrentMessageLimit = 1;

                        e.ConfigureConsumer<JobStatusUpdatedConsumer>(context);
                        e.ConfigureConsumer<CandidateAnswersJobStatusUpdatedConsumer>(context);
                    });

                    const string OesCandidatesAnswersQueue = MessageQueues.OesCandidatesAnswersQueue;

                    const string OesCandidateExamDetailsQueue = MessageQueues.OesCandidateExamDetailsQueue;

                    const string OesBlockCandidateAnswerQueue = MessageQueues.OesBlockCandidateAnswerQueue;

                    const string OesTrackingLogsQueue = MessageQueues.OesTrackingLogsQueue;

                    const string OesEvaluationScoresQueue = MessageQueues.OesEvaluationScoresQueue;

                    mqConfigurator.ReceiveEndpoint(OesCandidatesAnswersQueue, e => e.ConfigureConsumer<ReceiveResultsConsumer>(context));

                    mqConfigurator.ReceiveEndpoint(OesCandidateExamDetailsQueue, e => e.ConfigureConsumer<ReceiveExamDetailsConsumer>(context));

                    mqConfigurator.ReceiveEndpoint(OesBlockCandidateAnswerQueue, e => e.ConfigureConsumer<ReceiveBlockCandidateAnswerConsumer>(context));

                    mqConfigurator.ReceiveEndpoint(OesTrackingLogsQueue, e => e.ConfigureConsumer<ReceiveTrackingLogsConsumer>(context));

                    mqConfigurator.ReceiveEndpoint(OesEvaluationScoresQueue, e =>
                    {
                        e.Bind(SharedConstants.AnswerScoreExchangeName, b => b.ExchangeType = ExchangeType.Direct);

                        e.ConfigureConsumer<ReceiveEvaluationScoresConsumer>(context);
                    });
                });
            });

            return services;
        }
    }
}