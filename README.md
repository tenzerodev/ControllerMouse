# Controller Mouse for macOS

Use a PS4 (DualShock 4), PS5 (DualSense) or Nintendo Switch controller as a mouse / trackpad.
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

Buttons go by position, like on a PlayStation pad: on a Switch controller, "A / ✕" is the bottom button (labelled B).
Every button can be remapped: menu bar icon → Button Mapping.
Pointer and scroll speed, invert scroll, and swap sticks are in the menu too.

## Download (Mac)
1. Get **ControllerMouse-macOS.zip** from the [latest release](https://github.com/tenzerodev/ControllerMouse/releases/latest).
2. Unzip it and drag **ControllerMouse.app** into Applications.
3. The first time, double-click it. macOS will say it can't check the app (it isn't notarized by Apple):
   click **Done**, then go to **System Settings → Privacy & Security**, scroll down and click **Open Anyway**.
   On macOS 14 or earlier, **right-click → Open → Open** also works.
   If macOS says it's damaged or won't open, run: `xattr -cr /Applications/ControllerMouse.app`
4. Allow **Accessibility** when asked (System Settings → Privacy & Security → Accessibility), so it can move the pointer.

**Updating:** macOS treats each new version as a different app, so the pointer stops moving after an update even though the switch looks on.
Remove the old Controller Mouse entry from Accessibility (−) and allow it again.

Works on Apple Silicon and Intel Macs, macOS 13 Ventura or later.

## Windows
1. Get **ControllerMouse-Windows.zip** from the [latest release](https://github.com/tenzerodev/ControllerMouse/releases/latest) and unzip it.
   Keep **ControllerMouse.exe** and **SDL2.dll** in the same folder.
2. Run **ControllerMouse.exe**. If SmartScreen says "Windows protected your PC", click **More info → Run anyway** (the app isn't code-signed).
3. Connect a controller — PS4 and Switch Pro both work over **USB or Bluetooth** on Windows.

Same controls as the Mac version, with Windows actions: Y/△ = Task View, Options/+ = Start menu,
Share/− = Show Desktop, PS/Home = On-Screen Keyboard, touchpad click = left click.
Remap anything from the tray icon → Button Mapping.

- If nothing happens, close Steam (or turn off Steam Input) — it can take over the controller.
- It can't control apps running as Administrator unless Controller Mouse is also run as Administrator.
- Uses [SDL2](https://libsdl.org) (zlib license) for controller support. Source is in [`windows/`](windows) (C# / .NET 8).

## Build from source (Mac)
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
