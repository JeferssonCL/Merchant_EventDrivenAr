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

public class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IResponseHandlingHelper responseHandlingHelper,
    IRedisCacheService cacheService, 
    ILogger<ProductService> logger)
    : IRequestHandler<DeleteProductCommand, BaseResponse>
{
    public async Task<BaseResponse> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(request.Id);
        if (product == null) return responseHandlingHelper.NotFound<Category>($"The product with the follow id '{request.Id}' was not found.");
        var response = await productRepository.DeleteAsync(request.Id);
        
        await cacheService.RemoveAsync("Products");
        await cacheService.RemoveAsync($"Product_{request.Id}");
        return responseHandlingHelper.Ok("The product has been successfully deleted.", response); 
    }
}