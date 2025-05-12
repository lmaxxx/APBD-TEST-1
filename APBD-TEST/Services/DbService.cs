using APBD_TEST.Exceptions;
using APBD_TEST.Models;
using Microsoft.Data.SqlClient;

namespace APBD_TEST.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;
    public DbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
    }
    
    public async Task<FullVisitDto?> GetVisitById(int visitId)
    {
        var query = @"select v.date, c.first_name, c.last_name, c.date_of_birth, m.mechanic_id, m.licence_number, s.name, s.base_fee
                from Client C
                join Visit v on C.client_id = v.client_id
                join Visit_Service VS on v.visit_id = VS.visit_id
                join Service s on VS.service_id = s.service_id
                join Mechanic m on v.mechanic_id = m.mechanic_id
                where v.visit_id = @visitId;";

        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand();

        command.Connection = connection;
        command.CommandText = query;
        await connection.OpenAsync();

        command.Parameters.AddWithValue("@visitId", visitId);
        var reader = await command.ExecuteReaderAsync();
        FullVisitDto? visitDto = null;

        while (await reader.ReadAsync())
        {
            if (visitDto is null)
                visitDto = new FullVisitDto()
                {
                    date = reader.GetDateTime(0),
                    client = new ClientDto
                    {
                        firstName = reader.GetString(1),
                        lastName = reader.GetString(2),
                        dateOfBirth = reader.GetDateTime(3)
                    },
                    mechanic = new MechanicDto
                    {
                        mechanicId = reader.GetInt32(4),
                        licenceNumber = reader.GetString(5)
                    },
                    visitServices = new List<ServiceDto>()
                };

            var services = new ServiceDto
            {
                serviceName = reader.GetString(6),
                serviceFee = reader.GetDecimal(7)
            };

            visitDto.visitServices.Add(services);
        }

        if (visitDto is null) throw new NotFoundException($"There is no visit record with id {visitId}");
        
        return visitDto;
    }
    
    public async Task CreateVisit(CreateVisitDto createVisitDto)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        await using var command = connection.CreateCommand();
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            command.CommandText = "SELECT 1 FROM Visit WHERE visit_id = @visitId";
            command.Parameters.AddWithValue("@visitId", createVisitDto.visitId);
            var exists = await command.ExecuteScalarAsync();
            if (exists is not null)
                throw new ConflictException($"Visit with id {createVisitDto.visitId} already exists");

            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Client WHERE client_id = @clientId";
            command.Parameters.AddWithValue("@clientId", createVisitDto.clientId);
            if (await command.ExecuteScalarAsync() is null)
                throw new ConflictException($"Client with id {createVisitDto.clientId} not found");

            command.Parameters.Clear();
            command.CommandText = "SELECT mechanic_id FROM Mechanic WHERE licence_number = @licenceNumber";
            command.Parameters.AddWithValue("@licenceNumber", createVisitDto.mechanicLicenceNumber);
            var mechanicIdObj = await command.ExecuteScalarAsync();
            if (mechanicIdObj is null)
                throw new ConflictException($"Mechanic with licence number {createVisitDto.mechanicLicenceNumber} not found");

            var mechanicId = (int)mechanicIdObj;
            
            command.Parameters.Clear();
            command.CommandText = @"INSERT INTO Visit (visit_id, client_id, mechanic_id, date)
                        VALUES (@visitId, @clientId, @mechanicId, GETDATE())";
            command.Parameters.AddWithValue("@visitId", createVisitDto.visitId);
            command.Parameters.AddWithValue("@clientId", createVisitDto.clientId);
            command.Parameters.AddWithValue("@mechanicId", mechanicId);
            await command.ExecuteNonQueryAsync();

            foreach (var service in createVisitDto.services)
            {
                command.Parameters.Clear();
                command.CommandText = "SELECT service_id FROM Service WHERE name = @serviceName";
                command.Parameters.AddWithValue("@serviceName", service.serviceName);
                var serviceIdObj = await command.ExecuteScalarAsync();
                if (serviceIdObj is null)
                    throw new ConflictException($"Service with name {service.serviceName} not found");
            
                var serviceId = (int)serviceIdObj;
            
                command.Parameters.Clear();
                command.CommandText = @"INSERT INTO Visit_Service VALUES (@visitId, @serviceId, @serviceFee)";
                command.Parameters.AddWithValue("@visitId", createVisitDto.visitId);
                command.Parameters.AddWithValue("@serviceId", serviceId);
                command.Parameters.AddWithValue("@serviceFee", service.serviceFee);
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}