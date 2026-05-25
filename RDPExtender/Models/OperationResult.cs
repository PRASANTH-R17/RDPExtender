namespace RDPExtender.Models;

public sealed record OperationResult(bool Success, int ExitCode, string Message);
