using Commons.ResponseHandler.Handler.Interfaces;
using Commons.ResponseHandler.Responses.Bases;
using InventoryService.Application.QueryCommands.Products.Commands.Commands;
using InventoryService.Application.Services;
using InventoryService.Application.Services.CacheService.Interfaces;
using InventoryService.Domain.Concretes;
using InventoryService.Intraestructure.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.QueryCommands.Products.Commands.CommandHandlers;

public class UpdateProductThresholdCommandHandler(
    IProductRepository productRepository,
    IResponseHandlingHelper responseHandlingHelper,
    IRedisCacheService cacheService, 
    ILogger<ProductService> logger
    ) : IRequestHandler<UpdateProducrThresholdCommand, BaseResponse>
{
    public async Task<BaseResponse> Handle(UpdateProducrThresholdCommand request, CancellationToken cancellationToken)
    {
        Product? product = await productRepository.GetByIdAsync(request.ProductId);
        if (product == null) return responseHandlingHelper.NotFound<Product>("The product with the follow id " + request.ProductId + " was not found");

        product.LowStockThreshold = request.Threshold;
        await productRepository.UpdateAsync(product);
        await cacheService.RemoveAsync("Products");
        await cacheService.RemoveAsync($"Product_{request.ProductId}");
        return responseHandlingHelper.Ok("The product threshold has been successfully updated.", true);
    }
}
