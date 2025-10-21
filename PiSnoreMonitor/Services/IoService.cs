using PiSnoreMonitor.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PiSnoreMonitor.Services
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

        public async Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken)
        {
            return await System.IO.File.ReadAllTextAsync(path, cancellationToken);
        }

        public async Task WriteAllTextAsync(string path, string text, CancellationToken cancellationToken)
        {
            await System.IO.File.WriteAllTextAsync(path, text, cancellationToken);
        }
    }
}
