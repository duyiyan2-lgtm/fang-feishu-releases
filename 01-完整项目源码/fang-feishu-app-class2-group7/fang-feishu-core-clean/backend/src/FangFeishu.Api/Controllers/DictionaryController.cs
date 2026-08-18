using FangFeishu.Api.Common;
using FangFeishu.Api.Contracts;
using FangFeishu.Api.Data;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Controllers;

[Route("api/v1/dict")]
[Authorize]
public sealed class DictionaryController(AppDbContext db, IAuditService auditService) : BaseApiController
{
    [HttpGet("categories")]
    public async Task<IActionResult> Categories([FromQuery] bool includeDisabled = false)
    {
        var query = db.DictionaryCategories.Include(x => x.Items).AsQueryable();
        if (!includeDisabled || !CurrentUserIsAdmin)
        {
            query = query.Where(x => x.IsEnabled);
        }

        var categories = await query.OrderBy(x => x.Name).ToListAsync();
        return OkData(categories.Select(ToCategoryItem));
    }

    [HttpGet("categories/{code}")]
    public async Task<IActionResult> Category(string code, [FromQuery] bool includeDisabled = false)
    {
        var normalizedCode = NormalizeCode(code);
        var category = await db.DictionaryCategories
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Code == normalizedCode);
        if (category is null || (!category.IsEnabled && (!includeDisabled || !CurrentUserIsAdmin)))
        {
            return Fail(2301, "Dictionary category not found.", StatusCodes.Status404NotFound);
        }

        return OkData(ToCategoryDetail(category, includeDisabled && CurrentUserIsAdmin));
    }

    [HttpPost("categories")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateCategory(DictionaryCategoryRequest request)
    {
        if (!TryNormalizeCategory(request, out var code, out var name, out var error))
        {
            return Fail(2302, error);
        }

        if (await db.DictionaryCategories.AnyAsync(x => x.Code == code))
        {
            return Fail(2303, "Dictionary category code already exists.", StatusCodes.Status409Conflict);
        }

        var category = new DictionaryCategory
        {
            Code = code,
            Name = name,
            Description = NormalizeOptionalText(request.Description),
            IsEnabled = request.IsEnabled
        };
        db.DictionaryCategories.Add(category);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Dictionary", "CreateCategory", category.Id.ToString(), HttpContext);
        return CreatedData(ToCategoryDetail(category, true));
    }

    [HttpPut("categories/{code}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateCategory(string code, DictionaryCategoryRequest request)
    {
        var currentCode = NormalizeCode(code);
        var category = await db.DictionaryCategories.Include(x => x.Items).FirstOrDefaultAsync(x => x.Code == currentCode);
        if (category is null)
        {
            return Fail(2301, "Dictionary category not found.", StatusCodes.Status404NotFound);
        }

        if (!TryNormalizeCategory(request, out var newCode, out var name, out var error))
        {
            return Fail(2302, error);
        }

        if (await db.DictionaryCategories.AnyAsync(x => x.Id != category.Id && x.Code == newCode))
        {
            return Fail(2303, "Dictionary category code already exists.", StatusCodes.Status409Conflict);
        }

        category.Code = newCode;
        category.Name = name;
        category.Description = NormalizeOptionalText(request.Description);
        category.IsEnabled = request.IsEnabled;
        category.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Dictionary", "UpdateCategory", category.Id.ToString(), HttpContext);
        return OkData(ToCategoryDetail(category, true));
    }

    [HttpDelete("categories/{code}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteCategory(string code)
    {
        var normalizedCode = NormalizeCode(code);
        var category = await db.DictionaryCategories.FirstOrDefaultAsync(x => x.Code == normalizedCode);
        if (category is null)
        {
            return Fail(2301, "Dictionary category not found.", StatusCodes.Status404NotFound);
        }

        db.DictionaryCategories.Remove(category);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Dictionary", "DeleteCategory", category.Id.ToString(), HttpContext);
        return OkData(new { category.Id, category.Code });
    }

    [HttpGet("categories/{code}/items")]
    public async Task<IActionResult> Items(string code, [FromQuery] bool includeDisabled = false)
    {
        var normalizedCode = NormalizeCode(code);
        var category = await db.DictionaryCategories.FirstOrDefaultAsync(x => x.Code == normalizedCode);
        if (category is null || (!category.IsEnabled && (!includeDisabled || !CurrentUserIsAdmin)))
        {
            return Fail(2301, "Dictionary category not found.", StatusCodes.Status404NotFound);
        }

        var query = db.DictionaryItems.Where(x => x.CategoryId == category.Id);
        if (!includeDisabled || !CurrentUserIsAdmin)
        {
            query = query.Where(x => x.IsEnabled);
        }

        var items = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Label).ToListAsync();
        return OkData(items.Select(ToDictionaryItem));
    }

    [HttpPost("categories/{code}/items")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateItem(string code, DictionaryItemRequest request)
    {
        var category = await FindCategoryAsync(code);
        if (category is null)
        {
            return Fail(2301, "Dictionary category not found.", StatusCodes.Status404NotFound);
        }

        if (!TryNormalizeItem(request, out var itemCode, out var label, out var value, out var error))
        {
            return Fail(2304, error);
        }

        if (await db.DictionaryItems.AnyAsync(x => x.CategoryId == category.Id && x.Code == itemCode))
        {
            return Fail(2305, "Dictionary item code already exists in this category.", StatusCodes.Status409Conflict);
        }

        var item = new DictionaryItem
        {
            CategoryId = category.Id,
            Code = itemCode,
            Label = label,
            Value = value,
            Description = NormalizeOptionalText(request.Description),
            SortOrder = request.SortOrder,
            IsEnabled = request.IsEnabled
        };
        db.DictionaryItems.Add(item);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Dictionary", "CreateItem", item.Id.ToString(), HttpContext);
        return CreatedData(ToDictionaryItem(item));
    }

    [HttpPut("categories/{code}/items/{itemId:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateItem(string code, Guid itemId, DictionaryItemRequest request)
    {
        var category = await FindCategoryAsync(code);
        if (category is null)
        {
            return Fail(2301, "Dictionary category not found.", StatusCodes.Status404NotFound);
        }

        var item = await db.DictionaryItems.FirstOrDefaultAsync(x => x.Id == itemId && x.CategoryId == category.Id);
        if (item is null)
        {
            return Fail(2306, "Dictionary item not found.", StatusCodes.Status404NotFound);
        }

        if (!TryNormalizeItem(request, out var itemCode, out var label, out var value, out var error))
        {
            return Fail(2304, error);
        }

        if (await db.DictionaryItems.AnyAsync(x => x.CategoryId == category.Id && x.Id != item.Id && x.Code == itemCode))
        {
            return Fail(2305, "Dictionary item code already exists in this category.", StatusCodes.Status409Conflict);
        }

        item.Code = itemCode;
        item.Label = label;
        item.Value = value;
        item.Description = NormalizeOptionalText(request.Description);
        item.SortOrder = request.SortOrder;
        item.IsEnabled = request.IsEnabled;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Dictionary", "UpdateItem", item.Id.ToString(), HttpContext);
        return OkData(ToDictionaryItem(item));
    }

    [HttpDelete("categories/{code}/items/{itemId:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteItem(string code, Guid itemId)
    {
        var category = await FindCategoryAsync(code);
        if (category is null)
        {
            return Fail(2301, "Dictionary category not found.", StatusCodes.Status404NotFound);
        }

        var item = await db.DictionaryItems.FirstOrDefaultAsync(x => x.Id == itemId && x.CategoryId == category.Id);
        if (item is null)
        {
            return Fail(2306, "Dictionary item not found.", StatusCodes.Status404NotFound);
        }

        db.DictionaryItems.Remove(item);
        await db.SaveChangesAsync();
        await auditService.WriteAsync(CurrentUserId, "Dictionary", "DeleteItem", item.Id.ToString(), HttpContext);
        return OkData(new { item.Id, item.Code });
    }

    private Task<DictionaryCategory?> FindCategoryAsync(string code)
    {
        var normalizedCode = NormalizeCode(code);
        return db.DictionaryCategories.FirstOrDefaultAsync(x => x.Code == normalizedCode);
    }

    private static object ToCategoryItem(DictionaryCategory category) => new
    {
        category.Id,
        category.Code,
        category.Name,
        category.Description,
        category.IsEnabled,
        ItemCount = category.Items.Count,
        category.CreatedAt,
        category.UpdatedAt
    };

    private static object ToCategoryDetail(DictionaryCategory category, bool includeDisabled) => new
    {
        category.Id,
        category.Code,
        category.Name,
        category.Description,
        category.IsEnabled,
        Items = category.Items
            .Where(x => includeDisabled || x.IsEnabled)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Label)
            .Select(ToDictionaryItem),
        category.CreatedAt,
        category.UpdatedAt
    };

    private static object ToDictionaryItem(DictionaryItem item) => new
    {
        item.Id,
        item.CategoryId,
        item.Code,
        item.Label,
        item.Value,
        item.Description,
        item.SortOrder,
        item.IsEnabled,
        item.CreatedAt,
        item.UpdatedAt
    };

    private static bool TryNormalizeCategory(
        DictionaryCategoryRequest request,
        out string code,
        out string name,
        out string error)
    {
        code = NormalizeCode(request.Code);
        name = request.Name?.Trim() ?? string.Empty;
        if (!IsValidCode(code))
        {
            error = "Category code must be 1-64 characters and contain only letters, digits, underscores, or hyphens.";
            return false;
        }

        if (name.Length is < 1 or > 120)
        {
            error = "Category name must be 1-120 characters.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryNormalizeItem(
        DictionaryItemRequest request,
        out string code,
        out string label,
        out string value,
        out string error)
    {
        code = NormalizeCode(request.Code);
        label = request.Label?.Trim() ?? string.Empty;
        value = request.Value?.Trim() ?? string.Empty;
        if (!IsValidCode(code))
        {
            error = "Item code must be 1-64 characters and contain only letters, digits, underscores, or hyphens.";
            return false;
        }

        if (label.Length is < 1 or > 160 || value.Length is < 1 or > 500)
        {
            error = "Item label must be 1-160 characters and value must be 1-500 characters.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string NormalizeCode(string? value) => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool IsValidCode(string code) =>
        code.Length is >= 1 and <= 64 && code.All(x => char.IsLetterOrDigit(x) || x is '_' or '-');

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
