# 上线手册

## 目录结构

- `install.ps1` / `install.sh`：发布 Host 并生成运行包，Windows 版额外调用历史批处理安装服务。
- `uninstall.ps1` / `uninstall.sh`：卸载 Windows 服务（Linux 版仅提示说明）。
- `selfcheck.ps1` / `selfcheck.sh`：执行 `dotnet build` 进行快速自检。
- `dryrun.ps1` / `dryrun.sh`：运行 ConsoleDemo 回归脚本（`--regression`）。

## 使用建议

1. 在发布机上执行 `selfcheck` 确认编译通过。
2. 运行 `install` 生成发布目录，Windows 环境下可直接安装服务。
3. 使用 `dryrun` 验证“启动→三档→停机→恢复→断连→降级→恢复”流程。
4. 如需卸载 Windows 服务，执行 `uninstall.ps1`。
