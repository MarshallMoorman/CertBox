# CertBox

CertBox is a cross-platform tool built with Avalonia UI and .NET 9 to manage certificates in a JDK's `cacerts` file. It allows users to list, import, remove, and replace certificates with a simple, table-based interface featuring search and filter capabilities.

## Features
- View all certificate fields (read-only) in a `cacerts` file.
- Import certificates in common formats (`.pem`, `.crt`, `.cer`, `.der`).
- Remove or replace existing certificates.
- Reject expired certificates on import; highlight invalid/expired certificates in red.
- Cross-platform support: Windows, Linux, macOS (single-file executables + macOS `.app` bundle).

## Getting Started
1. Clone the repository: `git clone https://github.com/<your-username>/CertBox.git`
2. Navigate to `src/CertBox/`.
3. Restore dependencies: `dotnet restore`
4. Run the app: `dotnet run`

**Note**: This project is primarily developed on a MacBook Pro M3 Max (Apple Silicon). Ensure the .NET 9 SDK is installed with ARM64 support for development on similar hardware. The app uses a dark theme with colors inspired by the project icon: `#000000` (black) background, `#FFFFFF` (white) text/accents, and `#E0E0E0` (light gray) for secondary elements. The project relies on Avalonia 11.2.5 and CommunityToolkit.Mvvm 8.4.0.

## Building
- Requires .NET 9 SDK.
- Build for all platforms: `dotnet publish -c Release -r <runtime-id>` (e.g., `win-x64`, `linux-x64`, `osx-x64`).

## License
MIT License - see [LICENSE](LICENSE) for details.

## Contributing
Contributions welcome! See [CONTRIBUTING.md](#) (to be added) for guidelines.