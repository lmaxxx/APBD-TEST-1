namespace APBD_TEST.Models;

public class CreateVisitDto
{
    public int visitId { get; set; }
    public int clientId { get; set; }
    public string mechanicLicenceNumber { get; set; } = string.Empty;
    public List<ServiceDto> services { get; set; } = [];
}