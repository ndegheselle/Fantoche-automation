namespace Fantoche.Plugins.Shared;

/// <summary>
/// What an editor shows a task hands over, without running it : the sample of its output schema,
/// which knows nothing of the data. A task handing over part of its input tells it on the property
/// of its input, so the nodes after it read what that property resolved to :
/// <c>[JsonSchemaExtensionData(OutputPreview.SchemaKey, "Table")]</c> hands the property over as the
/// "Table" property of the output, <see cref="Whole"/> as the output itself. Several properties
/// handed over to the same place are merged, in the order of the input.
/// <para>
/// A list of <see cref="ColumnMapping"/> is handed over as the <see cref="TableRef.Mapping"/> of the
/// table at a place of the output with <see cref="MappingSchemaKey"/> instead : the names are written
/// as a list, while they are read as a dictionary.
/// </para>
/// <para>
/// Only what the editor shows : the task itself hands over whatever it produces, which has to be
/// what it declares here for the mappings written in the editor to hold once it runs.
/// </para>
/// </summary>
public static class OutputPreview
{
    public const string SchemaKey = "x-fantoche-output";
    public const string MappingSchemaKey = "x-fantoche-output-mapping";

    /// <summary>The output itself, rather than one of its properties.</summary>
    public const string Whole = "";
}
