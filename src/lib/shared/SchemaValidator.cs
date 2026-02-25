using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.Shared;

/// <summary>
/// Reads schema JSON files from the schema directory, auto-creates missing Azure Storage Tables,
/// and sets the global <see cref="AppState.SchemaValid"/> flag.
/// </summary>
public class SchemaValidator(TableServiceClient tableServiceClient, ILogger<SchemaValidator> logger)
    : ISchemaValidator
{
    private readonly TableServiceClient _tableServiceClient = tableServiceClient;
    private readonly ILogger<SchemaValidator> _logger = logger;

    /// <inheritdoc/>
    public async Task<bool> ValidateAndInitializeAsync()
    {
        try
        {
            string schemaDir = Path.Combine(AppContext.BaseDirectory, "schema");
            if (!Directory.Exists(schemaDir))
            {
                _logger.LogWarning("Schema directory not found at {Dir}; skipping validation.", schemaDir);
                AppState.SchemaValid = true;
                return true;
            }

            foreach (string file in Directory.GetFiles(schemaDir, "*.json"))
            {
                string json = await File.ReadAllTextAsync(file);
                SchemaDefinition? def = JsonConvert.DeserializeObject<SchemaDefinition>(json);
                if (def?.TableName is null) continue;

                await _tableServiceClient.CreateTableIfNotExistsAsync(def.TableName);
                _logger.LogInformation("Table '{Table}' verified/created.", def.TableName);
            }

            AppState.SchemaValid = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed; APIs will return service-unavailable.");
            AppState.SchemaValid = false;
            return false;
        }
    }

    private sealed record SchemaDefinition(
        string TableName,
        string PartitionKey,
        string RowKey,
        List<string> Columns);
}
