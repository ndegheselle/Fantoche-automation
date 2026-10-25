using System.Text.Json.Serialization;

namespace Fantoche.Plugins.Example.Tasks.Pdf;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnumPdfBlock
{
    Heading,
    Text,

    /// <summary>The rows of the table the document is made of.</summary>
    Table
}

/// <summary>A column a table block shows, under its header.</summary>
public class PdfColumn
{
    /// <summary>A name of the mapping of the table, or a stored column.</summary>
    public string Column { get; set; } = "";

    /// <summary>The name of the column when empty.</summary>
    public string Header { get; set; } = "";
}

/// <summary>
/// A part of the document. The text of a heading or a paragraph can hold <c>{index}</c> (the number
/// of the document, from 1), <c>{count}</c> (how many documents there are) and <c>{rows}</c> (how
/// many rows its table holds).
/// </summary>
public class PdfBlock
{
    public EnumPdfBlock Kind { get; set; } = EnumPdfBlock.Text;
    public string Text { get; set; } = "";

    /// <summary>For a table : every column of the table when empty.</summary>
    public List<PdfColumn> Columns { get; set; } = [];
}

/// <summary>What a document is made of, from top to bottom, the pages following as they fill up.</summary>
public class PdfLayout
{
    public bool Landscape { get; set; }
    public List<PdfBlock> Blocks { get; set; } = [];
}
