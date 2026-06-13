using System;
using Avalonia.Data.Converters;

namespace Automation.App.Converters;

public static class BooleanConverters
{
    public static readonly IValueConverter NullOrString =
        new FuncValueConverter<bool, string?, string?>((value, param) => value ? null : param);
    
    public static readonly IValueConverter EnumEquals =
        new FuncValueConverter<Enum, Enum, bool>((input, parameter) => input?.Equals(parameter) ?? false);
}
