using System.Text;

namespace CertBox.Models;

public class CertificateModel
{
    public string Alias { get; set; }
    public string Subject { get; set; }
    public string Issuer { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsExpired => ExpiryDate < DateTime.Now;
    public string Details => BuildDetails();

    private string BuildDetails()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Alias: {Alias}");
        sb.AppendLine($"Subject: {Subject}");
        sb.AppendLine($"Issuer: {Issuer}");
        sb.AppendLine($"Expiry: {ExpiryDate}");
        sb.AppendLine($"Is Expired: {(IsExpired ? "Yes" : "No")}");
        return sb.ToString();
    }
}