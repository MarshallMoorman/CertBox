namespace CertBox.Models
{
    public class CertificateModel
    {
        public string Alias { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public bool IsExpired { get; set; }
    }
}