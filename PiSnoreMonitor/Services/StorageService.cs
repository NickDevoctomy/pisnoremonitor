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

            static string NormalizeMount(string path)
            {
                if (string.IsNullOrEmpty(path)) return path;
                return path.Length > 1 && path.EndsWith("/")
                    ? path.TrimEnd('/')
                    : path;
            }

            var readyMounts = new HashSet<string>(
                DriveInfo.GetDrives()
                    .Where(d => d.IsReady)
                    .Select(d => NormalizeMount(d.RootDirectory.FullName))
            );

            var results = new List<string>();

            foreach (var line in File.ReadLines("/proc/mounts"))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                var device = parts[0];
                var mountPoint = NormalizeMount(parts[1]);

                if (!device.StartsWith("/dev/")) continue;
                if (!readyMounts.Contains(mountPoint)) continue;

                var devName = Path.GetFileName(device);

                var candidates = new[]
                {
                    $"/sys/class/block/{devName}/removable",
                    $"/sys/class/block/{Regex.Replace(devName, @"\\dp\\d+$", "")}/removable",
                    $"/sys/class/block/{Regex.Replace(devName, @"\\d+$", "")}/removable"
                }.Distinct();

                foreach (var path in candidates)
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
