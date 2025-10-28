#!/bin/bash
# SonarQube 代码分析脚本
# 使用方法: ./sonar-scan.sh

set -e

echo "========================================="
echo "ZakYip.Singulation SonarQube 代码分析"
echo "========================================="

# 检查环境变量
if [ -z "$SONAR_HOST_URL" ]; then
    echo "警告: SONAR_HOST_URL 未设置，使用默认值: http://localhost:9000"
    SONAR_HOST_URL="http://localhost:9000"
fi

if [ -z "$SONAR_TOKEN" ]; then
    echo "错误: SONAR_TOKEN 环境变量必须设置"
    echo "请使用以下命令设置: export SONAR_TOKEN=your-token-here"
    exit 1
fi

# 清理之前的构建产物
echo "清理构建产物..."
dotnet clean

# 开始 SonarScanner
echo "启动 SonarScanner..."
dotnet sonarscanner begin \
    /k:"ZakYip.Singulation" \
    /d:sonar.host.url="$SONAR_HOST_URL" \
    /d:sonar.login="$SONAR_TOKEN" \
    /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

# 构建项目
echo "构建项目..."
dotnet build --no-incremental

# 运行测试并生成覆盖率报告
echo "运行测试并生成代码覆盖率..."
dotnet test \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory ./TestResults \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# 结束 SonarScanner 并上传结果
echo "上传分析结果到 SonarQube..."
dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"

echo "========================================="
echo "SonarQube 代码分析完成!"
echo "请访问 $SONAR_HOST_URL 查看分析结果"
echo "========================================="
