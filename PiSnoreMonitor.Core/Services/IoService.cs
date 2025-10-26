using PiSnoreMonitor.Core.Enums;

namespace PiSnoreMonitor.Core.Services
{
    public class IoService : IIoService
    {
        public string CombinePaths(params string[] paths)
        {
            return Path.Combine(paths);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public bool Exists(string path)
        {
            return Path.Exists(path);
        }

        public string GetSpecialPath(SpecialPaths specialPath)
        {
            switch(specialPath)
            {
                case SpecialPaths.AppUserStorage:
                    {
                        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        path = CombinePaths(path, "PiSnoreMonitor");
                        return path;
                    }

                default:
                    throw new NotSupportedException($"Special path {specialPath} is not supported.");
            }
        }

        public async Task<string> ReadAllTextAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return await File.ReadAllTextAsync(
                path,
                cancellationToken);
        }

        public async Task WriteAllTextAsync(
            string path,
            string text,
            CancellationToken cancellationToken = default)
        {
            await System.IO.File.WriteAllTextAsync(
                path,
                text,
                cancellationToken);
        }
    }
}
