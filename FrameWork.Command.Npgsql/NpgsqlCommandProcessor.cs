using System.Data;
using System.Transactions;
using Npgsql;

namespace FrameWork.Command.Npgsql;

public class NpgsqlCommandProcessor : ICommandProcessor
{
    private readonly NpgsqlDataSource dataSource;

    public NpgsqlCommandProcessor(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public async Task PreExecute<TIn, TOut>(ICommand<TIn, TOut> command)
    {
        var npgsqlCommandBase = command as NpgsqlCommandBase<TIn, TOut>; 
        
        if (npgsqlCommandBase != null && npgsqlCommandBase.Connection == null)
        {
            await npgsqlCommandBase.LoadNpgsqlConnectionInfo(dataSource);
        }
    }

    public async Task PostExecute<TIn, TOut>(ICommand<TIn, TOut> command, CommandResult<TOut> result)
    {
        var npgsqlCommandBase = command as NpgsqlCommandBase<TIn, TOut>;

        if (npgsqlCommandBase != null && !result.IsSuccessful && npgsqlCommandBase.Connection.State != ConnectionState.Closed)
        {
            await npgsqlCommandBase.Transaction.RollbackAsync();
            await npgsqlCommandBase.Connection.DisposeAsync();
        }
        
        if (npgsqlCommandBase != null && result.IsSuccessful 
            && npgsqlCommandBase.Connection.State != ConnectionState.Closed)
        {
            var stacktrace = new System.Diagnostics.StackTrace().ToString();
            if (npgsqlCommandBase.IsRootCommand)
            {
                await npgsqlCommandBase.Transaction.CommitAsync();
            }
        }
    }
}