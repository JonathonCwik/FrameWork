namespace FrameWork.Command;

public interface ICommandExecutor
{
    Task<CommandResult<TOut>> Execute<TIn, TOut>(ICommand<TIn, TOut> command, TIn input);
}