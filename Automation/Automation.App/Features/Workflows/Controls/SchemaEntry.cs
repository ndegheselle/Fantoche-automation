using System.Collections.ObjectModel;
using Joufflu.Assets.Fonts;
using NJsonSchema;

namespace Automation.App.Features.Workflows.Controls
{
    /// <summary>
    /// One value a schema describes : what it is called, what it holds and, for an object or an
    /// array, the values under it. Built from a <see cref="JsonSchema"/> rather than read off it, a
    /// schema being a graph of references while what is displayed is a tree.
    /// </summary>
    public class SchemaEntry
    {
        /// <summary>
        /// Name of the value : the property it is declared as, "[ ]" for what an array holds, and
        /// empty for the schema itself.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// What it holds, in the words of the schema : "object", "string", "integer", and whatever a
        /// format or an enumeration adds to it.
        /// </summary>
        public string TypeLabel { get; }

        /// <summary>Glyph standing for the type, so a shape is read without reading it.</summary>
        public string Icon { get; }

        /// <summary>What the schema says of the value, empty when it says nothing.</summary>
        public string Description { get; }

        public bool HasDescription => Description.Length > 0;

        /// <summary>
        /// Whether the value has to be there : a caller leaving it out is not describing this shape.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Whether what is under it shows without being asked for. Only the shape as a whole does :
        /// that is what the reader came for, anything deeper being theirs to open.
        /// </summary>
        public bool IsExpanded { get; init; }

        public ObservableCollection<SchemaEntry> Children { get; } = [];

        private SchemaEntry(string name, JsonSchema schema, bool isRequired, HashSet<JsonSchema> walking)
        {
            JsonSchema actual = schema.ActualSchema;

            Name = name;
            IsRequired = isRequired;
            TypeLabel = LabelOf(actual);
            Icon = IconOf(actual.Type);
            Description = actual.Description ?? string.Empty;

            // A schema references other schemas and can reference itself : what is under a value
            // already being described higher up is what that description says.
            if (!walking.Add(actual))
                return;

            try
            {
                foreach (KeyValuePair<string, JsonSchemaProperty> property in actual.ActualProperties)
                    Children.Add(new SchemaEntry(property.Key, property.Value, property.Value.IsRequired, walking));

                // What an array holds is one entry rather than one per position : a schema describes
                // the values of an array, not their number.
                if (actual.Item != null)
                    Children.Add(new SchemaEntry("[ ]", actual.Item, isRequired: false, walking));
            }
            finally
            {
                walking.Remove(actual);
            }
        }

        /// <summary>
        /// [schema] as one entry standing for the shape as a whole, holding one entry per value it
        /// describes : a reader is told an object is expected before being told what goes in it, and
        /// a schema expecting something other than an object says so rather than looking empty.
        /// Nothing at all when there is no schema.
        /// </summary>
        public static IReadOnlyList<SchemaEntry> From(JsonSchema? schema)
        {
            if (schema == null)
                return [];

            return [new SchemaEntry(string.Empty, schema, isRequired: false, []) { IsExpanded = true }];
        }

        /// <summary>
        /// What [schema] holds, said the way a reader of JSON would say it.
        /// </summary>
        private static string LabelOf(JsonSchema schema)
        {
            List<string> parts = [TypeNameOf(schema.Type)];

            if (!string.IsNullOrEmpty(schema.Format))
                parts.Add($"({schema.Format})");

            if (schema.IsEnumeration)
                parts.Add($"one of {schema.Enumeration.Count}");

            return string.Join(" ", parts);
        }

        private static string TypeNameOf(JsonObjectType type) => type switch
        {
            JsonObjectType.None => "anything",
            JsonObjectType.Object => "object",
            JsonObjectType.Array => "array",
            JsonObjectType.String => "string",
            JsonObjectType.Integer => "integer",
            JsonObjectType.Number => "number",
            JsonObjectType.Boolean => "boolean",
            JsonObjectType.Null => "null",
            // A value allowed to be of several types, which the flags say.
            _ => string.Join(" or ", $"{type}".Split(',').Select(x => x.Trim().ToLowerInvariant())),
        };

        /// <summary>
        /// The glyph a type is read by. A value of several types is drawn as the shape it can hold
        /// rather than as any of them.
        /// </summary>
        private static string IconOf(JsonObjectType type)
        {
            if (type.HasFlag(JsonObjectType.Object))
                return LucideFontIcons.Braces;
            if (type.HasFlag(JsonObjectType.Array))
                return LucideFontIcons.Brackets;
            if (type.HasFlag(JsonObjectType.String))
                return LucideFontIcons.Type;
            if (type.HasFlag(JsonObjectType.Integer) || type.HasFlag(JsonObjectType.Number))
                return LucideFontIcons.Hash;
            if (type.HasFlag(JsonObjectType.Boolean))
                return LucideFontIcons.ToggleLeft;
            if (type.HasFlag(JsonObjectType.Null))
                return LucideFontIcons.CircleOff;

            // A schema saying nothing of a type accepts anything.
            return LucideFontIcons.CircleHelp;
        }
    }
}
