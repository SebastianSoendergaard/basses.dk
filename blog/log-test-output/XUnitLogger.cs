using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace Tests.Common.Logging
{
    // Taken from: https://www.meziantou.net/how-to-get-asp-net-core-logs-in-the-output-of-xunit-tests.htm

    internal sealed class XUnitLogger<T> : XUnitLogger, ILogger<T>
    {
        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider)
            : base(testOutputHelper, scopeProvider, typeof(T).FullName)
        {
        }
    }

    internal class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;
        private readonly LoggerExternalScopeProvider _scopeProvider;

        public static ILogger CreateLogger(ITestOutputHelper testOutputHelper) => new XUnitLogger(testOutputHelper, new LoggerExternalScopeProvider(), "");
        public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper) => new XUnitLogger<T>(testOutputHelper, new LoggerExternalScopeProvider());

        public XUnitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName)
        {
            _testOutputHelper = testOutputHelper;
            _scopeProvider = scopeProvider;
            _categoryName = categoryName;
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable BeginScope<TState>(TState state) => _scopeProvider.Push(state);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var logText = formatter(state, exception).Replace(Environment.NewLine, Environment.NewLine + "        ");

            var sb = new StringBuilder();
            sb.Append("\n").Append(GetLogLevelString(logLevel))
              .Append("[").Append(_categoryName).Append("] ")
              .Append("\n        ").Append(logText);


            if (exception != null)
            {
                sb.Append('\n').Append(exception);
            }

            // Append scopes
            _scopeProvider.ForEachScope((scope, state) =>
            {
                state.Append("\n        => ");
                state.Append(JsonSerializer.Serialize(scope));
            }, sb);

            try
            {
                _testOutputHelper.WriteLine(sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{nameof(XUnitLogger)} catched an unexpected exception while writing to test log: {ex.Message + Environment.NewLine + ex.StackTrace}");
            }
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "TRACE   ",
                LogLevel.Debug => "DEBUG   ",
                LogLevel.Information => "INFO    ",
                LogLevel.Warning => "WARNING ",
                LogLevel.Error => "ERROR   ",
                LogLevel.Critical => "CRITICAL",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }
    }

    internal sealed class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly LoggerExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_testOutputHelper, _scopeProvider, categoryName);
        }

        public void Dispose()
        {
        }
    }
}
