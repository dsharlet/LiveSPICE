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
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Tests
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("LiveSPICE CLI")
                .WithGlobalOption(GlobalOptions.Verbosity)
                .WithSubCommand(new TestCommand())
                .WithSubCommand(new BenchmarkCommand())
                .WithSubCommand(new OptimizeCommand())
                .WithCommand("color", "color console test", cmd =>
                {
                    var input = new Argument<string>("text", "text to display");
                    cmd.WithArgument(input);
                    cmd.SetHandler((input, log) =>
                    {
                        log.WriteLine(MessageType.Info, input);
                    }, input, Bind.FromServiceProvider<ILog>());
                });

            var commandLineBuilder = new CommandLineBuilder(rootCommand);

            commandLineBuilder.AddMiddleware(ctx =>
            {
                var verbosity = ctx.ParseResult.GetValueForOption(GlobalOptions.Verbosity);
                ctx.BindingContext.AddService<ILog>(s => new ConsoleLog
                {
                    Verbosity = verbosity
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
