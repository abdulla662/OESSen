using System.Diagnostics.Metrics;

namespace OES.Services.AIFeatures.Telemetry
{
    public static class AIMetrics
    {
        public static readonly Meter Meter = new("OES.AI");

        public static readonly Counter<long> Requests =
            Meter.CreateCounter<long>("ai.requests");

        public static readonly Histogram<double> RequestDuration =
            Meter.CreateHistogram<double>("ai.request.duration");
    }
}
