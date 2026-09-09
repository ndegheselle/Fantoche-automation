using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Automation.Shared.Data;
using NJsonSchema;

namespace Automation.App.Features.Workflows.Controls
{
    /// <summary>
    /// A JSON schema as a tree of the values it describes, each read by the glyph of its type rather
    /// than by its declaration : a shape is what a reader of a mapping needs in front of them, and
    /// the schema it is written as is not.
    /// <para>
    /// Give it a <see cref="Schema"/>, or the <see cref="SchemaJson"/> it is stored as — a graph
    /// holds its schemas as text (see
    /// <see cref="Automation.Shared.Data.Graph.BaseGraphTask.OutputSchemaJson"/>), so most callers
    /// have the text rather than the schema.
    /// </para>
    /// </summary>
    public partial class SchemaDisplay : UserControl
    {
        /// <summary>
        /// The schema to display, null when there is none to.
        /// </summary>
        public static readonly DependencyProperty SchemaProperty = DependencyProperty.Register(
            nameof(Schema), typeof(JsonSchema), typeof(SchemaDisplay),
            new PropertyMetadata(null, OnSchemaChanged));

        /// <summary>
        /// The schema as a graph stores it. Setting it parses it into <see cref="Schema"/> : what is
        /// not a schema displays as none rather than as an error, whoever edits the text being where
        /// that is reported.
        /// </summary>
        public static readonly DependencyProperty SchemaJsonProperty = DependencyProperty.Register(
            nameof(SchemaJson), typeof(string), typeof(SchemaDisplay),
            new PropertyMetadata(null, OnSchemaJsonChanged));

        /// <summary>
        /// What to say when the schema describes no value, which is not the same thing everywhere
        /// this is displayed.
        /// </summary>
        public static readonly DependencyProperty EmptyTextProperty = DependencyProperty.Register(
            nameof(EmptyText), typeof(string), typeof(SchemaDisplay),
            new PropertyMetadata("No shape is expected."));

        private static readonly DependencyPropertyKey IsEmptyPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsEmpty), typeof(bool), typeof(SchemaDisplay), new PropertyMetadata(true));

        /// <summary>
        /// Whether no schema was given at all. A schema describing nothing is not empty : it says
        /// that anything goes, which is worth reading. Written by the control, read by whatever it
        /// stands in front of.
        /// </summary>
        public static readonly DependencyProperty IsEmptyProperty = IsEmptyPropertyKey.DependencyProperty;

        public JsonSchema? Schema
        {
            get => (JsonSchema?)GetValue(SchemaProperty);
            set => SetValue(SchemaProperty, value);
        }

        public string? SchemaJson
        {
            get => (string?)GetValue(SchemaJsonProperty);
            set => SetValue(SchemaJsonProperty, value);
        }

        public string EmptyText
        {
            get => (string)GetValue(EmptyTextProperty);
            set => SetValue(EmptyTextProperty, value);
        }

        public bool IsEmpty => (bool)GetValue(IsEmptyProperty);

        /// <summary>
        /// The schema as a single root standing for the shape as a whole, holding what it describes.
        /// Empty when there is no schema.
        /// </summary>
        public ObservableCollection<SchemaEntry> Entries { get; } = [];

        public SchemaDisplay()
        {
            InitializeComponent();
        }

        private static void OnSchemaChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
            => ((SchemaDisplay)sender).Rebuild();

        private static void OnSchemaJsonChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
            => ((SchemaDisplay)sender).Schema = Parse(e.NewValue as string);

        /// <summary>
        /// [json] as a schema, null when it holds none or when what it holds is not one.
        /// </summary>
        private static JsonSchema? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return Schemas.Parse(json);
            }
            catch
            {
                return null;
            }
        }

        private void Rebuild()
        {
            Entries.Clear();
            foreach (SchemaEntry entry in SchemaEntry.From(Schema))
                Entries.Add(entry);

            SetValue(IsEmptyPropertyKey, Schema == null);
        }
    }
}
