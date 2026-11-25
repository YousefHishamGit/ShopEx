using NSubstitute;
using ShopEx.Application.DTOs.Maintenance;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.Services.ShopEx;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;

namespace ShopEx.Tests;

public class MaintenanceTicketServiceTests
{
    private readonly IMaintenanceTicketRepository _repo = Substitute.For<IMaintenanceTicketRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IStoreRepository _stores = Substitute.For<IStoreRepository>();
    private readonly MaintenanceTicketService _service;

    public MaintenanceTicketServiceTests()
    {
        // The transaction wrapper just runs the work; locks are no-ops in unit tests
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Func<Task>>()());
        _stores.GetNamesAsync(Arg.Any<CancellationToken>()).Returns(new Dictionary<int, string>());
        _service = new MaintenanceTicketService(_repo, _uow, _stores);
    }

    private MaintenanceTicket Ticket(MaintenanceStatus status, decimal[] operationCosts, decimal paid)
    {
        var ticket = new MaintenanceTicket
        {
            Id = 5,
            TicketNumber = "MT-20261125-0001",
            CustomerName = "عميل",
            CustomerPhone = "0100",
            DeviceName = "جهاز",
            Status = status
        };
        var opId = 1;
        foreach (var cost in operationCosts)
            ticket.Operations.Add(new MaintenanceTicketOperation { Id = opId++, OperationName = "op", Cost = cost });
        if (paid > 0)
            ticket.Payments.Add(new MaintenancePayment { Id = 1, Amount = paid });
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(ticket);
        return ticket;
    }

    [Fact]
    public async Task Deleting_an_operation_below_the_paid_amount_is_rejected()
    {
        Ticket(MaintenanceStatus.Completed, new[] { 300m, 200m }, paid: 400m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteOperationAsync(5, 1));
        Assert.Contains("المدفوع", ex.Message);
        _repo.DidNotReceive().RemoveOperation(Arg.Any<MaintenanceTicketOperation>());
    }

    [Fact]
    public async Task Deleting_an_operation_that_keeps_total_above_paid_is_allowed()
    {
        Ticket(MaintenanceStatus.UnderRepair, new[] { 300m, 200m }, paid: 100m);

        var result = await _service.DeleteOperationAsync(5, 1);

        _repo.Received(1).RemoveOperation(Arg.Is<MaintenanceTicketOperation>(o => o.Id == 1));
        Assert.Equal(200m, result.TotalCost);
        Assert.Equal(100m, result.RemainingAmount);
    }

    [Fact]
    public async Task Every_locked_change_takes_the_ticket_lock()
    {
        Ticket(MaintenanceStatus.UnderRepair, new[] { 300m }, paid: 0m);

        await _service.AddPaymentAsync(5, new AddTicketPaymentRequest { Amount = 100m });

        await _uow.Received(1).AcquireLockAsync("MaintenanceTicket:5", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Payment_above_remaining_is_rejected()
    {
        Ticket(MaintenanceStatus.Completed, new[] { 300m }, paid: 250m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddPaymentAsync(5, new AddTicketPaymentRequest { Amount = 60m }));
    }

    [Fact]
    public async Task Negative_operation_price_on_complete_is_rejected()
    {
        Ticket(MaintenanceStatus.UnderRepair, Array.Empty<decimal>(), paid: 0m);

        var request = new CompleteRepairRequest
        {
            Operations = new() { new AddTicketOperationRequest { OperationName = "شاشة", Cost = -50m } }
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CompleteRepairAsync(5, request));
    }

    [Fact]
    public async Task Deliver_collects_the_remaining_amount_by_default()
    {
        var ticket = Ticket(MaintenanceStatus.Completed, new[] { 300m }, paid: 100m);

        var result = await _service.DeliverTicketAsync(5);

        Assert.Equal(MaintenanceStatus.Delivered, result.Status);
        Assert.Equal(300m, result.PaidAmount);
        Assert.Equal(0m, result.RemainingAmount);
        Assert.Equal(2, ticket.Payments.Count);
    }

    private MaintenanceTicket BranchTicket(MaintenanceStatus status, int storeId)
    {
        var ticket = Ticket(status, new[] { 300m }, paid: 100m);
        ticket.StoreId = storeId;
        return ticket;
    }

    [Fact]
    public async Task Branch_cannot_deliver_another_branch_ticket()
    {
        BranchTicket(MaintenanceStatus.Completed, storeId: 2);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeliverTicketAsync(5, null, branchStoreId: 3));
    }

    [Fact]
    public async Task Branch_cannot_deliver_a_device_that_is_not_ready()
    {
        BranchTicket(MaintenanceStatus.UnderRepair, storeId: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeliverTicketAsync(5, null, branchStoreId: 2));
    }

    [Fact]
    public async Task Branch_must_collect_the_whole_remaining_amount()
    {
        BranchTicket(MaintenanceStatus.Completed, storeId: 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeliverTicketAsync(5, new DeliverTicketRequest { PaymentAmount = 150m }, branchStoreId: 2));
    }

    [Fact]
    public async Task Branch_delivers_its_ready_ticket_when_fully_paid()
    {
        BranchTicket(MaintenanceStatus.Completed, storeId: 2);

        var result = await _service.DeliverTicketAsync(5, new DeliverTicketRequest { PaymentAmount = 200m }, branchStoreId: 2);

        Assert.Equal(MaintenanceStatus.Delivered, result.Status);
        Assert.Equal(0m, result.RemainingAmount);
    }

    [Fact]
    public async Task Admin_can_deliver_with_an_amount_still_owed()
    {
        BranchTicket(MaintenanceStatus.UnderRepair, storeId: 2);

        var result = await _service.DeliverTicketAsync(5, new DeliverTicketRequest { PaymentAmount = 50m });

        Assert.Equal(MaintenanceStatus.Delivered, result.Status);
        Assert.Equal(150m, result.RemainingAmount);
    }

    [Fact]
    public async Task New_ticket_number_is_taken_under_the_numbering_lock()
    {
        _repo.GetNextTicketNumberAsync(Arg.Any<CancellationToken>()).Returns("MT-20261125-0042");

        var result = await _service.CreateAsync(new CreateMaintenanceTicketRequest
        {
            CustomerName = "عميل",
            CustomerPhone = "0100",
            DeviceName = "موبايل"
        });

        Assert.Equal("MT-20261125-0042", result.TicketNumber);
        Received.InOrder(() =>
        {
            _uow.AcquireLockAsync("MaintenanceTicket:Number", Arg.Any<CancellationToken>());
            _repo.GetNextTicketNumberAsync(Arg.Any<CancellationToken>());
        });
    }
}
