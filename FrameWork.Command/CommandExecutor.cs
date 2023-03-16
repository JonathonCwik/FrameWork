using Microsoft.Extensions.Logging;

namespace FrameWork.Command;

public class CommandExecutor : ICommandExecutor
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<CommandExecutor> logger;
    private readonly ICommandProcessor[] processors;

    public CommandExecutor(IServiceProvider serviceProvider, ILogger<CommandExecutor> logger, params ICommandProcessor[] processors)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
        this.processors = processors;
    }
    
    // HOW DO WE GET EXISTING CONNECTION / TRANSACTION INTO HERE???
    public async Task<CommandResult<TOut>> Execute<TIn, TOut>(ICommand<TIn, TOut> command, TIn input)
    {
        foreach (var processor in processors)
        {
            await processor.PreExecute(command);
        }

        command.ServiceProvider = serviceProvider;
        CommandResult<TOut> result;
        try
        {
            result = await command.Execute(input);
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Error executing {command.GetType().Name}: {e.Message}");
            result = CommandResult<TOut>.Unsuccessful(e.Message, "Exception");
        }

        foreach (var processor in processors)
        {
            await processor.PostExecute(command, result);
        }

        return result;
    }
}