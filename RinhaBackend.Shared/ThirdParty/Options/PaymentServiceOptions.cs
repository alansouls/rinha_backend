using System.Diagnostics.CodeAnalysis;

namespace RinhaBackend.Shared.ThirdParty.Options;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class PaymentServiceOptions
{
    public string DefaultUrl { get; set; } = string.Empty;
    
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    public string FallbackUrl { get; set; } = string.Empty;
    
    public TimeSpan FallbackTimeout { get; set; } = TimeSpan.FromSeconds(30);
}