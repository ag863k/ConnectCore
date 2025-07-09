using ConnectCore.Shared.Models;
using ConnectCore.Shared.DTOs;
using ConnectCore.ProductService.Data;
using Microsoft.EntityFrameworkCore;

namespace ConnectCore.ProductService.Services;

public interface IProductService
{
    Task<PagedResponse<ProductDto>> GetProductsAsync(int pageNumber = 1, int pageSize = 10, string? category = null, string? searchTerm = null);
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(Guid id);
    Task<ApiResponse<ProductDto>> GetProductBySkuAsync(string sku);
    Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductDto createProductDto);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto);
    Task<ApiResponse<bool>> DeleteProductAsync(Guid id);
    Task<ApiResponse<bool>> UpdateStockAsync(Guid id, int quantity);
    Task<ApiResponse<List<string>>> GetCategoriesAsync();
    Task<ApiResponse<bool>> CheckStockAvailabilityAsync(Guid id, int requestedQuantity);
}

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResponse<ProductDto>> GetProductsAsync(int pageNumber = 1, int pageSize = 10, string? category = null, string? searchTerm = null)
    {
        try
        {
            var query = _context.Products.Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => MapToDto(p))
                .ToListAsync();

            return PagedResponse<ProductDto>.Create(products, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return new PagedResponse<ProductDto>
            {
                Success = false,
                Message = "Error retrieving products"
            };
        }
    }

    public async Task<ApiResponse<ProductDto>> GetProductByIdAsync(Guid id)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null)
            {
                return ApiResponse<ProductDto>.FailureResult("Product not found");
            }

            return ApiResponse<ProductDto>.SuccessResult(MapToDto(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by ID {ProductId}", id);
            return ApiResponse<ProductDto>.FailureResult("Error retrieving product");
        }
    }

    public async Task<ApiResponse<ProductDto>> GetProductBySkuAsync(string sku)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku && !p.IsDeleted);
            if (product == null)
            {
                return ApiResponse<ProductDto>.FailureResult("Product not found");
            }

            return ApiResponse<ProductDto>.SuccessResult(MapToDto(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by SKU {SKU}", sku);
            return ApiResponse<ProductDto>.FailureResult("Error retrieving product");
        }
    }

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductDto createProductDto)
    {
        try
        {
            var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.SKU == createProductDto.SKU);
            if (existingProduct != null)
            {
                return ApiResponse<ProductDto>.FailureResult("Product with this SKU already exists");
            }

            var product = new Product
            {
                Name = createProductDto.Name,
                Description = createProductDto.Description,
                Price = createProductDto.Price,
                Category = createProductDto.Category,
                StockQuantity = createProductDto.StockQuantity,
                SKU = createProductDto.SKU,
                Status = ProductStatus.Available,
                CreatedBy = "API"
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created successfully with ID {ProductId}", product.Id);
            return ApiResponse<ProductDto>.SuccessResult(MapToDto(product), "Product created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return ApiResponse<ProductDto>.FailureResult("Error creating product");
        }
    }

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null)
            {
                return ApiResponse<ProductDto>.FailureResult("Product not found");
            }

            if (!string.IsNullOrEmpty(updateProductDto.Name))
                product.Name = updateProductDto.Name;

            if (!string.IsNullOrEmpty(updateProductDto.Description))
                product.Description = updateProductDto.Description;

            if (updateProductDto.Price.HasValue)
                product.Price = updateProductDto.Price.Value;

            if (!string.IsNullOrEmpty(updateProductDto.Category))
                product.Category = updateProductDto.Category;

            if (updateProductDto.StockQuantity.HasValue)
                product.StockQuantity = updateProductDto.StockQuantity.Value;

            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Product updated successfully with ID {ProductId}", id);
            return ApiResponse<ProductDto>.SuccessResult(MapToDto(product), "Product updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return ApiResponse<ProductDto>.FailureResult("Error updating product");
        }
    }

    public async Task<ApiResponse<bool>> DeleteProductAsync(Guid id)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null)
            {
                return ApiResponse<bool>.FailureResult("Product not found");
            }

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = "API";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Product deleted successfully with ID {ProductId}", id);
            return ApiResponse<bool>.SuccessResult(true, "Product deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return ApiResponse<bool>.FailureResult("Error deleting product");
        }
    }

    public async Task<ApiResponse<bool>> UpdateStockAsync(Guid id, int quantity)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null)
            {
                return ApiResponse<bool>.FailureResult("Product not found");
            }

            if (product.StockQuantity + quantity < 0)
            {
                return ApiResponse<bool>.FailureResult("Insufficient stock");
            }

            product.StockQuantity += quantity;
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = "API";

            product.Status = product.StockQuantity == 0 ? ProductStatus.OutOfStock : ProductStatus.Available;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Product stock updated successfully for ID {ProductId}, new quantity: {Quantity}", id, product.StockQuantity);
            return ApiResponse<bool>.SuccessResult(true, $"Stock updated successfully. New quantity: {product.StockQuantity}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
            return ApiResponse<bool>.FailureResult("Error updating stock");
        }
    }

    public async Task<ApiResponse<List<string>>> GetCategoriesAsync()
    {
        try
        {
            var categories = await _context.Products
                .Where(p => !p.IsDeleted)
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return ApiResponse<List<string>>.SuccessResult(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return ApiResponse<List<string>>.FailureResult("Error retrieving categories");
        }
    }

    public async Task<ApiResponse<bool>> CheckStockAvailabilityAsync(Guid id, int requestedQuantity)
    {
        try
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null)
            {
                return ApiResponse<bool>.FailureResult("Product not found");
            }

            var isAvailable = product.StockQuantity >= requestedQuantity && product.Status == ProductStatus.Available;
            var message = isAvailable ? "Stock is available" : $"Insufficient stock. Available: {product.StockQuantity}, Requested: {requestedQuantity}";

            return ApiResponse<bool>.SuccessResult(isAvailable, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock availability for product {ProductId}", id);
            return ApiResponse<bool>.FailureResult("Error checking stock availability");
        }
    }

    private static ProductDto MapToDto(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Category = product.Category,
            StockQuantity = product.StockQuantity,
            SKU = product.SKU,
            Status = product.Status.ToString(),
            CreatedAt = product.CreatedAt
        };
    }
}
