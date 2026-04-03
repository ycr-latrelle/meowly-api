using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MeowlyAPI.DTOs;
using MeowlyAPI.Services;

namespace MeowlyAPI.Controllers;

// ═══════════════════════════════════════════════════════
//  CUSTOMERS
// ═══════════════════════════════════════════════════════
[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _svc;
    private readonly JwtService _jwt;

    public CustomersController(CustomerService svc, JwtService jwt)
    {
        _svc = svc; _jwt = jwt;
    }

    // GET /api/customers  [Employee only]
    [HttpGet]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _svc.GetAllAsync();
        return Ok(list);
    }

    // GET /api/customers/{id}  [Employee only]
    [HttpGet("{id}")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetById(string id)
    {
        var c = await _svc.GetByIdAsync(id);
        return c == null ? NotFound(new { message = "Customer not found." }) : Ok(c);
    }

    // POST /api/customers/register  [Public]
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CustomerRegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var customer = await _svc.RegisterAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST /api/customers/login  [Public]
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] CustomerLoginDto dto)
    {
        var (customer, valid) = await _svc.ValidateAsync(dto.Email, dto.Password);
        if (!valid) return Unauthorized(new { message = "Invalid email or password." });

        var token    = _jwt.GenerateToken(customer.Id, "Customer");
        var response = await _svc.GetByIdAsync(customer.Id);
        return Ok(new CustomerAuthResponse(token, response!));
    }
}

// ═══════════════════════════════════════════════════════
//  EMPLOYEES
// ═══════════════════════════════════════════════════════
[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _svc;
    private readonly JwtService _jwt;

    public EmployeesController(EmployeeService svc, JwtService jwt)
    {
        _svc = svc; _jwt = jwt;
    }

    // GET /api/employees  [Employee only]
    [HttpGet]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _svc.GetAllAsync();
        return Ok(list);
    }

    // POST /api/employees/register  [Public — first-time setup]
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] EmployeeRegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var employee = await _svc.RegisterAsync(dto);
            // Return employeeId so the frontend can show it in the modal
            return Ok(new { employee.EmployeeId, employee.FirstName, employee.LastName });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // POST /api/employees/login  [Public]
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] EmployeeLoginDto dto)
    {
        var (employee, valid) = await _svc.ValidateAsync(dto.EmployeeId, dto.Password);
        if (!valid) return Unauthorized(new { message = "Invalid Employee ID or password." });

        var token    = _jwt.GenerateToken(employee.Id, "Employee", employee.EmployeeId);
        var response = new EmployeeResponseDto(
            employee.Id, employee.EmployeeId,
            employee.FirstName, employee.LastName,
            employee.Email, employee.Gender,
            employee.Dob, employee.CreatedAt);

        return Ok(new EmployeeAuthResponse(token, response));
    }
}

// ═══════════════════════════════════════════════════════
//  APPOINTMENTS
// ═══════════════════════════════════════════════════════
[ApiController]
[Route("api/appointments")]
[Authorize]   // Both employees and customers can access; narrow per action below
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _svc;

    public AppointmentsController(AppointmentService svc) => _svc = svc;

    // GET /api/appointments  [Employee only]
    [HttpGet]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetAll()
        => Ok(await _svc.GetAllAsync());

    // GET /api/appointments/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var a = await _svc.GetByIdAsync(id);
        return a == null ? NotFound(new { message = "Appointment not found." }) : Ok(a);
    }

    // POST /api/appointments  [Customers + Employees]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AppointmentCreateDto dto)
    {
        var appt = await _svc.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = appt.Id }, appt);
    }

    // PATCH /api/appointments/{id}/status  [Employee only]
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] AppointmentStatusDto dto)
    {
        var ok = await _svc.UpdateStatusAsync(id, dto.Status);
        if (!ok) return BadRequest(new { message = "Invalid status or appointment not found." });
        return Ok(new { message = "Status updated.", status = dto.Status });
    }

    // DELETE /api/appointments/{id}  [Employee only]
    [HttpDelete("{id}")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _svc.DeleteAsync(id);
        return ok ? NoContent() : NotFound(new { message = "Appointment not found." });
    }
}

// ═══════════════════════════════════════════════════════
//  PRODUCTS
// ═══════════════════════════════════════════════════════
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _svc;

    public ProductsController(ProductService svc) => _svc = svc;

    // GET /api/products?category=food  [Public — customers can browse]
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? category)
        => Ok(await _svc.GetAllAsync(category));

    // GET /api/products/{id}  [Public]
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id)
    {
        var p = await _svc.GetByIdAsync(id);
        return p == null ? NotFound(new { message = "Product not found." }) : Ok(p);
    }

    // POST /api/products  [Employee only]
    [HttpPost]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        var product = await _svc.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT /api/products/{id}  [Employee only]
    [HttpPut("{id}")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Update(string id, [FromBody] ProductUpdateDto dto)
    {
        var product = await _svc.UpdateAsync(id, dto);
        return product == null ? NotFound(new { message = "Product not found." }) : Ok(product);
    }

    // PATCH /api/products/{id}/stock  [Employee only]
    [HttpPatch("{id}/stock")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> UpdateStock(string id, [FromBody] StockUpdateDto dto)
    {
        var ok = await _svc.UpdateStockAsync(id, dto.Stock);
        return ok ? Ok(new { message = "Stock updated.", stock = dto.Stock })
                  : NotFound(new { message = "Product not found." });
    }

    // PATCH /api/products/stock/bulk  [Employee only]
    [HttpPatch("stock/bulk")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> BulkUpdateStock([FromBody] BulkStockDto dto)
    {
        await _svc.BulkUpdateStockAsync(dto.Updates);
        return Ok(new { message = "Bulk stock update complete.", count = dto.Updates.Count });
    }

    // DELETE /api/products/{id}  [Employee only]
    [HttpDelete("{id}")]
    [Authorize(Roles = "Employee")]
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _svc.DeleteAsync(id);
        return ok ? NoContent() : NotFound(new { message = "Product not found." });
    }
}
