namespace RDPExtender.Logging;

public sealed class ConsoleOperationLog : IOperationLog
{
    public static ConsoleOperationLog Instance { get; } = new();

    public void Info(string message) => Write(message, ConsoleColor.Gray);

    public void Success(string message) => Write(message, ConsoleColor.Green);

    public void Warning(string message) => Write(message, ConsoleColor.Yellow);

    public void Error(string message) => Write(message, ConsoleColor.Red);

    private static void Write(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
