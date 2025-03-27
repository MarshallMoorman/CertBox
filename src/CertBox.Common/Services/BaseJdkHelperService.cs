using System.Runtime.InteropServices;
using CertBox.Common.Services;
using Microsoft.Extensions.Logging;

namespace CertBox.Common
{
    public abstract class BaseJdkHelperService : IJdkHelperService
    {
        public BaseJdkHelperService()
        {
        }

        public abstract string GetJvmLibraryPath();

        public static IJdkHelperService CreateJdkHelperService(ILogger logger, IUserConfigService userConfigService)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsJdkHelperService(logger, userConfigService);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxJdkHelperService(logger, userConfigService);
            }

            return new MacOsJdkHelperService(logger, userConfigService);
        }
    }
}