using System.ComponentModel.DataAnnotations;

namespace APBD_TEST.Models;

public class FullVisitDto
{
    public DateTime date {get; set;}
    public ClientDto client {get; set;}
    public MechanicDto mechanic {get; set;}
    public List<ServiceDto> visitServices { get; set; } = [];
}