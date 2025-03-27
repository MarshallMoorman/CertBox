using CertBox.Common.Models;

namespace CertBox.Common.Services
{
    public interface IUserConfigService
    {
        UserConfig Config { get; }
        void SaveConfig();
        void UpdateJdkPath(string jdkPath);
    }
}