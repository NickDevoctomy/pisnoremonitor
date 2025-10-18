using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PiSnoreMonitor.Services
{
    public class StorageService : IStorageService
    {
        public List<string> GetRemovableStorageDrivePaths()
        {
            var driveList = DriveInfo.GetDrives();

            return [.. driveList
                .Where(drive => drive.DriveType == DriveType.Removable && drive.IsReady)
                .Select(drive => drive.RootDirectory.FullName)];
        }
    }
}
