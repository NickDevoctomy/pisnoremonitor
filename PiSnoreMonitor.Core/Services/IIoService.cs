using PiSnoreMonitor.Core.Enums;

namespace PiSnoreMonitor.Core.Services
{
    public interface IIoService
    {
        public string GetSpecialPath(SpecialPaths specialPath);

        public bool Exists(string path);

        public string CombinePaths(params string[] paths);

        public void CreateDirectory(string path);

        public Task<string> ReadAllTextAsync(
            string path,
            CancellationToken cancellationToken);

        public Task WriteAllTextAsync(
            string path,
            string text,
            CancellationToken cancellationToken);
    }
}
