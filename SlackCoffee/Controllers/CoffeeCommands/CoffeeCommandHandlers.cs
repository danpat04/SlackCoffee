using Microsoft.Extensions.Logging;
using SlackCoffee.Models;
using SlackCoffee.Services;
using SlackCoffee.Utils;
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
            if (ForManager)
                return $"[운영자 전용] {Description}";
            return Description;
        }

        public override int CompareTo(object other)
        {
            if (ForManager == ((CoffeeCommand)other).ForManager) return 0;
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

        private SlackResponse Ok(string text, bool inChannel = false)
        {
            return inChannel ? SimpleResponse.InChannel(text) : SimpleResponse.Ephemeral(text);
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
    }
}
