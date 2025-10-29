# Fix for localhost:5005 Access Issue

## Problem Description

The web server running on http://localhost:5005 was not accessible because the application startup was being blocked by synchronous Leadshine controller initialization calls.

## Root Cause

The issue occurred during application startup in this sequence:

1. **SafetyPipeline hosted service** starts during application initialization (before Kestrel)
2. **SafetyPipeline.ExecuteAsync** waits for all IO modules to start using `await Task.WhenAll(startTasks)`
3. **LeadshineSafetyIoModule.StartAsync** was called synchronously during startup
4. **LeadshineSafetyIoModule.StartAsync** called `LTDMC.dmc_read_inbit()` to read initial IO state
5. If the Leadshine controller was not initialized or not available, this call would:
   - Hang indefinitely
   - Take a very long time to timeout
   - Block the entire SafetyPipeline from completing startup
   - Prevent Kestrel from accepting HTTP connections on port 5005

## Solution

Modified `LeadshineSafetyIoModule.StartAsync` to use a timeout-protected asynchronous pattern:

1. Changed method signature from `Task StartAsync(...)` to `async Task StartAsync(...)`
2. Wrapped the synchronous `ReadInputBit` call in `Task.Run` to execute on thread pool
3. Added a 2-second timeout using `Task.WhenAny` pattern
4. Falls back to safe default values (local mode) if the read times out or fails

### Code Changes

**File: ZakYip.Singulation.Infrastructure/Safety/LeadshineSafetyIoModule.cs**

```csharp
// Before: Synchronous blocking call
bool rawState = ReadInputBit(
    _options.RemoteLocalModeBit, 
    _options.InvertRemoteLocalLogic ?? _options.InvertLogic);

// After: Async with timeout protection
var readTask = Task.Run(() => ReadInputBit(
    _options.RemoteLocalModeBit, 
    _options.InvertRemoteLocalLogic ?? _options.InvertLogic), ct);
var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), ct);
var completedTask = await Task.WhenAny(readTask, timeoutTask).ConfigureAwait(false);

if (completedTask == readTask) {
    bool rawState = await readTask.ConfigureAwait(false);
    bool isRemoteMode = _options.RemoteLocalActiveHigh ? rawState : !rawState;
    _lastRemoteLocalModeState = isRemoteMode;
    RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs(isRemoteMode, ...));
}
else {
    // Timeout - use safe defaults
    _logger.LogWarning("启动时读取远程/本地模式 IO 超时（控制器可能未初始化），默认为本地模式");
    _lastRemoteLocalModeState = false;
    RemoteLocalModeChanged?.Invoke(this, new RemoteLocalModeChangedEventArgs(false, "启动时读取超时，默认为本地模式"));
}
```

**File: ZakYip.Singulation.Host/Program.cs**

Added clarifying comment to IBusAdapter registration to document that hardware initialization should be delegated to background services and not performed during DI container construction. This reinforces the pattern that prevents blocking startup.

## Verification

To verify the fix works:

1. **Without Hardware**: Start the application without Leadshine controller connected
   - The application should start successfully
   - Port 5005 should be accessible within 2 seconds
   - Logs should show: "启动时读取远程/本地模式 IO 超时（控制器可能未初始化），默认为本地模式"
   - Application defaults to local mode and continues running

2. **With Hardware**: Start the application with Leadshine controller connected
   - The application should start successfully
   - Port 5005 should be accessible
   - Logs should show: "启动时读取远程/本地模式 IO 状态：远程模式" or "本地模式"
   - Application correctly reads and uses the actual IO state

3. **Health Check**: 
   ```bash
   curl http://localhost:5005/health
   ```
   Should return successfully within a few seconds of starting the application

4. **Swagger UI**:
   Navigate to http://localhost:5005/swagger in a browser
   Should load successfully regardless of hardware status

## Benefits

1. **Non-blocking startup**: Web server becomes accessible immediately
2. **Graceful degradation**: Application runs even when hardware is not available
3. **Better diagnostics**: Clear logging when timeout occurs
4. **Backward compatible**: Works exactly as before when hardware is present
5. **Future-proof**: Pattern can be applied to other hardware initialization code

## Related Files

- `ZakYip.Singulation.Infrastructure/Safety/LeadshineSafetyIoModule.cs`
- `ZakYip.Singulation.Infrastructure/Safety/SafetyPipeline.cs`
- `ZakYip.Singulation.Host/Program.cs`

## Testing Recommendations

1. Test startup with and without hardware
2. Verify IO functionality when hardware is available
3. Check that fallback to local mode works correctly
4. Monitor logs for timeout warnings
5. Verify web API remains responsive during startup
