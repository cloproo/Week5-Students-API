#!/usr/bin/env python3
"""Static checks only; dotnet build and HTTP smoke tests are required for runtime proof."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parents[1]
expected = [
    'StudentApi/StudentApi.csproj',
    'StudentApi/Program.cs',
    'StudentApi/Controllers/AuthController.cs',
    'StudentApi/Controllers/StudentsController.cs',
    'StudentApi/Dtos/AuthDtos.cs',
    'StudentApi/Dtos/StudentDtos.cs',
    'StudentApi/Data/AppDbContext.cs',
    'StudentApi/Swagger/JwtAuthorizeOperationFilter.cs',
    'StudentApi/Services/JwtTokenService.cs',
    'Dockerfile', 'compose.yaml', 'README.md', 'DEPLOYMENT.md',
    'SUBMISSION.md', '.github/workflows/ci.yml',
    'postman/Week5_Students_API.postman_collection.json',
]
for path in expected:
    assert (root/path).is_file(), f'Missing required file: {path}'

ET.parse(root/'StudentApi/StudentApi.csproj')
for path in root.rglob('*.json'):
    json.loads(path.read_text(encoding='utf-8'))

app = (root/'StudentApi/Controllers/AuthController.cs').read_text(encoding='utf-8')
for route in ('[HttpPost("register")]', '[HttpPost("login")]', '[HttpGet("me")]'):
    assert route in app, f'Missing auth endpoint: {route}'
assert 'VerifyHashedPassword' in app and 'HashPassword' in app
assert 'JwtRegisteredClaimNames.Sub' in app and '[Authorize]' in app

students = (root/'StudentApi/Controllers/StudentsController.cs').read_text(encoding='utf-8')
for route in ('[HttpGet]', '[HttpGet("{id:int}")]', '[HttpPost]', '[HttpPut("{id:int}")]', '[HttpDelete("{id:int}")]'):
    assert route in students, f'Missing CRUD endpoint: {route}'
for term in ('CreateStudentDto', 'UpdateStudentDto', 'CreatedAtAction',
             'NoContent()', 'Conflict(', 'NotFound(', 'filter.Search',
             'filter.Department', 'filter.Page', 'filter.PageSize', '[Authorize]'):
    assert term in students, f'Missing CRUD capability: {term}'

program = (root/'StudentApi/Program.cs').read_text(encoding='utf-8')
for term in ('AddSwaggerGen', 'AddSecurityDefinition("Bearer"', 'OperationFilter<JwtAuthorizeOperationFilter>',
             'UseAuthentication()', 'UseAuthorization()', 'UseSqlite(', 'UseNpgsql('):
    assert term in program, f'Missing runtime configuration: {term}'
assert '"Key":' not in (root/'StudentApi/appsettings.json').read_text(encoding='utf-8')
assert not (root/'.env').exists(), 'Private .env file must not be packaged'

print(f'STATIC CHECK PASSED: {len(expected)} critical files present, JSON/XML parsed, endpoints and security features found.')
print('WARNING: This does NOT prove .NET compilation, actual HTTP behavior or public hosting.')
