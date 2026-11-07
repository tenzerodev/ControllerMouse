# Controller Mouse for macOS

Use a PS4 (DualShock 4) or Nintendo Switch controller as a mouse / trackpad.
Lives in the menu bar. Works while any app is in front.

## Default controls
| Control | Does |
|---|---|
| Left stick | Move pointer |
| Right stick | Scroll (up/down/left/right) |
| PS4 touchpad | Trackpad (swipe to move, press to click) |
| A / ✕ | Left click (hold to drag) |
| B / ○ | Right click |
| X / □ | Escape |
| Y / △ | Mission Control |
| L1 / R1 | Back / Forward |
| L2 / ZL (hold) | Slow, precise pointer |
| R2 / ZR (hold) | Fast pointer |
| D-pad | Arrow keys |
| Options / + | Spotlight |
| Share / − | Show Desktop |
| Click both sticks | Turn Controller Mouse on/off |

Every button can be remapped: menu bar icon → Button Mapping.
Pointer and scroll speed, invert scroll, and swap sticks are in the menu too.

## Download
1. Get **ControllerMouse-macOS.zip** from the [latest release](https://github.com/tenzerodev/ControllerMouse/releases/latest).
2. Unzip it and drag **ControllerMouse.app** into Applications.
3. The first time, **right-click → Open → Open** (the app isn't notarized by Apple).
   If macOS says it's damaged or won't open, run: `xattr -cr /Applications/ControllerMouse.app`

Works on Apple Silicon and Intel Macs, macOS 13 Ventura or later.

## Build from source
```bash
git clone https://github.com/tenzerodev/ControllerMouse.git
cd ControllerMouse && bash build.sh
```
Move the app to Applications and open it. macOS will ask for **Accessibility**
access (System Settings → Privacy & Security → Accessibility) — turn it on,
otherwise it can't move the pointer.

After rebuilding, macOS treats it as a new app: remove the old
Controller Mouse entry from Accessibility (−) and allow it again.

## Connecting
- PS4: USB data cable, or Bluetooth (hold SHARE + PS until the light double-flashes).
- Switch Pro: Bluetooth (hold the SYNC button on top). Macs usually don't detect Switch controllers over USB.
- Switch off the console nearby while pairing — it steals the controller back.

## License
GPL-3.0 — see [LICENSE](LICENSE).
