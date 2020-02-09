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

    public abstract class Command : IComparable
    {
        public readonly string Id;

        public Command(string id)
        {
            Id = id;
        }

        public abstract string MakeDescription();

        public abstract int CompareTo(object other);
    }

    public class CommandAttribute : Attribute
    {
        public readonly Command Command;

        public CommandAttribute(Command command)
        {
            Command = command;
        }
    }

    public class CommandHandlers<T, TCommand> where TCommand : Command
    {
        public delegate Task<SlackResponse> CommandHandler(string text);

        private readonly Dictionary<string, KeyValuePair<TCommand, MethodInfo>> handlers =
            new Dictionary<string, KeyValuePair<TCommand, MethodInfo>>();

        public CommandHandlers()
        {
            var type = typeof(T);
            var attrType = typeof(CommandAttribute);

            foreach (var methodInfo in type.GetMethods())
            {
                if (methodInfo.GetCustomAttributes(attrType, true).FirstOrDefault() is CommandAttribute attr &&
                    attr.Command is TCommand c)
                {
                    var handler = new KeyValuePair<TCommand, MethodInfo>(c, methodInfo);
                    handlers.Add(c.Id, handler);
                }
            }
        }

        public bool TryGetHandler(string commandId, out KeyValuePair<TCommand, MethodInfo> handlerInfo)
        {
            return handlers.TryGetValue(commandId, out handlerInfo);
        }

        public IEnumerable<KeyValuePair<string, string>> GetDescriptions()
        {
            var commands = handlers.Values.Select(kv => kv.Key).ToList();
            commands.Sort();

            return commands.Select(c => new KeyValuePair<string, string>(c.Id, c.MakeDescription()));
        }
    }
}
