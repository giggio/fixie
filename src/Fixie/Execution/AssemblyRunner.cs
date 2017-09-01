namespace Fixie.Execution
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Reflection;
    using Cli;

    public class AssemblyRunner
    {
        const int Success = 0;
        const int FatalError = -1;

        public static int Main(string[] arguments)
        {
            try
            {
                var options = CommandLine.Parse<Options>(arguments);
                options.Validate();

                var assemblyFullPath = Assembly.GetEntryAssembly().Location;

                var assemblyDirectory = Path.GetDirectoryName(assemblyFullPath);

                var command = "Run Assembly";

                var namedPipe = Environment.GetEnvironmentVariable("FIXIE_NAMED_PIPE");
                if (namedPipe != null)
                {
                    using (var pipeClient = new NamedPipeClientStream(namedPipe))
                    using (var reader = new BinaryReader(pipeClient))
                        command = reader.ReadString();
                }

                using (var executionProxy = new ExecutionProxy(assemblyDirectory))
                {
                    if (command == "Discover Methods")
                    {
                        executionProxy.DiscoverMethods(assemblyFullPath, arguments);
                        return Success;
                    }

                    if (command == "Run Methods")
                        return executionProxy.RunMethods(assemblyFullPath, arguments, new string[] { }/*methods from command*/);

                    return executionProxy.RunAssembly(assemblyFullPath, arguments);
                }
            }
            catch (Exception exception)
            {
                using (Foreground.Red)
                    Console.WriteLine($"Fatal Error: {exception}");

                return FatalError;
            }
        }
    }
}