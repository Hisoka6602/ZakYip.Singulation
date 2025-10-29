using ZakYip.Singulation.Core.Contracts.Events;
using ZakYip.Singulation.Core.Contracts.ValueObjects;
using ZakYip.Singulation.Drivers.Abstractions;

namespace ZakYip.Singulation.Tests.TestHelpers {
    /// <summary>
    /// 用于测试的简单 AxisEventAggregator 模拟实现。
    /// 记录所有发布的事件以便测试验证。
    /// </summary>
    internal sealed class FakeAxisEventAggregator : IAxisEventAggregator {
        private readonly List<AxisCommandIssuedEventArgs> _commandIssuedEvents = new();
        private readonly List<AxisErrorEventArgs> _errorEvents = new();
        private readonly List<AxisDisconnectedEventArgs> _disconnectedEvents = new();
        private readonly List<DriverNotLoadedEventArgs> _driverNotLoadedEvents = new();
        private readonly List<AxisSpeedFeedbackEventArgs> _speedFeedbackEvents = new();

        public event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;
        public event EventHandler<AxisErrorEventArgs>? AxisFaulted;
        public event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;
        public event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;
        public event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        public IReadOnlyList<AxisCommandIssuedEventArgs> CommandIssuedEvents => _commandIssuedEvents.AsReadOnly();
        public IReadOnlyList<AxisErrorEventArgs> ErrorEvents => _errorEvents.AsReadOnly();
        public IReadOnlyList<AxisDisconnectedEventArgs> DisconnectedEvents => _disconnectedEvents.AsReadOnly();
        public IReadOnlyList<DriverNotLoadedEventArgs> DriverNotLoadedEvents => _driverNotLoadedEvents.AsReadOnly();
        public IReadOnlyList<AxisSpeedFeedbackEventArgs> SpeedFeedbackEvents => _speedFeedbackEvents.AsReadOnly();

        public void Attach(IAxisDrive drive) {
            // Mock implementation - do nothing
        }

        public void Detach(IAxisDrive drive) {
            // Mock implementation - do nothing
        }

        public void PublishCommandIssued(AxisCommandIssuedEventArgs args) {
            _commandIssuedEvents.Add(args);
            CommandIssued?.Invoke(this, args);
        }

        public void PublishError(AxisErrorEventArgs args) {
            _errorEvents.Add(args);
            AxisFaulted?.Invoke(this, args);
        }

        public void PublishDisconnected(AxisDisconnectedEventArgs args) {
            _disconnectedEvents.Add(args);
            AxisDisconnected?.Invoke(this, args);
        }

        public void PublishDriverNotLoaded(DriverNotLoadedEventArgs args) {
            _driverNotLoadedEvents.Add(args);
            DriverNotLoaded?.Invoke(this, args);
        }

        public void PublishSpeedFeedback(AxisSpeedFeedbackEventArgs args) {
            _speedFeedbackEvents.Add(args);
            SpeedFeedback?.Invoke(this, args);
        }

        public void Clear() {
            _commandIssuedEvents.Clear();
            _errorEvents.Clear();
            _disconnectedEvents.Clear();
            _driverNotLoadedEvents.Clear();
            _speedFeedbackEvents.Clear();
        }
    }
}
