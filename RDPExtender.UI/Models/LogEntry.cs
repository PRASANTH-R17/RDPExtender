namespace RDPExtender.UI.Models;

public sealed class LogEntry
{
    public LogEntry(string timestamp, string message)
    {
        Timestamp = timestamp;
        Message = message;
    }

    public string Timestamp { get; }

    public string Message { get; }

    public string Display => $"{Timestamp} {Message}";
}
