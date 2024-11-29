namespace PaymentService.Application.Dtos.Orders;

public class OrderWithCompleteDetailsDto
{
    public int OrderNumber{ get; set; }
    public string OrderStatus{ get; set; } = string.Empty;
    public DateOnly CreatedOrderDate{ get; set; }
    public double TotalPrice { get; set; }
    public List<OrderItemWithCompletedDetailsDto> OrderItems { get; set; } = [];
}