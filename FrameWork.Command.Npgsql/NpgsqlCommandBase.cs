using System.Data;
using Npgsql;

namespace FrameWork.Command.Npgsql;

public abstract class NpgsqlCommandBase<TIn, TOut> : ICommand<TIn, TOut>
{
    public NpgsqlDataSource DataSource { get; private set; }
    public NpgsqlConnection Connection { get; private set; }
    public NpgsqlTransaction Transaction { get; private set; }
    public IServiceProvider ServiceProvider { get; set; }
    public bool IsRootCommand { get; private set; } = true;

    public async Task LoadExtNpgsqlConnectionInfo(NpgsqlDataSource dataSource, NpgsqlConnection? connection = null,
        NpgsqlTransaction? transaction = null)
    {
        IsRootCommand = false;
        await LoadNpgsqlConnectionInfo(dataSource, connection, transaction);
    }
    
    internal async Task LoadNpgsqlConnectionInfo(NpgsqlDataSource dataSource, NpgsqlConnection? connection = null,
        NpgsqlTransaction? transaction = null)
    {
        DataSource = dataSource;
        Connection = connection ?? dataSource.CreateConnection();
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync();
        }

        if (connection != null && transaction == null)
        {
            Transaction = transaction ?? Connection.BeginTransaction();
        }
        else if (connection == null)
        {
            Transaction = Connection.BeginTransaction();
        }
        else
        {
            Transaction = transaction;
        }
    }
    
    protected abstract Task<CommandResult<TOut>> ExecuteNpgsqlCommand(TIn input, NpgsqlConnection connection, NpgsqlTransaction transaction);
    
    public async Task<CommandResult<TOut>> Execute(TIn input)
    {
        try
        {
            return await ExecuteNpgsqlCommand(input, Connection, Transaction);
        }
        catch (Exception)
        {
            if (Connection.State != ConnectionState.Closed)
            {
                await Transaction.RollbackAsync();
                await Connection.DisposeAsync();
            }

            throw;
        }
    }
}