using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;
using Drawing = System.Drawing;

namespace ControllerMouse;

// ─────────────────────────────────────────────────────────────────────────────
// Entry point
// ─────────────────────────────────────────────────────────────────────────────

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new System.Threading.Mutex(true, "tenzerodev.ControllerMouse", out bool firstInstance);
        if (!firstInstance) return;

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        try
        {
            using var app = new ControllerApp();
            Application.Run();
        }
        catch (DllNotFoundException)
        {
            MessageBox.Show("SDL2.dll is missing.\n\nKeep SDL2.dll in the same folder as ControllerMouse.exe.",
                "Controller Mouse", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Actions & buttons
// ─────────────────────────────────────────────────────────────────────────────

enum Act
{
    None, LeftClick, RightClick, MiddleClick, DoubleClick, Precision, Turbo,
    Back, Forward, TaskView, ShowDesktop, StartMenu, Search, AppSwitcher, OnScreenKeyboard,
    Escape, Enter, Space, Tab, Backspace, ArrowUp, ArrowDown, ArrowLeft, ArrowRight,
    Copy, Paste, Undo, VolumeUp, VolumeDown, Mute, PlayPause,
}

static class Acts
{
    public static string Title(Act a) => a switch
    {
        Act.None => "Nothing",
        Act.LeftClick => "Left Click (hold to drag)",
        Act.RightClick => "Right Click",
        Act.MiddleClick => "Middle Click",
        Act.DoubleClick => "Double Click",
        Act.Precision => "Hold: Slow Pointer",
        Act.Turbo => "Hold: Fast Pointer",
        Act.Back => "Back",
        Act.Forward => "Forward",
        Act.TaskView => "Task View (Win+Tab)",
        Act.ShowDesktop => "Show Desktop (Win+D)",
        Act.StartMenu => "Start Menu",
        Act.Search => "Search (Win+S)",
        Act.AppSwitcher => "Previous App (Alt+Tab)",
        Act.OnScreenKeyboard => "On-Screen Keyboard",
        Act.Escape => "Esc",
        Act.Enter => "Enter",
        Act.Space => "Space",
        Act.Tab => "Tab",
        Act.Backspace => "Backspace",
        Act.ArrowUp => "↑ Arrow",
        Act.ArrowDown => "↓ Arrow",
        Act.ArrowLeft => "← Arrow",
        Act.ArrowRight => "→ Arrow",
        Act.Copy => "Copy (Ctrl+C)",
        Act.Paste => "Paste (Ctrl+V)",
        Act.Undo => "Undo (Ctrl+Z)",
        Act.VolumeUp => "Volume Up",
        Act.VolumeDown => "Volume Down",
        Act.Mute => "Mute",
        Act.PlayPause => "Play / Pause Media",
        _ => a.ToString(),
    };

    // Virtual-key chord: modifiers..., key
    public static ushort[]? Keys(Act a) => a switch
    {
        Act.Back => new ushort[] { 0xA6 },                 // VK_BROWSER_BACK
        Act.Forward => new ushort[] { 0xA7 },              // VK_BROWSER_FORWARD
        Act.TaskView => new ushort[] { 0x5B, 0x09 },       // Win+Tab
        Act.ShowDesktop => new ushort[] { 0x5B, 0x44 },    // Win+D
        Act.StartMenu => new ushort[] { 0x5B },            // Win
        Act.Search => new ushort[] { 0x5B, 0x53 },         // Win+S
        Act.AppSwitcher => new ushort[] { 0x12, 0x09 },    // Alt+Tab
        Act.OnScreenKeyboard => new ushort[] { 0x5B, 0x11, 0x4F }, // Win+Ctrl+O
        Act.Escape => new ushort[] { 0x1B },
        Act.Enter => new ushort[] { 0x0D },
        Act.Space => new ushort[] { 0x20 },
        Act.Tab => new ushort[] { 0x09 },
        Act.Backspace => new ushort[] { 0x08 },
        Act.ArrowUp => new ushort[] { 0x26 },
        Act.ArrowDown => new ushort[] { 0x28 },
        Act.ArrowLeft => new ushort[] { 0x25 },
        Act.ArrowRight => new ushort[] { 0x27 },
        Act.Copy => new ushort[] { 0x11, 0x43 },
        Act.Paste => new ushort[] { 0x11, 0x56 },
        Act.Undo => new ushort[] { 0x11, 0x5A },
        Act.VolumeUp => new ushort[] { 0xAF },
        Act.VolumeDown => new ushort[] { 0xAE },
        Act.Mute => new ushort[] { 0xAD },
        Act.PlayPause => new ushort[] { 0xB3 },
        _ => null,
    };

    public static bool Repeats(Act a) =>
        a is Act.ArrowUp or Act.ArrowDown or Act.ArrowLeft or Act.ArrowRight
          or Act.Backspace or Act.VolumeUp or Act.VolumeDown;
}

enum Btn
{
    A, B, X, Y, LB, RB, LT, RT, LS, RS, DpadUp, DpadDown, DpadLeft, DpadRight,
    Start, Back, Guide, Touchpad,
}

static class Btns
{
    public static string Title(Btn b) => b switch
    {
        Btn.A => "A  ·  PS ✕ (bottom)",
        Btn.B => "B  ·  PS ○ (right)",
        Btn.X => "X  ·  PS □ (left)",
        Btn.Y => "Y  ·  PS △ (top)",
        Btn.LB => "L1 / L",
        Btn.RB => "R1 / R",
        Btn.LT => "L2 / ZL",
        Btn.RT => "R2 / ZR",
        Btn.LS => "Left Stick Click",
        Btn.RS => "Right Stick Click",
        Btn.DpadUp => "D-pad Up",
        Btn.DpadDown => "D-pad Down",
        Btn.DpadLeft => "D-pad Left",
        Btn.DpadRight => "D-pad Right",
        Btn.Start => "Options / +",
        Btn.Back => "Share / −",
        Btn.Guide => "PS / Home",
        Btn.Touchpad => "PS4 Touchpad Click",
        _ => b.ToString(),
    };

    public static Act Default(Btn b) => b switch
    {
        Btn.A => Act.LeftClick,
        Btn.B => Act.RightClick,
        Btn.X => Act.Escape,
        Btn.Y => Act.TaskView,
        Btn.LB => Act.Back,
        Btn.RB => Act.Forward,
        Btn.LT => Act.Precision,
        Btn.RT => Act.Turbo,
        Btn.DpadUp => Act.ArrowUp,
        Btn.DpadDown => Act.ArrowDown,
        Btn.DpadLeft => Act.ArrowLeft,
        Btn.DpadRight => Act.ArrowRight,
        Btn.Start => Act.StartMenu,
        Btn.Back => Act.ShowDesktop,
        Btn.Guide => Act.OnScreenKeyboard,
        Btn.Touchpad => Act.LeftClick,
        _ => Act.None,   // stick clicks: both together = on/off
    };

    // SDL_GameControllerButton values
    public static int? SdlButton(Btn b) => b switch
    {
        Btn.A => 0, Btn.B => 1, Btn.X => 2, Btn.Y => 3,
        Btn.Back => 4, Btn.Guide => 5, Btn.Start => 6,
        Btn.LS => 7, Btn.RS => 8, Btn.LB => 9, Btn.RB => 10,
        Btn.DpadUp => 11, Btn.DpadDown => 12, Btn.DpadLeft => 13, Btn.DpadRight => 14,
        Btn.Touchpad => 20,
        _ => null,
    };
}

// ─────────────────────────────────────────────────────────────────────────────
// Settings (%APPDATA%\ControllerMouse\settings.json)
// ─────────────────────────────────────────────────────────────────────────────

sealed class Settings
{
    public double PointerSpeed { get; set; } = 1.0;
    public double ScrollSpeed { get; set; } = 1.0;
    public bool InvertScroll { get; set; }
    public bool SwapSticks { get; set; }
    public Dictionary<string, string> Mapping { get; set; } = new();

    static string Folder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ControllerMouse");
    static string FilePath => Path.Combine(Folder, "settings.json");

    public Act ActionFor(Btn b) =>
        Mapping.TryGetValue(b.ToString(), out var s) && Enum.TryParse<Act>(s, out var a) ? a : Btns.Default(b);

    public static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings(); }
        catch { return new Settings(); }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// The app
// ─────────────────────────────────────────────────────────────────────────────

sealed class ControllerApp : IDisposable
{
    readonly Settings settings = Settings.Load();
    readonly NotifyIcon tray;
    readonly Timer timer;
    readonly Stopwatch clock = Stopwatch.StartNew();
    readonly Dictionary<int, IntPtr> pads = new();          // SDL instance id → controller
    readonly Dictionary<int, (float x, float y)?> touch = new();
    readonly IntPtr eventBuffer = Marshal.AllocHGlobal(128);
    double lastTick, lastScan = -10;

    bool enabled = true;
    HashSet<Btn> held = new();
    readonly Dictionary<Btn, Act> activeAct = new();
    int precision, turbo;
    (Act act, double next)? repeating;
    double remX, remY, wheelY, wheelX;

    static readonly (string name, double value)[] Speeds = { ("Slow", 0.5), ("Medium", 1.0), ("Fast", 1.6), ("Very Fast", 2.4) };

    public ControllerApp()
    {
        SDL.SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
        SDL.SDL_SetHint("SDL_GAMECONTROLLER_USE_BUTTON_LABELS", "0");   // A = bottom button on every pad
        SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_JOY_CONS", "1");
        SDL.SDL_SetHint("SDL_JOYSTICK_HIDAPI_COMBINE_JOY_CONS", "1");
        if (SDL.SDL_Init(SDL.INIT_GAMECONTROLLER | SDL.INIT_JOYSTICK | SDL.INIT_EVENTS) != 0)
            MessageBox.Show("Couldn't start the controller library:\n" + SDL.Error(), "Controller Mouse");

        Win.timeBeginPeriod(1);

        tray = new NotifyIcon
        {
            Icon = TrayIcon.Make(true),
            Text = "Controller Mouse",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip(),
        };
        BuildMenu();
        tray.ContextMenuStrip.Opening += (_, e) => { BuildMenu(); e.Cancel = false; };
        tray.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                typeof(NotifyIcon).GetMethod("ShowContextMenu",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(tray, null);
        };

        timer = new Timer { Interval = 8 };
        timer.Tick += (_, _) => Tick();
        timer.Start();
    }

    public void Dispose()
    {
        timer.Stop();
        ReleaseAll();
        foreach (var p in pads.Values) SDL.SDL_GameControllerClose(p);
        SDL.SDL_Quit();
        Win.timeEndPeriod(1);
        tray.Visible = false;
        tray.Dispose();
        Marshal.FreeHGlobal(eventBuffer);
    }

    // ── Controllers ──────────────────────────────────────────────────────────

    void ScanControllers()
    {
        // Close unplugged
        foreach (var (id, p) in pads.ToList())
            if (SDL.SDL_GameControllerGetAttached(p) == 0)
            {
                SDL.SDL_GameControllerClose(p);
                pads.Remove(id);
                touch.Remove(id);
            }

        // Open new
        int n = SDL.SDL_NumJoysticks();
        for (int i = 0; i < n; i++)
        {
            if (SDL.SDL_IsGameController(i) == 0) continue;
            int id = SDL.SDL_JoystickGetDeviceInstanceID(i);
            if (pads.ContainsKey(id)) continue;
            var p = SDL.SDL_GameControllerOpen(i);
            if (p != IntPtr.Zero) pads[id] = p;
        }
    }

    // ── Main loop (~100×/sec) ────────────────────────────────────────────────

    void Tick()
    {
        double now = clock.Elapsed.TotalSeconds;
        double dt = Math.Clamp(now - lastTick, 0.001, 0.05);
        lastTick = now;

        while (SDL.SDL_PollEvent(eventBuffer) > 0) { }   // let SDL process device changes
        if (now - lastScan > 1.0) { ScanControllers(); lastScan = now; }
        SDL.SDL_GameControllerUpdate();

        // Buttons (any controller)
        var nowHeld = new HashSet<Btn>();
        double mx = 0, my = 0, sx = 0, sy = 0;
        foreach (var (id, p) in pads)
        {
            foreach (Btn b in Enum.GetValues<Btn>())
            {
                if (Btns.SdlButton(b) is int sdl && SDL.SDL_GameControllerGetButton(p, sdl) != 0) nowHeld.Add(b);
            }
            if (Axis(p, 4) > 0.5 || (held.Contains(Btn.LT) && Axis(p, 4) > 0.25)) nowHeld.Add(Btn.LT);
            if (Axis(p, 5) > 0.5 || (held.Contains(Btn.RT) && Axis(p, 5) > 0.25)) nowHeld.Add(Btn.RT);

            var (lx, ly) = Shape(Axis(p, 0), Axis(p, 1));
            var (rx, ry) = Shape(Axis(p, 2), Axis(p, 3));
            if (settings.SwapSticks) { (lx, ly, rx, ry) = (rx, ry, lx, ly); }
            mx += lx; my += ly; sx += rx; sy += ry;

            Touchpad(id, p);
        }

        foreach (var b in nowHeld.Except(held).ToList()) Press(b, true, nowHeld);
        foreach (var b in held.Except(nowHeld).ToList()) Press(b, false, nowHeld);
        held = nowHeld;

        if (!enabled) return;

        // Pointer
        double speed = 1500 * settings.PointerSpeed * Multiplier * dt;
        Move(mx * speed, my * speed);

        // Scroll (120 = one notch)
        double s = 1100 * settings.ScrollSpeed * dt * (settings.InvertScroll ? -1 : 1);
        wheelY += -sy * s; wheelX += sx * s;
        int wy = (int)wheelY, wx = (int)wheelX;
        wheelY -= wy; wheelX -= wx;
        if (wy != 0) Input.Mouse(Input.WHEEL, 0, 0, wy);
        if (wx != 0) Input.Mouse(Input.HWHEEL, 0, 0, wx);

        // Key repeat for held arrows etc.
        if (repeating is { } r && now >= r.next && Acts.Keys(r.act) is { } keys)
        {
            Input.Chord(keys);
            repeating = (r.act, now + 0.05);
        }
    }

    static double Axis(IntPtr p, int axis) => SDL.SDL_GameControllerGetAxis(p, axis) / 32767.0;

    static (double, double) Shape(double x, double y, double dead = 0.12)
    {
        double mag = Math.Min(1, Math.Sqrt(x * x + y * y));
        if (mag <= dead) return (0, 0);
        double scaled = Math.Pow((mag - dead) / (1 - dead), 2);   // gentle near center, fast at the edge
        return (x / mag * scaled, y / mag * scaled);
    }

    double Multiplier => (precision > 0 ? 0.3 : 1) * (turbo > 0 ? 2.5 : 1);

    void Move(double dx, double dy)
    {
        remX += dx; remY += dy;
        int ix = (int)remX, iy = (int)remY;
        remX -= ix; remY -= iy;
        if (ix != 0 || iy != 0) Input.Mouse(Input.MOVE, ix, iy, 0);
    }

    void Touchpad(int id, IntPtr p)
    {
        if (SDL.SDL_GameControllerGetNumTouchpads(p) <= 0) return;
        SDL.SDL_GameControllerGetTouchpadFinger(p, 0, 0, out byte down, out float x, out float y, out _);
        if (down == 0 || !enabled) { touch[id] = null; return; }
        if (touch.GetValueOrDefault(id) is { } last)
        {
            double scale = 1200 * settings.PointerSpeed * Multiplier;
            Move((x - last.x) * scale, (y - last.y) * scale * 0.6);   // pad is wider than tall
        }
        touch[id] = (x, y);
    }

    // ── Buttons ──────────────────────────────────────────────────────────────

    void Press(Btn b, bool down, HashSet<Btn> nowHeld)
    {
        // Click both sticks together → on/off
        if (down && (b == Btn.LS || b == Btn.RS) && nowHeld.Contains(Btn.LS) && nowHeld.Contains(Btn.RS))
        {
            SetEnabled(!enabled);
            System.Media.SystemSounds.Asterisk.Play();
            return;
        }

        Act a;
        if (down)
        {
            if (!enabled) return;
            a = settings.ActionFor(b);
            activeAct[b] = a;
        }
        else
        {
            if (!activeAct.Remove(b, out a)) return;
        }
        Perform(a, down);
    }

    void Perform(Act a, bool down)
    {
        switch (a)
        {
            case Act.None: break;
            case Act.LeftClick: Input.Mouse(down ? Input.LEFTDOWN : Input.LEFTUP, 0, 0, 0); leftHeld = down; break;
            case Act.RightClick: Input.Mouse(down ? Input.RIGHTDOWN : Input.RIGHTUP, 0, 0, 0); rightHeld = down; break;
            case Act.MiddleClick: Input.Mouse(down ? Input.MIDDLEDOWN : Input.MIDDLEUP, 0, 0, 0); break;
            case Act.DoubleClick:
                if (down) for (int i = 0; i < 2; i++) { Input.Mouse(Input.LEFTDOWN, 0, 0, 0); Input.Mouse(Input.LEFTUP, 0, 0, 0); }
                break;
            case Act.Precision: precision += down ? 1 : -1; break;
            case Act.Turbo: turbo += down ? 1 : -1; break;
            default:
                if (Acts.Keys(a) is not { } keys) break;
                if (down)
                {
                    Input.Chord(keys);
                    if (Acts.Repeats(a)) repeating = (a, clock.Elapsed.TotalSeconds + 0.4);
                }
                else if (repeating?.act == a) repeating = null;
                break;
        }
    }

    bool leftHeld, rightHeld;

    void ReleaseAll()
    {
        if (leftHeld) Input.Mouse(Input.LEFTUP, 0, 0, 0);
        if (rightHeld) Input.Mouse(Input.RIGHTUP, 0, 0, 0);
        leftHeld = rightHeld = false;
        repeating = null;
    }

    void SetEnabled(bool on)
    {
        enabled = on;
        if (!on) ReleaseAll();
        var old = tray.Icon;
        tray.Icon = TrayIcon.Make(on);
        old?.Dispose();
        tray.Text = on ? "Controller Mouse" : "Controller Mouse (off)";
    }

    // ── Tray menu ────────────────────────────────────────────────────────────

    void BuildMenu()
    {
        var m = tray.ContextMenuStrip!;
        m.Items.Clear();

        if (pads.Count == 0) Info(m, "No controller connected");
        foreach (var p in pads.Values)
        {
            string name = Marshal.PtrToStringUTF8(SDL.SDL_GameControllerName(p)) ?? "Controller";
            string battery = SDL.SDL_JoystickCurrentPowerLevel(SDL.SDL_GameControllerGetJoystick(p)) switch
            {
                0 => " — battery empty", 1 => " — battery low", 2 => " — battery medium",
                3 or 5 => " — battery full", 4 => " — wired", _ => "",
            };
            Info(m, $"🎮 {name}{battery}");
        }

        m.Items.Add(new ToolStripSeparator());
        Add(m.Items, "Enabled  (click both sticks to toggle)", () => SetEnabled(!enabled)).Checked = enabled;

        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(SpeedMenu("Pointer Speed", settings.PointerSpeed, v => settings.PointerSpeed = v));
        m.Items.Add(SpeedMenu("Scroll Speed", settings.ScrollSpeed, v => settings.ScrollSpeed = v));
        Add(m.Items, "Invert Scrolling", () => { settings.InvertScroll = !settings.InvertScroll; settings.Save(); })
            .Checked = settings.InvertScroll;
        Add(m.Items, "Swap Sticks (right stick moves pointer)", () => { settings.SwapSticks = !settings.SwapSticks; settings.Save(); })
            .Checked = settings.SwapSticks;

        var map = new ToolStripMenuItem("Button Mapping");
        foreach (Btn b in Enum.GetValues<Btn>())
        {
            var current = settings.ActionFor(b);
            var item = new ToolStripMenuItem($"{Btns.Title(b)}  →  {Acts.Title(current)}");
            foreach (Act a in Enum.GetValues<Act>())
            {
                var btn = b; var act = a;
                Add(item.DropDownItems, Acts.Title(a), () => { settings.Mapping[btn.ToString()] = act.ToString(); settings.Save(); })
                    .Checked = a == current;
            }
            map.DropDownItems.Add(item);
        }
        map.DropDownItems.Add(new ToolStripSeparator());
        Add(map.DropDownItems, "Reset to Defaults", () => { settings.Mapping.Clear(); settings.Save(); });
        m.Items.Add(map);

        m.Items.Add(new ToolStripSeparator());
        Add(m.Items, "How to Connect a Controller…", ShowHelp);
        Add(m.Items, "Start with Windows", () => Startup.Enabled = !Startup.Enabled).Checked = Startup.Enabled;
        m.Items.Add(new ToolStripSeparator());
        Add(m.Items, "Quit Controller Mouse", () => Application.Exit());
    }

    ToolStripMenuItem SpeedMenu(string title, double current, Action<double> set)
    {
        var item = new ToolStripMenuItem(title);
        foreach (var (name, value) in Speeds)
        {
            var v = value;
            Add(item.DropDownItems, name, () => { set(v); settings.Save(); }).Checked = Math.Abs(value - current) < 0.01;
        }
        return item;
    }

    static ToolStripMenuItem Add(ToolStripItemCollection items, string text, Action onClick)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (_, _) => onClick();
        items.Add(item);
        return item;
    }

    static void Info(ContextMenuStrip m, string text) => m.Items.Add(new ToolStripMenuItem(text) { Enabled = false });

    static void ShowHelp() => MessageBox.Show(
        "PS4 (DualShock 4)\n" +
        " • Wired: plug in with a USB cable that carries data (some cheap cables only charge).\n" +
        " • Bluetooth: hold SHARE + PS for ~3 sec until the light bar double-flashes, then go to\n" +
        "   Settings → Bluetooth & devices → Add device → Bluetooth → \"Wireless Controller\".\n\n" +
        "Nintendo Switch Pro Controller\n" +
        " • Wired: USB-C cable works on Windows.\n" +
        " • Bluetooth: hold the small round SYNC button on top until the lights sweep, then add it\n" +
        "   in Bluetooth settings as \"Pro Controller\". Joy-Con: hold the sync button on the rail.\n\n" +
        "If it doesn't respond\n" +
        " • Turn off the PS4 / Switch console nearby — it grabs the controller back.\n" +
        " • Close Steam, or turn off Steam Input, which can take over the controller.\n" +
        " • Remove the controller in Bluetooth settings and pair again.\n" +
        " • Controller Mouse can't control apps running as Administrator unless it's also run as Administrator.",
        "Connecting your controller", MessageBoxButtons.OK, MessageBoxIcon.Information);
}

// ─────────────────────────────────────────────────────────────────────────────
// Start with Windows
// ─────────────────────────────────────────────────────────────────────────────

static class Startup
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string Name = "ControllerMouse";

    public static bool Enabled
    {
        get { using var k = Registry.CurrentUser.OpenSubKey(RunKey); return k?.GetValue(Name) is string; }
        set
        {
            using var k = Registry.CurrentUser.CreateSubKey(RunKey);
            if (value) k.SetValue(Name, $"\"{Environment.ProcessPath}\""); else k.DeleteValue(Name, false);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Tray icon drawn from the Windows symbol font
// ─────────────────────────────────────────────────────────────────────────────

static class TrayIcon
{
    public static Drawing.Icon Make(bool on)
    {
        using var bmp = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bmp))
        {
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            var color = on ? Drawing.Color.FromArgb(255, 22, 163, 74) : Drawing.Color.FromArgb(255, 120, 120, 120);
            using var bg = new Drawing.SolidBrush(color);
            g.FillEllipse(bg, 0, 0, 31, 31);
            using var font = new Drawing.Font("Segoe MDL2 Assets", 17, Drawing.GraphicsUnit.Pixel);
            const string glyph = "";   // Game controller
            var size = g.MeasureString(glyph, font);
            g.DrawString(glyph, font, Drawing.Brushes.White, (32 - size.Width) / 2 + 1, (32 - size.Height) / 2 + 1);
        }
        return Drawing.Icon.FromHandle(bmp.GetHicon());
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// SendInput
// ─────────────────────────────────────────────────────────────────────────────

static class Input
{
    public const uint MOVE = 0x1, LEFTDOWN = 0x2, LEFTUP = 0x4, RIGHTDOWN = 0x8, RIGHTUP = 0x10,
                      MIDDLEDOWN = 0x20, MIDDLEUP = 0x40, WHEEL = 0x800, HWHEEL = 0x1000;
    const uint KEYUP = 0x2, EXTENDED = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT { public int dx, dy; public int mouseData; public uint dwFlags, time; public IntPtr extra; }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr extra; }

    [StructLayout(LayoutKind.Explicit)]
    struct UNION { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public UNION u; }

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint count, INPUT[] inputs, int size);

    public static void Mouse(uint flags, int dx, int dy, int data)
    {
        var i = new INPUT { type = 0 };
        i.u.mi = new MOUSEINPUT { dx = dx, dy = dy, mouseData = data, dwFlags = flags };
        SendInput(1, new[] { i }, Marshal.SizeOf<INPUT>());
    }

    static bool IsExtended(ushort vk) =>
        vk is 0x25 or 0x26 or 0x27 or 0x28 or 0x2D or 0x2E or 0x21 or 0x22 or 0x23 or 0x24 or 0x5B;

    static INPUT Key(ushort vk, bool up)
    {
        var i = new INPUT { type = 1 };
        i.u.ki = new KEYBDINPUT { wVk = vk, dwFlags = (up ? KEYUP : 0) | (IsExtended(vk) ? EXTENDED : 0) };
        return i;
    }

    /// Press keys in order, release in reverse (e.g. Ctrl+C).
    public static void Chord(ushort[] keys)
    {
        var list = new List<INPUT>();
        foreach (var k in keys) list.Add(Key(k, false));
        for (int j = keys.Length - 1; j >= 0; j--) list.Add(Key(keys[j], true));
        SendInput((uint)list.Count, list.ToArray(), Marshal.SizeOf<INPUT>());
    }
}

static class Win
{
    [DllImport("winmm.dll")] public static extern uint timeBeginPeriod(uint ms);
    [DllImport("winmm.dll")] public static extern uint timeEndPeriod(uint ms);
}

// ─────────────────────────────────────────────────────────────────────────────
// SDL2 (only the functions we use)
// ─────────────────────────────────────────────────────────────────────────────

static class SDL
{
    const string Lib = "SDL2";
    public const uint INIT_JOYSTICK = 0x200, INIT_GAMECONTROLLER = 0x2000, INIT_EVENTS = 0x4000;

    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_Init(uint flags);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern void SDL_Quit();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_SetHint([MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] static extern IntPtr SDL_GetError();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_PollEvent(IntPtr ev);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_NumJoysticks();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_IsGameController(int index);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_JoystickGetDeviceInstanceID(int index);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr SDL_GameControllerOpen(int index);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern void SDL_GameControllerClose(IntPtr gc);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_GameControllerGetAttached(IntPtr gc);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern void SDL_GameControllerUpdate();
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern short SDL_GameControllerGetAxis(IntPtr gc, int axis);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern byte SDL_GameControllerGetButton(IntPtr gc, int button);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr SDL_GameControllerName(IntPtr gc);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern IntPtr SDL_GameControllerGetJoystick(IntPtr gc);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_JoystickCurrentPowerLevel(IntPtr joystick);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)] public static extern int SDL_GameControllerGetNumTouchpads(IntPtr gc);
    [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_GameControllerGetTouchpadFinger(IntPtr gc, int touchpad, int finger,
        out byte state, out float x, out float y, out float pressure);

    public static string Error() => Marshal.PtrToStringUTF8(SDL_GetError()) ?? "unknown error";
}
