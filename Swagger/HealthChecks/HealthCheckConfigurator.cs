using Microsoft.Extensions.DependencyInjection;

namespace n_ate.Swagger.HealthChecks
{
    public class HealthCheckConfigurator
    {
        private IHealthChecksBuilder builder;
        private bool liveCheckSet;
        private bool readyCheckSet;
        private bool startedCheckSet;

        internal HealthCheckConfigurator(IHealthChecksBuilder builder)
        {
            this.builder = builder;
        }

        public HealthCheckConfigurator SetLive<THealthCheck>() where THealthCheck : LiveHealthCheck
        {
            if (liveCheckSet) throw new InvalidOperationException($"Multiple calls made to {nameof(SetLive)}(). Must only call once.");
            liveCheckSet = true;
            builder.AddCheck<THealthCheck>("liveness_health_check", tags: new[] { "live" });
            return this;
        }

        public HealthCheckConfigurator SetReady<THealthCheck>() where THealthCheck : ReadyHealthCheck
        {
            if (readyCheckSet) throw new InvalidOperationException($"Multiple calls made to {nameof(SetReady)}(). Must only call once.");
            readyCheckSet = true;
            builder.AddCheck<THealthCheck>("readiness_health_check", tags: new[] { "ready" });
            return this;
        }

        public HealthCheckConfigurator SetStarted<THealthCheck>() where THealthCheck : StartedHealthCheck
        {
            if (startedCheckSet) throw new InvalidOperationException($"Multiple calls made to {nameof(SetStarted)}(). Must only call once.");
            startedCheckSet = true;
            builder.AddCheck<THealthCheck>("startup_health_check", tags: new[] { "startup" });
            return this;
        }

        /// <summary>
        /// Adds defaults if any health checks are not configured.
        /// </summary>
        internal HealthCheckConfigurator Complete()
        {
            if (!liveCheckSet) builder.AddCheck<LiveHealthCheck>("liveness_health_check", tags: new[] { "live" });
            if (!readyCheckSet) builder.AddCheck<ReadyHealthCheck>("readiness_health_check", tags: new[] { "ready" });
            if (!startedCheckSet) builder.AddCheck<StartedHealthCheck>("startup_health_check", tags: new[] { "startup" });
            return this;
        }
    }
}