using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CertBox.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CertBox.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private ObservableCollection<CertificateModel> _certificates = new();

        public MainWindowViewModel()
        {
            // Placeholder: Add dummy data for testing
            Certificates.Add(new CertificateModel 
            { 
                Alias = "test1", 
                Subject = "CN=Test1", 
                Issuer = "CN=Issuer1", 
                ExpiryDate = "2025-12-31", 
                IsExpired = false 
            });
            Certificates.Add(new CertificateModel 
            { 
                Alias = "test2", 
                Subject = "CN=Test2", 
                Issuer = "CN=Issuer2", 
                ExpiryDate = "2023-01-01", 
                IsExpired = true 
            });
        }

        [RelayCommand]
        private async Task Import()
        {
            // TODO: Implement import logic
            await Task.CompletedTask; // Placeholder
        }

        [RelayCommand]
        private void Remove()
        {
            // TODO: Implement remove logic
        }

        partial void OnSearchQueryChanged(string value)
        {
            // TODO: Implement search/filter logic
        }
    }
}