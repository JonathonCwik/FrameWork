using Npgsql;

namespace FrameWork.Command.Npgsql;

public interface IDataSourceProvider
{
    NpgsqlDataSource Get();
}