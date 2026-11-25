using ShopEx.Application.Services.ShopEx;
using ShopEx.Domain.Enum;

namespace ShopEx.Tests;

public class SupplierInvoiceMathTests
{
    private static readonly (decimal, decimal)[] Lines = { (2m, 50m), (1m, 100m) }; // subtotal 200

    [Fact]
    public void No_discount()
    {
        var t = SupplierInvoiceService.CalculateTotals(Lines, SupplierDiscountType.None, null);

        Assert.Equal(200m, t.SubTotal);
        Assert.Equal(0m, t.DiscountAmount);
        Assert.Equal(200m, t.Total);
        Assert.Equal(SupplierDiscountType.None, t.DiscountType);
    }

    [Fact]
    public void Percent_discount_is_resolved_to_an_amount()
    {
        var t = SupplierInvoiceService.CalculateTotals(Lines, SupplierDiscountType.Percent, 12.5m);

        Assert.Equal(25m, t.DiscountAmount);
        Assert.Equal(175m, t.Total);
        Assert.Equal(12.5m, t.DiscountValue);
    }

    [Fact]
    public void Fixed_discount()
    {
        var t = SupplierInvoiceService.CalculateTotals(Lines, SupplierDiscountType.Amount, 30m);

        Assert.Equal(30m, t.DiscountAmount);
        Assert.Equal(170m, t.Total);
    }

    [Fact]
    public void Zero_value_means_no_discount_whatever_the_type()
    {
        var t = SupplierInvoiceService.CalculateTotals(Lines, SupplierDiscountType.Percent, 0m);

        Assert.Equal(SupplierDiscountType.None, t.DiscountType);
        Assert.Equal(200m, t.Total);
    }

    [Theory]
    [InlineData(SupplierDiscountType.Percent, 100.01)]
    [InlineData(SupplierDiscountType.Amount, 200.01)]
    public void Discount_above_limit_is_rejected(SupplierDiscountType type, double value)
    {
        Assert.Throws<ArgumentException>(() =>
            SupplierInvoiceService.CalculateTotals(Lines, type, (decimal)value));
    }

    [Fact]
    public void Average_cost_weights_old_and_new_stock()
    {
        // 10 @ 20 + 30 @ 40 => 1400 / 40 = 35
        Assert.Equal(35m, SupplierInvoiceService.WeightedAverageCost(10m, 20m, 30m, 40m));
    }

    [Fact]
    public void Average_cost_of_empty_stock_is_the_new_cost()
    {
        Assert.Equal(40m, SupplierInvoiceService.WeightedAverageCost(0m, 99m, 5m, 40m));
    }

    [Fact]
    public void Negative_stock_does_not_inflate_the_average_cost()
    {
        // 5 units were sold before they were received: the new cost must stay 40, not 10*40/5 = 80
        Assert.Equal(40m, SupplierInvoiceService.WeightedAverageCost(-5m, 30m, 10m, 40m));
    }
}
