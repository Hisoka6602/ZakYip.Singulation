using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZakYip.Singulation.Drivers.Abstractions.Events;

namespace ZakYip.Singulation.Drivers.Abstractions {

    /// <summary>
    /// 聚合多个轴的驱动事件，并对外统一转发。
    /// </summary>
    public interface IAxisEventAggregator {

        void Attach(IAxisDrive drive);

        void Detach(IAxisDrive drive);

        event EventHandler<AxisSpeedFeedbackEventArgs>? SpeedFeedback;

        event EventHandler<AxisCommandIssuedEventArgs>? CommandIssued;

        event EventHandler<AxisErrorEventArgs>? AxisFaulted;

        event EventHandler<AxisDisconnectedEventArgs>? AxisDisconnected;

        event EventHandler<DriverNotLoadedEventArgs>? DriverNotLoaded;
    }
}