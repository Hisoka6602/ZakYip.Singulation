using System;

namespace ZakYip.Singulation.Core.Contracts.ValueObjects {
    /// <summary>
    /// 运动学参数集合，包含速度、加速度与时间跨度。
    /// </summary>
    public readonly record struct KinematicParams {
        /// <summary>目标线速度（mm/s）。</summary>
        public double LinearVelocity { get; init; }

        /// <summary>目标角速度（rad/s）。</summary>
        public double AngularVelocity { get; init; }

        /// <summary>加速度（mm/s²）。</summary>
        public double Acceleration { get; init; }

        /// <summary>角加速度（rad/s²）。</summary>
        public double AngularAcceleration { get; init; }

        /// <summary>持续时间。</summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// 通过参数构造运动学参数集合。
        /// </summary>
        public KinematicParams(
            double linearVelocity,
            double angularVelocity,
            double acceleration,
            double angularAcceleration,
            TimeSpan duration
        ) {
            LinearVelocity = linearVelocity;
            AngularVelocity = angularVelocity;
            Acceleration = acceleration;
            AngularAcceleration = angularAcceleration;
            Duration = duration;
        }
    }
}
