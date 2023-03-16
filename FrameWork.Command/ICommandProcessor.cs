namespace FrameWork.Command;

public interface ICommandProcessor
{
    Task PreExecute<TIn, TOut>(ICommand<TIn, TOut> command);
    Task PostExecute<TIn, TOut>(ICommand<TIn, TOut> command, CommandResult<TOut> result);
}