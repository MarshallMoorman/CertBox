# CertBox

CertBox is a cross-platform tool built with Avalonia UI and .NET 9 to manage certificates in a JDK's `cacerts` file. It allows users to list, import, remove, and replace certificates with a simple, table-based interface featuring search and filter capabilities.

## Features
- View all certificate fields (read-only) in a `cacerts` file.
- Select a `cacerts` file at runtime using a file picker.
- Import certificates in common formats (`.pem`, `.crt`, `.cer`, `.der`).
- Remove or replace existing certificates.
- Reject expired certificates on import; highlight invalid/expired certificates in red (fixed vertical alignment issue in the `Expiry` column).
- Cross-platform support: Windows, Linux, macOS (single-file executables + macOS `.app` bundle).

## Getting Started
1. Clone the repository: `git clone https://github.com/<your-username>/CertBox.git`
2. Navigate to `src/CertBox/`.
3. Restore dependencies: `dotnet restore`
4. Run the app: `dotnet run`

**Note**: This project is primarily developed on a MacBook Pro M3 Max (Apple Silicon). Ensure the .NET 9 SDK is installed with ARM64 support for development on similar hardware. The app uses a dark theme with colors inspired by the project icon: `#000000` (black) background, `#FFFFFF` (white) text/accents, and `#E0E0E0` (light gray) for secondary elements. The project relies on Avalonia 11.2.5 and CommunityToolkit.Mvvm 8.4.0.

**Finding a `cacerts` File**: CertBox requires a JDK `cacerts` file to operate. This file is typically located in your JDK installation, such as:
- macOS: `/Library/Java/JavaVirtualMachines/<jdk-version>/Contents/Home/lib/security/cacerts`
- Windows: `C:\Program Files\Java\<jdk-version>\lib\security\cacerts`
- Linux: `/usr/lib/jvm/<jdk-version>/lib/security/cacerts`
The default password for `cacerts` is `"changeit"`. You can select a `cacerts` file at runtime using the file picker in the app.

## Dependencies
- **IKVM and IKVM.Image.JDK**: Used to load JKS `cacerts` files via Java’s `java.security.KeyStore`. `IKVM.Image.JDK` embeds a JDK runtime, ensuring users don’t need to install a JDK separately. Both packages are required due to an oversight in package dependencies.
- **Microsoft.Extensions.DependencyInjection**: Provides dependency injection for services and ViewModels.
- **Microsoft.Extensions.Logging with Serilog**: Configures logging to a file (`logs/log-.txt`) with daily rolling, controlled via `appsettings.json`.
- **Microsoft.Extensions.Configuration**: Loads configuration from `appsettings.json`.

## Building
- Requires .NET 9 SDK.
- Build for all platforms: `dotnet publish -c Release -r <runtime-id>` (e.g., `win-x64`, `linux-x64`, `osx-x64`).

## License
MIT License - see [LICENSE](LICENSE) for details.

## Contributing
Contributions welcome! See [CONTRIBUTING.md](#) (to be added) for guidelines.