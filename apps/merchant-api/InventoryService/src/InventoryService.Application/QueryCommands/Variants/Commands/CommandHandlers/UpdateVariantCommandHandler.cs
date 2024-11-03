using InventoryService.Application.Dtos.Images;
using InventoryService.Application.Dtos.Variants;
using InventoryService.Application.QueryCommands.Images.Commands.Commands;
using InventoryService.Application.QueryCommands.Variants.Commands.Commands;
using InventoryService.Domain.Concretes;
using InventoryService.Intraestructure.Repositories.Interfaces;
using MediatR;

namespace InventoryService.Application.QueryCommands.Variants.Commands.CommandHandlers;

public class UpdateVariantCommandHandler(IRepository<Variant> variantRepository) : IRequestHandler<UpdateVariantCommand, VariantDto>
{
    public async Task<VariantDto> Handle(UpdateVariantCommand request, CancellationToken cancellationToken)
    {
        var variantDto = request.Variant;
        var variantToUpdate = await variantRepository.GetByIdAsync(variantDto.Id);
        if (variantToUpdate == null) throw new ArgumentException("The requested variant was not found.");

        variantToUpdate.Name = variantDto.Name ?? variantToUpdate.Name;
        variantToUpdate.IsActive = variantDto.IsActive ?? variantToUpdate.IsActive;

        await variantRepository.UpdateAsync(variantToUpdate);
        return new VariantDto
        {
            Id = variantToUpdate.Id,
            Name = variantToUpdate.Name,
            IsActive = variantToUpdate.IsActive,
            values = variantToUpdate.ProductAttributes.Select(attribute => new ValueDto
            {
                Id = attribute.Id,
                Name = attribute.Value
            }).ToList()
        };
    }
}