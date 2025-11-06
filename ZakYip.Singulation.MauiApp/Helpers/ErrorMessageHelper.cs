namespace ZakYip.Singulation.MauiApp.Helpers;

/// <summary>
/// 将技术错误信息转换为用户友好的提示信息
/// </summary>
public static class ErrorMessageHelper
{
    /// <summary>
    /// 将技术异常信息转换为用户友好的消息
    /// </summary>
    public static string GetFriendlyErrorMessage(string technicalMessage)
    {
        if (string.IsNullOrWhiteSpace(technicalMessage))
            return "操作失败，请重试";

        var lowerMessage = technicalMessage.ToLower();

        // 网络相关错误
        if (lowerMessage.Contains("timeout") || lowerMessage.Contains("timed out"))
            return "连接超时，请检查网络连接";
        
        if (lowerMessage.Contains("connection") && (lowerMessage.Contains("refused") || lowerMessage.Contains("failed")))
            return "无法连接到服务器，请检查服务器是否运行";
        
        if (lowerMessage.Contains("no such host") || lowerMessage.Contains("name resolution"))
            return "无法找到服务器地址，请检查配置";
        
        if (lowerMessage.Contains("network") && lowerMessage.Contains("unreachable"))
            return "网络不可达，请检查网络连接";
        
        // HTTP 状态码
        if (lowerMessage.Contains("404") || lowerMessage.Contains("not found"))
            return "请求的资源不存在";
        
        if (lowerMessage.Contains("401") || lowerMessage.Contains("unauthorized"))
            return "未授权，请检查登录状态";
        
        if (lowerMessage.Contains("403") || lowerMessage.Contains("forbidden"))
            return "没有访问权限";
        
        if (lowerMessage.Contains("500") || lowerMessage.Contains("internal server"))
            return "服务器内部错误，请稍后重试";
        
        if (lowerMessage.Contains("503") || lowerMessage.Contains("service unavailable"))
            return "服务暂时不可用，请稍后重试";
        
        // SignalR 相关错误
        if (lowerMessage.Contains("signalr") || lowerMessage.Contains("hub"))
            return "实时连接失败，请检查网络";
        
        // UDP 相关错误
        if (lowerMessage.Contains("udp") || lowerMessage.Contains("broadcast"))
            return "网络发现失败，请尝试手动配置";
        
        // 数据解析错误
        if (lowerMessage.Contains("json") || lowerMessage.Contains("deserialize"))
            return "数据格式错误，请联系技术支持";
        
        // 参数错误
        if (lowerMessage.Contains("invalid") || lowerMessage.Contains("argument"))
            return "参数错误，请检查输入";
        
        // 权限错误
        if (lowerMessage.Contains("permission") || lowerMessage.Contains("access denied"))
            return "没有操作权限";
        
        // 默认：简化技术消息
        return SimplifyTechnicalMessage(technicalMessage);
    }
    
    /// <summary>
    /// 简化技术消息，移除堆栈跟踪和技术细节
    /// </summary>
    private static string SimplifyTechnicalMessage(string message)
    {
        // 只取第一行或第一句话
        var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.Length > 0 ? lines[0] : message;
        
        // 截取第一个句号或冒号之前的内容
        var sentences = firstLine.Split(new[] { '.', ':' }, 2);
        var simplified = sentences.Length > 0 ? sentences[0].Trim() : firstLine;
        
        // 限制长度
        if (simplified.Length > 100)
            simplified = simplified.Substring(0, 97) + "...";
        
        // 如果简化后的消息过于技术化，使用通用消息
        if (simplified.Length < 10 || simplified.Any(char.IsDigit))
            return "操作失败，请稍后重试";
        
        return simplified;
    }
    
    /// <summary>
    /// 获取操作成功的友好消息
    /// </summary>
    public static string GetSuccessMessage(string operation)
    {
        return operation switch
        {
            "refresh" => "刷新成功",
            "enable" => "使能成功",
            "disable" => "禁用成功",
            "connect" => "连接成功",
            "disconnect" => "断开连接成功",
            "save" => "保存成功",
            "delete" => "删除成功",
            "update" => "更新成功",
            "send" => "发送成功",
            _ => "操作成功"
        };
    }
}
