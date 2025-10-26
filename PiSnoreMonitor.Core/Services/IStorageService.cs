using System.Collections.Generic;

namespace PiSnoreMonitor.Core.Services
{
    public interface IStorageService
    {
        public List<string> GetRemovableStorageDrivePaths();
    }
}
