namespace RDPExtender.Logging;

public interface IOperationLog
{
    void Info(string message);

    void Success(string message);

    void Warning(string message);

    void Error(string message);
}
