using System;
using System.Text.Json.Serialization;

namespace PiSnoreMonitor.Services
{
    public class SideCarInfo(string filePath)
    {
        [JsonIgnore]
        public string FilePath { get; set; } = filePath;
        
        public DateTime? StartedRecordingAt { get; set; } = DateTime.Now;
        
        public DateTime? StoppedRecordingAt { get; set; }

        public TimeSpan ElapsedRecordingTime => 
            StoppedRecordingAt.HasValue && StartedRecordingAt.HasValue
            ? StoppedRecordingAt.Value - StartedRecordingAt.Value
            : TimeSpan.Zero;
    }
}
