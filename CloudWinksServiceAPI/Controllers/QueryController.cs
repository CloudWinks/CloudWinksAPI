using Microsoft.AspNetCore.Mvc;
using CloudWinksServiceAPI.Models;
using System.Threading.Tasks;
using CloudWinksServiceAPI.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.Json;


namespace CloudWinksServiceAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly IDynamicQueryService _dynamicQueryService;
        private readonly IConfiguration _configuration; // Add this line
        private readonly DatabaseConnectionManager _connectionManager;


        public QueryController(IDynamicQueryService dynamicQueryService, IConfiguration configuration, DatabaseConnectionManager connectionManager)
        {
            _dynamicQueryService = dynamicQueryService;
            _configuration = configuration; // Assign injected IConfiguration
            _connectionManager = connectionManager;
        }

        [HttpPost("query")]
        public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
        {
            if (request.Parameters == null)
            {
                request.Parameters = new dbFields();
            }
            var result = await _dynamicQueryService.ExecuteQueryWithParametersAsync(request.TableName, request.Parameters);
            return Ok(new ApiResponse<dbQueryResult>
            {
                Status = "Success",
                Data = result
            });
        }

        [HttpPost("appquery")]
        public async Task<IActionResult> ExecuteListQueryAsync([FromBody] AppQueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.TableName))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Status = "Error",
                    Data = "TableName is required."
                });
            }

            try
            {
                // Execute the query without parameters
                var result = await _dynamicQueryService.ExecuteQueryWithoutParametersAsync(request.TableName);

                return Ok(new ApiResponse<dbQueryResult>
                {
                    Status = "Success",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Status = "Error",
                    Data = $"An error occurred: {ex.Message}"
                });
            }
        }



        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteNonQuery([FromBody] GETQueryRequest request)
        {
            if (request.Parameters == null)
            {
                request.Parameters = new dbFields();
            }
            var rowsAffected = await _dynamicQueryService.ExecuteNonQueryAsync(request.QueryName, request.Parameters);
            return Ok(new ApiResponse<int>
            {
                Status = "Success",
                Data = rowsAffected
            });
        }

        [HttpPost("spexecute")]
        public async Task<IActionResult> ExecuteNonQuery([FromBody] SpNoParamRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.QueryName))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Status = "Error",
                    Data = "QueryName is required."
                });
            }

            try
            {
                var result = await _dynamicQueryService.ExecuteStoredProcedureAsync(request.QueryName);

                return Ok(new ApiResponse<List<dbRecord>>
                {
                    Status = "Success",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Status = "Error",
                    Data = $"An error occurred: {ex.Message}"
                });
            }
        }



        // display data from sp
        [HttpPost("spcontrol")]
        public async Task<IActionResult> ExecuteStoredProcedureWithParameters([FromBody] GETQueryRequest request)
        {
            if (request == null)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Status = "Error",
                    Data = "Request payload is missing or invalid."
                });
            }

            if (string.IsNullOrWhiteSpace(request.QueryName))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Status = "Error",
                    Data = "QueryName is required."
                });
            }

            if (request.Parameters == null || !request.Parameters.Any())
            {
                return BadRequest(new ApiResponse<string>
                {
                    Status = "Error",
                    Data = "Parameters are required."
                });
            }

            // Log the incoming request for debugging
            Console.WriteLine($"QueryName: {request.QueryName}");
            foreach (var param in request.Parameters)
            {
                Console.WriteLine($"Param Name: {param.Name}, Value: {param.Value}, Type: {param.Type}");
            }

            try
            {
                var result = await _dynamicQueryService.ExecuteStoredProcedureWithParametersAsync(request.QueryName, request.Parameters);

                return Ok(new ApiResponse<dbQueryResult>
                {
                    Status = "Success",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Status = "Error",
                    Data = $"An error occurred: {ex.Message}"
                });
            }
        }


        [HttpPost("spCWApps")]
        public async Task<IActionResult> ExecuteStoredProcedureCWApps([FromBody] SpNoParamRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.QueryName))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Status = "Error",
                    Data = "QueryName is required."
                });
            }

            try
            {
                // Execute the stored procedure and get mapped dbRecords
                var result = await _dynamicQueryService.ExecuteStoredProcedureAsync(request.QueryName);

                return Ok(new ApiResponse<List<dbRecord>>
                {
                    Status = "Success",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string>
                {
                    Status = "Error",
                    Data = $"An error occurred while executing the stored procedure: {ex.Message}"
                });
            }
        }


        [HttpPost("UpdateOrCreateUserBySocialID")]
        public async Task<IActionResult> UpdateOrCreateUserBySocialID([FromBody] UserRequestModel request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.SocialID) || string.IsNullOrWhiteSpace(request.SocialType))
                {
                    return BadRequest("SocialID and SocialType are required.");
                }

                // Map parameters
                var parameters = new dbFields
        {
            new dbField { Name = "@SocialID", Value = request.SocialID },
            new dbField { Name = "@SocialType", Value = request.SocialType },
            new dbField { Name = "@Email", Value = request.Email },
            new dbField { Name = "@FirstName", Value = request.FirstName },
            new dbField { Name = "@LastName", Value = request.LastName },
            new dbField { Name = "@Username", Value = request.Username },
            new dbField { Name = "@PhoneNumber", Value = request.PhoneNumber },
            new dbField { Name = "@DateOfBirth", Value = request.DateOfBirth },
            new dbField { Name = "@Gender", Value = request.Gender },
            new dbField { Name = "@Country", Value = request.Country },
            new dbField { Name = "@LanguagePreference", Value = request.LanguagePreference },
            new dbField { Name = "@ThemePreference", Value = request.ThemePreference },
            new dbField { Name = "@NotificationPreferences", Value = request.NotificationPreferences }
        };

                // Call the  method
                var result = await _dynamicQueryService.ExecuteStoredProcedureForUsersParametersAsync("CW_User_UpdateCreateBySocialID", parameters);

                // Process result
                if (result.RowCount == 0)
                {
                    return NotFound("User not found or created.");
                }

                return Ok(result.Records);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("GenericExecute")]
        public async Task<IActionResult> GenericExecute([FromBody] ExecuteRequest request)
        {
            try
            {
                // Use appId to get the connection string
                string connectionString = _connectionManager.GetOrAddConnectionString(request.AppId);

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Check if it's a stored procedure or query
                    var isStoredProcedure = IsStoredProcedure(request.Name, connection);

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = isStoredProcedure ? request.Name : $"SELECT * FROM [{request.Name}] FOR JSON PATH";
                        command.CommandType = isStoredProcedure ? CommandType.StoredProcedure : CommandType.Text;

                        // Add parameters if provided
                        if (request.Parameters != null && isStoredProcedure)
                        {
                            foreach (var param in request.Parameters)
                            {
                                var value = ConvertJsonElement(param.Value);
                                command.Parameters.AddWithValue(param.Key, value ?? DBNull.Value);
                            }
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var results = new List<object>();
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader.GetValue(i);
                                }
                                results.Add(row);
                            }

                            return Ok(new { status = "success", data = results });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }




        //[HttpPost("GenericExecute")]
        //public async Task<IActionResult> GenericExecute([FromBody] ExecuteRequest request)
        //{
        //    try
        //    {
        //        // Get the database connection string
        //        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        //        using (var connection = new SqlConnection(connectionString))
        //        {
        //            await connection.OpenAsync();

        //            // Check if the name corresponds to a stored procedure
        //            var isStoredProcedure = IsStoredProcedure(request.Name, connection);

        //            // Prepare the command
        //            using (var command = connection.CreateCommand())
        //            {
        //                command.CommandText = isStoredProcedure
        //                    ? request.Name
        //                    : $"SELECT(SELECT * FROM [{request.Name}] FOR JSON PATH) AS JsonResult"; // Assume it's a table name if not a stored procedure
        //                command.CommandType = isStoredProcedure
        //                    ? CommandType.StoredProcedure
        //                    : CommandType.Text;

        //                // Add parameters if provided
        //                if (request.Parameters != null && isStoredProcedure)
        //                {
        //                    foreach (var param in request.Parameters)
        //                    {
        //                        var value = ConvertJsonElement(param.Value); // Convert JsonElement to .NET type
        //                        command.Parameters.AddWithValue(param.Key, value ?? DBNull.Value);
        //                    }
        //                }

        //                // Execute the query or stored procedure
        //                using (var reader = await command.ExecuteReaderAsync())
        //                {
        //                    // Collect the raw results
        //                    var result = new List<Dictionary<string, object>>();
        //                    while (await reader.ReadAsync())
        //                    {
        //                        var row = new Dictionary<string, object>();
        //                        for (int i = 0; i < reader.FieldCount; i++)
        //                        {
        //                            row[reader.GetName(i)] = reader.GetValue(i);
        //                        }
        //                        result.Add(row);
        //                    }

        //                    // Normalize the response
        //                    var normalizedResult = NormalizeResponse(result);

        //                    return Ok(new
        //                    {
        //                        status = "success",
        //                        data = normalizedResult.Data,
        //                        rowCount = normalizedResult.RowCount
        //                    });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = "error", message = $"An error occurred: {ex.Message}" });
        //    }
        //}





        private (List<object> Data, int RowCount) NormalizeResponse(List<Dictionary<string, object>> result)
        {
            var normalizedData = new List<object>();
            var rowCount = 0;

            foreach (var row in result)
            {
                // Check if the row contains a JSON string (e.g., from a stored procedure)
                if (row.Count == 1 && row.Values.First() is string jsonString && IsJson(jsonString))
                {
                    // Parse the JSON string into an object and add to the normalized data
                    var parsedData = JsonSerializer.Deserialize<object>(jsonString);
                    normalizedData.Add(parsedData);
                }
                else
                {
                    // Add the raw row as-is
                    normalizedData.Add(row);
                }

                rowCount++;
            }

            return (normalizedData, rowCount);
        }

        // Helper method to convert JsonElement to .NET types

        private bool IsJson(string input)
        {
            input = input?.Trim(); // Ensure input is not null and trimmed
            return !string.IsNullOrEmpty(input) &&
                   ((input.StartsWith("{") && input.EndsWith("}")) || // JSON Object
                    (input.StartsWith("[") && input.EndsWith("]")));   // JSON Array
        }
        private object ConvertJsonElement(object value)
        {
            if (value is JsonElement jsonElement)
            {
                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.String:
                        return jsonElement.GetString();
                    case JsonValueKind.Number:
                        if (jsonElement.TryGetInt32(out int intValue)) return intValue;
                        if (jsonElement.TryGetDouble(out double doubleValue)) return doubleValue;
                        return jsonElement.GetDecimal();
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return jsonElement.GetBoolean();
                    case JsonValueKind.Null:
                        return null;
                    default:
                        throw new InvalidOperationException($"Unsupported JsonElement type: {jsonElement.ValueKind}");
                }
            }
            return value; // If it's not a JsonElement, return the original value
        }

        // Helper method to check if the name is a stored procedure
        private bool IsStoredProcedure(string name, SqlConnection connection)
        {
            var query = "SELECT COUNT(*) FROM sys.procedures WHERE name = @name";
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@name", name);
                return (int)command.ExecuteScalar() > 0;
            }
        }


        public class ExecuteRequest
        {
            public string Name { get; set; } // Name of the query or stored procedure
            public Dictionary<string, object> Parameters { get; set; } // Parameters for the stored procedure or query
        }







        public class GETQueryRequest
        {
            // public string TableName { get; set; }
            public string QueryName { get; set; }
            public dbFields Parameters { get; set; }
        }

        public class QueryRequest
        {
            public string TableName { get; set; }
            public string QueryName { get; set; }
            public dbFields Parameters { get; set; }
        }
        public class AppQueryRequest
        {
            public string TableName { get; set; }



        }
        public class SpNoParamRequest
        {
            public string QueryName { get; set; }
        }
    }
}