namespace WhoIsSpy.Lib.Shared.Interfaces;

/// <summary>Startup schema validation contract.</summary>
public interface ISchemaValidator
{
    /// <summary>
    /// Validates that all required Azure Storage Tables exist with correct schema.
    /// Creates missing tables from schema JSON files.
    /// Never throws — returns false and logs on failure.
    /// </summary>
    Task<bool> ValidateAndInitializeAsync();
}
