using Joufflu.Data.Model;
using NJsonSchema;

namespace Fantoche.App.Common
{
    /// <summary>
    /// The tree a form fills in, built from the schema of what it fills in.
    /// </summary>
    public static class SchemaTree
    {
        /// <summary>
        /// The tree of the object [schema] describes, each field described the way its schema
        /// describes it (the summary of a property of the parameters of a task). Null when [schema]
        /// describes no object.
        /// </summary>
        public static DataObject? Of(JsonSchema? schema)
        {
            JsonSchema? actual = schema?.ActualSchema;
            if (actual?.IsObject != true || actual.ToDataNode() is not DataObject tree)
                return null;

            Describe(tree, actual);
            return tree;
        }

        /// <summary>
        /// Joufflu.Data builds the tree without the descriptions of the schema, which the form shows
        /// next to each field : they are copied here until it reads them itself.
        /// </summary>
        private static void Describe(DataNode node, JsonSchema schema)
        {
            JsonSchema actual = schema.ActualSchema;
            switch (node)
            {
                case DataObject values:
                    foreach (DataNode property in values.Properties)
                    {
                        if (property.Key == null || !actual.ActualProperties.TryGetValue(property.Key, out JsonSchemaProperty? propertySchema))
                            continue;

                        // A property of a type of its own (a table) is described on the property, not on its type.
                        property.Description = propertySchema.Description ?? propertySchema.ActualSchema.Description;
                        Describe(property, propertySchema);
                    }
                    break;
                case DataArray { Template: DataNode template } when actual.Item != null:
                    Describe(template, actual.Item);
                    break;
            }
        }
    }
}
