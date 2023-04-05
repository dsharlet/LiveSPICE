using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public static class CommandExtensions
    {
        public static Command WithArgument<T>(this Command command, Argument<T> argument)
        {
            command.AddArgument(argument);
            return command;
        }

        public static Command WithArgument<T>(this Command command, string name, string description, IArgumentArity? arity = null)
        {
            var argument = new Argument<T>(name)
            {
                Description = description,
                Arity = arity ?? ArgumentArity.ExactlyOne
            };

            return command.WithArgument(argument);
        }

        public static TCommand WithCommand<TCommand>(this TCommand command, string name, string description, Action<Command> commandBuilder)
            where TCommand : Command
        {
            var subCommand = new Command(name, description);
            commandBuilder.Invoke(subCommand);
            command.AddCommand(subCommand);
            return command;
        }

        public static TCommand WithGlobalOption<TCommand>(this TCommand command, Option option)
            where TCommand : Command
        {
            command.AddGlobalOption(option);
            return command;
        }

        public static Command WithHandler(this Command command, ICommandHandler handler)
        {
            command.Handler = handler;
            return command;
        }

        public static Command WithOption<T>(this Command command, Option<T> option)
        {
            command.AddOption(option);
            return command;
        }

        public static Command WithOption<T>(this Command command, string[] aliases, string description)
        {
            var option = new Option<T>(aliases, description);
            command.AddOption(option);
            return command;
        }

        public static Command WithOption<T>(this Command command, string[] aliases, Func<T> getDefaultValue, string description)
        {
            var option = new Option<T>(
                aliases,
                getDefaultValue,
                description
            );

            command.AddOption(option);
            return command;
        }
    }
}