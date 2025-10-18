using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

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

            // Unix-like: enumerate mounted devices and consult /sys for "removable"
            var readyDrivesByMount = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .ToDictionary(d => d.RootDirectory.FullName.TrimEnd('/'));

            var results = new List<string>();

            foreach (var line in File.ReadLines("/proc/mounts"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                var device = parts[0];           // e.g. /dev/sda1
                var mountPoint = parts[1];       // e.g. /media/pi/MYUSB

                if (!device.StartsWith("/dev/")) continue; // skip pseudo fs (tmpfs, proc, etc.)
                if (!readyDrivesByMount.ContainsKey(mountPoint)) continue;

                var devName = Path.GetFileName(device); // sda1, nvme0n1p1, etc.
                                                        // Try the partition first, then its parent block device
                var candidates = new[]
                {
                    $"/sys/class/block/{devName}/removable",
                    $"/sys/class/block/{Regex.Replace(devName, @"\dp\d+$", "")}/removable", // nvme0n1p1 -> nvme0n1
                    $"/sys/class/block/{Regex.Replace(devName, @"\d+$", "")}/removable"     // sda1 -> sda
                };

                foreach (var path in candidates.Distinct())
                {
                    if (File.Exists(path) && File.ReadAllText(path).Trim() == "1")
                    {
                        results.Add(mountPoint);
                        break;
                    }
                }
            }

            return results;
        }
    }
}
