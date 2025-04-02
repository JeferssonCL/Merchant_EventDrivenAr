

using System.Diagnostics;
using Commons.ResponseHandler.Handler.Interfaces;
using Commons.ResponseHandler.Responses.Bases;
using InventoryService.Application.Dtos;
using InventoryService.Application.Dtos.Products;
using InventoryService.Application.QueryCommands.Products.Queries.Queries;
using InventoryService.Application.Services;
using InventoryService.Application.Services.CacheService.Interfaces;
using InventoryService.Intraestructure.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.QueryCommands.Products.Queries.QueryHandlers;
public class GetProductsByStoreIdQueryHandler(
    IProductRepository productRepository,
    ProductService productService,
    IRedisCacheService cacheService, 
    ILogger<ProductService> logger,
    IResponseHandlingHelper responseHandlingHelper) : IRequestHandler<GetProductsByStoreIdQuery, BaseResponse>
{

    public async Task<BaseResponse> Handle(GetProductsByStoreIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"Products_Store_{request.StoreId}";
        var stopwatch = Stopwatch.StartNew();
        var cachedProducts = await cacheService.GetAsync<PaginatedResponseDto<ProductDto>>(cacheKey);
        
        if (cachedProducts != null)
        {
            stopwatch.Stop();
            logger.LogInformation("Source: Cache, TimeToGet: {ElapsedTime} ms", stopwatch.ElapsedMilliseconds);
            return responseHandlingHelper.Ok($"Products obtained from cache. Time: {stopwatch.ElapsedMilliseconds} ms.", cachedProducts);
        }
        
        var products = await productRepository.GetProductsByStoreId(request.StoreId, request.Page, request.PageSize, request.QueryParams);

        var productsDto = products.Select(e =>
            productService.GetProductDtoByProduct(e, [])).ToList();

        var totalItems = await productRepository.GetCountProductsByStoreId(request.StoreId, request.QueryParams);

        var productsToDisplay = new PaginatedResponseDto<ProductDto>(productsDto, totalItems, request.Page, request.PageSize);

        await cacheService.SetAsync(cacheKey, productsToDisplay, TimeSpan.FromMinutes(60));
        logger.LogInformation("Source: Database, TimeToGet: {ElapsedTime} ms", stopwatch.ElapsedMilliseconds);
        return responseHandlingHelper.Ok($"Products have been successfully obtained from database. Time: {stopwatch.ElapsedMilliseconds} ms.", productsToDisplay);
    }
}
