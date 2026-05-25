using System.IO;
using System.Text;
using RDPExtender.Logging;
using RDPExtender.Models;

namespace RDPExtender;

public static class RdpActionService
{
    public static Task<OperationResult> PatchAsync(IOperationLog log, CancellationToken cancellationToken = default)
    {
        return RunAsync(PatcherRunner.Run, log, cancellationToken);
    }

    public static Task<OperationResult> RevertAsync(IOperationLog log, CancellationToken cancellationToken = default)
    {
        return RunAsync(RevertRunner.Run, log, cancellationToken);
    }

    private static async Task<OperationResult> RunAsync(
        Func<int> action,
        IOperationLog log,
        CancellationToken cancellationToken)
    {
        try
        {
            var exitCode = await Task.Run(() => RunWithConsoleCapture(action, log), cancellationToken)
                .ConfigureAwait(false);
            return exitCode == 0
                ? new OperationResult(true, 0, "Completed successfully.")
                : new OperationResult(false, exitCode, "Operation failed. See logs for details.");
        }
        catch (OperationCanceledException)
        {
            log.Warning("Operation was cancelled.");
            return new OperationResult(false, 1, "Operation was cancelled.");
        }
        catch (Exception ex)
        {
            log.Error(ex.Message);
            return new OperationResult(false, 1, ex.Message);
        }
    }

    private static int RunWithConsoleCapture(Func<int> action, IOperationLog log)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var forwarder = new LogForwardingWriter(log);
        try
        {
            Console.SetOut(forwarder);
            Console.SetError(forwarder);
            return action();
        }
        finally
        {
            forwarder.Flush();
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private sealed class LogForwardingWriter : TextWriter
    {
        private readonly IOperationLog _log;
        private readonly StringBuilder _buffer = new();

        public LogForwardingWriter(IOperationLog log)
        {
            _log = log;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(char value)
        {
            if (value == '\n')
            {
                EmitLine();
                return;
            }

            if (value == '\r')
            {
                return;
            }

            _buffer.Append(value);
        }

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            foreach (var ch in value)
            {
                Write(ch);
            }
        }

        public override void WriteLine(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _buffer.Append(value);
            }
            EmitLine();
        }

        public override void WriteLine()
        {
            EmitLine();
        }

        public override void Flush()
        {
            if (_buffer.Length > 0)
            {
                EmitLine();
            }
        }

        private void EmitLine()
        {
            var line = _buffer.ToString();
            _buffer.Clear();

            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (line.StartsWith("WARNING", StringComparison.OrdinalIgnoreCase))
            {
                _log.Warning(line);
            }
            else if (line.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                _log.Error(line);
            }
            else
            {
                _log.Info(line);
            }
        }
    }
}
