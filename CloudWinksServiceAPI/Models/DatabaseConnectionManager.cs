using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;

public class DatabaseConnectionManager
{
    private readonly ConcurrentDictionary<int, string> _connectionStrings = new ConcurrentDictionary<int, string>();
    private readonly string _defaultConnectionString;
    private readonly ILogger<DatabaseConnectionManager> _logger;

    public DatabaseConnectionManager(string defaultConnectionString, ILogger<DatabaseConnectionManager> logger)
    {
        _defaultConnectionString = defaultConnectionString ?? throw new ArgumentNullException(nameof(defaultConnectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GetOrAddConnectionString(int appId)
    {
        try
        {
            // Check if the connection string for this appId is already cached
            return _connectionStrings.GetOrAdd(appId, id =>
            {
                // Fetch app-specific details from the database
                var config = FetchAppConfig(id);

                // Construct the connection string
                return new SqlConnectionStringBuilder
                {
                    UserID = config.UserId,
                    Password = config.Password,
                    InitialCatalog = config.Database,
                    DataSource = config.Server,
                    MultipleActiveResultSets = true,
                    TrustServerCertificate = true
                }.ConnectionString;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error generating connection string for App ID {appId}. Falling back to default connection string.");
            return _defaultConnectionString; // Fallback to the default connection string
        }
    }

    private (string UserId, string Password, string Database, string Server) FetchAppConfig(int appId)
    {
        try
        {
            using (var connection = new SqlConnection(_defaultConnectionString))
            {
                connection.Open();

                const string query = "SELECT _userId, _userPassword, _userDatabase, _server FROM dbo.Applications WHERE _appid = @appId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@appId", appId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return (
                                UserId: reader["_userId"].ToString(),
                                Password: reader["_userPassword"].ToString(),
                                Database: reader["_userDatabase"].ToString(),
                                Server: reader["_server"].ToString()
                            );
                        }
                        else
                        {
                            throw new Exception($"App with ID {appId} not found in dbo.Applications.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching app configuration for App ID {appId}.");
            throw; // Re-throw the exception to ensure proper handling
        }
    }
}
