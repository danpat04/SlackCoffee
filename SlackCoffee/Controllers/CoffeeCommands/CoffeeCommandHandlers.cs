﻿using Microsoft.Extensions.Logging;
using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.Utils;
using System.Text;
using System.Threading.Tasks;

namespace SlackCoffee.Controllers.CoffeeCommands
{
    public class CoffeeCommand : Command
    {
        public readonly string Description;
        public readonly bool ForManager;

        public CoffeeCommand(string id, string description, bool forManager)
            : base(id)
        {
            Description = description;
            ForManager = forManager;
        }

        public override string MakeDescription()
        {
            return Description;
        }

        public override int CompareTo(object other)
        {
            var oc = (CoffeeCommand)other;
            if (ForManager == oc.ForManager) return Id.CompareTo(oc.Id);
            return ForManager ? -1 : 1;
        }
    }

    public class CoffeeCommandAttribute: CommandAttribute
    {
        public CoffeeCommandAttribute(string command, string description, bool forManager)
            : base(new CoffeeCommand(command, description, forManager)) { }

        public CoffeeCommandAttribute(string command, string description)
            : base(new CoffeeCommand(command, description, false)) { }
    }

    public interface ICoffeeCommandHandlers
    {
        Task<SlackResponse> HandleCommandAsync(CoffeeService coffee, User user, string commandId, string options, ILogger logger = null);
    }

    public partial class CoffeeCommandHandlers : ICoffeeCommandHandlers
    {
        private static CommandHandlers<CoffeeCommandHandlers, CoffeeCommand> handlers = new  CommandHandlers<CoffeeCommandHandlers, CoffeeCommand>();

        private MultipleResponse Ok(string text = null, bool inChannel = false)
        {
            return new MultipleResponse(text, inChannel);
        }

        public async Task<SlackResponse> HandleCommandAsync(CoffeeService coffee, User user, string commandId, string options, ILogger logger = null)
        {
            if (!handlers.TryGetHandler(commandId, out var handlerInfo))
                throw new CommandNotFoundException();

            var command = handlerInfo.Key;
            var methodInfo = handlerInfo.Value;

            if (command.ForManager && !user.IsManager)
                throw new OnlyForManagerException();

            return await (Task<SlackResponse>)methodInfo.Invoke(this, new object[] { coffee, user, options });
        }

        [CoffeeCommand("도움", "명령어 목록을 표시합니다", false)]
        public async Task<SlackResponse> GetHelp(CoffeeService coffee, User user, string text)
        {
            var sb = new StringBuilder();
            foreach ((var command, var name, var description) in handlers.GetDescriptions())
            {
                if (command.ForManager && !user.IsManager)
                    continue;

                sb.AppendLine($"*{name}* : {description}");
            }
            return Ok(sb.ToString());
        }
    }
}
