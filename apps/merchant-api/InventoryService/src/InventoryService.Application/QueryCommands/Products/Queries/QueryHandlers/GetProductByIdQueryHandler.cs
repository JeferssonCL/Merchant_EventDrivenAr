using System.Diagnostics;
using Commons.ResponseHandler.Handler.Interfaces;
using Commons.ResponseHandler.Responses.Bases;
using InventoryService.Application.Dtos.Products;
using InventoryService.Application.QueryCommands.Products.Queries.Queries;
using InventoryService.Application.Services;
using InventoryService.Application.Services.CacheService.Interfaces;
using InventoryService.Intraestructure.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.QueryCommands.Products.Queries.QueryHandlers;

public class GetProductByIdQueryHandler(
    IProductRepository productRepository, 
    ProductService productService, 
    IResponseHandlingHelper responseHandlingHelper,
    IRedisCacheService cacheService,    
    ILogger<ProductService> logger)
    : IRequestHandler<GetProductByIdQuery, BaseResponse>
{
    public async Task<BaseResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"Product_{request.Id}";
        var stopwatch = Stopwatch.StartNew();
        var cachedProduct = await cacheService.GetAsync<ProductDto>(cacheKey);
        if (cachedProduct != null)
        {
            stopwatch.Stop();
            logger.LogInformation("Source: Cache, TimeToGet: {ElapsedTime} ms", stopwatch.ElapsedMilliseconds);
            return responseHandlingHelper.Ok("Product obtained from cache.", cachedProduct);
        }
        
        var existingProduct = await productRepository.GetByIdAsync(request.Id);
        stopwatch.Stop();

        if (existingProduct == null)
        {
            logger.LogInformation("Source: Database, TimeToGet: {ElapsedTime} ms", stopwatch.ElapsedMilliseconds);
            return responseHandlingHelper.NotFound<ProductDto>("The product with the follow id " + request.Id +
                                                               " was not found");
        }
        
        var productDto = productService.GetProductDtoByProduct(existingProduct, []);
        await cacheService.SetAsync(cacheKey, productDto, TimeSpan.FromMinutes(60));
        
        logger.LogInformation("Source: Database, TimeToGet: {ElapsedTime} ms", stopwatch.ElapsedMilliseconds);
        return responseHandlingHelper.Ok("The product has been successfully obtained.", productDto);
    }
}