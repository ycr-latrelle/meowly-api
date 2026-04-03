using MeowlyAPI.DTOs;
using MeowlyAPI.Models;

namespace MeowlyAPI.Services;

// ═══════════════════════════════════════════════════════
//  CUSTOMER SERVICE
// ═══════════════════════════════════════════════════════
public class CustomerService
{
    private const string Node = "customers";
    private readonly FirebaseService _db;

    public CustomerService(FirebaseService db) => _db = db;

    public async Task<List<CustomerResponseDto>> GetAllAsync()
    {
        var all = await _db.GetAllAsync<Customer>(Node);
        return all.Values
            .Select(ToResponse)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
    }

    public async Task<CustomerResponseDto?> GetByIdAsync(string id)
    {
        var c = await _db.GetOneAsync<Customer>($"{Node}/{id}");
        return c == null ? null : ToResponse(c);
    }

    public async Task<Customer?> FindByEmailAsync(string email)
    {
        var results = await _db.QueryByFieldAsync<Customer>(Node, "email", email);
        return results.Values.FirstOrDefault();
    }

    public async Task<CustomerResponseDto> RegisterAsync(CustomerRegisterDto dto)
    {
        // Check duplicate email
        var existing = await FindByEmailAsync(dto.Email);
        if (existing != null)
            throw new InvalidOperationException("Email is already registered.");

        var customer = new Customer
        {
            FirstName    = dto.FirstName,
            LastName     = dto.LastName,
            Email        = dto.Email.ToLowerInvariant(),
            PasswordHash = PasswordHelper.Hash(dto.Password),
            Gender       = dto.Gender,
            Dob          = dto.Dob,
            CreatedAt    = DateTime.UtcNow.ToString("o"),
        };

        // Push returns the Firebase-generated key
        var key = await _db.PushAsync(Node, customer);
        customer.Id = key;
        // Write the id back into the node
        await _db.PatchAsync($"{Node}/{key}", new { id = key });

        return ToResponse(customer);
    }

    public async Task<(Customer customer, bool valid)> ValidateAsync(string email, string password)
    {
        var customer = await FindByEmailAsync(email.ToLowerInvariant());
        if (customer == null) return (null!, false);
        return (customer, PasswordHelper.Verify(password, customer.PasswordHash));
    }

    private static CustomerResponseDto ToResponse(Customer c) => new(
        c.Id, c.FirstName, c.LastName, c.Email, c.Gender, c.Dob, c.CreatedAt);
}

// ═══════════════════════════════════════════════════════
//  EMPLOYEE SERVICE
// ═══════════════════════════════════════════════════════
public class EmployeeService
{
    private const string Node = "employees";
    private readonly FirebaseService _db;
    private readonly EmailService _email;

    public EmployeeService(FirebaseService db, EmailService email)
    {
        _db    = db;
        _email = email;
    }

    public async Task<List<EmployeeResponseDto>> GetAllAsync()
    {
        var all = await _db.GetAllAsync<Employee>(Node);
        return all.Values
            .Select(ToResponse)
            .OrderByDescending(e => e.CreatedAt)
            .ToList();
    }

    public async Task<Employee?> FindByEmailAsync(string email)
    {
        var results = await _db.QueryByFieldAsync<Employee>(Node, "email", email);
        return results.Values.FirstOrDefault();
    }

    public async Task<Employee?> FindByEmployeeIdAsync(string employeeId)
    {
        var results = await _db.QueryByFieldAsync<Employee>(Node, "employeeId", employeeId);
        return results.Values.FirstOrDefault();
    }

    public async Task<EmployeeResponseDto> RegisterAsync(EmployeeRegisterDto dto)
    {
        var existing = await FindByEmailAsync(dto.Email);
        if (existing != null)
            throw new InvalidOperationException("Email is already registered.");

        // Generate unique 8-digit employee ID: 1000XXXX
        string employeeId;
        do
        {
            var rand = new Random();
            employeeId = $"1000{rand.Next(1000, 9999)}";
        }
        while (await FindByEmployeeIdAsync(employeeId) != null); // ensure uniqueness

        var employee = new Employee
        {
            EmployeeId   = employeeId,
            FirstName    = dto.FirstName,
            LastName     = dto.LastName,
            Email        = dto.Email.ToLowerInvariant(),
            PasswordHash = PasswordHelper.Hash(dto.Password),
            Gender       = dto.Gender,
            Dob          = dto.Dob,
            CreatedAt    = DateTime.UtcNow.ToString("o"),
        };

        var key = await _db.PushAsync(Node, employee);
        employee.Id = key;
        await _db.PatchAsync($"{Node}/{key}", new { id = key });

        // Send employee ID via email (fire-and-forget, don't block response)
        _ = _email.SendEmployeeIdEmailAsync(employee.Email, employee.FirstName, employeeId);

        return ToResponse(employee);
    }

    public async Task<(Employee employee, bool valid)> ValidateAsync(string employeeId, string password)
    {
        var emp = await FindByEmployeeIdAsync(employeeId);
        if (emp == null) return (null!, false);
        return (emp, PasswordHelper.Verify(password, emp.PasswordHash));
    }

    private static EmployeeResponseDto ToResponse(Employee e) => new(
        e.Id, e.EmployeeId, e.FirstName, e.LastName, e.Email, e.Gender, e.Dob, e.CreatedAt);
}
