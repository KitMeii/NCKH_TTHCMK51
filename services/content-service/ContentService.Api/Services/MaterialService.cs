using ContentService.Api.Data;
using ContentService.Api.Dtos;
using ContentService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Common;

namespace ContentService.Api.Services;

public sealed class MaterialService(ContentDbContext db) : IMaterialService
{
    public async Task<IReadOnlyList<MaterialResponse>> ListAsync(bool includeInactive, string? chapter, CancellationToken ct)
    {
        var query = db.Materials.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(m => m.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(chapter))
        {
            query = query.Where(m => m.Chapter == chapter);
        }

        var materials = await query.OrderBy(m => m.Chapter).ThenByDescending(m => m.CreatedAtUtc).ToListAsync(ct);
        return materials.Select(ToResponse).ToList();
    }

    public async Task<MaterialResponse> GetByIdAsync(Guid id, bool includeInactive, CancellationToken ct)
    {
        var material = await db.Materials.FindAsync([id], ct)
            ?? throw new NotFoundException("Không tìm thấy tài liệu.");

        if (!includeInactive && !material.IsActive)
        {
            throw new NotFoundException("Không tìm thấy tài liệu.");
        }

        return ToResponse(material);
    }

    public async Task<MaterialResponse> CreateAsync(CreateMaterialRequest request, Guid uploadedBy, CancellationToken ct)
    {
        var material = new Material
        {
            Title = request.Title.Trim(),
            Chapter = request.Chapter?.Trim(),
            Description = request.Description?.Trim(),
            FileName = request.FileName.Trim(),
            FileUrl = request.FileUrl.Trim(),
            FileSize = request.FileSize,
            UploadedBy = uploadedBy,
        };

        db.Materials.Add(material);
        await db.SaveChangesAsync(ct);
        return ToResponse(material);
    }

    public async Task<MaterialResponse> UpdateAsync(Guid id, UpdateMaterialRequest request, CancellationToken ct)
    {
        var material = await db.Materials.FindAsync([id], ct)
            ?? throw new NotFoundException("Không tìm thấy tài liệu.");

        material.Title = request.Title.Trim();
        material.Chapter = request.Chapter?.Trim();
        material.Description = request.Description?.Trim();
        material.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return ToResponse(material);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var material = await db.Materials.FindAsync([id], ct)
            ?? throw new NotFoundException("Không tìm thấy tài liệu.");

        db.Materials.Remove(material);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> IncrementViewCountAsync(Guid id, CancellationToken ct)
    {
        var material = await db.Materials.FindAsync([id], ct)
            ?? throw new NotFoundException("Không tìm thấy tài liệu.");

        material.ViewCount++;
        await db.SaveChangesAsync(ct);
        return material.ViewCount;
    }

    private static MaterialResponse ToResponse(Material m) => new(
        m.Id, m.Title, m.Chapter, m.Description, m.FileName, m.FileUrl, m.FileSize,
        m.UploadedBy, m.IsActive, m.ViewCount, m.CreatedAtUtc);
}
