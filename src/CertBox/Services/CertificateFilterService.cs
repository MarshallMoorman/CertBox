using System.Collections.ObjectModel;
using CertBox.Models;

namespace CertBox.Services
{
    public class CertificateFilterService
    {
        private readonly ObservableCollection<CertificateModel> _allCertificates;

        public CertificateFilterService(ObservableCollection<CertificateModel> allCertificates)
        {
            _allCertificates = allCertificates;
        }

        public ObservableCollection<CertificateModel> FilterCertificates(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return new ObservableCollection<CertificateModel>(_allCertificates);
            }

            return new ObservableCollection<CertificateModel>(
                _allCertificates.Where(c =>
                    c.Alias.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    c.Issuer.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    c.Subject.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    c.ExpiryDate.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            );
        }

        public string ClearSearch()
        {
            return string.Empty;
        }
    }
}