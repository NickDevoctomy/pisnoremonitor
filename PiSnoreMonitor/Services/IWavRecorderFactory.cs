using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Services
{
    public interface IWavRecorderFactory
    {
        public Task<IWavRecorder> CreateAsync(int deviceId, bool stereo, CancellationToken cancellationToken);
    }
}
