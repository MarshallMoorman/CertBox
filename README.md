# CertBox

CertBox is a cross-platform tool built with Avalonia UI and .NET 9 to manage certificates in a JDK's `cacerts` file. It allows users to list, import, remove, and replace certificates with a simple, table-based interface featuring search and filter capabilities.

## Features
- View all certificate fields (read-only) in a `cacerts` file with a searchable and filterable table-based UI using custom icons and the Inter font.
- Select a `cacerts` file at runtime using a file picker, with the selected file path displayed in the UI.
- Import certificates in common formats (`.pem`, `.crt`, `.cer`, `.der`), rejecting expired certificates.
- Remove or replace existing certificates with selection in a `DataGrid`.
- Reject expired certificates on import; highlight invalid/expired certificates in red (fixed vertical alignment issue in the `Expiry` column).
- Highlight expired certificates in the `DataGrid` with a red background for the entire row, using the `IsExpired` property of `CertificateModel` to dynamically apply the `expired` class.
- Persist user state in `~/.certbox/user_config.json`, including the last opened keystore path, selected theme (dark or light), window size (width and height), and JDK path. On Windows, the `.certbox` directory is hidden for a cleaner user experience.
- Cross-platform support: Windows, Linux, macOS (single-file executables + macOS `.app` bundle).
- Added a fade-in animation for the details pane when a certificate is selected.
- **UI Enhancements**:
  - Added custom icons for "Deep Search", "Cancel Deep Search", and "Clear Search" buttons using `StreamGeometry` in `Assets/Icons.axaml`.
  - Implemented a details pane that appears when a certificate is selected, taking 1/4 of the window width, with the `DataGrid` taking 3/4. When no certificate is selected, the details pane and splitter are hidden, and the `DataGrid` takes 100% width.
  - Adjusted `GridSplitter` styling to be less harsh: set thickness to 1 pixel, increased opacity to 1.0, and added margins via adjacent elements (`DataGrid`, `ListBox`, details pane) to create visual gaps.
  - Added a style targeting the `DataGrid` named `CertificateList` using `DataGrid#CertificateList` to apply a darker background (`#1A1A1A`) and thicker border (2 pixels).
- **Drag-and-Drop Support**:
  - Added drag-and-drop functionality to open keystores by dropping files onto the `KeystoreList` and import certificates by dropping files onto the `CertificateList`.
  - Implemented visual feedback for drag-and-drop with a green border around the `KeystoreList` and `CertificateList` during drag-over.
- **JDK Path Configuration**:
  - Automatically detects the JDK path by searching common locations on each platform (e.g., `/Library/Java/JavaVirtualMachines` on macOS, `/usr/lib/jvm` on Linux, `C:\Program Files\Java` on Windows).
  - Allows users to manually configure the JDK path via a settings button, storing it in `user_config.json` as `JdkPath`.
  - Automatically searches for a `cacerts` file in the JDK’s `lib/security` directory after the JDK path is set, adding it to the list of keystores.
  - Displays an error message at startup if no JDK path is configured, prompting the user to set it via the settings button.
- **macOS-Specific Features**:
  - Prompts for Full Disk Access on macOS to allow searching the entire filesystem for keystores. If access is denied, a dialog guides the user to enable Full Disk Access in System Settings > Privacy & Security.
  - Uses a custom-generated `CertBox.icns` file for the macOS app icon, created from `graphics/certbox_icon.png` during the build process.

## Getting Started
1. Clone the repository: `git clone https://github.com/MarshallMoorman/CertBox.git`
2. Navigate to `src/CertBox/`.
3. Restore dependencies: `dotnet restore`
4. Run the app: `dotnet run`

**Note**: This project is primarily developed on a MacBook Pro M3 Max (Apple Silicon). Ensure the .NET 9 SDK is installed with ARM64 support for development on similar hardware. The app uses a dark theme with colors inspired by the project icon: `#000000` (black) background, `#FFFFFF` (white) text/accents, and `#E0E0E0` (light gray) for secondary elements. The project relies on Avalonia 11.2.5 and CommunityToolkit.Mvvm 8.4.0.

**Finding a `cacerts` File**: CertBox requires a JDK `cacerts` file to operate and a JDK installed on the user’s system to provide the `keytool` utility. The `cacerts` file is typically located in your JDK installation, such as:
- macOS: `/Library/Java/JavaVirtualMachines/<jdk-version>/Contents/Home/lib/security/cacerts`
- Windows: `C:\Program Files\Java\<jdk-version>\lib\security\cacerts`
- Linux: `/usr/lib/jvm/<jdk-version>/lib/security/cacerts`
The default password for `cacerts` is `"changeit"`. You can select a `cacerts` file at runtime using the file picker in the app, or CertBox will attempt to find one in your JDK’s `lib/security` directory if the JDK path is configured. For testing, a sample `cacerts` file and certificates are provided in `tests/resources`.

**macOS Full Disk Access**: On macOS, CertBox requires Full Disk Access to search the entire filesystem for keystores. On first launch, it will prompt for access to a protected location (e.g., Desktop). If denied, a dialog will guide you to enable Full Disk Access in System Settings > Privacy & Security > Full Disk Access. After granting access, restart CertBox.

## Dependencies
- **Microsoft.Extensions.DependencyInjection**: Provides dependency injection for services and ViewModels.
- **Microsoft.Extensions.Logging with Serilog**: Configures logging to a file (`logs/log-.txt`) with daily rolling, controlled via `appsettings.json`.
- **Microsoft.Extensions.Configuration**: Loads configuration from `appsettings.json`.
- **BouncyCastle.NetCore**: Used for generating self-signed certificates for testing purposes.
- **JDK (User-Provided)**: CertBox uses the `keytool` utility (bundled with a JDK) to manage JKS `cacerts` files. Users must have a JDK installed (version 8, 11, 17, or later), and the JDK path must be configured in the app via the settings button or automatically detected.

## Building
- Requires .NET 9 SDK.
- Build for all platforms: `dotnet publish -c Release -r <runtime-id>` (e.g., `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`).
- On macOS, the build process generates a `.app` bundle with a custom icon (`CertBox.icns`) created from `graphics/certbox_icon.png` using `sips` and `iconutil`.

## Testing
- A test `cacerts` file and sample certificates (`sample_valid.pem`, `sample_expired.pem`) are generated by the `CertBox.TestGenerator` project. Run the following to generate test data:
  ```bash
  cd src/CertBox.TestGenerator
  dotnet run
  ```
- Unit tests are set up in the CertBox.Tests project using xUnit, located in tests/CertBox.Tests.

## License
MIT License - see [LICENSE](LICENSE) for details.
