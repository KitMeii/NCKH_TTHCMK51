namespace ContentService.Api.Dtos;

public sealed record CreateMaterialRequest(string Title, string? Chapter, string? Description, string FileName, string FileUrl, long FileSize);

public sealed record UpdateMaterialRequest(string Title, string? Chapter, string? Description, bool IsActive);

public sealed record MaterialResponse(
    Guid Id,
    string Title,
    string? Chapter,
    string? Description,
    string FileName,
    string FileUrl,
    long FileSize,
    Guid UploadedBy,
    bool IsActive,
    int ViewCount,
    DateTime CreatedAtUtc);
