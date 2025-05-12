using APBD_TEST.Models;

namespace APBD_TEST.Services;

public interface IDbService
{
    Task<FullVisitDto?> GetVisitById(int visitId);
    Task CreateVisit(CreateVisitDto createVisitDto);
}