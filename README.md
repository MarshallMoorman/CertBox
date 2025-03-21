# CertBox

CertBox is a cross-platform tool built with Avalonia UI and .NET 9 to manage certificates in a JDK's `cacerts` file. It allows users to list, import, remove, and replace certificates with a simple, table-based interface featuring search and filter capabilities.

## Features
- View all certificate fields (read-only) in a `cacerts` file.
- Select a `cacerts` file at runtime using a file picker.
- Import certificates in common formats (`.pem`, `.crt`, `.cer`, `.der`), rejecting expired certificates.
- Remove or replace existing certificates with selection in a `DataGrid`.
- Reject expired certificates on import; highlight invalid/expired certificates in red (fixed vertical alignment issue in the `Expiry` column).
- Highlight expired certificates in the `DataGrid` with a red background for the entire row, using the `IsExpired` property of `CertificateModel` to dynamically apply the `expired` class.
- Persist user state in `~/.certbox/user_config.json`, including the last opened keystore path, selected theme (dark or light), and window size (width and height).
- Cross-platform support: Windows, Linux, macOS (single-file executables + macOS `.app` bundle).
- View all certificate fields (read-only) in a `cacerts` file with a searchable and filterable table-based UI using custom icons and the Inter font.
- Added a fade-in animation for the details pane when a certificate is selected.
- **UI Enhancements**:
  - Added custom icons for "Deep Search", "Cancel Deep Search", and "Clear Search" buttons using `StreamGeometry` in `Assets/Icons.axaml`.
  - Implemented a details pane that appears when a certificate is selected, taking 1/4 of the window width, with the `DataGrid` taking 3/4. When no certificate is selected, the details pane and splitter are hidden, and the `DataGrid` takes 100% width.
  - Adjusted `GridSplitter` styling to be less harsh: set thickness to 1 pixel, increased opacity to 1.0, and added margins via adjacent elements (`DataGrid`, `ListBox`, details pane) to create visual gaps.
  - Fixed vertical splitter resizing by using `ColumnDefinitions="3*,Auto,1*"` and moving margins to adjacent elements.
  - Added a style targeting the `DataGrid` named `CertificateList` using `DataGrid#CertificateList` to apply a darker background (`#1A1A1A`) and thicker border (2 pixels).
- **Drag-and-Drop Support**:
  - Added drag-and-drop functionality to open keystores by dropping files onto the `KeystoreList` and import certificates by dropping files onto the `CertificateList`.
  - Implemented visual feedback for drag-and-drop with a green border around the `KeystoreList` and `CertificateList` during drag-over, using `DragOver` and `DragLeave` events in the code-behind.
- **Error Handling and State Management**:
  - Improved error handling in `MainWindowViewModel` by adding validation for invalid keystore paths and file access issues, with user-friendly error messages.
  - Introduced a `ViewState` class to manage UI state (`IsErrorPaneVisible`, `IsDeepSearchRunning`, `ErrorMessage`), fixing binding issues for the error pane and deep search progress bar/cancel button.
  - Fixed an `"Uninitialized keystore"` error during import by resetting the keystore state in `CertificateService` and clearing `SelectedFilePath` when loading fails.

## Getting Started
1. Clone the repository: `git clone https://github.com/MarshallMoorman/CertBox.git`
2. Navigate to `src/CertBox/`.
3. Restore dependencies: `dotnet restore`
4. Run the app: `dotnet run`

**Note**: This project is primarily developed on a MacBook Pro M3 Max (Apple Silicon). Ensure the .NET 9 SDK is installed with ARM64 support for development on similar hardware. The app uses a dark theme with colors inspired by the project icon: `#000000` (black) background, `#FFFFFF` (white) text/accents, and `#E0E0E0` (light gray) for secondary elements. The project relies on Avalonia 11.2.5 and CommunityToolkit.Mvvm 8.4.0.

**Finding a `cacerts` File**: CertBox requires a JDK `cacerts` file to operate. This file is typically located in your JDK installation, such as:
- macOS: `/Library/Java/JavaVirtualMachines/<jdk-version>/Contents/Home/lib/security/cacerts`
- Windows: `C:\Program Files\Java\<jdk-version>\lib\security\cacerts`
- Linux: `/usr/lib/jvm/<jdk-version>/lib/security/cacerts`
The default password for `cacerts` is `"changeit"`. You can select a `cacerts` file at runtime using the file picker in the app. For testing, a sample `cacerts` file and certificates are provided in `tests/resources`.

## Dependencies
- **IKVM and IKVM.Image.JDK**: Used to load JKS `cacerts` files via Java’s `java.security.KeyStore`. `IKVM.Image.JDK` embeds a JDK runtime, ensuring users don’t need to install a JDK separately. Both packages are required due to an oversight in package dependencies.
- **Microsoft.Extensions.DependencyInjection**: Provides dependency injection for services and ViewModels.
- **Microsoft.Extensions.Logging with Serilog**: Configures logging to a file (`logs/log-.txt`) with daily rolling, controlled via `appsettings.json`.
- **Microsoft.Extensions.Configuration**: Loads configuration from `appsettings.json`.
- **BouncyCastle.NetCore**: Used for generating self-signed certificates for testing purposes.

## Building
- Requires .NET 9 SDK.
- Build for all platforms: `dotnet publish -c Release -r <runtime-id>` (e.g., `win-x64`, `linux-x64`, `osx-x64`).

## Testing
- A test `cacerts` file and sample certificates (`sample_valid.pem`, `sample_expired.pem`) are generated by the `CertBox.TestGenerator` project. Run the following to generate test data:
  ```bash
  cd src/CertBox.TestGenerator
  dotnet run
  ```
- Unit tests are set up in the CertBox.Tests project using xUnit, located in tests/CertBox.Tests.

## License
MIT License - see [LICENSE](LICENSE) for details.
