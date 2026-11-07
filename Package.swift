// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "ControllerMouse",
    platforms: [.macOS(.v13)],
    targets: [
        .executableTarget(
            name: "ControllerMouse",
            path: "Sources/ControllerMouse",
            linkerSettings: [
                .linkedFramework("GameController"),
                .linkedFramework("ApplicationServices"),
            ]
        )
    ]
)
