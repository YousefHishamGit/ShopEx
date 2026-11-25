using NSubstitute;
using ShopEx.Application.DTOs.ShopEx;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.Services.ShopEx;
using ShopEx.Domain.RMSEntity;

namespace ShopEx.Tests;

public class SupplierInvoiceServiceTests
{
    private readonly ISupplierInvoiceRepository _invoices = Substitute.For<ISupplierInvoiceRepository>();
    private readonly ISupplierRepository _suppliers = Substitute.For<ISupplierRepository>();
    private readonly IItemRepository _items = Substitute.For<IItemRepository>();
    private readonly SupplierInvoiceService _service;

    public SupplierInvoiceServiceTests()
    {
        _suppliers.GetByIdAsync(1).Returns(new Supplier { ID = 1, SupplierName = "مورد" });
        _service = new SupplierInvoiceService(
            _invoices, Substitute.For<IUnitOfWork>(), _suppliers,
            Substitute.For<IWarehouseRepository>(), Substitute.For<IStockRepository>(), _items);
    }

    private static CreateSupplierInvoiceRequest Request(params int[] itemIds) => new()
    {
        RMSSupplierID = 1,
        InvoiceNumber = "INV-1",
        Items = itemIds.Select(id => new CreateSupplierInvoiceItemRequest { RMSItemID = id, Quantity = 1m, UnitPrice = 10m }).ToList()
    };

    [Fact]
    public async Task Invoice_with_an_item_missing_from_RMS_is_rejected()
    {
        _items.GetByIdsAsync(Arg.Any<IEnumerable<int>>())
              .Returns(new Dictionary<int, Item> { [10] = new Item { ID = 10 } });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(Request(10, 999)));

        Assert.Contains("999", ex.Message);
        await _invoices.DidNotReceive().AddAsync(Arg.Any<ShopEx.Domain.Entity.SupplierInvoice>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_item_id_is_rejected_before_any_lookup()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(Request(0)));
        await _items.DidNotReceive().GetByIdsAsync(Arg.Any<IEnumerable<int>>());
    }
}
