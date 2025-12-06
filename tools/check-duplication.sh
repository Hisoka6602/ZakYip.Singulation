#!/bin/bash
# 代码重复检测脚本 - 影分身防线工具

set -e

REPO_DIR="/home/runner/work/ZakYip.Singulation/ZakYip.Singulation"
cd "$REPO_DIR"

echo "🛡️ ===== 影分身防线：代码重复检测 ====="
echo ""

# 颜色定义
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

ISSUES_FOUND=0

# ===== 检查1: SafeExecute模式重复 =====
echo -e "${BLUE}📋 检查1: SafeExecute模式重复${NC}"
echo "预期：≤2处（CabinetIsolator + ICabinetIsolator接口）"

SAFE_EXEC_IMPL=$(grep -r "public.*SafeExecute.*Action" --include="*.cs" | \
    grep -v "obj/" | grep -v "bin/" | grep -v "Obsolete" | \
    grep -v "interface " | wc -l)

if [ "$SAFE_EXEC_IMPL" -gt 2 ]; then
    echo -e "${RED}❌ 发现 $SAFE_EXEC_IMPL 处SafeExecute实现（预期≤2）${NC}"
    echo "   违规位置："
    grep -rn "public.*SafeExecute.*Action" --include="*.cs" | \
        grep -v "obj/" | grep -v "bin/" | grep -v "Obsolete" | \
        grep -v "interface " | head -10
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
else
    echo -e "${GREEN}✅ 通过：SafeExecute实现数量合规（$SAFE_EXEC_IMPL处）${NC}"
fi
echo ""

# ===== 检查2: 手动事件触发模式 =====
echo -e "${BLUE}📋 检查2: 手动事件触发模式${NC}"
echo "建议：使用LeadshineHelpers.FireEachNonBlocking()"

MANUAL_FIRE_COUNT=$(grep -r "Task\.Run.*=>.*Invoke\|Task\.Run.*{.*Invoke" --include="*.cs" | \
    grep -v "obj/" | grep -v "bin/" | \
    grep -v "FireEachNonBlocking" | wc -l)

if [ "$MANUAL_FIRE_COUNT" -gt 15 ]; then
    echo -e "${YELLOW}⚠️  发现 $MANUAL_FIRE_COUNT 处手动事件触发${NC}"
    echo "   建议使用 LeadshineHelpers.FireEachNonBlocking()"
    echo "   前5处示例："
    grep -rn "Task\.Run.*=>.*Invoke\|Task\.Run.*{.*Invoke" --include="*.cs" | \
        grep -v "obj/" | grep -v "bin/" | \
        grep -v "FireEachNonBlocking" | head -5
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
else
    echo -e "${GREEN}✅ 通过：手动事件触发数量可接受（$MANUAL_FIRE_COUNT处）${NC}"
fi
echo ""

# ===== 检查3: 重复的方法名（可能的重复实现） =====
echo -e "${BLUE}📋 检查3: 重复的方法名${NC}"
echo "检查是否有过多的相似方法名..."

echo "统计前10个最常见的方法名："
find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | \
    xargs grep -h "^\s*public\|^\s*private\|^\s*protected" | \
    grep -oP '(?<=\s)\w+(?=\()' | \
    sort | uniq -c | sort -rn | head -10

echo ""

# ===== 检查4: 重复的类名 =====
echo -e "${BLUE}📋 检查4: 重复的类名${NC}"

DUPLICATE_CLASSES=$(find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | \
    xargs basename -a | sort | uniq -d | wc -l)

if [ "$DUPLICATE_CLASSES" -gt 5 ]; then
    echo -e "${YELLOW}⚠️  发现 $DUPLICATE_CLASSES 个重复的类名${NC}"
    echo "   重复的类名："
    find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | \
        xargs basename -a | sort | uniq -d | head -10
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
else
    echo -e "${GREEN}✅ 通过：重复类名数量可接受（$DUPLICATE_CLASSES个）${NC}"
fi
echo ""

# ===== 检查5: 相似的验证代码模式 =====
echo -e "${BLUE}📋 检查5: 参数验证模式重复${NC}"

VALIDATION_PATTERNS=$(grep -r "if.*<.*||.*>.*throw.*ArgumentException\|if.*<.*||.*>.*throw.*ArgumentOutOfRangeException" \
    --include="*.cs" | grep -v "obj/" | grep -v "bin/" | wc -l)

if [ "$VALIDATION_PATTERNS" -gt 50 ]; then
    echo -e "${YELLOW}⚠️  发现 $VALIDATION_PATTERNS 处参数验证模式${NC}"
    echo "   建议：创建统一的验证工具类"
    echo "   示例位置（前3处）："
    grep -rn "if.*<.*||.*>.*throw.*ArgumentException\|if.*<.*||.*>.*throw.*ArgumentOutOfRangeException" \
        --include="*.cs" | grep -v "obj/" | grep -v "bin/" | head -3
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
else
    echo -e "${GREEN}✅ 通过：参数验证模式数量可接受（$VALIDATION_PATTERNS处）${NC}"
fi
echo ""

# ===== 检查6: 异常处理模式重复 =====
echo -e "${BLUE}📋 检查6: 异常处理模式（catch Exception）${NC}"

CATCH_EXCEPTION=$(grep -r "catch (Exception" --include="*.cs" | \
    grep -v "obj/" | grep -v "bin/" | wc -l)

echo "发现 $CATCH_EXCEPTION 处捕获通用Exception"
if [ "$CATCH_EXCEPTION" -gt 250 ]; then
    echo -e "${RED}❌ 捕获通用Exception数量过多（>250）${NC}"
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
elif [ "$CATCH_EXCEPTION" -gt 200 ]; then
    echo -e "${YELLOW}⚠️  捕获通用Exception数量偏高（>200）${NC}"
else
    echo -e "${GREEN}✅ 捕获通用Exception数量可接受${NC}"
fi
echo ""

# ===== 检查7: 循环中创建对象 =====
echo -e "${BLUE}📋 检查7: 循环中创建对象（性能问题）${NC}"

LOOP_NEW_COUNT=$(grep -r "for\|foreach\|while" --include="*.cs" -A 5 | \
    grep "new " | grep -v "obj/" | grep -v "bin/" | wc -l)

if [ "$LOOP_NEW_COUNT" -gt 60 ]; then
    echo -e "${YELLOW}⚠️  发现约 $LOOP_NEW_COUNT 处循环中创建对象${NC}"
    echo "   建议：使用对象池或在循环外创建"
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
else
    echo -e "${GREEN}✅ 通过：循环中创建对象数量可接受${NC}"
fi
echo ""

# ===== 检查8: 代码复杂度 =====
echo -e "${BLUE}📋 检查8: 代码复杂度（方法长度）${NC}"

LONG_METHODS=$(find . -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | \
    xargs awk '/^[[:space:]]*(public|private|protected).*\(/ {count=0} 
               /^[[:space:]]*{/ {braces++} 
               /^[[:space:]]*}/ {braces--; if(braces==0 && count>50) print FILENAME":"NR":"count} 
               {if(braces>0) count++}' | wc -l)

if [ "$LONG_METHODS" -gt 10 ]; then
    echo -e "${YELLOW}⚠️  发现 $LONG_METHODS 个超过50行的方法${NC}"
    echo "   建议：拆分长方法，遵循单一职责原则"
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
else
    echo -e "${GREEN}✅ 通过：长方法数量可接受${NC}"
fi
echo ""

# ===== 总结 =====
echo "========================================="
echo ""
if [ "$ISSUES_FOUND" -eq 0 ]; then
    echo -e "${GREEN}✅ 恭喜！所有检查通过，未发现严重的代码重复问题。${NC}"
    echo ""
    exit 0
elif [ "$ISSUES_FOUND" -le 2 ]; then
    echo -e "${YELLOW}⚠️  发现 $ISSUES_FOUND 个需要关注的问题。${NC}"
    echo ""
    echo "建议查看上述警告并考虑改进。"
    exit 0
else
    echo -e "${RED}❌ 发现 $ISSUES_FOUND 个严重问题需要修复。${NC}"
    echo ""
    echo "请参考 ANTI_DUPLICATION_DEFENSE.md 文档进行改进。"
    exit 1
fi
