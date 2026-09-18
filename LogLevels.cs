using System.Text.Json;
using Microsoft.Extensions.Logging;
using Zapqio.Runner.Core;

namespace ExampleModule
{
    /// <summary>
    /// Jeden wpis na każdy poziom, każdym z trzech kanałów, jakie moduł ma do dyspozycji od
    /// <c>Module.Core</c> 1.2: statyczny <see cref="RunnerLog"/>, wstrzyknięty <see cref="ILogger{T}"/>
    /// i stara konsola. Służy do oglądania w panelu, co runner przepuszcza przy danym progu
    /// <c>MinRemoteLogLevel</c>, jak panel koloruje poziomy i jak wygląda wpis z wyjątkiem.
    /// </summary>
    public class LogLevels : IRunnerMethod
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        private readonly ILogger<LogLevels> _logger;

        /// <summary>
        /// <c>ILogger&lt;T&gt;</c> rejestruje w kontenerze modułów runner (od 0.1.11) i kieruje w to samo
        /// miejsce co <see cref="RunnerLog"/>, z tym samym progiem. Pod starszym runnerem ten konstruktor
        /// nie ma czego dostać: metoda nie powstanie, a pozostałe metody modułu działają normalnie.
        /// </summary>
        public LogLevels(ILogger<LogLevels> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public string NameMethod() => "Log levels";

        /// <inheritdoc />
        public Type InData() => typeof(LogLevelsIn);

        /// <inheritdoc />
        public Type OutData() => typeof(LogLevelsOut);

        /// <inheritdoc />
        public async Task<string> Run(string data)
        {
            var input = string.IsNullOrWhiteSpace(data)
                ? new LogLevelsIn()
                : JsonSerializer.Deserialize<LogLevelsIn>(data, Json) ?? new LogLevelsIn();
            var text = input.Text ?? "";

            // 1. RunnerLog - jawnie nazwana waga. Statyczny, więc działa też z metod pomocniczych,
            //    klas statycznych i wątków, które metoda uruchomi. IsEnabled przed Debug oszczędza
            //    budowania treści, której runner i tak nie wyśle (Debug jest domyślnie poniżej progu).
            if (RunnerLog.IsEnabled(RunnerLogLevel.Debug))
            {
                RunnerLog.Debug($"[RunnerLog] Debug: {text} (domyślnie nie idzie do panelu, zostaje w logu plikowym runnera)");
            }
            RunnerLog.Info($"[RunnerLog] Info: {text}");
            RunnerLog.Warning($"[RunnerLog] Warning: {text}");
            RunnerLog.Error($"[RunnerLog] Error: {text}");
            RunnerLog.Critical($"[RunnerLog] Critical: {text}");

            // 2. Wstrzyknięty ILogger - to samo ujście i ten sam próg. Trace schodzi do Debug, bo
            //    protokół nie ma osobnego poziomu poniżej Debug.
            _logger.LogTrace("[ILogger] Trace -> Debug: {Text}", text);
            _logger.LogDebug("[ILogger] Debug: {Text}", text);
            _logger.LogInformation("[ILogger] Information -> Info: {Text}", text);
            _logger.LogWarning("[ILogger] Warning: {Text}", text);
            _logger.LogError("[ILogger] Error: {Text}", text);
            _logger.LogCritical("[ILogger] Critical: {Text}", text);

            // 3. Konsola - jak w modułach sprzed 1.2: stdout to Info, stderr to Error. Próg runnera
            //    obejmuje i ten kanał, więc przy MinRemoteLogLevel=Warning stdout nie dojdzie.
            Console.WriteLine($"[Console.Out] -> Info: {text}");
            Console.Error.WriteLine($"[Console.Error] -> Error: {text}");

            if (input.WithException)
            {
                try
                {
                    throw new InvalidOperationException("boom - wyjątek na pokaz");
                }
                catch (Exception ex)
                {
                    // Treść wyjątku (ze śladem stosu) dopisuje się pod wiadomością, w jednym wpisie.
                    RunnerLog.Error(ex, "[RunnerLog] Error z wyjątkiem");
                    _logger.LogError(ex, "[ILogger] Error z wyjątkiem");
                }
            }

            // Wątek uruchomiony przez metodę: kontekst zadania płynie przez AsyncLocal, więc wpis
            // trafia do właściwego zadania, choć powstał poza wątkiem, na którym runner wywołał Run.
            await Task.Run(() => RunnerLog.Warning($"[RunnerLog] Warning z Task.Run, zadanie {JobContext.Current?.JobId}"));

            // Co przy bieżącym progu runnera w ogóle wychodzi na łącze - do porównania z historią w panelu.
            return JsonSerializer.Serialize(new LogLevelsOut
            {
                Debug = RunnerLog.IsEnabled(RunnerLogLevel.Debug),
                Info = RunnerLog.IsEnabled(RunnerLogLevel.Info),
                Warning = RunnerLog.IsEnabled(RunnerLogLevel.Warning),
                Error = RunnerLog.IsEnabled(RunnerLogLevel.Error),
                Critical = RunnerLog.IsEnabled(RunnerLogLevel.Critical),
            });
        }
    }

    /// <summary>Input data for Log levels method.</summary>
    public class LogLevelsIn
    {
        /// <summary>Text repeated in every log entry. Default "hello".</summary>
        public string Text { get; set; } = "hello";

        /// <summary>Also log a caught exception on the Error level (RunnerLog and ILogger). Default false.</summary>
        public bool WithException { get; set; }
    }

    /// <summary>Output data from Log levels method: which levels the runner sends to the platform at its current threshold.</summary>
    public class LogLevelsOut
    {
        public bool Debug { get; set; }

        public bool Info { get; set; }

        public bool Warning { get; set; }

        public bool Error { get; set; }

        public bool Critical { get; set; }
    }
}
