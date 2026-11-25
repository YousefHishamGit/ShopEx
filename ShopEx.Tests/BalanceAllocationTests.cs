using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.Services.ShopEx;

namespace ShopEx.Tests;

/// <summary>Closing a branch balance is split over its open charges, oldest first.</summary>
public class BalanceAllocationTests
{
    private static List<OpenChargeRow> Charges(params (int Id, decimal Remaining)[] rows)
        => rows.Select(r => new OpenChargeRow { Id = r.Id, Remaining = r.Remaining }).ToList();

    [Fact]
    public void Takes_from_oldest_charge_first()
    {
        var result = BalanceService.AllocateFifo(Charges((1, 1000m), (2, 1500m)), 1200m, 0m);

        Assert.Equal(new[] { (1, 1000m, 0m), (2, 200m, 0m) }, result);
    }

    [Fact]
    public void Single_charge_gets_all_the_profit()
    {
        var result = BalanceService.AllocateFifo(Charges((7, 500m), (8, 500m)), 300m, 25m);

        Assert.Equal(new[] { (7, 300m, 25m) }, result);
    }

    [Fact]
    public void Profit_is_split_by_amount_and_adds_up_exactly()
    {
        // 1/3 and 2/3 of 10.00 don't round evenly: the last charge takes the rounding leftover
        var result = BalanceService.AllocateFifo(Charges((1, 100m), (2, 200m)), 300m, 10m);

        Assert.Equal(2, result.Count);
        Assert.Equal(3.33m, result[0].Profit);
        Assert.Equal(6.67m, result[1].Profit);
        Assert.Equal(10m, result.Sum(r => r.Profit));
        Assert.Equal(300m, result.Sum(r => r.Amount));
    }

    [Fact]
    public void Exact_total_closes_every_charge()
    {
        var result = BalanceService.AllocateFifo(Charges((1, 100m), (2, 50.5m)), 150.5m, 0m);

        Assert.Equal(150.5m, result.Sum(r => r.Amount));
        Assert.All(result, r => Assert.True(r.Amount > 0));
    }

    [Fact]
    public void More_than_available_is_rejected()
    {
        Assert.Throws<InvalidOperationException>(() =>
            BalanceService.AllocateFifo(Charges((1, 100m)), 100.01m, 0m));
    }

    [Fact]
    public void Empty_charges_are_skipped()
    {
        var result = BalanceService.AllocateFifo(Charges((1, 0m), (2, 40m)), 40m, 4m);

        Assert.Equal(new[] { (2, 40m, 4m) }, result);
    }
}
