namespace AdminService.Api.Dtos;

public sealed record UserSummaryResponse(Guid Id, string Email, string Name, string Role);

public sealed record ChangeRoleRequest(string Role);

public sealed record RoleChangeAuditResponse(Guid Id, Guid AdminUserId, Guid TargetUserId, string OldRole, string NewRole, DateTime ChangedAtUtc);

public sealed record SystemConfigResponse(string Key, string Value, DateTime UpdatedAtUtc);

public sealed record SetConfigRequest(string Value);

public sealed record SystemOverviewResponse(int TotalStudents, int TotalTeachers, int TotalAdmins, int TotalMaterials, int TotalQuestions, int TotalOralQuestions);
