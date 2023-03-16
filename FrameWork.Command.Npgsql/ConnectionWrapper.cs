using Npgsql;

namespace FrameWork.Command.Npgsql;

public static class ConnectionWrapper
{
    /// <summary>
    /// If a connection is not provided, it will create a connection so the execute function has a connection to the db.
    /// If a transaction is provided, it will auto rollback on exception, but will NOT auto commit. Caller is responsible
    /// for committing.
    /// </summary>
    /// <param name="dataSource"></param>
    /// <param name="execute"></param>
    /// <param name="existingConnection"></param>
    /// <param name="transaction"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<T> UseOrGetConnection<T>(NpgsqlDataSource dataSource,
        Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> execute,
        NpgsqlConnection? existingConnection = null, 
        NpgsqlTransaction? transaction = null)
    {
        var createdConnection = false;
        if (existingConnection == null)
        {
            existingConnection = dataSource.CreateConnection();
            await existingConnection.OpenAsync();
            createdConnection = true;
            transaction = null;
        }

        T results;
        
        try
        {
            results = await execute(existingConnection, transaction);
        }
        catch (Exception)
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }
            throw;
        }

        if (createdConnection)
        {
            await existingConnection.CloseAsync();
            await existingConnection.DisposeAsync();
        }

        return results;
    }
}