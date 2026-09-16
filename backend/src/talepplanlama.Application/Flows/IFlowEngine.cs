using System.Threading.Tasks;

namespace talepplanlama.Flows
{
    public interface IFlowEngine
    {
        Task<ExecutionResult> ExecuteFlowAsync(FlowDefinition flow, FlowContext context);
        Task TriggerAsync(string triggerType, string resourceName, object data);
        FlowDefinition GetFlowById(string flowId);
    }
}
