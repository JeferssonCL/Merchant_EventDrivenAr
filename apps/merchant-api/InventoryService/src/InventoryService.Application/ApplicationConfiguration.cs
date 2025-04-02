using Commons.ResponseHandler.Handler.Concretes;
using Commons.ResponseHandler.Handler.Interfaces;
using FluentValidation;
using InventoryService.Application.Dtos.Categories;
using InventoryService.Application.Dtos.Images;
using InventoryService.Application.Dtos.Products;
using InventoryService.Application.Dtos.ProductVariants;
using InventoryService.Application.Dtos.Variants;
using InventoryService.Application.Services;
using InventoryService.Application.Services.CacheService.Concretes;
using InventoryService.Application.Services.CacheService.Interfaces;
using InventoryService.Application.Validators.Categories;
using InventoryService.Application.Validators.Images;
using InventoryService.Application.Validators.Products;
using InventoryService.Application.Validators.ProductVariants;
using InventoryService.Application.Validators.Variants;
using InventoryService.Domain.Concretes;
using InventoryService.Intraestructure.Repositories.Concretes;
using InventoryService.Intraestructure.Repositories.Interfaces;
using InventoryService.Intraestructure.Repositories.Utils;
using Microsoft.Extensions.DependencyInjection;


namespace InventoryService.Application;

public static class ApplicationConfiguration
{
    public static void AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(typeof(CreateImageDto).Assembly)
        );
        
        services.AddScoped<HttpClient>();
        services.AddScoped<IRepository<Category>, CategoryRepository>();
        services.AddScoped<IRepository<Image>, ImageRepository>();
        services.AddScoped<IRepository<ProductAttribute>, ProductAttributeRepository>();
        services.AddScoped<IRepository<ProductVariant>, ProductVariantRepository>();
        services.AddScoped<IRepository<Variant>, VariantRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWishListRepository, WishListRepository>();
        
        services.AddTransient<ProductVariantService>();
        services.AddTransient<HttpStockThresholdService>();
        services.AddTransient<ProductService>();
        services.AddTransient<ProductFiltersManager>();
        services.AddTransient<StockThresholdNoticationService>();
        services.AddScoped<IResponseHandlingHelper, ResponseHandlingHelper>();
        services.AddSingleton<IRedisCacheService, RedisCacheService>();
        
        services.AddScoped<IValidator<CreateCategoryDto>, CreateCategoryValidator>();
        services.AddScoped<IValidator<UpdateCategoryDto>, UpdateCategoryValidator>();
        services.AddScoped<IValidator<CreateImageDto>, CreateImageValidator>();
        services.AddScoped<IValidator<UpdateImageDto>, UpdateImageValidator>();
        services.AddScoped<IValidator<CreateVariantDto>, CreateVariantValidator>();
        services.AddScoped<IValidator<UpdateVariantDto>, UpdateVariantValidator>();
        services.AddScoped<IValidator<CreateProductVariantDto>, CreateProductVariantValidator>();
        services.AddScoped<IValidator<UpdateProductVariantDto>, UpdateProductVariantValidator>();
        services.AddScoped<IValidator<CreateProductDto>, CreateProductValidator>();
        services.AddScoped<IValidator<UpdateProductDto>, UpdateProductValidator>();
    }
}