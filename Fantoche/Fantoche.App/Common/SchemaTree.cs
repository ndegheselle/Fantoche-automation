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
        /// The tree of the object [schema] describes, null when it describes none. Joufflu.Data reads
        /// what the form shows of each field from the schema : the summary of a property of the
        /// parameters of a task as its description, its rules and its input (see FormInputs).
        /// </summary>
        public static DataObject? Of(JsonSchema? schema)
        {
            JsonSchema? actual = schema?.ActualSchema;
            if (actual?.IsObject != true)
                return null;
            return actual.ToDataNode() as DataObject;
        }
    }
}
