using ContentService.Api.Dtos;

namespace ContentService.Api.Services;

public interface IMaterialService
{
    Task<IReadOnlyList<MaterialResponse>> ListAsync(bool includeInactive, string? chapter, CancellationToken ct);
    Task<MaterialResponse> GetByIdAsync(Guid id, bool includeInactive, CancellationToken ct);
    Task<MaterialResponse> CreateAsync(CreateMaterialRequest request, Guid uploadedBy, CancellationToken ct);
    Task<MaterialResponse> UpdateAsync(Guid id, UpdateMaterialRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<int> IncrementViewCountAsync(Guid id, CancellationToken ct);
}
