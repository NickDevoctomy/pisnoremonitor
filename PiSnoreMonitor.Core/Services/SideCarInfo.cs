using System.Text.Json.Serialization;

namespace PiSnoreMonitor.Core.Services
{
    public class SideCarInfo(string filePath) : IEquatable<SideCarInfo>
    {
        [JsonIgnore]
        public string FilePath { get; set; } = filePath;
        
        public DateTime? StartedRecordingAt { get; set; } = DateTime.Now;
        
        public DateTime? StoppedRecordingAt { get; set; }

        public TimeSpan ElapsedRecordingTime => 
            StoppedRecordingAt.HasValue && StartedRecordingAt.HasValue
            ? StoppedRecordingAt.Value - StartedRecordingAt.Value
            : TimeSpan.Zero;

        public bool Equals(SideCarInfo? other)
        {
            return
                other != null &&
                FilePath.Equals(other.FilePath) &&
                StartedRecordingAt.Equals(other.StartedRecordingAt) &&
                StoppedRecordingAt.Equals(other.StoppedRecordingAt);
        }
    }
}
