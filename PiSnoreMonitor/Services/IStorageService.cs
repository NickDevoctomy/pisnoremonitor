using System.Collections.Generic;

namespace PiSnoreMonitor.Services
{
    public interface IStorageService
    {
        public List<string> GetRemovableStorageDrivePaths();
    }
}
