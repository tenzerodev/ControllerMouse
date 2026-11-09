import AppKit
import GameController
import ApplicationServices
import ServiceManagement

// MARK: - What a button can do

enum Action: String, CaseIterable {
    case none
    case leftClick, rightClick, middleClick, doubleClick
    case precision, turbo
    case back, forward
    case missionControl, showDesktop, spotlight, appSwitcher
    case escape, returnKey, space, tab, delete
    case arrowUp, arrowDown, arrowLeft, arrowRight
    case copy, paste, undo

    var title: String {
        switch self {
        case .none: return "Nothing"
        case .leftClick: return "Left Click (hold to drag)"
        case .rightClick: return "Right Click"
        case .middleClick: return "Middle Click"
        case .doubleClick: return "Double Click"
        case .precision: return "Hold: Slow Pointer"
        case .turbo: return "Hold: Fast Pointer"
        case .back: return "Back (⌘[)"
        case .forward: return "Forward (⌘])"
        case .missionControl: return "Mission Control"
        case .showDesktop: return "Show Desktop"
        case .spotlight: return "Spotlight (⌘Space)"
        case .appSwitcher: return "Previous App (⌘Tab)"
        case .escape: return "Escape"
        case .returnKey: return "Return"
        case .space: return "Space"
        case .tab: return "Tab"
        case .delete: return "Delete"
        case .arrowUp: return "↑ Arrow"
        case .arrowDown: return "↓ Arrow"
        case .arrowLeft: return "← Arrow"
        case .arrowRight: return "→ Arrow"
        case .copy: return "Copy (⌘C)"
        case .paste: return "Paste (⌘V)"
        case .undo: return "Undo (⌘Z)"
        }
    }

    /// Key code + modifiers for keyboard actions.
    var key: (CGKeyCode, CGEventFlags)? {
        switch self {
        case .back: return (33, .maskCommand)
        case .forward: return (30, .maskCommand)
        case .showDesktop: return (103, [])          // F11
        case .spotlight: return (49, .maskCommand)
        case .appSwitcher: return (48, .maskCommand)
        case .escape: return (53, [])
        case .returnKey: return (36, [])
        case .space: return (49, [])
        case .tab: return (48, [])
        case .delete: return (51, [])
        case .arrowUp: return (126, [])
        case .arrowDown: return (125, [])
        case .arrowLeft: return (123, [])
        case .arrowRight: return (124, [])
        case .copy: return (8, .maskCommand)
        case .paste: return (9, .maskCommand)
        case .undo: return (6, .maskCommand)
        default: return nil
        }
    }

    /// Keys that repeat while the button is held.
    var repeats: Bool {
        [.arrowUp, .arrowDown, .arrowLeft, .arrowRight, .delete].contains(self)
    }
}

// MARK: - Controller buttons

enum Button: String, CaseIterable {
    case a, b, x, y, l1, r1, l2, r2, l3, r3
    case dpadUp, dpadDown, dpadLeft, dpadRight
    case menu, options

    var title: String {
        switch self {
        case .a: return "A  ·  PS ✕"
        case .b: return "B  ·  PS ○"
        case .x: return "X  ·  PS □"
        case .y: return "Y  ·  PS △"
        case .l1: return "L1 / L"
        case .r1: return "R1 / R"
        case .l2: return "L2 / ZL"
        case .r2: return "R2 / ZR"
        case .l3: return "Left Stick Click"
        case .r3: return "Right Stick Click"
        case .dpadUp: return "D-pad Up"
        case .dpadDown: return "D-pad Down"
        case .dpadLeft: return "D-pad Left"
        case .dpadRight: return "D-pad Right"
        case .menu: return "Options / +"
        case .options: return "Share / −"
        }
    }

    var defaultAction: Action {
        switch self {
        case .a: return .leftClick
        case .b: return .rightClick
        case .x: return .escape
        case .y: return .missionControl
        case .l1: return .back
        case .r1: return .forward
        case .l2: return .precision
        case .r2: return .turbo
        case .l3, .r3: return .none          // both together = on/off switch
        case .dpadUp: return .arrowUp
        case .dpadDown: return .arrowDown
        case .dpadLeft: return .arrowLeft
        case .dpadRight: return .arrowRight
        case .menu: return .spotlight
        case .options: return .showDesktop
        }
    }

    func input(in g: GCExtendedGamepad) -> GCControllerButtonInput? {
        switch self {
        case .a: return g.buttonA
        case .b: return g.buttonB
        case .x: return g.buttonX
        case .y: return g.buttonY
        case .l1: return g.leftShoulder
        case .r1: return g.rightShoulder
        case .l2: return g.leftTrigger
        case .r2: return g.rightTrigger
        case .l3: return g.leftThumbstickButton
        case .r3: return g.rightThumbstickButton
        case .dpadUp: return g.dpad.up
        case .dpadDown: return g.dpad.down
        case .dpadLeft: return g.dpad.left
        case .dpadRight: return g.dpad.right
        case .menu: return g.buttonMenu
        case .options: return g.buttonOptions
        }
    }

    /// A single sideways Joy-Con only has a few buttons.
    func input(in m: GCMicroGamepad) -> GCControllerButtonInput? {
        switch self {
        case .a: return m.buttonA
        case .x: return m.buttonX
        case .menu: return m.buttonMenu
        default: return nil
        }
    }
}

// MARK: - Posting mouse & keyboard events

final class Output {
    private let source = CGEventSource(stateID: .hidSystemState)
    private(set) var leftHeld = false
    private(set) var rightHeld = false
    private var lastLeftDown = Date.distantPast
    private var lastLeftPoint = CGPoint.zero
    private var clickCount = 1
    private var remainder = CGPoint.zero

    var location: CGPoint { CGEvent(source: nil)?.location ?? .zero }

    func move(dx: CGFloat, dy: CGFloat) {
        // Keep sub-pixel motion so slow movements still add up.
        let tx = dx + remainder.x, ty = dy + remainder.y
        let ix = tx.rounded(.towardZero), iy = ty.rounded(.towardZero)
        remainder = CGPoint(x: tx - ix, y: ty - iy)
        guard ix != 0 || iy != 0 else { return }

        let old = location
        let target = clamp(CGPoint(x: old.x + ix, y: old.y + iy), from: old)
        let type: CGEventType = leftHeld ? .leftMouseDragged : rightHeld ? .rightMouseDragged : .mouseMoved
        let button: CGMouseButton = rightHeld && !leftHeld ? .right : .left
        let e = CGEvent(mouseEventSource: source, mouseType: type, mouseCursorPosition: target, mouseButton: button)
        e?.setIntegerValueField(.mouseEventDeltaX, value: Int64(ix))
        e?.setIntegerValueField(.mouseEventDeltaY, value: Int64(iy))
        e?.post(tap: .cghidEventTap)
    }

    func scroll(dx: CGFloat, dy: CGFloat) {
        let y = Int32(dy.rounded()), x = Int32(dx.rounded())
        guard x != 0 || y != 0 else { return }
        CGEvent(scrollWheelEvent2Source: source, units: .pixel, wheelCount: 2,
                wheel1: y, wheel2: x, wheel3: 0)?.post(tap: .cghidEventTap)
    }

    func left(_ down: Bool) {
        let p = location
        if down {
            let quick = Date().timeIntervalSince(lastLeftDown) < NSEvent.doubleClickInterval
            let near = hypot(p.x - lastLeftPoint.x, p.y - lastLeftPoint.y) < 6
            clickCount = (quick && near) ? min(clickCount + 1, 3) : 1
            lastLeftDown = Date(); lastLeftPoint = p
        }
        leftHeld = down
        post(down ? .leftMouseDown : .leftMouseUp, .left, at: p, clicks: clickCount)
    }

    func right(_ down: Bool) {
        rightHeld = down
        post(down ? .rightMouseDown : .rightMouseUp, .right, at: location, clicks: 1)
    }

    func middle(_ down: Bool) {
        post(down ? .otherMouseDown : .otherMouseUp, .center, at: location, clicks: 1)
    }

    func doubleClick() {
        let p = location
        for n in 1...2 {
            post(.leftMouseDown, .left, at: p, clicks: n)
            post(.leftMouseUp, .left, at: p, clicks: n)
        }
    }

    func releaseAll() {
        if leftHeld { left(false) }
        if rightHeld { right(false) }
    }

    func tap(_ code: CGKeyCode, _ flags: CGEventFlags) {
        for down in [true, false] {
            let e = CGEvent(keyboardEventSource: source, virtualKey: code, keyDown: down)
            e?.flags = flags
            e?.post(tap: .cghidEventTap)
        }
    }

    private func post(_ type: CGEventType, _ button: CGMouseButton, at p: CGPoint, clicks: Int) {
        let e = CGEvent(mouseEventSource: source, mouseType: type, mouseCursorPosition: p, mouseButton: button)
        e?.setIntegerValueField(.mouseEventClickState, value: Int64(clicks))
        if button == .center { e?.setIntegerValueField(.mouseEventButtonNumber, value: 2) }
        e?.post(tap: .cghidEventTap)
    }

    /// Keep the pointer on a real screen (handles multiple monitors).
    private func clamp(_ p: CGPoint, from old: CGPoint) -> CGPoint {
        var count: UInt32 = 0
        CGGetActiveDisplayList(0, nil, &count)
        var ids = [CGDirectDisplayID](repeating: 0, count: Int(count))
        CGGetActiveDisplayList(count, &ids, &count)
        let rects = ids.map { CGDisplayBounds($0) }
        if rects.contains(where: { $0.contains(p) }) { return p }
        let home = rects.first(where: { $0.contains(old) }) ?? rects.first ?? .zero
        return CGPoint(x: min(max(p.x, home.minX), home.maxX - 1),
                       y: min(max(p.y, home.minY), home.maxY - 1))
    }
}

// MARK: - App

final class AppDelegate: NSObject, NSApplicationDelegate, NSMenuDelegate {
    private var statusItem: NSStatusItem!
    private let out = Output()
    private var timer: Timer?

    private var enabled = true { didSet { if !enabled { releaseEverything() }; updateIcon(); updateTimer() } }
    private var pressed = Set<Button>()
    private var activeAction: [Button: Action] = [:]   // action captured at press time
    private var precisionHeld = 0, turboHeld = 0
    private var repeating: (action: Action, next: Date)?
    private var touchLast: [ObjectIdentifier: CGPoint] = [:]

    // MARK: Settings

    private let defaults = UserDefaults.standard
    private let speeds: [(String, Double)] = [("Slow", 0.5), ("Medium", 1.0), ("Fast", 1.6), ("Very Fast", 2.4)]

    private var pointerSpeed: Double {
        get { defaults.object(forKey: "pointerSpeed") as? Double ?? 1.0 }
        set { defaults.set(newValue, forKey: "pointerSpeed") }
    }
    private var scrollSpeed: Double {
        get { defaults.object(forKey: "scrollSpeed") as? Double ?? 1.0 }
        set { defaults.set(newValue, forKey: "scrollSpeed") }
    }
    private var invertScroll: Bool {
        get { defaults.bool(forKey: "invertScroll") }
        set { defaults.set(newValue, forKey: "invertScroll") }
    }
    private var swapSticks: Bool {
        get { defaults.bool(forKey: "swapSticks") }
        set { defaults.set(newValue, forKey: "swapSticks") }
    }

    private func action(for b: Button) -> Action {
        (defaults.dictionary(forKey: "mapping") as? [String: String])?[b.rawValue]
            .flatMap(Action.init(rawValue:)) ?? b.defaultAction
    }

    private func setAction(_ a: Action, for b: Button) {
        var map = defaults.dictionary(forKey: "mapping") as? [String: String] ?? [:]
        map[b.rawValue] = a.rawValue
        defaults.set(map, forKey: "mapping")
    }

    // MARK: Launch

    func applicationDidFinishLaunching(_ notification: Notification) {
        GCController.shouldMonitorBackgroundEvents = true   // keep reading the controller while other apps are in front

        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        let menu = NSMenu()
        menu.delegate = self
        statusItem.menu = menu
        updateIcon()

        // Ask for Accessibility (needed to move the pointer / click / type).
        let opts = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        _ = AXIsProcessTrustedWithOptions(opts)

        let nc = NotificationCenter.default
        nc.addObserver(self, selector: #selector(controllerConnected(_:)), name: .GCControllerDidConnect, object: nil)
        nc.addObserver(self, selector: #selector(controllerDisconnected(_:)), name: .GCControllerDidDisconnect, object: nil)
        GCController.controllers().forEach(configure)
        GCController.startWirelessControllerDiscovery {}
        updateTimer()
    }

    /// Polls the sticks only while a controller is connected and the app is on, so it uses no battery otherwise.
    private func updateTimer(ignoring gone: GCController? = nil) {
        let connected = GCController.controllers().contains { $0 !== gone }
        if enabled && connected {
            guard timer == nil else { return }
            let t = Timer(timeInterval: 1.0 / 120.0, repeats: true) { [weak self] _ in self?.tick() }
            RunLoop.main.add(t, forMode: .common)
            timer = t
        } else {
            timer?.invalidate()
            timer = nil
        }
    }

    /// Lets go of every click, key, repeat and speed modifier. A disconnected controller never sends "released".
    private func releaseEverything() {
        out.releaseAll()
        pressed.removeAll()
        activeAction.removeAll()
        repeating = nil
        precisionHeld = 0
        turboHeld = 0
    }

    private func updateIcon() {
        let name = enabled ? "gamecontroller.fill" : "gamecontroller"
        statusItem?.button?.image = NSImage(systemSymbolName: name, accessibilityDescription: "Controller Mouse")
        statusItem?.button?.appearsDisabled = !enabled
    }

    // MARK: Controllers

    @objc private func controllerConnected(_ n: Notification) {
        if let c = n.object as? GCController { configure(c) }
        updateTimer()
    }

    @objc private func controllerDisconnected(_ n: Notification) {
        let c = n.object as? GCController
        if let c { touchLast[ObjectIdentifier(c)] = nil }
        releaseEverything()
        updateTimer(ignoring: c)
    }

    private func configure(_ c: GCController) {
        c.handlerQueue = .main
        if let g = c.extendedGamepad {
            for b in Button.allCases {
                b.input(in: g)?.pressedChangedHandler = { [weak self] _, _, isDown in self?.handle(b, isDown) }
            }
            // DualShock 4 / DualSense touchpad works like a laptop trackpad.
            let id = ObjectIdentifier(c)
            let pad: GCControllerDirectionPad?
            let click: GCControllerButtonInput?
            if let ds = g as? GCDualShockGamepad { pad = ds.touchpadPrimary; click = ds.touchpadButton }
            else if let ds = g as? GCDualSenseGamepad { pad = ds.touchpadPrimary; click = ds.touchpadButton }
            else { pad = nil; click = nil }
            pad?.valueChangedHandler = { [weak self] _, x, y in self?.touchpad(id, x: x, y: y) }
            click?.pressedChangedHandler = { [weak self] _, _, isDown in
                guard let self, self.enabled else { return }
                self.out.left(isDown)
            }
        } else if let m = c.microGamepad {
            m.allowsRotation = true
            for b in [Button.a, .x, .menu] {
                b.input(in: m)?.pressedChangedHandler = { [weak self] _, _, isDown in self?.handle(b, isDown) }
            }
        }
    }

    private func touchpad(_ id: ObjectIdentifier, x: Float, y: Float) {
        guard enabled else { return }
        let p = CGPoint(x: CGFloat(x), y: CGFloat(y))
        if x == 0 && y == 0 { touchLast[id] = nil; return }      // finger lifted
        if let last = touchLast[id] {
            let scale = 700 * pointerSpeed * modifierMultiplier
            out.move(dx: (p.x - last.x) * scale, dy: -(p.y - last.y) * scale)
        }
        touchLast[id] = p
    }

    // MARK: Buttons

    private func handle(_ b: Button, _ isDown: Bool) {
        if isDown { pressed.insert(b) } else { pressed.remove(b) }

        // Click both sticks together to turn the app on/off.
        if isDown && (b == .l3 || b == .r3) && pressed.isSuperset(of: [.l3, .r3]) {
            enabled.toggle()
            NSSound(named: enabled ? "Pop" : "Bottle")?.play()
            return
        }
        guard enabled || !isDown else { return }

        let a: Action
        if isDown {
            a = action(for: b)
            activeAction[b] = a
        } else {
            guard let held = activeAction.removeValue(forKey: b) else { return }
            a = held
        }
        perform(a, isDown)
    }

    private func perform(_ a: Action, _ isDown: Bool) {
        switch a {
        case .none: break
        case .leftClick: out.left(isDown)
        case .rightClick: out.right(isDown)
        case .middleClick: out.middle(isDown)
        case .doubleClick: if isDown { out.doubleClick() }
        case .precision: precisionHeld += isDown ? 1 : -1
        case .turbo: turboHeld += isDown ? 1 : -1
        case .missionControl:
            if isDown {
                NSWorkspace.shared.openApplication(at: URL(fileURLWithPath: "/System/Applications/Mission Control.app"),
                                                   configuration: .init())
            }
        default:
            guard let (code, flags) = a.key else { return }
            if isDown {
                out.tap(code, flags)
                if a.repeats { repeating = (a, Date().addingTimeInterval(0.4)) }
            } else if repeating?.action == a {
                repeating = nil
            }
        }
    }

    private var modifierMultiplier: Double {
        var m = 1.0
        if precisionHeld > 0 { m *= 0.3 }
        if turboHeld > 0 { m *= 2.5 }
        return m
    }

    // MARK: Sticks (polled 120×/sec for smooth motion)

    private func shaped(_ x: Float, _ y: Float, deadzone: Double = 0.12) -> (Double, Double) {
        let mag = min(1, hypot(Double(x), Double(y)))
        guard mag > deadzone else { return (0, 0) }
        let scaled = pow((mag - deadzone) / (1 - deadzone), 2.0)   // gentle near center, fast at the edge
        return (Double(x) / mag * scaled, Double(y) / mag * scaled)
    }

    private func tick() {
        guard enabled else { return }
        let dt = 1.0 / 120.0
        var move = (0.0, 0.0), wheel = (0.0, 0.0)

        for c in GCController.controllers() {
            if let g = c.extendedGamepad {
                let ptr = swapSticks ? g.rightThumbstick : g.leftThumbstick
                let scr = swapSticks ? g.leftThumbstick : g.rightThumbstick
                let p = shaped(ptr.xAxis.value, ptr.yAxis.value)
                let s = shaped(scr.xAxis.value, scr.yAxis.value)
                move.0 += p.0; move.1 += p.1
                wheel.0 += s.0; wheel.1 += s.1
            } else if let m = c.microGamepad {
                let p = shaped(m.dpad.xAxis.value, m.dpad.yAxis.value)
                move.0 += p.0; move.1 += p.1
            }
        }

        let speed = 1500.0 * pointerSpeed * modifierMultiplier * dt
        out.move(dx: CGFloat(move.0 * speed), dy: CGFloat(-move.1 * speed))

        let sSpeed = 1800.0 * scrollSpeed * dt * (invertScroll ? -1 : 1)
        out.scroll(dx: CGFloat(-wheel.0 * sSpeed), dy: CGFloat(wheel.1 * sSpeed))

        if let r = repeating, Date() >= r.next, let (code, flags) = r.action.key {
            out.tap(code, flags)
            repeating = (r.action, Date().addingTimeInterval(0.05))
        }
    }

    // MARK: Menu (rebuilt every time it opens)

    func menuNeedsUpdate(_ menu: NSMenu) {
        menu.removeAllItems()

        let controllers = GCController.controllers()
        if controllers.isEmpty {
            info(menu, "No controller connected")
        } else {
            for c in controllers {
                let name = c.vendorName ?? "Controller"
                let battery = c.battery.map { " — \(Int($0.batteryLevel * 100))%" } ?? ""
                info(menu, "🎮 \(name)\(battery)")
            }
        }

        if !AXIsProcessTrusted() {
            menu.addItem(.separator())
            add(menu, "⚠️ Allow Accessibility Access…", #selector(openAccessibility))
        }

        menu.addItem(.separator())
        let on = add(menu, "Enabled  (click both sticks to toggle)", #selector(toggleEnabled))
        on.state = enabled ? .on : .off

        menu.addItem(.separator())
        menu.addItem(speedMenu("Pointer Speed", current: pointerSpeed, #selector(setPointerSpeed(_:))))
        menu.addItem(speedMenu("Scroll Speed", current: scrollSpeed, #selector(setScrollSpeed(_:))))
        add(menu, "Invert Scrolling", #selector(toggleInvert)).state = invertScroll ? .on : .off
        add(menu, "Swap Sticks (right stick moves pointer)", #selector(toggleSwap)).state = swapSticks ? .on : .off

        let mapItem = NSMenuItem(title: "Button Mapping", action: nil, keyEquivalent: "")
        let mapMenu = NSMenu()
        for b in Button.allCases {
            let current = action(for: b)
            let item = NSMenuItem(title: "\(b.title)  →  \(current.title)", action: nil, keyEquivalent: "")
            let sub = NSMenu()
            for a in Action.allCases {
                let ai = add(sub, a.title, #selector(chooseAction(_:)))
                ai.representedObject = [b.rawValue, a.rawValue]
                ai.state = a == current ? .on : .off
            }
            item.submenu = sub
            mapMenu.addItem(item)
        }
        mapMenu.addItem(.separator())
        add(mapMenu, "Reset to Defaults", #selector(resetMapping))
        mapItem.submenu = mapMenu
        menu.addItem(mapItem)

        menu.addItem(.separator())
        add(menu, "How to Connect a Controller…", #selector(showPairingHelp))
        add(menu, "Launch at Login", #selector(toggleLogin)).state =
            SMAppService.mainApp.status == .enabled ? .on : .off
        menu.addItem(.separator())
        add(menu, "Quit Controller Mouse", #selector(quit), key: "q")
    }

    @discardableResult
    private func add(_ menu: NSMenu, _ title: String, _ action: Selector, key: String = "") -> NSMenuItem {
        let item = NSMenuItem(title: title, action: action, keyEquivalent: key)
        item.target = self
        menu.addItem(item)
        return item
    }

    private func info(_ menu: NSMenu, _ title: String) {
        let item = NSMenuItem(title: title, action: nil, keyEquivalent: "")
        item.isEnabled = false
        menu.addItem(item)
    }

    private func speedMenu(_ title: String, current: Double, _ sel: Selector) -> NSMenuItem {
        let item = NSMenuItem(title: title, action: nil, keyEquivalent: "")
        let sub = NSMenu()
        for (name, value) in speeds {
            let s = add(sub, name, sel)
            s.representedObject = value
            s.state = abs(value - current) < 0.01 ? .on : .off
        }
        item.submenu = sub
        return item
    }

    // MARK: Menu actions

    @objc private func toggleEnabled() { enabled.toggle() }
    @objc private func toggleInvert() { invertScroll.toggle() }
    @objc private func toggleSwap() { swapSticks.toggle() }
    @objc private func setPointerSpeed(_ s: NSMenuItem) { pointerSpeed = s.representedObject as? Double ?? 1 }
    @objc private func setScrollSpeed(_ s: NSMenuItem) { scrollSpeed = s.representedObject as? Double ?? 1 }

    @objc private func chooseAction(_ s: NSMenuItem) {
        guard let pair = s.representedObject as? [String], pair.count == 2,
              let b = Button(rawValue: pair[0]), let a = Action(rawValue: pair[1]) else { return }
        setAction(a, for: b)
    }

    @objc private func resetMapping() { defaults.removeObject(forKey: "mapping") }

    @objc private func openAccessibility() {
        let opts = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        _ = AXIsProcessTrustedWithOptions(opts)
        if let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility") {
            NSWorkspace.shared.open(url)
        }
    }

    @objc private func showPairingHelp() {
        let alert = NSAlert()
        alert.messageText = "Connecting your controller"
        alert.informativeText = """
        PS4 (DualShock 4)
        • Wired: plug in with a USB cable that carries data (many cheap cables only charge).
        • Bluetooth: hold SHARE + PS together for ~3 sec until the light bar double-flashes, then pick "DUALSHOCK 4 Wireless Controller" in System Settings → Bluetooth.

        Nintendo Switch Pro Controller
        • Bluetooth: hold the small round SYNC button on top (next to the USB-C port) until the 4 lights sweep back and forth, then pick "Pro Controller" in Bluetooth settings.
        • Joy-Con: hold the small sync button on the rail side.
        • Over USB, Macs usually don't recognise Switch controllers — use Bluetooth.

        If pairing fails
        • Turn the PS4 / Switch console off (or unplug it) — it grabs the controller back.
        • In Bluetooth settings, remove the controller (ⓘ → Forget) and pair again.
        • Charge the controller for a while first.
        """
        NSApp.activate(ignoringOtherApps: true)
        alert.runModal()
    }

    @objc private func toggleLogin() {
        do {
            if SMAppService.mainApp.status == .enabled { try SMAppService.mainApp.unregister() }
            else { try SMAppService.mainApp.register() }
        } catch {
            let alert = NSAlert()
            alert.messageText = "Couldn't change Launch at Login"
            alert.informativeText = "Move ControllerMouse.app to Applications and try again.\n\n\(error.localizedDescription)"
            NSApp.activate(ignoringOtherApps: true)
            alert.runModal()
        }
    }

    @objc private func quit() { NSApp.terminate(nil) }
}

// MARK: - Entry point

let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
app.setActivationPolicy(.accessory)   // menu bar only
app.run()
