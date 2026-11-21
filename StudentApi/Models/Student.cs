namespace StudentApi.Models;

public sealed class Student
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public int Age { get; set; }
    public required string Department { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
