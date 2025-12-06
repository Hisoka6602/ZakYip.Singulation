# 工具目录 (Tools Directory)

本目录包含影分身防线（Anti-Duplication Defense）的自动化工具和脚本。

## 📁 文件列表

### 1. update-tech-debt.sh
**用途**: 技术债务管理助手

**功能**:
- 列出所有技术债务
- 显示技术债务统计
- 显示待处理项
- 检查P0关键债务
- 计算健康度评分
- 提交PR前检查

**使用方法**:
```bash
# 列出所有技术债务
./tools/update-tech-debt.sh list

# 显示统计信息
./tools/update-tech-debt.sh stats

# 仅显示待处理的技术债务
./tools/update-tech-debt.sh pending

# 检查P0关键债务
./tools/update-tech-debt.sh p0

# 计算健康度
./tools/update-tech-debt.sh health

# 提交PR前检查
./tools/update-tech-debt.sh check
```

**示例输出**:
```
=== 提交PR前技术债务检查 ===

✅ 无关键(P0)技术债务

⚠️  发现 2 个待处理的高优先级(P1)技术债务

建议在提交PR前处理，或在PR中说明原因。

技术债务健康度: 75/100
✅ 良好

✅ 可以提交PR
```

---

### 2. check-duplication.sh
**用途**: 全面的代码重复检测脚本

**功能**:
- 检测SafeExecute模式重复
- 检测手动事件触发模式
- 检测重复的方法名和类名
- 检测参数验证模式重复
- 检测异常处理模式
- 检测循环中创建对象
- 检测代码复杂度

**使用方法**:
```bash
# 在项目根目录运行
./tools/check-duplication.sh
```

**退出码**:
- `0`: 所有检查通过或只有轻微问题
- `1`: 发现严重问题，需要修复

**示例输出**:
```
🛡️ ===== 影分身防线：代码重复检测 =====

📋 检查1: SafeExecute模式重复
预期：≤2处（CabinetIsolator + ICabinetIsolator接口）
✅ 通过：SafeExecute实现数量合规（2处）

📋 检查2: 手动事件触发模式
建议：使用LeadshineHelpers.FireEachNonBlocking()
⚠️  发现 18 处手动事件触发
   建议使用 LeadshineHelpers.FireEachNonBlocking()
...
```

---

### 3. pre-commit
**用途**: Git pre-commit hook，在提交前检查代码重复

**功能**:
- 检查待提交文件是否使用了项目工具类
- 检查方法复杂度
- 检查重复的代码块
- 交互式确认（发现问题时）

**安装方法**:
```bash
# 复制到 .git/hooks 目录
cp tools/pre-commit .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

**或使用符号链接**:
```bash
ln -s ../../tools/pre-commit .git/hooks/pre-commit
```

**工作流程**:
```
1. 开发者执行 git commit
2. pre-commit hook 自动运行
3. 检查待提交的C#文件
4. 如果发现问题：
   - 显示警告信息
   - 询问是否继续提交
   - 用户可以选择取消并修复
5. 如果没有问题：
   - 直接提交
```

**示例输出**:
```
🛡️ 运行影分身防线检查...
检查 3 个C#文件...

🔍 检查1: 是否正确使用项目工具类...
⚠️  MyService.cs 包含手动事件触发代码
   → 建议使用 LeadshineHelpers.FireEachNonBlocking()

🔍 检查2: 方法复杂度...
✅ 通过

🔍 检查3: 重复的代码块...
✅ 通过

=========================================
⚠️  发现 1 个需要注意的问题

这些是警告，不会阻止提交，但建议修复。
参考文档: ANTI_DUPLICATION_DEFENSE.md

是否继续提交? (y/n)
```

---

## 🔄 CI/CD 集成

这些工具已集成到GitHub Actions工作流中：

### .github/workflows/anti-duplication.yml

**触发条件**:
- Pull Request到main或develop分支
- Push到main或develop分支
- 只在C#文件变更时运行

**执行步骤**:
1. 检出代码
2. 设置.NET环境
3. 运行check-duplication.sh
4. 编译项目并运行分析器
5. 生成检查摘要
6. 如果发现严重问题则失败

**在PR中查看结果**:
- 工作流状态显示在PR页面
- 点击"Details"查看详细日志
- 查看"Summary"查看检查摘要

---

## 🛠️ 开发和维护

### 修改检测规则

编辑 `check-duplication.sh`:

```bash
# 添加新的检查
echo -e "${BLUE}📋 检查X: 新检查${NC}"

# 实现检查逻辑
YOUR_CHECK=$(grep -r "pattern" --include="*.cs" | wc -l)

if [ "$YOUR_CHECK" -gt THRESHOLD ]; then
    echo -e "${RED}❌ 发现问题${NC}"
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
else
    echo -e "${GREEN}✅ 通过${NC}"
fi
```

### 调整阈值

根据项目规模调整检测阈值：

```bash
# 在 check-duplication.sh 中
if [ "$SAFE_EXEC_IMPL" -gt 2 ]; then  # 修改这个数字
```

### 添加例外

如果某些代码需要例外处理：

```bash
# 在检测命令中添加 grep -v 排除
grep -r "pattern" --include="*.cs" | \
    grep -v "obj/" | \
    grep -v "bin/" | \
    grep -v "MySpecialFile.cs" | \  # 添加例外
    wc -l
```

---

## 📊 检查项说明

### SafeExecute模式
**预期**: ≤2处（接口定义 + 实现类）  
**原因**: 避免多处实现相同的安全执行逻辑

### 手动事件触发
**阈值**: ≤15处  
**原因**: 应使用统一的事件触发辅助方法

### 参数验证模式
**阈值**: ≤50处  
**原因**: 应提取统一的验证工具类

### 捕获通用Exception
**阈值**: ≤250处  
**原因**: 应捕获更具体的异常类型

### 循环中创建对象
**阈值**: ≤60处  
**原因**: 性能考虑，应使用对象池或在循环外创建

### 方法长度
**阈值**: ≤50行  
**原因**: 保持方法简洁，遵循单一职责原则

---

## 🎯 最佳实践

### 1. 本地开发
```bash
# 提交前手动运行检查
./tools/check-duplication.sh

# 如果发现问题，参考ANTI_DUPLICATION_DEFENSE.md修复
```

### 2. 代码审查
```bash
# 审查PR时运行检查
git checkout pr-branch
./tools/check-duplication.sh
```

### 3. 定期检查
```bash
# 每周/每月运行一次全面检查
./tools/check-duplication.sh > duplication-report-$(date +%Y%m%d).txt
```

---

## 🔗 相关文档

- [ANTI_DUPLICATION_DEFENSE.md](../ANTI_DUPLICATION_DEFENSE.md) - 影分身防线完整文档
- [QUICK_FIX_GUIDE.md](../QUICK_FIX_GUIDE.md) - 快速修复指南
- [ISSUE_DETECTION_REPORT.md](../ISSUE_DETECTION_REPORT.md) - 问题检测报告
- [copilot-instructions.md](../copilot-instructions.md) - 编码规范

---

## 📝 更新日志

### 2025-12-06
- 创建check-duplication.sh脚本
- 创建pre-commit hook
- 集成GitHub Actions工作流
- 添加8项检查规则

---

**维护者**: ZakYip.Singulation 团队  
**最后更新**: 2025-12-06
