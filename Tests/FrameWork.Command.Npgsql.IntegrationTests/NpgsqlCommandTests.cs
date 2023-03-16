using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace FrameWork.Command.Npgsql;

public class NpgsqlCommandTests
{
    private CommandExecutor commandExecutor;
    IContainer? pgContainer;
    private static NpgsqlDataSourceBuilder dataSourceBuilder;
    
    [SetUp]
    public async Task Setup()
    {
        pgContainer = new ContainerBuilder()
            .WithName(Guid.NewGuid().ToString())
            .WithImage("postgres:14.6")
            .WithEnvironment("POSTGRES_PASSWORD", "pass")
            .WithEnvironment("POSTGRES_DB", "postgres")
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(5432))
            .Build();

        await pgContainer.StartAsync();
        
        dataSourceBuilder = new NpgsqlDataSourceBuilder(
            $"Server=localhost;Port={pgContainer.GetMappedPublicPort(5432).ToString()};Database=postgres;User Id=postgres;Password=pass");

        commandExecutor = new CommandExecutor(Mock.Of<IServiceProvider>(), 
            Mock.Of<ILogger<CommandExecutor>>(),
            new NpgsqlCommandProcessor(dataSourceBuilder.Build()));

        await using var connection = dataSourceBuilder.Build().CreateConnection();
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE users (
                            id SERIAL PRIMARY KEY,
                            name text NOT NULL
            );
            CREATE TABLE orgs (
                            id SERIAL PRIMARY KEY,
                            name text NOT NULL,
                            owner_id integer NOT NULL,
                            CONSTRAINT fk_user FOREIGN KEY(owner_id) REFERENCES users(id)
            );
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    [Test]
    public async Task ExecuteCreateOrgWithoutUserIDCreatesBoth()
    {
        var orgName = "TestOrg";
        
        var result = await commandExecutor.Execute(new CreateOrgCommand(commandExecutor), new CreateOrg()
        {
            Name = orgName,
            OwnerName = "NewUser"
        });
        
        Assert.That(result.IsSuccessful, "Failed execution: " + result.Message);

        var users = await GetAllUsers();
        
        Assert.That(users.Count, Is.EqualTo(1));
        Assert.That(users.First().ID, Is.EqualTo(result.SuccessData!.OwnerID));

        var orgs = await GetAllOrgs();
        
        Assert.That(orgs.Count, Is.EqualTo(1));
        Assert.That(orgs.First().Name, Is.EqualTo(orgName));
    }
    
    [Test]
    public async Task ExecuteCreateOrgWithExceptionInCreateUserRollsbackEverything()
    {
        var orgName = "TestOrg";
        
        var result = await commandExecutor.Execute(new CreateOrgCommand(commandExecutor), new CreateOrg
        {
            Name = orgName,
            OwnerName = "NewUser",
            MakeCreateUserFail = true
        });
        
        Assert.That(result.IsSuccessful, Is.False);

        var users = await GetAllUsers();
        
        Assert.That(users.Count, Is.EqualTo(0));
        var orgs = await GetAllOrgs();
        Assert.That(orgs.Count, Is.EqualTo(0));
    }

    [TearDown]
    public async Task ClearDB()
    {
        await using var connection = dataSourceBuilder.Build().CreateConnection();
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM orgs;
            DELETE FROM users;
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<List<User>> GetAllUsers(NpgsqlConnection? conn = null, NpgsqlTransaction? transaction = null)
    {
        var connection = conn?? dataSourceBuilder.Build().CreateConnection();
        if (conn == null)
        {
            await connection.OpenAsync();
        }

        transaction = transaction ?? await connection.BeginTransactionAsync();
        await using var cmd = new NpgsqlCommand("SELECT * FROM users", connection, transaction);

        await using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<User>();
        while (await reader.ReadAsync())
        {
            results.Add(new User
            {
                ID = (int)reader["id"],
                Name = (string)reader["name"]
            });
        }

        return results;
    }
    
    private async Task<List<Org>> GetAllOrgs()
    {
        await using var connection = dataSourceBuilder.Build().CreateConnection();
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM orgs";

        await using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<Org>();
        while (await reader.ReadAsync())
        {
            results.Add(new Org
            {
                ID = (int)reader["id"],
                Name = (string)reader["name"],
                OwnerID = (int)reader["owner_id"]
            });
        }

        return results;
    }

    class CreateOrg
    {
        public string Name { get; set; }
        public int? OwnerID { get; set; }
        public string OwnerName { get; set; }
        public bool MakeCreateUserFail { get; set; } = false;
    }

    class Org
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int OwnerID { get; set; }
    }

    class CreateUser
    {
        public string Name { get; set; }
        public bool TriggerException { get; set; } = false;
    }

    class User
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }

    class CreateOrgCommand : NpgsqlCommandBase<CreateOrg, Org>
    {
        private readonly ICommandExecutor commandExecutor;

        public CreateOrgCommand(ICommandExecutor commandExecutor)
        {
            this.commandExecutor = commandExecutor;
        }
        
        protected override async Task<CommandResult<Org>> ExecuteNpgsqlCommand(CreateOrg input, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            CreateUserCommand createUserCommand = new CreateUserCommand();
            await createUserCommand.LoadExtNpgsqlConnectionInfo(DataSource, connection, transaction);
            
            var ownerID = input.OwnerID;
            
            if (ownerID == null)
            {
                // if the CommandExecutor is not used then exceptions will be thrown rather than caught and the try 
                // catch will need to be here (as on exception we still need to rollback the transaction)
                var result = await commandExecutor.Execute(createUserCommand, new CreateUser { Name = input.OwnerName, TriggerException = input.MakeCreateUserFail});
                if (!result.IsSuccessful)
                {
                    // returning Unsuccessful here will rollback the transaction
                    // No need to even think about transactions
                    return CommandResult<Org>.Unsuccessful("Failed to create org due to failure to create owner", 
                        result.Reason!);
                }

                ownerID = result.SuccessData!.ID;
            }
            
            await using var command = 
                new NpgsqlCommand("INSERT INTO orgs (name, owner_id) VALUES ($1, $2) RETURNING id", 
                    connection, transaction)
                {
                    Parameters =
                    {
                        new NpgsqlParameter { Value = input.Name },
                        new NpgsqlParameter { Value = ownerID.Value }
                    }
                };

            var id = (int)(await command.ExecuteScalarAsync() ?? 0);

            return CommandResult<Org>.Successful(new Org
            {
                ID = id,
                OwnerID = ownerID.Value,
                Name = input.Name
            });
            
        }
    }

    class CreateUserCommand : NpgsqlCommandBase<CreateUser, User>
    {
        protected override async Task<CommandResult<User>> ExecuteNpgsqlCommand(CreateUser input, 
            NpgsqlConnection connection, 
            NpgsqlTransaction transaction)
        {
            await using var command = new NpgsqlCommand("INSERT INTO users (name) VALUES ($1) RETURNING id",
                connection, transaction)
            {
                Parameters = { new NpgsqlParameter { Value = input.Name } }
            };
            
            var id = (int)(await command.ExecuteScalarAsync() ?? 0);

            if (input.TriggerException)
            {
                throw new Exception("Fail happened");
            }
            
            return CommandResult<User>.Successful(new User()
            {
                ID = id,
                Name = input.Name
            });
        }
    }
}