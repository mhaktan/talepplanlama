using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace talepplanlama.Flows
{
    public class FlowSchedulerService : BackgroundService
    {
        private readonly IFlowEngine _flowEngine;
        private readonly ILogger<FlowSchedulerService> _logger;

        public FlowSchedulerService(IFlowEngine flowEngine, ILogger<FlowSchedulerService> logger)
        {
            _flowEngine = flowEngine;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var flows = LoadScheduledFlows();
            if (flows.Count == 0) return;

            _logger.LogInformation("FlowScheduler: {Count} scheduled flows loaded", flows.Count);

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTimeOffset.UtcNow;
                foreach (var (flow, cron) in flows)
                {
                    var next = cron.GetNextOccurrence(now, TimeZoneInfo.Utc);
                    if (next == null) continue;

                    var delay = next.Value - now;
                    if (delay.TotalSeconds < 60) // due within this minute
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                _logger.LogInformation("FlowScheduler: triggering {FlowName}", flow.Name);
                                await _flowEngine.TriggerAsync("schedule", flow.ResourceName ?? "", new { });
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "FlowScheduler: error executing {FlowName}", flow.Name);
                            }
                        }, stoppingToken);
                    }
                }

                // Check every 60 seconds
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        private List<(FlowDefinition flow, CronExpression cron)> LoadScheduledFlows()
        {
            var result = new List<(FlowDefinition, CronExpression)>();
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flowDefinitions.json");
            if (!File.Exists(path)) return result;

            var json = File.ReadAllText(path);
            var flows = JsonSerializer.Deserialize<List<FlowDefinition>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            foreach (var flow in flows.Where(f => f.Enabled))
            {
                var scheduleTrigger = flow.Nodes
                    .FirstOrDefault(n => n.Type == "trigger" && n.Trigger?.Type == "schedule");
                if (scheduleTrigger?.Trigger?.CronExpression == null) continue;

                try
                {
                    var cron = CronExpression.Parse(scheduleTrigger.Trigger.CronExpression);
                    result.Add((flow, cron));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FlowScheduler: invalid cron for {FlowName}", flow.Name);
                }
            }

            return result;
        }
    }
}
