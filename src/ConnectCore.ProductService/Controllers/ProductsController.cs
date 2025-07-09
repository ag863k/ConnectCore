using Microsoft.AspNetCore.Mvc;
using ConnectCore.ProductService.Services;
using ConnectCore.Shared.DTOs;

namespace ConnectCore.ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductDto>>> GetProducts(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? searchTerm = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _productService.GetProductsAsync(pageNumber, pageSize, category, searchTerm);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id)
    {
        var result = await _productService.GetProductByIdAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("by-sku/{sku}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductBySku(string sku)
    {
        var result = await _productService.GetProductBySkuAsync(sku);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto createProductDto)
    {
        var result = await _productService.CreateProductAsync(createProductDto);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetProduct), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid id, [FromBody] UpdateProductDto updateProductDto)
    {
        var result = await _productService.UpdateProductAsync(id, updateProductDto);
        
        if (!result.Success)
        {
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(Guid id)
    {
        var result = await _productService.DeleteProductAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:guid}/stock")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        var result = await _productService.UpdateStockAsync(id, request.Quantity);
        
        if (!result.Success)
        {
            return result.Message!.Contains("not found") ? NotFound(result) : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}/stock-availability")]
    public async Task<ActionResult<ApiResponse<bool>>> CheckStockAvailability(Guid id, [FromQuery] int quantity)
    {
        var result = await _productService.CheckStockAvailabilityAsync(id, quantity);
        
        if (!result.Success && result.Message!.Contains("not found"))
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetCategories()
    {
        var result = await _productService.GetCategoriesAsync();
        return Ok(result);
    }
}

public record UpdateStockRequest(int Quantity);
