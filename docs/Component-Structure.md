# SingulationHomePage Component Structure

## Visual Component Hierarchy

```
SingulationHomePage (ContentPage)
│
├── ScrollView
│   └── Grid (Main Container - Padding: 16, RowSpacing: 16)
│       │
│       ├── [Row 0] Header Frame (CardFrame style)
│       │   └── Grid (3 columns)
│       │       ├── Label "分件助手" (Column 0)
│       │       ├── Button 🔍 Search (Column 1)
│       │       └── Button ⚙️ Settings (Column 2)
│       │
│       ├── [Row 1] Toolbar Grid Row 1 (4 columns)
│       │   ├── Button 🔄 刷新控制器 (Secondary style)
│       │   ├── Button 🛡️ 安全指令 (Primary style)
│       │   ├── Button ✓ 全部使能 (Success style)
│       │   └── Button ✗ 全部禁用 (Disabled style)
│       │
│       ├── [Row 2] Toolbar Grid Row 2 (Full width)
│       │   └── Button 📊 轴速度设置 (Primary style)
│       │
│       ├── [Row 3] Batch Information Frame
│       │   └── Label "批次：{BatchNumber}"
│       │
│       ├── [Row 4] Mode Switcher Frame (CardFrame style)
│       │   └── Grid (2 columns)
│       │       ├── Button "自动分离" (Toggle)
│       │       └── Button "手动分离" (Toggle)
│       │
│       ├── [Row 5] Motor Grid CollectionView
│       │   └── GridItemsLayout (3 columns, 8dp spacing)
│       │       └── ItemTemplate (20 items: M01-M20)
│       │           └── Frame (CardFrame style)
│       │               ├── TapGestureRecognizer
│       │               ├── DataTriggers (Selected/Abnormal/Disabled)
│       │               └── VerticalStackLayout
│       │                   ├── Label: Motor ID (M01-M20)
│       │                   ├── Label: RPM Value (Large)
│       │                   └── Label: Unit (r/min)
│       │
│       └── [Row 6] Main Action Button
│           └── Button "分离" (Primary style, with shadow)
```

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────┐
│          SingulationHomeViewModel (ViewModel)           │
├─────────────────────────────────────────────────────────┤
│ Properties:                                             │
│  • BatchNumber: string = "DJ61957AAK00025"              │
│  • SelectedMode: string = "Auto"                        │
│  • MotorAxes: ObservableCollection<MotorAxisInfo>      │
│  • SelectedMotor: MotorAxisInfo?                        │
├─────────────────────────────────────────────────────────┤
│ Commands:                                               │
│  • SearchCommand                                        │
│  • SettingsCommand                                      │
│  • RefreshControllerCommand                             │
│  • SafetyCommandCommand                                 │
│  • EnableAllCommand                                     │
│  • DisableAllCommand                                    │
│  • AxisSpeedSettingCommand                              │
│  • SelectModeCommand                                    │
│  • SeparateCommand                                      │
│  • SelectMotorCommand                                   │
└─────────────────────────────────────────────────────────┘
                        ↕ Data Binding
┌─────────────────────────────────────────────────────────┐
│         SingulationHomePage (View - XAML)               │
├─────────────────────────────────────────────────────────┤
│ UI Elements:                                            │
│  • Header (Title + Action Buttons)                      │
│  • Toolbar Row 1 (4 Buttons)                            │
│  • Toolbar Row 2 (1 Button)                             │
│  • Batch Info Display                                   │
│  • Mode Switcher (2 Toggle Buttons)                     │
│  • Motor Grid (20 Cards in 3 columns)                   │
│  • Main Action Button                                   │
└─────────────────────────────────────────────────────────┘
                        ↕ User Interaction
┌─────────────────────────────────────────────────────────┐
│                User Actions & Dialogs                   │
├─────────────────────────────────────────────────────────┤
│  • DisplayActionSheet (Safety Commands)                 │
│  • DisplayPromptAsync (Speed Setting)                   │
│  • DisplayAlert (Confirmations & Results)               │
└─────────────────────────────────────────────────────────┘
```

## Motor State Machine

```
┌─────────────┐
│   Normal    │ ← Default state
│  (White BG) │
└──────┬──────┘
       │
       ├─→ Tap ──→ ┌─────────────┐
       │           │  Selected   │
       │           │ (Blue BG)   │
       │           └──────┬──────┘
       │                  │
       ├─→ Error ─→ ┌──────────┐
       │            │ Abnormal │
       │            │ (Red BG) │
       │            └──────────┘
       │
       └─→ Disable → ┌───────────┐
                     │ Disabled  │
                     │ (Gray)    │
                     └───────────┘
```

## Command Execution Flow

### Safety Command Flow
```
User Taps "安全指令" Button
    ↓
SafetyCommandCommand.Execute()
    ↓
DisplayActionSheet("安全指令", options: ["启动", "停止", "重置", "取消"])
    ↓
User Selects Option
    ↓
DisplayAlert("已执行: {option}")
```

### Speed Setting Flow
```
User Taps "轴速度设置" Button
    ↓
AxisSpeedSettingCommand.Execute()
    ↓
DisplayPromptAsync("请输入目标速度 (r/min)", default: "2000")
    ↓
User Enters Value
    ↓
Validate & Parse Input
    ↓
Update All MotorAxes.Rpm
    ↓
DisplayAlert("已设置所有轴速度为: {speed} r/min")
```

### Motor Selection Flow
```
User Taps Motor Card
    ↓
SelectMotorCommand.Execute(motor)
    ↓
Deselect All Motors (IsSelected = false)
    ↓
Select Tapped Motor (IsSelected = true)
    ↓
Update UI (Card Background → Blue)
```

### Separation Flow
```
User Taps "分离" Button
    ↓
SeparateCommand.Execute()
    ↓
DisplayAlert("确认执行{mode}操作吗？\n批次: {BatchNumber}")
    ↓
User Confirms
    ↓
DisplayAlert("{mode}操作已启动")
```

## Style Inheritance

```
App.xaml Resources
    ↓
SingulationHomePage.xaml Resources
    ├── Theme Colors (Primary, Danger, Success, Disabled, etc.)
    ├── ToolbarButton Style (radius: 20, padding: 15,12)
    └── CardFrame Style (radius: 24, shadow: soft)
        ↓
    Applied to UI Elements
        ├── Header Frame
        ├── Toolbar Buttons
        ├── Mode Switcher Frame
        └── Motor Cards
```

## Responsive Layout Strategy

```
┌──────────────────────────────────────┐
│  iPhone Portrait (Primary Target)   │
│  Width: 375-428pt                   │
│  SafeArea Insets: Top/Bottom        │
└──────────────────────────────────────┘
                ↓
┌──────────────────────────────────────┐
│  ScrollView (Vertical)               │
│  ├── Content fits width              │
│  ├── Height: Auto (scrollable)       │
│  └── Padding: 16dp                   │
└──────────────────────────────────────┘
                ↓
┌──────────────────────────────────────┐
│  Motor Grid                          │
│  ├── 3 Columns (Fixed)               │
│  ├── Column Width: (Width-48)/3      │
│  ├── Spacing: 8dp                    │
│  └── Height: Auto                    │
└──────────────────────────────────────┘
```

## Color Application Map

```
Component                   Color           Usage
────────────────────────────────────────────────────
Page Background             #F6F7FB         Base layer
Card Backgrounds            #FFFFFF         Content containers
Primary Buttons             #3B82F6         Main actions
Secondary Buttons           #F1F5F9         Optional actions
Success Buttons             #10B981         Enable/positive
Disabled Buttons            #94A3B8         Disable/negative
Abnormal State              #EF4444         Errors/warnings
Text Primary                #0F172A         Main content
Text Secondary              #64748B         Supporting text
Batch Info Background       #F1F5F9         Information display
Mode Switcher Active        #3B82F6         Selected state
Mode Switcher Inactive      Transparent     Unselected state
Motor Normal                #FFFFFF         Default state
Motor Selected              #3B82F6         Interaction state
Motor Abnormal              #EF4444         Error state
Motor Disabled Border       #CBD5E1         Inactive state
```

## Performance Considerations

1. **CollectionView with GridItemsLayout**: 
   - Virtualization for efficient rendering of 20 motor cards
   - Only visible items are rendered

2. **Observable Collections**:
   - Automatic UI updates on data changes
   - Minimal re-rendering

3. **Command Pattern**:
   - Async/await for non-blocking UI
   - Command CanExecute for button states

4. **Data Binding**:
   - One-way binding for read-only data
   - Two-way binding for user inputs
   - Property change notifications

## Future Enhancement Points

- [ ] Integration with real motor controller API
- [ ] Real-time speed updates via SignalR
- [ ] Historical data charts
- [ ] Multi-selection support for motors
- [ ] Batch operation history
- [ ] Custom themes/color schemes
- [ ] Accessibility improvements (VoiceOver/TalkBack)
- [ ] Offline mode support
