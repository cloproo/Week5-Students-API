using System.ComponentModel.DataAnnotations;

namespace StudentApi.Dtos;

public sealed class CreateStudentDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Range(15, 110)]
    public int Age { get; init; }

    [Required, StringLength(80, MinimumLength = 2)]
    public required string Department { get; init; }
}

public sealed class UpdateStudentDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string FullName { get; init; }

    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Range(15, 110)]
    public int Age { get; init; }

    [Required, StringLength(80, MinimumLength = 2)]
    public required string Department { get; init; }
}

public sealed class StudentsQueryDto
{
    public string? Search { get; init; }
    public string? Department { get; init; }

    [Range(15, 110)]
    public int? MinAge { get; init; }

    [Range(15, 110)]
    public int? MaxAge { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;
}

public sealed record StudentDto(int Id, string FullName, string Email, int Age, string Department,
    DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize,
    int TotalCount, int TotalPages);
