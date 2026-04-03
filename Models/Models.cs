using Newtonsoft.Json;

namespace MeowlyAPI.Models;

// ── CUSTOMER ─────────────────────────────────────────
public class Customer
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonProperty("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonProperty("dob")]
    public string Dob { get; set; } = string.Empty;

    [JsonProperty("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

// ── EMPLOYEE ─────────────────────────────────────────
public class Employee
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("employeeId")]
    public string EmployeeId { get; set; } = string.Empty;

    [JsonProperty("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonProperty("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonProperty("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonProperty("dob")]
    public string Dob { get; set; } = string.Empty;

    [JsonProperty("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

// ── APPOINTMENT ──────────────────────────────────────
public class Appointment
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("ownerName")]
    public string OwnerName { get; set; } = string.Empty;

    [JsonProperty("petName")]
    public string PetName { get; set; } = string.Empty;

    [JsonProperty("petType")]
    public string PetType { get; set; } = string.Empty;

    [JsonProperty("contact")]
    public string Contact { get; set; } = string.Empty;

    [JsonProperty("dateTime")]
    public string DateTime { get; set; } = string.Empty;

    [JsonProperty("service")]
    public string Service { get; set; } = string.Empty;

    [JsonProperty("payment")]
    public string Payment { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = "Pending";

    [JsonProperty("customerId")]
    public string? CustomerId { get; set; }

    [JsonProperty("bookedBy")]
    public string? BookedBy { get; set; }

    [JsonProperty("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

// ── PRODUCT ──────────────────────────────────────────
public class Product
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("category")]
    public string Category { get; set; } = string.Empty;

    [JsonProperty("price")]
    public decimal Price { get; set; }

    [JsonProperty("stock")]
    public int Stock { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [JsonProperty("icon")]
    public string Icon { get; set; } = string.Empty;

    [JsonProperty("image")]
    public string Image { get; set; } = string.Empty;

    [JsonProperty("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonProperty("updatedAt")]
    public string? UpdatedAt { get; set; }
}
