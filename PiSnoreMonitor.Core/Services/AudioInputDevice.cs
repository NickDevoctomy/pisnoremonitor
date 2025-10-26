namespace PiSnoreMonitor.Core.Services
{
    public class AudioInputDevice(
        int id,
        string name)
    {
        public int Id { get; set; } = id;

        public string Name { get; set; } = name;
    }
}
