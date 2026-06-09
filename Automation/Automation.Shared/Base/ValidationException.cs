namespace Automation.Shared.Base;

public class ValidationException : Exception
{
    public Dictionary<string, List<string>>? Errors { get; set; }
    public ValidationException(Dictionary<string, List<string>>? errors)
    {
        Errors = errors;
    }
}

public class Warning
{
    public Warning(string key, string message)
    {
        Key = key;
        Message = message;
    }
    
    public string Key { get; set; }
    public string Message { get; set; }
}