using System;
using System.IO;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using ZakYip.Singulation.Infrastructure.Persistence;

namespace ZakYip.Singulation.Tests {

    /// <summary>
    /// 测试数据库文件自动创建功能。
    /// </summary>
    internal sealed class DatabaseAutoCreationTests {

        [MiniFact]
        public void CreatesDirectoryWhenNotExists() {
            // 使用临时目录进行测试
            var testDir = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}");
            var dbPath = Path.Combine(testDir, "singulation.db");

            try {
                // 确保测试目录不存在
                if (Directory.Exists(testDir)) {
                    Directory.Delete(testDir, true);
                }

                // 注册服务
                var services = new ServiceCollection();
                services.AddLiteDbAxisSettings(dbPath);
                var sp = services.BuildServiceProvider();

                // 获取数据库实例（这会触发目录创建）
                var db = sp.GetRequiredService<ILiteDatabase>();

                // 验证目录已创建
                MiniAssert.True(Directory.Exists(testDir), "数据库目录应该已创建");

                // 验证数据库文件已创建
                MiniAssert.True(File.Exists(dbPath), "数据库文件应该已创建");

                // 清理
                db.Dispose();
            }
            finally {
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
        public void WorksWhenDirectoryAlreadyExists() {
            // 使用临时目录进行测试
            var testDir = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}");
            var dbPath = Path.Combine(testDir, "singulation.db");

            try {
                // 预先创建目录
                Directory.CreateDirectory(testDir);

                // 注册服务
                var services = new ServiceCollection();
                services.AddLiteDbAxisSettings(dbPath);
                var sp = services.BuildServiceProvider();

                // 获取数据库实例
                var db = sp.GetRequiredService<ILiteDatabase>();

                // 验证目录存在
                MiniAssert.True(Directory.Exists(testDir), "数据库目录应该存在");

                // 验证数据库文件已创建
                MiniAssert.True(File.Exists(dbPath), "数据库文件应该已创建");

                // 清理
                db.Dispose();
            }
            finally {
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
        public void WorksWithRootLevelPath() {
            // 测试根级别路径（不带目录）
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");

            try {
                // 注册服务
                var services = new ServiceCollection();
                services.AddLiteDbAxisSettings(dbPath);
                var sp = services.BuildServiceProvider();

                // 获取数据库实例
                var db = sp.GetRequiredService<ILiteDatabase>();

                // 验证数据库文件已创建
                MiniAssert.True(File.Exists(dbPath), "数据库文件应该已创建");

                // 清理
                db.Dispose();
            }
            finally {
                // 清理测试文件
                if (File.Exists(dbPath)) {
                    try {
                        File.Delete(dbPath);
                    }
                    catch {
                        // 忽略清理失败
                    }
                }
            }
        }
    }
}
