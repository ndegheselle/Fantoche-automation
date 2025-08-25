using System;
using System.Collections.Generic;
using System.Text;
using Usuel.Shared.Data;

namespace Automation.Dal.Models
{
    internal class SchemaValue : SchemaProperty
    {
        public string SettingReference { get; set; }
        public dynamic Value { get; set; }

        public SchemaValue(string name, EnumValueType type) : base(name, type)
        {
        }
    }
}
