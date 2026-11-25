# Vivaldi Updater Design System

## 1. Atmosphere & Identity

The application is a compact desktop control panel: calm at rest, explicit while updating, and reassuring after a network interruption. Its signature is a compact update rail that keeps progress, resume, retry, and source-switch information visible without modal interruption. Pink remains the warm default; Nintendo keeps its red accent.

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

The base unit is 4 device-independent pixels. Primary spacing tokens are 4, 8, 12, 16, 20, and 24. The desktop window uses a 560 DIP width and 20 DIP content insets. The primary work area caps its height. The settings pane scrolls independently while status and the primary action remain in place:

`Header -> component status cards -> settings tabs -> update rail -> primary action`

Status and settings rows use grids rather than positional margins so Chinese and English labels can resize without drifting.

## 5. Components

### Window surface

- Structure: a single rounded surface, a title bar with monochrome actions, and a shared content grid.
- States: active and inactive window retain readable contrast.
- Accessibility: native text rendering and keyboard-reachable controls.

### Component status card

- Structure: aligned component title, installed version, available version, and status columns.
- Variants: Vivaldi and Vivaldi++.
- States: current, update available, not installed, unavailable.
- Current and not-installed states use neutral text. Update availability uses an accent tint, and errors use semantic text on the surface.
- Checking/unknown state does not render an empty badge; the status area appears only after a concrete result exists.
- Accessibility: status text accompanies semantic color.

### Setting row

- Structure: wrapping label with a trailing control. Thin dividers separate rows, and settings tabs share the same insets.
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

Toggle and hover feedback is immediate. Indeterminate progress animates while the total is unknown. Keyboard focus is visually distinct.

## 7. Depth & Surface

One subtle outer shadow separates the window from the desktop. Tonal surfaces separate component status and update progress. Controls use borders and state colors to define their edges.

## 8. Accessibility Constraints & Accepted Debt

- Target: WCAG 2.2 AA contrast where applicable to desktop UI.
- Body text remains at least 12 pixels for metadata and 14 pixels for task text.
- Every icon-only button has an automation name and tooltip.
- Every interactive control has a visible keyboard focus state.
- Download states remain understandable without relying on color.
- Chinese and English labels must fit at the current window width.

The custom title bar retains keyboard-reachable minimize and close buttons with automation names and tooltips.
