# Vivaldi Updater Design System

## 1. Atmosphere & Identity

The application is a compact desktop control panel: calm at rest, explicit while updating, and reassuring after a network interruption. Its signature is a full-width update rail that keeps progress, resume, retry, and source-switch information visible without modal interruption. Pink remains the warm default; Nintendo remains the high-contrast alternative.

## 2. Color

All WPF colors are exposed through dynamic brush resources so theme changes update the running window.

| Role / resource | Pink | Nintendo | Usage |
|---|---:|---:|---|
| `TextPrimaryBrush` | `#342A30` | `#202124` | Titles and primary values |
| `TextMutedBrush` | `#6F4A5C` | `#4B5563` | Labels and secondary text |
| `TextSubtleBrush` | `#75606B` | `#606A78` | Progress detail and metadata |
| `AccentBrush` | `#D84F72` | `#E60012` | Primary action and focus |
| `AccentHoverBrush` | `#C93D64` | `#CC0010` | Hover state |
| `AccentPressedBrush` | `#AC2F54` | `#A8000D` | Pressed state |
| `AccentSoftBrush` | `#FBE4EC` | `#FFE8EA` | Selected and informational surfaces |
| `SurfaceBrush` | `#FCFFFFFF` | `#FCFFFFFF` | Elevated content surface |
| `SurfaceSecondaryBrush` | `#F7EDF2` | `#F4F4F5` | Status cards and update rail |
| `SurfaceBorderBrush` | `#D9C3CD` | `#D1D5DB` | Card and input boundaries |
| `TrackOffBrush` | `#E9DCE2` | `#E5E7EB` | Toggle and progress track |
| `SuccessBrush` | `#23845A` | `#008C45` | Completed and current states |
| `InfoBrush` | `#5C55B8` | `#007AA8` | Informational state |
| `WarningBrush` | `#C56A1A` | `#C65300` | Update available and retry state |
| `ErrorBrush` | `#C5424F` | `#B3261E` | Failure state |

Accent color is reserved for actions, selection, progress, and focus. Status meaning is always paired with text; color alone never carries state.

## 3. Typography

Primary font is Segoe UI, inherited from the window. No external font files are shipped.

| Level | Size | Weight | Usage |
|---|---:|---:|---|
| Window title | 16 | SemiBold | Product name |
| Card title | 15 | SemiBold | Vivaldi and Vivaldi++ |
| Body | 14 | Normal | Settings and status |
| Metadata | 12 | Normal | Versions, sizes, speed, retry detail |
| Action | 15 | SemiBold | Primary button |

Version and progress numbers use tabular alignment through stable grid columns.

## 4. Spacing & Layout

The base unit is 4 device-independent pixels. Primary spacing tokens are 4, 8, 12, 16, 20, and 24. The fixed desktop window keeps a 560-pixel width, uses 20-pixel content padding, and follows this hierarchy:

`Header -> component status cards -> settings tabs -> update rail -> primary action`

Status and settings rows use grids rather than positional margins so Chinese and English labels can resize without drifting.

## 5. Components

### Window surface

- Structure: rounded outer surface, title bar, content grid.
- States: active and inactive window retain readable contrast.
- Accessibility: native text rendering and keyboard-reachable controls.

### Component status card

- Structure: component title, installed/latest version line, text status.
- Variants: Vivaldi and Vivaldi++.
- States: current, update available, not installed, unavailable.
- Accessibility: status text accompanies semantic color.

### Setting row

- Structure: flexible label column and fixed control column.
- States: default, hover, focus, disabled.
- Accessibility: visible focus boundary and minimum 32-pixel control height.

### Update rail

- Structure: primary status, detail text, percentage, progress track.
- States: hidden, connecting, downloading, resuming, retrying, switching source, completed, failed.
- Accessibility: retry and resume are written in plain text; unknown totals use indeterminate progress.

### Primary action

- States: default, hover, pressed, keyboard focus, disabled.
- Accessibility: foreground contrast remains readable in every theme and disabled state.

## 6. Motion & Interaction

Only existing WPF control-state transitions are used. Toggle and hover feedback complete within 200 milliseconds. Progress may animate while total length is unknown; no decorative motion is added. Keyboard focus is persistent and visually distinct.

## 7. Depth & Surface

The depth strategy is mixed but restrained: one tinted outer shadow for the window, one subtle shadow for the main surface, and borders or tonal shifts for inner components. Inner controls do not stack multiple shadows. Pink shadows use a warm plum tint; Nintendo shadows use a neutral charcoal tint.

## 8. Accessibility Constraints & Accepted Debt

- Target: WCAG 2.2 AA contrast where applicable to desktop UI.
- Body text remains at least 12 pixels for metadata and 14 pixels for task text.
- Every icon-only button has an automation name and tooltip.
- Every interactive control has a visible keyboard focus state.
- Download states remain understandable without relying on color.
- Chinese and English labels must fit at the current window width.

Accepted debt: the custom frameless window retains non-native minimize/maximize/close controls. They remain keyboard reachable and receive automation names in this change; replacing the window chrome is outside this task.
