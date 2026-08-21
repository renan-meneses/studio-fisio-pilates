using Microsoft.Extensions.Logging;

namespace Clinica.UnitTests.Observability;

/// <summary>
/// ILogger simples que captura mensagens e scopes para asserção direta
/// (o formatter de console não é necessário: os scopes são o contrato).
/// </summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> propriedades)
        {
            _entries.Add(new LogEntry(LogLevel.None, string.Empty, propriedades.ToList()));
        }
        return new NoopDisposable();
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new LogEntry(logLevel, formatter(state, exception), null));
    }

    public sealed record LogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyList<KeyValuePair<string, object?>>? Scope);

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}