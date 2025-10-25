using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Configuration
{
    public interface IAppSettingsLoader
    {
        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken);
        public Task SaveAsync(CancellationToken cancellationToken);
    }
}
