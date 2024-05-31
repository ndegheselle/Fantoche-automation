namespace Automation.Base
{
    public class TaskScope : IContextElement
    {
        public dynamic? Context { get; set; }
        public string Name { get; set; }
        // Can either be a ITask or a TaskScope
        public List<IContextElement> Childrens { get; set; } = new List<IContextElement>();
    }
}
