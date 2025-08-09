namespace RinhaBackend.Shared.ThirdParty.Dtos;

public class ServiceHealthResponseDto
{
    public bool IsFailing { get; set; }
    
    public int MinResponseTime { get; set; }
}