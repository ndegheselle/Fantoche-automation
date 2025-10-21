using Usuel.Shared;

namespace Automation.App.Shared.Base.History
{
    public interface IReversibleCommand : ICustomCommand
    {
        /// <summary>
        /// Command that does the exact oposit of this command.
        /// </summary>
        public IReversibleCommand? Reverse { get; set; }
        /// <summary>
        /// Execute the command.
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="withHistory">If the command should add an history line.</param>
        public void Execute(object? parameter, bool withHistory);
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

        public override void Execute(object? parameter) => Execute(parameter, true);
        public void Execute(object? parameter, bool withHistory)
        {
            if (withHistory && IsBatched)
                _handler.BeginBatch();

            base.Execute(parameter);

            if (withHistory)
                _handler.Add(this, parameter);
            if (withHistory && IsBatched)
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

        public override void Execute(object? parameter) => Execute(parameter, true);
        public void Execute(object? parameter, bool withHistory)
        {
            if (withHistory && IsBatched)
                _handler.BeginBatch();

            base.Execute(parameter);

            if (withHistory)
                _handler.Add(this, parameter);
            if (withHistory && IsBatched)
                _handler.EndBatch();
        }
    }
}
