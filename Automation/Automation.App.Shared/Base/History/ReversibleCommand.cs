using Usuel.Shared;

namespace Automation.App.Shared.Base.History
{
    public interface IReversibleCommand : ICustomCommand
    {
        /// <summary>
        /// Command that does the exact oposit of this command.
        /// </summary>
        public IReversibleCommand? Reverse { get; set; }
    }

    public class ReversibleCommand : DelegateCommand, IReversibleCommand
    {
        public IReversibleCommand? Reverse { get; set; }
        public bool IsBatched { get; set; } = false;

        private readonly HistoryHandler _handler;

        public ReversibleCommand(HistoryHandler handler, Action action, Func<bool>? executeCondition = default) : base(
            action,
            executeCondition)
        { _handler = handler; }

        public override void Execute(object? parameter)
        {
            if (IsBatched)
                _handler.BeginBatch();
            base.Execute(parameter);
            if (IsBatched)
                _handler.EndBatch();
        }
    }

    public class ReversibleCommand<T> : DelegateCommand<T>, IReversibleCommand
    {
        public IReversibleCommand? Reverse { get; set; }
        public bool IsBatched { get; set; } = false;

        private readonly HistoryHandler _handler;

        public ReversibleCommand(HistoryHandler handler, Action<T> action, Func<T, bool>? executeCondition = default) : base(
            action,
            executeCondition)
        { _handler = handler; }

        public override void Execute(object? parameter) { Execute(parameter, false); }
                
        public override void Execute(object? parameter, bool isReversed)
        {
            if (IsBatched)
                _handler.BeginBatch();
            base.Execute(parameter);
            _handler.Add(this, parameter);
            if (IsBatched)
                _handler.EndBatch();
        }
    }
}
