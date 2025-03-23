using System.Text;

namespace CertBox.Models
{
    public class CertificateModel
    {
        public required string Alias { get; set; }
        public required string Subject { get; set; }
        public required string Issuer { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsExpired => ExpiryDate < DateTime.Now;
        public string Details => BuildDetails();

        private string BuildDetails()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Alias: {Alias}");
            sb.AppendLine();
            sb.AppendLine($"Subject: {Subject}");
            sb.AppendLine();
            sb.AppendLine($"Issuer: {Issuer}");
            sb.AppendLine();
            sb.AppendLine($"Expiry: {ExpiryDate}");
            sb.AppendLine();
            sb.AppendLine($"Is Expired: {(IsExpired ? "Yes" : "No")}");
            return sb.ToString();
        }
    }
}