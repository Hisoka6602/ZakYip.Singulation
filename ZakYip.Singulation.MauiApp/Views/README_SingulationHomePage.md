# Singulation Home Page - Implementation Summary

## Overview
The Singulation Home Page (`SingulationHomePage.xaml`) has been implemented with support for both **portrait** and **landscape** orientations, providing an optimized industrial control interface for the ZakYip Singulation system.

## Features Implemented

### 1. Orientation-Aware Layout
- **Portrait Mode**: Vertical layout with 3-column motor grid (optimized for phone vertical orientation)
- **Landscape Mode**: Horizontal layout with left control panel and 5-column motor grid (optimized for phone horizontal/tablet orientation)
- Dynamic switching using `OnSizeAllocated` method in code-behind

### 2. Design System (iOS-style, Light Theme)
- **Primary Color**: #3B82F6 (Blue)
- **Danger Color**: #EF4444 (Red)
- **Success Color**: #10B981 (Green)
- **Disabled Color**: #94A3B8 (Gray)
- **Background**: #F6F7FB (Light Gray)
- **Text Primary**: #0F172A (Dark)
- **Text Secondary**: #64748B (Gray)
- **Border Radius**: 20-24dp (large, rounded corners)
- **Shadows**: Soft, low elevation for depth

### 3. Portrait Layout Structure
```
┌─────────────────────────────────┐
│ Header (Title + Icons)          │
├─────────────────────────────────┤
│ Toolbar Row 1 (4 buttons)       │
│ [Refresh][Safety][Enable][Dis]  │
├─────────────────────────────────┤
│ Toolbar Row 2                    │
│ [Axis Speed Setting]             │
├─────────────────────────────────┤
│ Batch: DJ61957AAK00025           │
├─────────────────────────────────┤
│ Mode: [Auto] | [Manual]          │
├─────────────────────────────────┤
│ Motor Grid (3 cols × 7 rows)    │
│ M01  M02  M03                    │
│ ...  ...  ...                    │
│ M19  M20                         │
├─────────────────────────────────┤
│ [    分离 (Separate)     ]       │
└─────────────────────────────────┘
```

### 4. Landscape Layout Structure
```
┌────────────────────────────────────────────────────────┐
│ Header (Title + Search + Settings Icons)               │
├──────────────────┬─────────────────────────────────────┤
│ Control Panel    │ Right Info Panel                    │
│ (2-col grid)     │                                     │
│                  │ Batch: DJ61957AAK00025              │
│ [Refresh][Safe]  │ ┌─────────────────────────────────┐ │
│ [Enable][Disable]│ │ Mode: [Auto] | [Manual]         │ │
│ [Axis Speed Set] │ └─────────────────────────────────┘ │
│ (spans 2 cols)   │                                     │
│                  │ Motor Grid (5 cols × 4 rows)       │
│                  │ M01  M02  M03  M04  M05            │
│                  │ M06  M07  M08  M09  M10            │
│                  │ M11  M12  M13  M14  M15            │
│                  │ M16  M17  M18  M19  M20            │
└──────────────────┴─────────────────────────────────────┘
│         [    分离 (Separate)     ]                      │
└────────────────────────────────────────────────────────┘
```

### 5. Control Buttons

#### Portrait Mode (4 buttons in row 1)
1. **刷新控制器 (Refresh Controller)** - Secondary style (light gray)
2. **安全指令 (Safety Command)** - Primary style (blue) with popup menu (启动/停止/重置)
3. **全部使能 (Enable All)** - Success style (green)
4. **全部禁用 (Disable All)** - Disabled style (gray)

#### Landscape Mode (2×3 grid layout)
Same buttons with adjusted styling:
- **刷新控制器**: White background with border
- **安全指令**: Blue background
- **全部使能**: Light green background with green border
- **全部禁用**: Light gray background with gray border
- **轴速度设置**: Spans 2 columns, blue background

### 6. Motor Axis Cards (M01-M20)
Each card displays:
- **Motor ID**: M01, M02, ..., M20
- **RPM Value**: Large, bold number (e.g., 1000, 2000, 2500)
- **Unit**: "r/min" (revolutions per minute)

#### Motor States:
1. **Normal**: White card with dark text
2. **Selected**: Blue background (#3B82F6) with white text
3. **Abnormal**: Red background (#EF4444) with white text
4. **Disabled**: Transparent background with gray border and gray text

### 7. Mode Selector
Capsule-style segmented control:
- **自动分离 (Auto Separation)**: Default active mode
- **手动分离 (Manual Separation)**: Alternative mode
- Active button shows blue background with white text
- Inactive button shows transparent background with gray text

### 8. Main Action Button
- Large, prominent button: **分离 (Separate)**
- Blue background with white text
- Large corner radius (30dp) for capsule shape
- Blue shadow for depth
- Height: 60dp
- In landscape: Centered with fixed width (300px)

## Technical Implementation

### Files Modified
1. `SingulationHomePage.xaml` - Main XAML layout with both portrait and landscape layouts
2. `SingulationHomePage.xaml.cs` - Code-behind with orientation detection

### Code-Behind Logic
```csharp
protected override void OnSizeAllocated(double width, double height)
{
    base.OnSizeAllocated(width, height);
    
    // Determine if device is in landscape mode
    bool isLandscape = width > height;
    
    // Toggle layout visibility
    if (_isLandscape != isLandscape)
    {
        _isLandscape = isLandscape;
        UpdateLayout();
    }
}

private void UpdateLayout()
{
    PortraitLayout.IsVisible = !_isLandscape;
    LandscapeLayout.IsVisible = _isLandscape;
}
```

### View Model (No Changes Required)
The existing `SingulationHomeViewModel.cs` already provides all necessary:
- Commands for all buttons
- Observable collection of 20 motors (M01-M20)
- Batch number and mode selection
- Motor selection and state management

## Dependencies
- .NET MAUI 8.0
- Prism.Maui 9.0
- FontAwesome icons (already included)

## Testing Notes
- The layout automatically switches when device orientation changes
- Both layouts share the same ViewModel and data binding
- All existing functionality (commands, selections, etc.) works in both orientations
- The landscape layout provides better usability for larger screens and tablets

## Future Enhancements
- Add animation transitions when switching orientations
- Implement touch gestures for motor card interactions
- Add visual indicators for loading states
- Implement the actual motor control API integration
