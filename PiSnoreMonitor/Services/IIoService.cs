using PiSnoreMonitor.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Services
{
    public interface IIoService
    {
        public string GetSpecialPath(SpecialPaths specialPath);

        public Task<string> ReadAllTextAsync(
            string path,
            CancellationToken cancellationToken);

        public Task WriteAllTextAsync(
            string path,
            string text,
            CancellationToken cancellationToken);
    }
}
