using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;

namespace SensorsWorker
{
    public class SensorsHub : Hub<ISensorsClient>
    {
    }

    public interface ISensorsClient
    {
        Task RecieveDataAsync(SensorsDto data, CancellationToken cancellationToken);
    }
}