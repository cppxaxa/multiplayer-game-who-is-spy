using Azure;
using Azure.Data.Tables;
using WhoIsSpy.Lib.Shared.Interfaces;

namespace WhoIsSpy.Lib.Shared;

/// <summary>Generic Azure Table Storage wrapper providing CRUD operations.</summary>
public class TableServiceBase<T>(TableClient tableClient)
    : ITableService<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _client = tableClient;

    /// <inheritdoc/>
    public async Task<T?> GetAsync(string partitionKey, string rowKey)
    {
        try
        {
            Response<T> response = await _client.GetEntityAsync<T>(partitionKey, rowKey);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task UpsertAsync(T entity) =>
        await _client.UpsertEntityAsync(entity, TableUpdateMode.Replace);

    /// <inheritdoc/>
    public async Task DeleteAsync(string partitionKey, string rowKey)
    {
        try
        {
            await _client.DeleteEntityAsync(partitionKey, rowKey);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Already gone — treat as success.
        }
    }

    /// <inheritdoc/>
    public async Task<List<T>> QueryAsync(string filter)
    {
        List<T> results = [];
        await foreach (T entity in _client.QueryAsync<T>(filter))
            results.Add(entity);
        return results;
    }
}
