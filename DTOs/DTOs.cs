namespace MeowlyAPI.DTOs;

// ── CUSTOMER ─────────────────────────────────────────
public record CustomerRegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Gender,
    string Dob
);

public record CustomerLoginDto(string Email, string Password);

public record CustomerResponseDto(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string Gender,
    string Dob,
    string CreatedAt
);

// ── EMPLOYEE ─────────────────────────────────────────
public record EmployeeRegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Gender,
    string Dob
);

public record EmployeeLoginDto(string EmployeeId, string Password);

public record EmployeeResponseDto(
    string Id,
    string EmployeeId,
    string FirstName,
    string LastName,
    string Email,
    string Gender,
    string Dob,
    string CreatedAt
);

// ── AUTH RESPONSES ────────────────────────────────────
public record CustomerAuthResponse(string Token, CustomerResponseDto Customer);
public record EmployeeAuthResponse(string Token, EmployeeResponseDto Employee);

// ── APPOINTMENT ──────────────────────────────────────
public record AppointmentCreateDto(
    string OwnerName,
    string PetName,
    string PetType,
    string Contact,
    string DateTime,
    string Service,
    string Payment,
    string? CustomerId,
    string? BookedBy,
    string Status = "Pending"
);

public record AppointmentStatusDto(string Status);

// ── PRODUCT ──────────────────────────────────────────
public record ProductCreateDto(
    string Name,
    string Category,
    decimal Price,
    int Stock,
    string Description,
    string Icon,
    string Image
);

public record ProductUpdateDto(
    string Name,
    string Category,
    decimal Price,
    int Stock,
    string Description,
    string Icon,
    string Image
);

public record StockUpdateDto(int Stock);

public record BulkStockItem(string Id, int Stock);
public record BulkStockDto(List<BulkStockItem> Updates);
