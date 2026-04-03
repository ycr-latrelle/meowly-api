using MeowlyAPI.DTOs;
using MeowlyAPI.Models;

namespace MeowlyAPI.Services;

// ═══════════════════════════════════════════════════════
//  APPOINTMENT SERVICE
// ═══════════════════════════════════════════════════════
public class AppointmentService
{
    private const string Node = "appointments";
    private readonly FirebaseService _db;

    public AppointmentService(FirebaseService db) => _db = db;

    public async Task<List<Appointment>> GetAllAsync()
    {
        var all = await _db.GetAllAsync<Appointment>(Node);
        return all.Values
            .OrderByDescending(a => a.CreatedAt)
            .ToList();
    }

    public async Task<Appointment?> GetByIdAsync(string id)
        => await _db.GetOneAsync<Appointment>($"{Node}/{id}");

    public async Task<Appointment> CreateAsync(AppointmentCreateDto dto)
    {
        var appt = new Appointment
        {
            OwnerName  = dto.OwnerName,
            PetName    = dto.PetName,
            PetType    = dto.PetType,
            Contact    = dto.Contact,
            DateTime   = dto.DateTime,
            Service    = dto.Service,
            Payment    = dto.Payment,
            Status     = dto.Status,
            CustomerId = dto.CustomerId,
            BookedBy   = dto.BookedBy,
            CreatedAt  = System.DateTime.UtcNow.ToString("o"),
        };

        var key = await _db.PushAsync(Node, appt);
        appt.Id = key;
        await _db.PatchAsync($"{Node}/{key}", new { id = key });
        return appt;
    }

    public async Task<bool> UpdateStatusAsync(string id, string status)
    {
        var allowed = new[] { "Pending", "Confirmed", "Done", "Cancelled" };
        if (!allowed.Contains(status)) return false;

        var existing = await GetByIdAsync(id);
        if (existing == null) return false;

        await _db.PatchAsync($"{Node}/{id}", new { status });
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null) return false;
        await _db.DeleteAsync($"{Node}/{id}");
        return true;
    }
}

// ═══════════════════════════════════════════════════════
//  PRODUCT SERVICE
// ═══════════════════════════════════════════════════════
public class ProductService
{
    private const string Node = "products";
    private readonly FirebaseService _db;

    public ProductService(FirebaseService db) => _db = db;

    public async Task<List<Product>> GetAllAsync(string? category = null)
    {
        var all = await _db.GetAllAsync<Product>(Node);
        var list = all.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category))
            list = list.Where(p => p.Category == category);
        return list.OrderByDescending(p => p.CreatedAt).ToList();
    }

    public async Task<Product?> GetByIdAsync(string id)
        => await _db.GetOneAsync<Product>($"{Node}/{id}");

    public async Task<Product> CreateAsync(ProductCreateDto dto)
    {
        var product = new Product
        {
            Name        = dto.Name,
            Category    = dto.Category,
            Price       = dto.Price,
            Stock       = dto.Stock,
            Description = dto.Description,
            Icon        = dto.Icon,
            Image       = dto.Image,
            CreatedAt   = DateTime.UtcNow.ToString("o"),
        };

        var key = await _db.PushAsync(Node, product);
        product.Id = key;
        await _db.PatchAsync($"{Node}/{key}", new { id = key });
        return product;
    }

    public async Task<Product?> UpdateAsync(string id, ProductUpdateDto dto)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null) return null;

        existing.Name        = dto.Name;
        existing.Category    = dto.Category;
        existing.Price       = dto.Price;
        existing.Stock       = dto.Stock;
        existing.Description = dto.Description;
        existing.Icon        = dto.Icon;
        existing.Image       = dto.Image;
        existing.UpdatedAt   = DateTime.UtcNow.ToString("o");

        await _db.SetAsync($"{Node}/{id}", existing);
        return existing;
    }

    public async Task<bool> UpdateStockAsync(string id, int stock)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null) return false;
        await _db.PatchAsync($"{Node}/{id}", new
        {
            stock,
            updatedAt = DateTime.UtcNow.ToString("o")
        });
        return true;
    }

    public async Task BulkUpdateStockAsync(List<BulkStockItem> updates)
    {
        var tasks = updates.Select(u => UpdateStockAsync(u.Id, u.Stock));
        await Task.WhenAll(tasks);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await GetByIdAsync(id);
        if (existing == null) return false;
        await _db.DeleteAsync($"{Node}/{id}");
        return true;
    }
}
