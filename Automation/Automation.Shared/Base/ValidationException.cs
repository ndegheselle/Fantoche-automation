namespace Automation.Shared.Base
{
    public class ValidationException : Exception
    {
        public Dictionary<string, List<string>>? Errors { get; set; }
        public ValidationException(Dictionary<string, List<string>>? errors)
        {
            Errors = errors;
        }
    }
}
