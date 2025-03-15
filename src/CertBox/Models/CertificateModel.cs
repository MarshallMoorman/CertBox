namespace CertBox.Models
{
    public class CertificateModel
    {
        public string Alias { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool IsExpired => ExpiryDate < DateTime.Now;
    }
}