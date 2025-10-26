using PiSnoreMonitor.Core.Enums;

namespace PiSnoreMonitor.Core.Services
{
    public class IoService : IIoService
    {
        public string GetSpecialPath(SpecialPaths specialPath)
        {
            switch(specialPath)
            {
                case SpecialPaths.AppUserStorage:
                    {
                        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        path = System.IO.Path.Combine(path, "PiSnoreMonitor");
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
            return await System.IO.File.ReadAllTextAsync(
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
