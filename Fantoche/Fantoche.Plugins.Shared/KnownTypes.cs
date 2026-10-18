namespace Fantoche.Plugins.Shared;

/// <summary>
/// Types the engine knows : their schema is tagged with <see cref="SchemaKey"/> rather than read as
/// any other object, so an editor can handle them on their own (e.g. a table picker). A type is
/// declared with <c>[JsonSchemaExtensionData(KnownTypes.SchemaKey, KnownTypes.Table)]</c> on its class.
/// </summary>
public static class KnownTypes
{
    public const string SchemaKey = "x-fantoche-type";
    public const string Table = "table";
    public const string File = "file";
}
