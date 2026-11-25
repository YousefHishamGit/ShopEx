using NSubstitute;
using ShopEx.Application.DTOs.SupplierReturn;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.Services.ShopEx;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;

namespace ShopEx.Tests;

public class SupplierReturnServiceTests
{
    private readonly ISupplierReturnRepository _returns = Substitute.For<ISupplierReturnRepository>();
    private readonly IStockRepository _stock = Substitute.For<IStockRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly SupplierReturnService _service;

    public SupplierReturnServiceTests()
    {
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _service = new SupplierReturnService(
            _returns, Substitute.For<IWarehouseRepository>(), _stock, _uow,
            Substitute.For<ISupplierRepository>(), Substitute.For<ISupplierInvoiceRepository>());
    }

    /// <summary>A return of 2 x 50 = 100, already confirmed, with <paramref name="refunded"/> paid back.</summary>
    private SupplierReturn Return(SupplierReturnStatus status, decimal refunded = 0m)
    {
        var r = new SupplierReturn { ID = 9, ReturnNumber = "RET-1", WarehouseID = 1, Status = status };
        r.Items.Add(new SupplierReturnItem { RMSItemID = 100, Quantity = 2m, UnitPrice = 50m });
        if (refunded > 0) r.Refunds.Add(new SupplierRefund { Amount = refunded });
        _returns.GetByIdAsync(9, Arg.Any<CancellationToken>()).Returns(r);
        return r;
    }

    [Fact]
    public async Task Refund_takes_the_return_lock()
    {
        Return(SupplierReturnStatus.Confirmed);

        await _service.AddRefundAsync(9, new AddSupplierRefundRequest { Amount = 40m });

        await _uow.Received(1).AcquireLockAsync("SupplierReturn:9", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Second_refund_beyond_what_is_left_is_rejected()
    {
        // First click already refunded the full 100: the second one re-reads it under the lock and fails
        Return(SupplierReturnStatus.Confirmed, refunded: 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddRefundAsync(9, new AddSupplierRefundRequest { Amount = 100m }));
        await _returns.DidNotReceive().AddRefundAsync(Arg.Any<SupplierRefund>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirming_twice_is_rejected_and_stock_is_not_touched_again()
    {
        Return(SupplierReturnStatus.Confirmed);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ConfirmReturnAsync(9));
        await _stock.DidNotReceive().GetBalanceAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refund_on_a_draft_confirms_it_first()
    {
        var r = Return(SupplierReturnStatus.Draft);
        var balance = new StockBalance { WarehouseID = 1, RMSItemID = 100, QuantityOnHand = 5m };
        _stock.GetBalanceAsync(1, 100, Arg.Any<CancellationToken>()).Returns(balance);

        var result = await _service.AddRefundAsync(9, new AddSupplierRefundRequest { Amount = 100m });

        Assert.Equal(SupplierReturnStatus.Confirmed, r.Status);
        Assert.Equal(3m, balance.QuantityOnHand);
        Assert.Equal(100m, result.RefundedAmount);
    }

    [Fact]
    public async Task Cancel_only_works_on_a_draft()
    {
        Return(SupplierReturnStatus.Confirmed);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelReturnAsync(9));
    }
}
