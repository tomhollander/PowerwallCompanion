namespace PowerwallCompanion.Lib
{
    public interface ITelemetryPlatformAdapter
    {
        string UserId { get; }
        string Platform { get; }
        string AppName { get; }
        string AppVersion { get; }
        string OSVersion { get; }
        string Region { get; }
    }
}