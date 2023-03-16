namespace FrameWork.Command;

public interface ICommand<in TIn, TOut>
{
    IServiceProvider ServiceProvider { get; set; }
    
    Task<CommandResult<TOut>> Execute(TIn input);
}