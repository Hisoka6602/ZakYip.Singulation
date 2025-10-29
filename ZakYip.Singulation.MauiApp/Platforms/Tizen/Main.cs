using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using System;

namespace ZakYip.Singulation.MauiApp {
    /// <summary>
    /// Tizen 平台的主程序入口类。
    /// </summary>
    internal class Program : MauiApplication {
        /// <inheritdoc />
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        /// <summary>
        /// 应用程序的入口点。
        /// </summary>
        /// <param name="args">命令行参数。</param>
        static void Main(string[] args) {
            var app = new Program();
            app.Run(args);
        }
    }
}
