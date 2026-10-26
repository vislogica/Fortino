namespace Fortino.Web.Infrastructure;

// Registro in memoria: tiene le ultime righe di log, mostrate nella pagina "Registro".
// I capitoli sui log (2 e 14) mostrano cosa finisce qui dentro.
public sealed record LogRecord(DateTime At, LogLevel Level, string Category, string Message);

public sealed class InMemoryLogStore
{
    private const int Capacity = 1000;
    private readonly LinkedList<LogRecord> _records = new();
    private readonly Lock _lock = new();

    public void Add(LogRecord record)
    {
        lock (_lock)
        {
            _records.AddFirst(record);
            if (_records.Count > Capacity) _records.RemoveLast();
        }
    }

    public IReadOnlyList<LogRecord> Snapshot()
    {
        lock (_lock) return [.. _records];
    }

    public void Clear()
    {
        lock (_lock) _records.Clear();
    }
}

public sealed class InMemoryLoggerProvider(InMemoryLogStore store) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new InMemoryLogger(store, categoryName);

    public void Dispose() { }

    private sealed class InMemoryLogger(InMemoryLogStore store, string category) : ILogger
    {
        // Si tengono i messaggi di Fortino e solo gli avvisi del framework, per non affogare nel rumore.
        public bool IsEnabled(LogLevel logLevel) =>
            category.StartsWith("Fortino", StringComparison.Ordinal)
                ? logLevel >= LogLevel.Information
                : logLevel >= LogLevel.Warning;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            if (exception is not null) message += Environment.NewLine + exception.GetType().Name + ": " + exception.Message;
            store.Add(new LogRecord(DateTime.Now, logLevel, category, message));
        }
    }
}
