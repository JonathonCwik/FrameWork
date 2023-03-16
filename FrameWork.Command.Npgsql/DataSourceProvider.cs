using Npgsql;

namespace FrameWork.Command.Npgsql;

public class DataSourceProvider : IDataSourceProvider
{
    private readonly NpgsqlDataSource dataSource;

    public DataSourceProvider(NpgsqlDataSource dataSource)
    {
        this.dataSource = dataSource;
    }

    public NpgsqlDataSource Get()
    {
        return dataSource;
    }
}