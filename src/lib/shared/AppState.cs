namespace WhoIsSpy.Lib.Shared;

/// <summary>Global application runtime state flags.</summary>
public static class AppState
{
    /// <summary>
    /// Set to true once schema validation passes at startup.
    /// All public API endpoints check this flag before processing requests.
    /// </summary>
    public static bool SchemaValid { get; set; }
}
