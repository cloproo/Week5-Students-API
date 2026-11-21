#!/usr/bin/env python3
"""End-to-end HTTP contract smoke test. No third-party Python packages required.
Usage: python scripts/smoke_test.py --base http://localhost:5080
Does NOT start the server; point it at your running local or hosted instance.
"""
import argparse
import json
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", default="http://localhost:5080", help="http(s)://host[:port]")
    opts = parser.parse_args()
    base = opts.base.rstrip("/")
    checks = 0

    def call(method, path, body=None, token=None):
        url = base + path
        data = json.dumps(body).encode("utf-8") if body is not None else None
        headers = {"Accept": "application/json"}
        if body is not None:
            headers["Content-Type"] = "application/json"
        if token:
            headers["Authorization"] = "Bearer " + token
        request = urllib.request.Request(url, data=data, headers=headers, method=method)
        try:
            with urllib.request.urlopen(request, timeout=25) as response:
                raw = response.read()
                return response.status, dict(response.headers), json.loads(raw) if raw else None
        except urllib.error.HTTPError as error:
            raw = error.read()
            try:
                parsed = json.loads(raw) if raw else None
            except json.JSONDecodeError:
                parsed = raw.decode("utf-8", errors="replace")
            return error.code, dict(error.headers), parsed

    def check(label, method, path, expected, body=None, token=None):
        nonlocal checks
        status, headers, value = call(method, path, body, token)
        checks += 1
        if status != expected:
            raise AssertionError(f"{label}: expected HTTP {expected}, received {status}; response={value}")
        print(f"[{checks:02d}] PASS HTTP {status}: {label}")
        return headers, value

    unique = uuid.uuid4().hex[:12]
    email = f"week5.user.{unique}@example.com"
    student_email = f"week5.student.{unique}@example.com"
    password = "Week5!_DemoStrongPassword_2026"

    try:
        check("health and database", "GET", "/health", 200)
        check("reject missing token", "GET", "/api/students", 401)
        check("reject me without token", "GET", "/api/auth/me", 401)
        register = {"fullName": "Demo User", "email": email, "password": password}
        check("register user", "POST", "/api/auth/register", 201, register)
        check("duplicate register conflict", "POST", "/api/auth/register", 409, register)
        check("invalid password", "POST", "/api/auth/login", 401,
              {"email": email, "password": "incorrect"})
        _, login = check("login and JWT", "POST", "/api/auth/login", 200,
                         {"email": email, "password": password})
        token = login["token"]
        assert token.count(".") == 2 and login["expiresAtUtc"] and login["user"]["email"] == email
        _, me = check("read identity claims", "GET", "/api/auth/me", 200, token=token)
        assert me["email"] == email and me["id"]
        create = {"fullName": "Sara Demo", "email": student_email, "age": 21,
                  "department": "Cybersecurity"}
        headers, new_student = check("create student", "POST", "/api/students", 201,
                                     create, token)
        student_id = new_student["id"]
        assert headers.get("Location") or headers.get("location"), "201 must contain Location header"
        check("duplicate student conflict", "POST", "/api/students", 409, create, token)
        check("fetch student", "GET", f"/api/students/{student_id}", 200, token=token)
        _, page = check("search, department, age filters, pagination", "GET",
                        "/api/students?search=sara&department=Cybersecurity&minAge=18&maxAge=30&page=1&pageSize=2",
                        200, token=token)
        assert page["totalCount"] >= 1 and any(s["id"] == student_id for s in page["items"])
        check("reject invalid page size", "GET", "/api/students?pageSize=101", 400,
              token=token)
        check("reject invalid min/max age", "GET", "/api/students?minAge=35&maxAge=20", 400,
              token=token)
        check("reject invalid student DTO", "POST", "/api/students", 400,
              {**create, "age": 8, "email": "not-an-email"}, token)
        update = {**create, "fullName": "Sara Updated", "age": 22,
                  "department": "Computer Science"}
        _, changed = check("update student", "PUT", f"/api/students/{student_id}", 200,
                           update, token)
        assert changed["age"] == 22 and changed["department"] == "Computer Science"
        check("update missing student", "PUT", "/api/students/2147483647", 404,
              update, token)
        second = {**create, "email": f"second.{unique}@example.com"}
        _, second_student = check("create second student", "POST", "/api/students", 201,
                                  second, token)
        check("update email conflict", "PUT", f"/api/students/{student_id}", 409,
              {**update, "email": second["email"]}, token)
        check("delete first", "DELETE", f"/api/students/{student_id}", 204, token=token)
        check("deleted record is 404", "GET", f"/api/students/{student_id}", 404,
              token=token)
        check("delete already removed is 404", "DELETE", f"/api/students/{student_id}",
              404, token=token)
        check("cleanup second student", "DELETE", f"/api/students/{second_student['id']}",
              204, token=token)
    except (urllib.error.URLError, TimeoutError) as error:
        print(f"FAILED: cannot reach {base}: {error}", file=sys.stderr)
        return 2
    except (AssertionError, ValueError, KeyError, TypeError) as error:
        print(f"FAILED: {error}", file=sys.stderr)
        return 1
    print(f"SUCCESS: {checks} checks passed against {base}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
