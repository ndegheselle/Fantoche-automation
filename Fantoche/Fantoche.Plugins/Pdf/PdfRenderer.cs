using System.Globalization;
using Fantoche.Plugins.Shared;
using Fantoche.Plugins.Tables;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Fantoche.Plugins.Pdf;

/// <summary>
/// Draw a <see cref="PdfLayout"/> for one table with PDFsharp : the blocks one under the other, a
/// new page started whenever the next line doesn't fit, the header of a table repeated on each page.
/// </summary>
internal static class PdfRenderer
{
    static PdfRenderer()
    {
        // Outside of Windows PDFsharp finds no font on its own : the one embedded makes a document look
        // the same on every worker. PDFsharp refuses a second resolver, and another load of the plugin
        // sharing the same PDFsharp may already have set one.
        GlobalFontSettings.FontResolver ??= new EmbeddedFontResolver();

        // From 6.5 on, PDFsharp refuses to write a document until a license is chosen : the plugin
        // is under its free one, the AGPL.
        PdfSharpLicense.SetAgplLicense();
    }

    /// <summary>The document a layout without blocks stands for : the whole table.</summary>
    private static readonly PdfBlock WholeTable = new() { Kind = EnumPdfBlock.Table };

    public static async Task<MemoryStream> RenderAsync(PdfLayout layout, TableRef source, Placeholders placeholders,
        IStorage storage, CancellationToken cancellation)
    {
        using var document = new PdfDocument();
        IReadOnlyList<PdfBlock> blocks = layout.Blocks.Count > 0 ? layout.Blocks : [WholeTable];

        using (PageWriter writer = new(document, layout.Landscape))
        {
            foreach (PdfBlock block in blocks)
            {
                switch (block.Kind)
                {
                    case EnumPdfBlock.Heading:
                        writer.Heading(placeholders.Replace(block.Text));
                        break;
                    case EnumPdfBlock.Text:
                        writer.Paragraph(placeholders.Replace(block.Text));
                        break;
                    case EnumPdfBlock.Table:
                        await WriteTableAsync(writer, block, source, storage, cancellation);
                        break;
                }
            }
        }

        var content = new MemoryStream();
        document.Save(content, false);
        content.Position = 0;
        return content;
    }

    private static async Task WriteTableAsync(PageWriter writer, PdfBlock block, TableRef source, IStorage storage, CancellationToken cancellation)
    {
        List<PdfColumn> columns = block.Columns.Count > 0
            ? block.Columns
            : [.. source.Columns.Select(x => new PdfColumn { Column = source.NameOf(x.Name) })];
        List<string> stored = [.. columns.Select(x => Sql.ColumnOf(source, x.Column))];
        List<string> headers = [.. columns.Select(x => string.IsNullOrWhiteSpace(x.Header) ? x.Column : x.Header)];

        writer.StartTable(headers);
        await foreach (var row in storage.ReadRowsAsync(source, cancellation))
            writer.Row([.. stored.Select(column => TextOf(row[column]))]);
        writer.EndTable();
    }

    private static string TextOf(object? value) => value switch
    {
        null => "",
        DateTime date when date.TimeOfDay == TimeSpan.Zero => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        DateTime date => date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
    };

    /// <summary>
    /// Where the next line goes : the page being drawn and how far down it is, a new one started
    /// once the next line doesn't fit.
    /// </summary>
    private sealed class PageWriter : IDisposable
    {
        private const string FontFamily = "DejaVu Sans";
        private const double Margin = 40;
        private const double HeadingSize = 18;
        private const double TextSize = 11;
        private const double CellSize = 9;
        private const double CellPadding = 3;

        private readonly PdfDocument _document;
        private readonly bool _isLandscape;
        private readonly XFont _headingFont = new(FontFamily, HeadingSize, XFontStyleEx.Bold);
        private readonly XFont _textFont = new(FontFamily, TextSize);
        private readonly XFont _cellFont = new(FontFamily, CellSize);
        private readonly XFont _headerFont = new(FontFamily, CellSize, XFontStyleEx.Bold);

        private XGraphics? _graphics;
        private double _y;
        private double _bottom;
        private double _width;

        /// <summary>The headers of the table being written, repeated on each page it goes on.</summary>
        private List<string>? _headers;

        public PageWriter(PdfDocument document, bool isLandscape)
        {
            _document = document;
            _isLandscape = isLandscape;
        }

        public void Dispose() => _graphics?.Dispose();

        public void Heading(string text)
        {
            double height = HeadingSize * 1.6;
            Reserve(height);
            Graphics.DrawString(text, _headingFont, XBrushes.Black, new XRect(Margin, _y, _width, height), XStringFormats.TopLeft);
            _y += height;
        }

        public void Paragraph(string text)
        {
            double height = TextSize * 1.4;
            foreach (string line in WrappedLines(text, _textFont))
            {
                Reserve(height);
                Graphics.DrawString(line, _textFont, XBrushes.Black, new XRect(Margin, _y, _width, height), XStringFormats.TopLeft);
                _y += height;
            }
            _y += height / 2;
        }

        public void StartTable(List<string> headers)
        {
            _headers = headers;
            Reserve(RowHeight * 2);
            Header();
        }

        public void Row(IReadOnlyList<string> cells)
        {
            if (_y + RowHeight > _bottom)
            {
                NewPage();
                Header();
            }
            DrawCells(cells, _cellFont);
            _y += RowHeight;
        }

        public void EndTable()
        {
            _headers = null;
            _y += RowHeight;
        }

        private static double RowHeight => CellSize + CellPadding * 2;

        private XGraphics Graphics => _graphics ?? throw new InvalidOperationException("No page started.");

        private void Header()
        {
            List<string> headers = _headers!;
            Graphics.DrawRectangle(XBrushes.Gainsboro, Margin, _y, _width, RowHeight);
            DrawCells(headers, _headerFont);
            _y += RowHeight;
        }

        private void DrawCells(IReadOnlyList<string> cells, XFont font)
        {
            double cellWidth = _width / cells.Count;
            for (int i = 0; i < cells.Count; i++)
            {
                string text = Fitted(cells[i], font, cellWidth - CellPadding * 2);
                XRect cell = new(Margin + i * cellWidth + CellPadding, _y + CellPadding, cellWidth - CellPadding * 2, CellSize);
                Graphics.DrawString(text, font, XBrushes.Black, cell, XStringFormats.TopLeft);
            }
        }

        /// <summary>A new page when [height] doesn't fit on this one, the first page when none is started.</summary>
        private void Reserve(double height)
        {
            if (_graphics == null || _y + height > _bottom)
                NewPage();
        }

        private void NewPage()
        {
            _graphics?.Dispose();
            PdfPage page = _document.AddPage();
            page.Orientation = _isLandscape ? PageOrientation.Landscape : PageOrientation.Portrait;
            _graphics = XGraphics.FromPdfPage(page);
            _y = Margin;
            _bottom = page.Height.Point - Margin;
            _width = page.Width.Point - Margin * 2;
        }

        /// <summary>[text] cut to [width], an ellipsis telling it was.</summary>
        private string Fitted(string text, XFont font, double width)
        {
            double measured = Graphics.MeasureString(text, font).Width;
            if (measured <= width)
                return text;

            // Started from the share of the text that fits rather than from its end : a long text
            // in a narrow column would be measured once per character otherwise, on every row.
            int length = Math.Min(text.Length - 1, (int)(text.Length * width / measured));
            while (length > 0 && Graphics.MeasureString($"{text[..length]}…", font).Width > width)
                length--;
            return $"{text[..length]}…";
        }

        /// <summary>The lines [text] is wrapped into on the width of the page, its own line breaks kept.</summary>
        private IEnumerable<string> WrappedLines(string text, XFont font)
        {
            Reserve(0);
            foreach (string paragraph in text.Split('\n'))
            {
                string line = "";
                foreach (string word in paragraph.Split(' '))
                {
                    string next = line.Length == 0 ? word : $"{line} {word}";
                    if (line.Length > 0 && Graphics.MeasureString(next, font).Width > _width)
                    {
                        yield return line;
                        line = word;
                    }
                    else
                    {
                        line = next;
                    }
                }
                yield return line;
            }
        }
    }

    /// <summary>
    /// The embedded font for every family and style asked for, bold and italic being simulated.
    /// </summary>
    private sealed class EmbeddedFontResolver : IFontResolver
    {
        private const string FaceName = "DejaVuSans";

        public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) => new(FaceName, bold, italic);

        public byte[]? GetFont(string faceName)
        {
            using Stream font = typeof(PdfRenderer).Assembly.GetManifestResourceStream($"{FaceName}.ttf")
                ?? throw new InvalidOperationException($"Font [{FaceName}] is not embedded in the plugin.");
            using var bytes = new MemoryStream();
            font.CopyTo(bytes);
            return bytes.ToArray();
        }
    }
}
