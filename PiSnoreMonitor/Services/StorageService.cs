using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PiSnoreMonitor.Services
{
    public class StorageService : IStorageService
    {
        public List<string> GetRemovableStorageDrivePaths()
        {
            if (OperatingSystem.IsWindows())
            {
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Removable)
                    .Select(d => d.RootDirectory.FullName)
                    .ToList();
            }

            return DriveInfo.GetDrives()
                .Where(d => d.Name.StartsWith($"/media/{Environment.UserName}", StringComparison.Ordinal))
                .Select(d => d.RootDirectory.FullName)
                .ToList();
        }
    }
}
