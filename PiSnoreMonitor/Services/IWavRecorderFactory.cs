using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Services
{
    public interface IWavRecorderFactory
    {
        public Task<IWavRecorder> CreateAsync(CancellationToken cancellationToken);
    }
}
