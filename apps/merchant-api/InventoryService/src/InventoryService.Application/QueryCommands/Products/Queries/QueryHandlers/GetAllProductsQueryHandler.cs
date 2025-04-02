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

public class GetAllProductsQueryHandler(IProductRepository productRepository, 
    IWishListRepository wishListRepository,
    ProductService productService,
    IRedisCacheService cacheService, 
    ILogger<ProductService> logger,
    IResponseHandlingHelper responseHandlingHelper)
    : IRequestHandler<GetAllProductsQuery, BaseResponse>
{
    private const string CacheKey = "Products";

    public async Task<BaseResponse> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cachedProducts = await cacheService.GetAsync<PaginatedResponseDto<ProductDto>>(CacheKey);
        
        if (cachedProducts != null)
        {
            logger.LogInformation("Source: Cache, TimeToGet: {ElapsedTime} ms", stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
            return responseHandlingHelper.Ok($"Products have been successfully obtained from cache. Time: {stopwatch.ElapsedMilliseconds} ms.", cachedProducts);
        }
        
        var productsLikedIds = new List<Guid>();
        if (request.UserId != null)
        {
            var totalCount = await wishListRepository.GetWishListCountByUserId((Guid)request.UserId);
            var productsLiked = await wishListRepository.GetWishListByUserId((Guid)request.UserId,  1,totalCount);
            productsLikedIds = productsLiked.Select(w => w.ProductId).ToList();
        }
        
        var totalProducts = await productRepository.GetAllAsync(request.Page, request.PageSize);
        stopwatch.Stop();
        var totalProductsDto = totalProducts.Select(e => 
            productService.GetProductDtoByProduct(e, productsLikedIds)).ToList();
        var totalItems = await productRepository.GetCountAsync();

        var productsToDisplay = new PaginatedResponseDto<ProductDto>(totalProductsDto, totalItems, request.Page, request.PageSize);
        await cacheService.SetAsync(CacheKey, productsToDisplay, TimeSpan.FromMinutes(60));

        return responseHandlingHelper.Ok($"Products have been successfully obtained from database. Time: {stopwatch.ElapsedMilliseconds} ms.", productsToDisplay);    
    }
}