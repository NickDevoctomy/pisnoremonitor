using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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

            var results = new List<string>();

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    ArgumentList = { "-c", "lsblk -J -o MOUNTPOINT,RM" },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using var proc = Process.Start(psi)!;
                string json = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(json))
                    return results;

                var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("blockdevices", out var arr) || arr.ValueKind != JsonValueKind.Array)
                    return results;

                foreach (var mp in CollectMounts(arr))
                {
                    // Verify it's actually ready
                    try
                    {
                        var di = new DriveInfo(mp);
                        if (di.IsReady) results.Add(Normalize(mp));
                    }
                    catch { /* ignore odd entries */ }
                }

                return results.Distinct(StringComparer.Ordinal).ToList();
            }
            catch
            {
                return results;
            }

            static IEnumerable<string> CollectMounts(JsonElement devices)
            {
                foreach (var dev in devices.EnumerateArray())
                {
                    int rm = dev.TryGetProperty("rm", out var rmEl) && rmEl.TryGetInt32(out var v) ? v : 0;
                    string? mp = dev.TryGetProperty("mountpoint", out var mpEl) && mpEl.ValueKind == JsonValueKind.String
                        ? mpEl.GetString()
                        : null;

                    if (rm == 1 && !string.IsNullOrEmpty(mp))
                        yield return mp!;

                    if (dev.TryGetProperty("children", out var kids) && kids.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var c in CollectMounts(kids))
                            yield return c;
                    }
                }
            }

            static string Normalize(string p) =>
                string.IsNullOrEmpty(p) ? p : (p.Length > 1 && p.EndsWith("/")) ? p.TrimEnd('/') : p;
        }
    }
}
