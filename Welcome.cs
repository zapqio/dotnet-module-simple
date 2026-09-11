using System.Text.Json;
using Zapqio.Runner.Core;

namespace ExampleModule
{
    /// <summary>
    /// Example runner method that generates greeting messages.
    /// Demonstrates how to implement IRunnerMethod with dependency injection.
    /// </summary>
    public class Welcome : IRunnerMethod
    {
        private readonly MyName _myName;

        /// <summary>
        /// Creates Welcome method with injected MyName service.
        /// </summary>
        /// <param name="myName">Injected name service</param>
        public Welcome(MyName myName)
        {
            _myName = myName;
        }

        /// <inheritdoc />
        public Type InData()
        {
            return typeof(DataIn);
        }

        /// <inheritdoc />
        public string NameMethod()
        {
            return "Welcome Jon";
        }

        /// <inheritdoc />
        public Type OutData()
        {
            return typeof(DataOut);
        }

        /// <inheritdoc />
        public async Task<string> Run(string data)
        {
            var inData = JsonSerializer.Deserialize<DataIn>(data);
            var outData = new List<string>();
            Console.WriteLine("test log info");
            // Kontekst zadania z hosta runnera (Module.Core 1.1): ten sam JobId przy każdej próbie
            // tego samego zadania, inny AttemptId. Patrz Wait.cs - tam jest z tego użytek.
            var context = JobContext.Current;
            Console.WriteLine($"job {context?.JobId} attempt {context?.AttemptId} method {context?.MethodName}");
            Console.Error.WriteLine("test log error");
            for (int i = 0; i < inData.Count; i++)
            {
                var w = $"Hello, {_myName.Name}!";
                outData.Add(w);
            }
            return JsonSerializer.Serialize(outData);
        }
    }
}
