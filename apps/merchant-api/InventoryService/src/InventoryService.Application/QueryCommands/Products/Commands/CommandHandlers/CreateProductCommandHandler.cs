using Commons.ResponseHandler.Handler.Interfaces;
using Commons.ResponseHandler.Responses.Bases;
using FluentValidation;
using InventoryService.Application.Dtos.Categories;
using InventoryService.Application.Dtos.Products;
using InventoryService.Application.Dtos.ProductVariants;
using InventoryService.Application.QueryCommands.Products.Commands.Commands;
using InventoryService.Application.Services;
using InventoryService.Application.Services.CacheService.Interfaces;
using InventoryService.Domain.Concretes;
using InventoryService.Intraestructure.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.QueryCommands.Products.Commands.CommandHandlers;

public class CreateProductCommandHandler(
    IValidator<CreateProductDto> validator,
    IProductRepository productRepository,
    IRepository<Category> categoryRepository,
    IRepository<Image> imageRepository,
    ProductVariantService productVariantService,
    IResponseHandlingHelper responseHandlingHelper,
    IRedisCacheService cacheService, 
    ILogger<ProductService> logger) :
    IRequestHandler<CreateProductCommand, BaseResponse>
{
    public async Task<BaseResponse> Handle(CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var productDto = request.ProductDto;
        var response = await validator.ValidateAsync(productDto, cancellationToken);
        if (!response.IsValid) return responseHandlingHelper.BadRequest<CreateProductVariantDto>(
            "The operation to create a product was not completed, please check the errors.", 
            response.Errors.Select(e => e.ErrorMessage).ToList());

        ICollection<Category> categories = new List<Category>();
        foreach (var categoryId in productDto.CategoryIds)
        {
            var categoryToAdd = await categoryRepository.GetByIdAsync(categoryId);
            if (categoryToAdd == null) return responseHandlingHelper.NotFound<CategoryDto>(
                "The category with the follow id " + categoryId + " was not found");
            categories.Add(categoryToAdd);
        }
        
        var productToAdd = await productRepository.AddAsync(new Product
        {
            StoreId = productDto.StoreId,
            Name = productDto.Name,
            Description = productDto.Description,
            BasePrice = productDto.Price,
            Brand = productDto.Brand,
            Categories = categories,
            LowStockThreshold = productDto.LowStockThreshold
        });
        
        await cacheService.RemoveAsync("Products");

        foreach (var imageDto in productDto.Images)
        {
            await imageRepository.AddAsync(new Image { ProductId = productToAdd.Id, Url = imageDto.Url, AltText = imageDto.AltText });
        }

        foreach (var productVariantDto in productDto.ProductVariants)
        {
            var createProductVariantDto = new CreateProductVariantDto
            {
                ProductId = productToAdd.Id,
                Image = productVariantDto.Image,
                PriceAdjustment = productVariantDto.PriceAdjustment,
                StockQuantity = productVariantDto.StockQuantity,
                Attributes = productVariantDto.Attributes
            };
            
            await productVariantService.CreateProductVariant(createProductVariantDto, productToAdd.Id);
        }

        return responseHandlingHelper.Created($"The product was added successfully.", productToAdd.Id);
    }
}