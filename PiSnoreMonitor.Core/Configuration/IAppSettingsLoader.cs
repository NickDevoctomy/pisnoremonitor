namespace PiSnoreMonitor.Core.Configuration
{
    public interface IAppSettingsLoader<T>
    {
        public Task<T> LoadAsync(CancellationToken cancellationToken);
        public Task SaveAsync(CancellationToken cancellationToken);
    }
}
