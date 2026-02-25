using Azure.Data.Tables;

namespace WhoIsSpy.Lib.Shared.Interfaces;

/// <summary>Generic Azure Table Storage access contract.</summary>
public interface ITableService<T> where T : class, ITableEntity, new()
{
    /// <summary>Retrieves a single entity by partition key and row key.</summary>
    Task<T?> GetAsync(string partitionKey, string rowKey);

    /// <summary>Inserts or replaces an entity.</summary>
    Task UpsertAsync(T entity);

    /// <summary>Deletes an entity by partition key and row key.</summary>
    Task DeleteAsync(string partitionKey, string rowKey);

    /// <summary>Returns all entities matching the given OData filter string.</summary>
    Task<List<T>> QueryAsync(string filter);
}
