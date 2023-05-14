using System;
using System.Collections.Generic;
using System.CommandLine.Invocation;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine.Binding;

namespace Tests
{
    public static class CommandExtensions
    {
        public static Command WithArgument<T>(this Command command, Argument<T> argument)
        {
            command.AddArgument(argument);
            return command;
        }

        public static Command WithArgument<T>(this Command command, string name, string description, ArgumentArity? arity = null)
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

        public static TCommand WithSubCommand<TCommand>(this TCommand command, Command subCommand)
            where TCommand : Command
        {
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


        public static void SetHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            this Command command,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> handle,
            IValueDescriptor<T1> symbol1,
            IValueDescriptor<T2> symbol2,
            IValueDescriptor<T3> symbol3,
            IValueDescriptor<T4> symbol4,
            IValueDescriptor<T5> symbol5,
            IValueDescriptor<T6> symbol6,
            IValueDescriptor<T7> symbol7,
            IValueDescriptor<T8> symbol8,
            IValueDescriptor<T9> symbol9)
        {
            command.SetHandler(
            context =>
            {
                var value1 = GetValueForHandlerParameter(symbol1, context);
                var value2 = GetValueForHandlerParameter(symbol2, context);
                var value3 = GetValueForHandlerParameter(symbol3, context);
                var value4 = GetValueForHandlerParameter(symbol4, context);
                var value5 = GetValueForHandlerParameter(symbol5, context);
                var value6 = GetValueForHandlerParameter(symbol6, context);
                var value7 = GetValueForHandlerParameter(symbol7, context);
                var value8 = GetValueForHandlerParameter(symbol8, context);
                var value9 = GetValueForHandlerParameter(symbol9, context);

                handle(value1!, value2!, value3!, value4!, value5!, value6!, value7!, value8!, value9!);
            });
        }

        public static void SetHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            this Command command,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> handle,
            IValueDescriptor<T1> symbol1,
            IValueDescriptor<T2> symbol2,
            IValueDescriptor<T3> symbol3,
            IValueDescriptor<T4> symbol4,
            IValueDescriptor<T5> symbol5,
            IValueDescriptor<T6> symbol6,
            IValueDescriptor<T7> symbol7,
            IValueDescriptor<T8> symbol8,
            IValueDescriptor<T9> symbol9,
            IValueDescriptor<T10> symbol10)
        {
            command.SetHandler(
            context =>
            {
                var value1 = GetValueForHandlerParameter(symbol1, context);
                var value2 = GetValueForHandlerParameter(symbol2, context);
                var value3 = GetValueForHandlerParameter(symbol3, context);
                var value4 = GetValueForHandlerParameter(symbol4, context);
                var value5 = GetValueForHandlerParameter(symbol5, context);
                var value6 = GetValueForHandlerParameter(symbol6, context);
                var value7 = GetValueForHandlerParameter(symbol7, context);
                var value8 = GetValueForHandlerParameter(symbol8, context);
                var value9 = GetValueForHandlerParameter(symbol9, context);
                var value10 = GetValueForHandlerParameter(symbol10, context);

                handle(value1!, value2!, value3!, value4!, value5!, value6!, value7!, value8!, value9!, value10!);
            });
        }

        public static void SetHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
    this Command command,
    Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> handle,
    IValueDescriptor<T1> symbol1,
    IValueDescriptor<T2> symbol2,
    IValueDescriptor<T3> symbol3,
    IValueDescriptor<T4> symbol4,
    IValueDescriptor<T5> symbol5,
    IValueDescriptor<T6> symbol6,
    IValueDescriptor<T7> symbol7,
    IValueDescriptor<T8> symbol8,
    IValueDescriptor<T9> symbol9,
    IValueDescriptor<T10> symbol10,
    IValueDescriptor<T11> symbol11)
        {
            command.SetHandler(
            context =>
            {
                var value1 = GetValueForHandlerParameter(symbol1, context);
                var value2 = GetValueForHandlerParameter(symbol2, context);
                var value3 = GetValueForHandlerParameter(symbol3, context);
                var value4 = GetValueForHandlerParameter(symbol4, context);
                var value5 = GetValueForHandlerParameter(symbol5, context);
                var value6 = GetValueForHandlerParameter(symbol6, context);
                var value7 = GetValueForHandlerParameter(symbol7, context);
                var value8 = GetValueForHandlerParameter(symbol8, context);
                var value9 = GetValueForHandlerParameter(symbol9, context);
                var value10 = GetValueForHandlerParameter(symbol10, context);
                var value11 = GetValueForHandlerParameter(symbol11, context);

                handle(value1!, value2!, value3!, value4!, value5!, value6!, value7!, value8!, value9!, value10!, value11!);
            });
        }

        private static T? GetValueForHandlerParameter<T>(
            IValueDescriptor<T> symbol,
            InvocationContext context)
        {
            if (symbol is IValueSource valueSource &&
                valueSource.TryGetValue(symbol, context.BindingContext, out var boundValue) &&
                boundValue is T value)
            {
                return value;
            }
            else
            {
                return symbol switch
                {
                    Argument<T> argument => context.ParseResult.GetValueForArgument(argument),
                    Option<T> option => context.ParseResult.GetValueForOption(option),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
        }
    }
}