using System.Diagnostics;
using AutoMapper;
using Commons.ResponseHandler.Handler.Interfaces;
using Commons.ResponseHandler.Responses.Bases;
using FluentValidation;
using InventoryService.Application.Comparers.Image;
using InventoryService.Application.Comparers.Variant;
using InventoryService.Application.Dtos.Categories;
using InventoryService.Application.Dtos.Images;
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

public class UpdateProductCommandHandler(
    IValidator<UpdateProductDto> validator,
    IProductRepository productRepository,
    IRepository<Category> categoryRepository,
    IRepository<Image> imageRepository,
    IRepository<ProductVariant> productVariantRepository,
    ProductVariantService productVariantService, 
    IResponseHandlingHelper responseHandlingHelper,
    IMapper mapper,
    IRedisCacheService cacheService, 
    ILogger<ProductService> logger
    ) : 
    IRequestHandler<UpdateProductCommand, BaseResponse>
{
    public async Task<BaseResponse> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var productDto = request.ProductDto;
        var previousStatusProduct = await productRepository.GetByIdAsync(request.Id);

        var response = await validator.ValidateAsync(productDto, cancellationToken);
        if (!response.IsValid) return responseHandlingHelper.BadRequest<UpdateProductDto>(
            "The operation to update a product was not completed, please check the errors",
            response.Errors.Select(e => e.ErrorMessage).ToList());

        ICollection<Category> categories = new List<Category>();
        foreach (var categoryId in productDto.CategoryIds)
        {
            var categoryToAdd = await categoryRepository.GetByIdAsync(categoryId);
            if (categoryToAdd == null) return responseHandlingHelper.NotFound<CategoryDto>(
                "The category with the follow id " + categoryId + " was not found");
            categories.Add(categoryToAdd);
        }
        
        Debug.Assert(previousStatusProduct != null, nameof(previousStatusProduct) + " != null");
        previousStatusProduct.StoreId = productDto.StoreId;
        previousStatusProduct.Name = productDto.Name;
        previousStatusProduct.Description = productDto.Description;
        previousStatusProduct.BasePrice = productDto.Price;
        previousStatusProduct.Brand = productDto.Brand;
        previousStatusProduct.Categories = categories;
        
        await productRepository.UpdateAsync(previousStatusProduct);
        await cacheService.RemoveAsync("Products");
        await cacheService.RemoveAsync($"Product_{request.Id}");

        bool imagesUpdate = await HandleImagesUpdate(
            request.Id,
            previousStatusProduct.Images.Select(mapper.Map<UpdateImageDto>).ToList(), 
            productDto.Images);
        bool variantsUpdate = await HandleProductVariantUpdate(
            request.Id,
            previousStatusProduct.ProductVariants.Select(i 
                => new UpdateProductVariantDto {
                    Id = i.Id,
                    Image = mapper.Map<ProductVariantImageDto>(i.Image),
                    PriceAdjustment = i.PriceAdjustment,
                    StockQuantity = i.StockQuantity,
                    Attributes = i.Attributes.Select(attr 
                        =>
                    {
                        Debug.Assert(attr.Variant != null, "attr.Variant != null");
                        return new ProductVariantAttributeDto
                        {
                            Name = attr.Variant.Name,
                            Value = attr.Value
                        };
                    }).ToList()
                }).ToList(),
            productDto.ProductVariants
            );

        if (!imagesUpdate || !variantsUpdate)
        {
            return responseHandlingHelper.InternalServerError<UpdateProductDto>(
                "Some error happens while try to update information of the product");
        }
        
        return responseHandlingHelper.Ok("Product Updated", request.ProductDto);
    }

    private async Task<bool> HandleImagesUpdate(
        Guid productId,
        ICollection<UpdateImageDto> previousImages,
        ICollection<UpdateImageDto> currentImages)
    {
        try
        {
            var currentImagesDto = currentImages
                .Select(mapper.Map<UpdateImageDto>)
                .ToList();
            var comparer = new ImageEntityComparer();
            var newImages = currentImagesDto.Except(previousImages, comparer);
            var deletedImages = previousImages.Except(currentImagesDto, comparer);
        
            foreach (UpdateImageDto image in newImages)
            {
                if (image.AltText == null || image.Url == null) return false;
                await imageRepository.AddAsync(new Image
                {
                    ProductId = productId,
                    AltText = image.AltText!,
                    Url = image.Url,
                });
            }
        
            foreach (UpdateImageDto image in deletedImages)
            {
                await imageRepository.DeleteAsync(image.Id);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> HandleProductVariantUpdate(
        Guid productId,
        ICollection<UpdateProductVariantDto> previousProductVariants,
        ICollection<UpdateProductVariantDto> currentProductVariants
        )
    {
        try
        {
            var comparer = new UpdateProductVariantDtoComparer();
            var newVariants = currentProductVariants
                .Where(v => !v.Id.HasValue);
            Console.WriteLine(newVariants.ToString());

            var deletedVariants = previousProductVariants
                .Except(currentProductVariants, comparer)
                .Where(v => v.Id != Guid.Empty);

            var updatedVariants = currentProductVariants
                .Intersect(previousProductVariants, comparer)
                .Where(v => v.Id != Guid.Empty);
            
            foreach (UpdateProductVariantDto newVariant in newVariants)
            {
                var productVariant = new CreateProductVariantDto
                {
                    ProductId = productId,
                    Image = newVariant.Image,
                    PriceAdjustment = newVariant.PriceAdjustment ?? 0,
                    StockQuantity = newVariant.StockQuantity ?? 0,
                    Attributes = newVariant.Attributes.ToList() ?? []
                };
                await productVariantService.CreateProductVariant(productVariant, productId);
            }
            
            foreach (UpdateProductVariantDto deletedVariant in deletedVariants)
            {
                if (deletedVariant.Id != null) await productVariantRepository.DeleteAsync(deletedVariant.Id.Value);
            }
            
            foreach (UpdateProductVariantDto updatedVariant in updatedVariants)
            {
                if (updatedVariant.Id != null)
                {
                    var existingProductVariant = await productVariantRepository.GetByIdAsync(updatedVariant.Id.Value);
                    if (existingProductVariant == null) return false;
                    await productVariantService.UpdateProductVariant(updatedVariant, existingProductVariant);
                }
            }

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
