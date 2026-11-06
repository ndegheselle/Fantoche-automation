using NJsonSchema.Annotations;

namespace Fantoche.Plugins.Shared;

/// <summary>
/// How a string of the parameters is typed in the form generated from them (Joufflu.Data's
/// <c>DataForm</c>), written in the schema of the parameters : the form reads nothing else. The
/// keys and values are the ones Joufflu.Data reads (<c>DataInputs</c>), repeated here as this
/// assembly doesn't reference WPF.
/// </summary>
public static class FormInputs
{
    public const string SchemaKey = "x-input";
    public const string FileFilterSchemaKey = "x-file-filter";
    public const string File = "file";
    public const string Multiline = "multiline";
}

/// <summary>
/// A path to a file, typed or picked with the dialog of the application. [filter] is the filter of
/// that dialog : <c>"CSV files|*.csv|All files|*.*"</c>. The path is read where the task runs.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FilePathAttribute(string filter = "") : Attribute, IJsonSchemaExtensionDataAttribute
{
    public IReadOnlyDictionary<string, object> ExtensionData { get; } = new Dictionary<string, object>
    {
        [FormInputs.SchemaKey] = FormInputs.File,
        [FormInputs.FileFilterSchemaKey] = filter,
    };
}

/// <summary>A text of several lines.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MultilineAttribute : Attribute, IJsonSchemaExtensionDataAttribute
{
    public IReadOnlyDictionary<string, object> ExtensionData { get; } = new Dictionary<string, object>
    {
        [FormInputs.SchemaKey] = FormInputs.Multiline,
    };
}
