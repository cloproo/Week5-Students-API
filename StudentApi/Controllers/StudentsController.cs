using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentApi.Data;
using StudentApi.Dtos;
using StudentApi.Models;

namespace StudentApi.Controllers;

[ApiController]
[Authorize]
[Route("api/students")]
public sealed class StudentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StudentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<StudentDto>>> GetAll([FromQuery] StudentsQueryDto filter,
        CancellationToken ct)
    {
        if (filter.MinAge.HasValue && filter.MaxAge.HasValue && filter.MinAge > filter.MaxAge)
            return BadRequest(new { message = "minAge cannot be greater than maxAge." });

        IQueryable<Student> query = db.Students.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(s => s.FullName.ToLower().Contains(search)
                || s.Email.ToLower().Contains(search)
                || s.Department.ToLower().Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            var department = filter.Department.Trim().ToLowerInvariant();
            query = query.Where(s => s.Department.ToLower() == department);
        }
        if (filter.MinAge.HasValue)
            query = query.Where(s => s.Age >= filter.MinAge.Value);
        if (filter.MaxAge.HasValue)
            query = query.Where(s => s.Age <= filter.MaxAge.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderBy(s => s.Id)
            .Skip((int)Math.Min(((long)filter.Page - 1) * filter.PageSize, int.MaxValue))
            .Take(filter.PageSize)
            .Select(s => new StudentDto(s.Id, s.FullName, s.Email, s.Age, s.Department,
                s.CreatedAtUtc, s.UpdatedAtUtc))
            .ToListAsync(ct);

        return Ok(new PagedResponse<StudentDto>(items, filter.Page, filter.PageSize,
            totalCount, (int)Math.Ceiling(totalCount / (double)filter.PageSize)));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentDto>> GetById(int id, CancellationToken ct)
    {
        var student = await db.Students.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StudentDto(s.Id, s.FullName, s.Email, s.Age, s.Department,
                s.CreatedAtUtc, s.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct);
        if (student is null)
            return NotFound(new { message = "Student not found." });
        return Ok(student);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentDto>> Create(CreateStudentDto request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Students.AnyAsync(s => s.Email == email, ct))
            return Conflict(new { message = "A student with this email already exists." });

        var student = new Student
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Age = request.Age,
            Department = request.Department.Trim()
        };
        db.Students.Add(student);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await db.Students.AnyAsync(s => s.Email == email, ct))
                return Conflict(new { message = "A student with this email already exists." });
            throw;
        }
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, ToDto(student));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StudentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StudentDto>> Update(int id, UpdateStudentDto request, CancellationToken ct)
    {
        var student = await db.Students.FindAsync(new object[] { id }, ct);
        if (student is null)
            return NotFound(new { message = "Student not found." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Students.AnyAsync(s => s.Email == email && s.Id != id, ct))
            return Conflict(new { message = "A student with this email already exists." });

        student.FullName = request.FullName.Trim();
        student.Email = email;
        student.Age = request.Age;
        student.Department = request.Department.Trim();
        student.UpdatedAtUtc = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await db.Students.AnyAsync(s => s.Email == email && s.Id != id, ct))
                return Conflict(new { message = "A student with this email already exists." });
            throw;
        }
        return Ok(ToDto(student));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var student = await db.Students.FindAsync(new object[] { id }, ct);
        if (student is null)
            return NotFound(new { message = "Student not found." });

        db.Students.Remove(student);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static StudentDto ToDto(Student s) => new(s.Id, s.FullName, s.Email,
        s.Age, s.Department, s.CreatedAtUtc, s.UpdatedAtUtc);
}
