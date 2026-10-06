using backend.DTOs;

namespace backend.Services;

public interface IDestinationService
{
    public Task<DestinationResponseDto> CreateDestinationAsync(DestinationRequestDto destinationRequestDto, Guid tenantId);
    public Task<DestinationResponseDto?> GetDestinationAsync(Guid tenantId, Guid id);
    public Task<List<DestinationResponseDto>> GetDestinationsAsync(Guid tenantId);
    public Task<DestinationResponseDto?> UpdateDestinationAsync(DestinationRequestDto destinationRequestDto, Guid tenantId, Guid id);
    public Task<bool> DeleteDestinationAsync(Guid tenantId, Guid id);
}
