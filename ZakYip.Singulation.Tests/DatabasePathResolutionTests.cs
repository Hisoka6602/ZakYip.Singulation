using System;
using System.IO;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Singulation.Infrastructure.Persistence;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试数据库文件路径解析功能，确保相对路径相对于应用程序基目录解析。
    /// </summary>
    public sealed class DatabasePathResolutionTests {

        [MiniFact]
        public void RelativePathResolvesAgainstBaseDirectory() {
            // 使用相对路径
            var relativePath = "test_data/test.db";
            
            // 计算期望的绝对路径
            var expectedPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            var expectedDir = Path.GetDirectoryName(expectedPath);
            
            ILiteDatabase? db = null;

            try {
                // 清理可能存在的测试目录
                if (Directory.Exists(expectedDir)) {
                    Directory.Delete(expectedDir, true);
                }

                // 注册服务
                var services = new ServiceCollection();
                services.AddLiteDbAxisSettings(relativePath);
                var sp = services.BuildServiceProvider();

                // 获取数据库实例
                db = sp.GetRequiredService<ILiteDatabase>();

                // 验证目录在应用程序基目录下创建
                MiniAssert.True(Directory.Exists(expectedDir), "数据库目录应该在应用程序基目录下创建");
                
                // 验证数据库文件在正确位置创建
                MiniAssert.True(File.Exists(expectedPath), $"数据库文件应该在 {expectedPath} 创建");
            }
            finally {
                // 清理数据库连接
                db?.Dispose();
                
                // 清理测试目录
                if (Directory.Exists(expectedDir)) {
                    try {
                        Directory.Delete(expectedDir, true);
                    }
                    catch {
                        // 忽略清理失败
                    }
                }
            }
        }

        [MiniFact]
        public void AbsolutePathRemainsUnchanged() {
            // 使用绝对路径
            var testDir = Path.Combine(Path.GetTempPath(), $"db_test_{Guid.NewGuid()}");
            var absolutePath = Path.Combine(testDir, "test.db");
            
            ILiteDatabase? db = null;

            try {
                // 确保测试目录不存在
                if (Directory.Exists(testDir)) {
                    Directory.Delete(testDir, true);
                }

                // 注册服务
                var services = new ServiceCollection();
                services.AddLiteDbAxisSettings(absolutePath);
                var sp = services.BuildServiceProvider();

                // 获取数据库实例
                db = sp.GetRequiredService<ILiteDatabase>();

                // 验证目录在指定的绝对路径创建
                MiniAssert.True(Directory.Exists(testDir), "数据库目录应该在指定的绝对路径创建");
                
                // 验证数据库文件在指定位置创建
                MiniAssert.True(File.Exists(absolutePath), "数据库文件应该在指定的绝对路径创建");
            }
            finally {
                // 清理数据库连接
                db?.Dispose();
                
                // 清理测试目录
                if (Directory.Exists(testDir)) {
                    try {
                        Directory.Delete(testDir, true);
                    }
                    catch {
                        // 忽略清理失败
                    }
                }
            }
        }

        [MiniFact]
        public void DefaultPathUsesBaseDirectory() {
            // 默认路径
            var defaultPath = "data/singulation.db";
            var expectedPath = Path.Combine(AppContext.BaseDirectory, defaultPath);
            var expectedDir = Path.GetDirectoryName(expectedPath);
            
            ILiteDatabase? db = null;

            try {
                // 清理可能存在的测试目录
                if (Directory.Exists(expectedDir)) {
                    Directory.Delete(expectedDir, true);
                }

                // 注册服务（使用默认参数）
                var services = new ServiceCollection();
                services.AddLiteDbAxisSettings();
                var sp = services.BuildServiceProvider();

                // 获取数据库实例
                db = sp.GetRequiredService<ILiteDatabase>();

                // 验证目录在应用程序基目录下创建
                MiniAssert.True(Directory.Exists(expectedDir), "默认数据库目录应该在应用程序基目录下创建");
                
                // 验证数据库文件在正确位置创建
                MiniAssert.True(File.Exists(expectedPath), $"默认数据库文件应该在 {expectedPath} 创建");
            }
            finally {
                // 清理数据库连接
                db?.Dispose();
                
                // 清理测试目录
                if (Directory.Exists(expectedDir)) {
                    try {
                        Directory.Delete(expectedDir, true);
                    }
                    catch {
                        // 忽略清理失败
                    }
                }
            }
        }
    }
}
