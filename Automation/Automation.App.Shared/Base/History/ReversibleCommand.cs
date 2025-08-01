using Usuel.Shared;

namespace Automation.App.Shared.Base.History
{
    public interface IReversibleCommand : ICustomCommand
    {
        /// <summary>
        /// Command that does the exact oposit of this command.
        /// </summary>
        public IReversibleCommand? Reverse { get; set; }

        void Execute(object? parameter, bool withHistory);
    }

    public class ReversibleCommand : DelegateCommand, IReversibleCommand
    {
        public IReversibleCommand? Reverse { get; set; }

        private readonly HistoryHandler _handler;

        public ReversibleCommand(HistoryHandler handler, Action action, Func<bool>? executeCondition = default) : base(
            action,
            executeCondition)
        { _handler = handler; }

        public override void Execute(object? parameter) { Execute(parameter, true); }

        public void Execute(object? parameter, bool withHistory)
        {
            base.Execute(parameter);
            if (withHistory == true)
                _handler.Add(this, parameter);
        }
    }

    public class ReversibleCommand<T> : DelegateCommand<T>, IReversibleCommand
    {
        public IReversibleCommand? Reverse { get; set; }

        private readonly HistoryHandler _handler;

        public ReversibleCommand(HistoryHandler handler, Action<T> action, Func<T, bool>? executeCondition = default) : base(
            action,
            executeCondition)
        { _handler = handler; }

        public override void Execute(object? parameter) { Execute(parameter, true); }

        public void Execute(object? parameter, bool withHistory)
        {
            base.Execute(parameter);
            if (withHistory == true)
                _handler.Add(this, parameter);
        }
    }
}
