using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Circuit;
using Tests.Genetic;
using System.Collections.Generic;
using Util;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using LiveSPICE.CLI.Utils;
using LiveSPICE.CLI.Commands;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using LiveSPICE.CLI;
using LiveSPICE.Cli.Utils;

namespace Tests
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("LiveSPICE CLI")
                .WithGlobalOption(GlobalOptions.SampleRate)
                .WithGlobalOption(GlobalOptions.Oversample)
                .WithGlobalOption(GlobalOptions.Iterations)
                .WithGlobalOption(GlobalOptions.Verbose)
                .WithSubCommand(new TestCommand())
                .WithSubCommand(new BenchmarkCommand())
                .WithSubCommand(new OptimizeCommand());

            var commandLineBuilder = new CommandLineBuilder(rootCommand);

            commandLineBuilder.AddMiddleware(ctx =>
            {
                var verbose = ctx.ParseResult.GetValueForOption(GlobalOptions.Verbose);
                ctx.BindingContext.AddService<ILog>(s => new ConsoleLog
                {
                    Verbosity = verbose ? MessageType.Verbose : MessageType.Info
                });
                ctx.BindingContext.AddService(s => new SchematicReader(s.GetService<ILog>()));
                ctx.BindingContext.AddService(s => new BenchmarkRunner(s.GetService<ILog>()));

            });

            return await commandLineBuilder
                .UseDefaults()
                .Build()
                .InvokeAsync(args);
        }
    }
}
