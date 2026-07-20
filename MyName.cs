using Zapqio.Runner.Core;

namespace ExampleModule
{
    /// <summary>
    /// Example injectable service for runner modules.
    /// Demonstrates how to use IRunnerInjection for DI.
    /// </summary>
    public class MyName : IRunnerInjection
    {
        /// <summary>
        /// Name used in greeting messages.
        /// </summary>
        public string Name { get; set; } = "Jon";
    }
}
