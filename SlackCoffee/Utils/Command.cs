using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SlackCoffee.Utils
{
    public class CommandHandlerException : Exception
    {
        public CommandHandlerException(string message, Exception inner) : base(message, inner) { }
    }

    public abstract class BaseCommand
    {
        public readonly string Id;

        public BaseCommand(string id)
        {
            Id = id;
        }
    }

    public abstract class Command<TUser> : BaseCommand, IComparable
    {
        public Command(string id) : base(id) { }

        public abstract bool Authorized(TUser user);

        public abstract string MakeDescription();

        public abstract int CompareTo(object other);
    }

    public class CommandAttribute : Attribute
    {
        public readonly BaseCommand Command;

        public CommandAttribute(BaseCommand command)
        {
            Command = command;
        }
    }

    public class CommandHandlers<T, TUser>
    {
        public delegate Task<IActionResult> CommandHandler(string text);

        private readonly Dictionary<string, KeyValuePair<Command<TUser>, System.Reflection.MethodInfo>> handlers =
            new Dictionary<string, KeyValuePair<Command<TUser>, System.Reflection.MethodInfo>>();

        public CommandHandlers()
        {
            var type = typeof(T);
            var attrType = typeof(CommandAttribute);

            foreach (var methodInfo in type.GetMethods())
            {
                if (methodInfo.GetCustomAttributes(attrType, true).FirstOrDefault() is CommandAttribute attr &&
                    attr.Command is Command<TUser> c)
                {
                    var handler = new KeyValuePair<Command<TUser>, System.Reflection.MethodInfo>(c, methodInfo);
                    handlers.Add(c.Id, handler);
                }
            }
        }

        public CommandHandler GetHandler(T target, TUser user, string commandId)
        {
            if (!handlers.TryGetValue(commandId, out var value))
                return null;

            var command = value.Key;
            var methodInfo = value.Value;

            if (!command.Authorized(user))
                return null;

            return async (text) =>
            {
                object result;
                Task<IActionResult> task;
                try
                {
                    result = methodInfo.Invoke(target, new object[] { user, text });
                }
                catch (Exception e)
                {
                    if (e is TargetParameterCountException || e is ArgumentException)
                        throw new CommandHandlerException("Error occurred while handling command", e);
                    throw;
                }

                try
                {
                    task = (Task<IActionResult>)result;
                }
                catch (InvalidCastException e)
                {
                    throw new CommandHandlerException("Error occurred while handling command", e);
                }
                return await task;
            };
        }

        public IEnumerable<KeyValuePair<string, string>> GetDescriptions()
        {
            var commands = handlers.Values.Select(kv => kv.Key).ToList();
            commands.Sort();

            return commands.Select(c => new KeyValuePair<string, string>(c.Id, c.MakeDescription()));
        }
    }
}
