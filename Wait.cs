using System.Text.Json;
using Zapqio.Runner.Core;

namespace ExampleModule
{
    /// <summary>
    /// Metoda, która trwa: śpi tyle sekund, ile dostała, i oddaje kontekst zadania. Służy do
    /// oglądania, co platforma robi, gdy runner zniknie w trakcie kroku (wynik nieznany, ponowienie,
    /// wynik dosłany po powrocie), i pokazuje, jak moduł czyta <see cref="JobContext.Current"/>.
    /// </summary>
    public class Wait : IRunnerMethod
    {
        /// <inheritdoc />
        public string NameMethod() => "Wait";

        /// <inheritdoc />
        public Type InData() => typeof(WaitIn);

        /// <inheritdoc />
        public Type OutData() => typeof(WaitOut);

        /// <inheritdoc />
        public async Task<string> Run(string data)
        {
            var input = string.IsNullOrWhiteSpace(data)
                ? new WaitIn()
                : JsonSerializer.Deserialize<WaitIn>(data) ?? new WaitIn();

            // Ten sam JobId przy każdej próbie i każdym ponowieniu tego zadania, inny AttemptId.
            // Gdyby ta metoda wystawiała fakturę, tu należałoby sprawdzić, czy faktura z takim JobId
            // już istnieje, i oddać ją zamiast tworzyć drugą - platforma wysyła zadanie ponownie
            // tylko wtedy, gdy nie wie, czy poprzednia próba doszła do końca.
            var context = JobContext.Current;
            Console.WriteLine($"Wait {input.Seconds}s; job {context?.JobId} attempt {context?.AttemptId} method {context?.MethodName}");

            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(input.Seconds, 0, 3600)));

            return JsonSerializer.Serialize(new WaitOut
            {
                JobId = context?.JobId,
                AttemptId = context?.AttemptId,
                WaitedSeconds = input.Seconds
            });
        }
    }

    /// <summary>Input data for Wait method.</summary>
    public class WaitIn
    {
        /// <summary>How long to sleep, in seconds (0-3600). Default 5.</summary>
        public int Seconds { get; set; } = 5;
    }

    /// <summary>Output data from Wait method.</summary>
    public class WaitOut
    {
        /// <summary>Operation id from JobContext - the same on every attempt of this job.</summary>
        public Guid? JobId { get; set; }

        /// <summary>Attempt id from JobContext - different on every dispatch.</summary>
        public Guid? AttemptId { get; set; }

        public int WaitedSeconds { get; set; }
    }
}
