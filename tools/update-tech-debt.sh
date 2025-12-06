#!/bin/bash
# 技术债务更新助手 - Technical Debt Update Helper
# 用法: ./tools/update-tech-debt.sh [add|complete|list|stats]

set -e

TECH_DEBT_FILE="TECHNICAL_DEBT.md"
TODAY=$(date +%Y-%m-%d)

# 颜色定义
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

function show_usage() {
    echo "用法: $0 [命令]"
    echo ""
    echo "命令:"
    echo "  list       - 列出所有技术债务"
    echo "  stats      - 显示技术债务统计"
    echo "  pending    - 仅显示待处理的技术债务"
    echo "  p0         - 显示关键技术债务（P0）"
    echo "  health     - 计算并显示技术债务健康度"
    echo "  check      - 检查是否有必须处理的技术债务"
    echo ""
    echo "示例:"
    echo "  $0 list     # 列出所有技术债务"
    echo "  $0 check    # 提交PR前检查"
}

function list_tech_debt() {
    echo -e "${BLUE}=== 技术债务列表 ===${NC}"
    echo ""
    
    # 提取所有技术债务项
    grep -A 15 "^### TD-" "$TECH_DEBT_FILE" | head -100
}

function show_stats() {
    echo -e "${BLUE}=== 技术债务统计 ===${NC}"
    echo ""
    
    # 统计各优先级数量（使用更可靠的方法）
    P0_COUNT=$(grep "优先级\*\*: P0" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    P1_COUNT=$(grep "优先级\*\*: P1" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    P2_COUNT=$(grep "优先级\*\*: P2" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    P3_COUNT=$(grep "优先级\*\*: P3" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    
    # 统计各状态数量
    PENDING_COUNT=$(grep "状态\*\*: ⏳ 待处理" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    IN_PROGRESS_COUNT=$(grep "状态\*\*: 🔄 进行中" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    DONE_COUNT=$(grep "状态\*\*: ✅ 已完成" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    
    echo "按优先级:"
    echo "  P0 (关键): ${P0_COUNT} 个"
    echo "  P1 (高):   ${P1_COUNT} 个"
    echo "  P2 (中):   ${P2_COUNT} 个"
    echo "  P3 (低):   ${P3_COUNT} 个"
    echo ""
    echo "按状态:"
    echo "  ⏳ 待处理: ${PENDING_COUNT} 个"
    echo "  🔄 进行中: ${IN_PROGRESS_COUNT} 个"
    echo "  ✅ 已完成: ${DONE_COUNT} 个"
    echo ""
    
    # 计算健康度
    HEALTH=$((100 - P0_COUNT * 25 - P1_COUNT * 10 - P2_COUNT * 3 - P3_COUNT * 1))
    
    echo "技术债务健康度: $HEALTH/100"
    
    if [ "$HEALTH" -ge 90 ]; then
        echo -e "${GREEN}评级: 优秀 ✅${NC}"
    elif [ "$HEALTH" -ge 75 ]; then
        echo -e "${GREEN}评级: 良好 ✅${NC}"
    elif [ "$HEALTH" -ge 60 ]; then
        echo -e "${YELLOW}评级: 一般 ⚠️${NC}"
    elif [ "$HEALTH" -ge 45 ]; then
        echo -e "${RED}评级: 需改进 🔴${NC}"
    else
        echo -e "${RED}评级: 危险 ⛔${NC}"
    fi
}

function show_pending() {
    echo -e "${YELLOW}=== 待处理的技术债务 ===${NC}"
    echo ""
    
    # 提取待处理项
    awk '/^### TD-[0-9]+:/ {found=1; item=$0} 
         found && /状态\*\*: ⏳ 待处理/ {print item; found=0}' \
         "$TECH_DEBT_FILE"
}

function show_p0() {
    echo -e "${RED}=== P0 - 关键技术债务 ===${NC}"
    echo ""
    
    P0_COUNT=$(grep "优先级\*\*: P0" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    
    if [ "$P0_COUNT" -eq 0 ]; then
        echo -e "${GREEN}✅ 无关键技术债务${NC}"
    else
        # 提取P0项
        awk '/^### TD-[0-9]+:/ {found=1; item=$0} 
             found && /优先级\*\*: P0/ {print item; found=0}' \
             "$TECH_DEBT_FILE"
    fi
}

function calculate_health() {
    P0_COUNT=$(grep "优先级\*\*: P0" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    P1_COUNT=$(grep "优先级\*\*: P1" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    P2_COUNT=$(grep "优先级\*\*: P2" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    P3_COUNT=$(grep "优先级\*\*: P3" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    
    HEALTH=$((100 - P0_COUNT * 25 - P1_COUNT * 10 - P2_COUNT * 3 - P3_COUNT * 1))
    
    echo -e "${BLUE}技术债务健康度: $HEALTH/100${NC}"
    
    if [ "$HEALTH" -ge 90 ]; then
        echo -e "${GREEN}✅ 优秀${NC}"
        return 0
    elif [ "$HEALTH" -ge 75 ]; then
        echo -e "${GREEN}✅ 良好${NC}"
        return 0
    elif [ "$HEALTH" -ge 60 ]; then
        echo -e "${YELLOW}⚠️  一般${NC}"
        return 0
    elif [ "$HEALTH" -ge 45 ]; then
        echo -e "${RED}🔴 需改进${NC}"
        return 1
    else
        echo -e "${RED}⛔ 危险${NC}"
        return 2
    fi
}

function check_before_pr() {
    echo -e "${BLUE}=== 提交PR前技术债务检查 ===${NC}"
    echo ""
    
    # 检查P0
    P0_COUNT=$(grep "优先级\*\*: P0" "$TECH_DEBT_FILE" 2>/dev/null | wc -l | xargs)
    
    if [ "$P0_COUNT" -gt 0 ]; then
        echo -e "${RED}❌ 发现 $P0_COUNT 个关键(P0)技术债务${NC}"
        echo ""
        show_p0
        echo ""
        echo -e "${RED}请在提交PR前处理所有P0技术债务！${NC}"
        return 1
    fi
    
    # 检查P1
    P1_PENDING=$(awk '/^### TD-[0-9]+:/ {item=$0} 
                      /状态\*\*: ⏳ 待处理/ && /优先级\*\*: P1/ {count++} 
                      END {print count+0}' "$TECH_DEBT_FILE")
    
    if [ "$P1_PENDING" -gt 0 ]; then
        echo -e "${YELLOW}⚠️  发现 $P1_PENDING 个待处理的高优先级(P1)技术债务${NC}"
        echo ""
        echo "建议在提交PR前处理，或在PR中说明原因。"
        echo ""
    fi
    
    # 显示健康度
    calculate_health
    
    echo ""
    echo -e "${GREEN}✅ 可以提交PR${NC}"
    return 0
}

# 主逻辑
if [ ! -f "$TECH_DEBT_FILE" ]; then
    echo -e "${RED}错误: 找不到 $TECH_DEBT_FILE${NC}"
    exit 1
fi

case "${1:-list}" in
    list)
        list_tech_debt
        ;;
    stats)
        show_stats
        ;;
    pending)
        show_pending
        ;;
    p0)
        show_p0
        ;;
    health)
        calculate_health
        ;;
    check)
        check_before_pr
        ;;
    help|--help|-h)
        show_usage
        ;;
    *)
        echo -e "${RED}未知命令: $1${NC}"
        echo ""
        show_usage
        exit 1
        ;;
esac
