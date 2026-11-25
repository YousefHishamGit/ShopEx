using ShopEx.Application.DTOs.Maintenance;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class MaintenanceTicketService : IMaintenanceTicketService
    {
        private readonly IMaintenanceTicketRepository _ticketRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreRepository _storeRepository;

        public MaintenanceTicketService(
            IMaintenanceTicketRepository ticketRepository,
            IUnitOfWork unitOfWork,
            IStoreRepository storeRepository)
        {
            _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
            _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // المرحلة الأولى: إنشاء واستلام تذكرة الصيانة
        public async Task<MaintenanceTicketResponse> CreateAsync(CreateMaintenanceTicketRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.CustomerName))
                throw new ArgumentException("اسم العميل مطلوب.", nameof(request.CustomerName));

            if (string.IsNullOrWhiteSpace(request.CustomerPhone))
                throw new ArgumentException("رقم هاتف العميل مطلوب.", nameof(request.CustomerPhone));

            if (string.IsNullOrWhiteSpace(request.DeviceName))
                throw new ArgumentException("نوع أو اسم الجهاز مطلوب.", nameof(request.DeviceName));

            var ticket = new MaintenanceTicket
            {
                ReceivedBy = request.ReceivedBy?.Trim() ?? string.Empty,
                StoreId = request.StoreId,
                CustomerName = request.CustomerName.Trim(),
                CustomerPhone = request.CustomerPhone.Trim(),
                DeviceName = request.DeviceName.Trim(),
                Brand = request.Brand?.Trim(),
                Model = request.Model?.Trim(),
                Color = request.Color?.Trim(),
                SerialNumber = request.SerialNumber?.Trim(),
                IssueDescription = request.IssueDescription?.Trim(),
                Accessories = request.Accessories?.Trim(),
                Notes = request.Notes?.Trim(),
                ReceivedAt = DateTime.Now,
                ExpectedCost = Math.Round(request.ExpectedCost, 2, MidpointRounding.AwayFromZero),
                Status = MaintenanceStatus.Received
            };

            // إن كان هناك عربون مدفوع عند الاستلام
            if (request.Deposit.HasValue && request.Deposit.Value > 0)
            {
                ticket.Payments.Add(new MaintenancePayment
                {
                    Amount = Math.Round(request.Deposit.Value, 2, MidpointRounding.AwayFromZero),
                    PaidAt = DateTime.Now,
                    PaymentMethod = request.DepositPaymentMethod ?? MaintenancePaymentMethod.Cash,
                    Notes = "عربون استلام الجهاز"
                });
            }

            // Numbering is serialized: two branches creating a ticket at the same moment get consecutive numbers
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.AcquireLockAsync(TicketNumberLock, ct);
                ticket.TicketNumber = await _ticketRepository.GetNextTicketNumberAsync(ct);
                await _ticketRepository.AddAsync(ticket, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return await ToResponseAsync(ticket, ct);
        }

        private const string TicketNumberLock = "MaintenanceTicket:Number";
        private static string TicketLock(int ticketId) => $"MaintenanceTicket:{ticketId}";

        /// <summary>Runs <paramref name="work"/> in a transaction holding the ticket lock, on a freshly read ticket.</summary>
        private async Task<MaintenanceTicket> WithLockedTicketAsync(int ticketId, Func<MaintenanceTicket, Task> work, CancellationToken ct)
        {
            MaintenanceTicket? ticket = null;
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.AcquireLockAsync(TicketLock(ticketId), ct);
                ticket = await _ticketRepository.GetByIdAsync(ticketId, ct)
                         ?? throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticketId} غير موجودة.");
                await work(ticket);
            }, ct);
            return ticket!;
        }

        public async Task<MaintenanceTicketResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var ticket = await _ticketRepository.GetByIdReadOnlyAsync(id, ct);
            return ticket == null ? null : await ToResponseAsync(ticket, ct);
        }

        public async Task<MaintenanceTicketResponse?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber))
                throw new ArgumentException("رقم التذكرة مطلوب.", nameof(ticketNumber));

            var ticket = await _ticketRepository.GetByTicketNumberAsync(ticketNumber.Trim(), ct);
            return ticket == null ? null : await ToResponseAsync(ticket, ct);
        }

        public async Task<List<MaintenanceTicketSummaryResponse>> GetAllAsync(
            MaintenanceStatus? status = null,
            string? customerPhone = null,
            int? storeId = null,
            CancellationToken ct = default)
        {
            List<MaintenanceTicket> tickets;

            if (status.HasValue)
            {
                tickets = await _ticketRepository.GetByStatusAsync(status.Value, storeId, ct);
            }
            else if (!string.IsNullOrWhiteSpace(customerPhone))
            {
                tickets = await _ticketRepository.GetByCustomerPhoneAsync(customerPhone.Trim(), storeId, ct);
            }
            else
            {
                tickets = await _ticketRepository.GetAllAsync(storeId, ct);
            }

            var names = tickets.Any(t => t.StoreId.HasValue)
                ? await _storeRepository.GetNamesAsync(ct)
                : new Dictionary<int, string>();

            return tickets
                .Select(t =>
                {
                    var dto = MapToSummaryResponse(t);
                    dto.StoreName = StoreNameOf(names, t.StoreId);
                    return dto;
                })
                .ToList();
        }

        public async Task<PagedMaintenanceTicketsResponse> GetPagedAsync(
            MaintenanceStatus? status, string? search, int? storeId, int page = 1, int pageSize = 25, CancellationToken ct = default)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(page, 1);

            // الاستعلامات تتابعية لأنها تشترك في نفس الـ DbContext
            var counts = await _ticketRepository.GetStatusCountsAsync(storeId, ct);
            var (tickets, total) = await _ticketRepository.GetPagedAsync(status, search, storeId, (page - 1) * pageSize, pageSize, ct);
            var (totalPrice, totalPartCost) = await _ticketRepository.GetCostTotalsAsync(status, search, storeId, ct);

            int Count(params MaintenanceStatus[] s) => s.Sum(x => counts.TryGetValue(x, out var c) ? c : 0);

            var names = tickets.Any(t => t.StoreId.HasValue)
                ? await _storeRepository.GetNamesAsync(ct)
                : new Dictionary<int, string>();

            return new PagedMaintenanceTicketsResponse
            {
                Items = tickets.Select(t =>
                {
                    var dto = MapToSummaryResponse(t);
                    dto.StoreName = StoreNameOf(names, t.StoreId);
                    return dto;
                }).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                CountTotal = counts.Values.Sum(),
                CountReceived = Count(MaintenanceStatus.Received, MaintenanceStatus.UnderRepair),
                CountCompleted = Count(MaintenanceStatus.Completed),
                CountDelivered = Count(MaintenanceStatus.Delivered),
                TotalPartCost = totalPartCost,
                TotalProfit = totalPrice - totalPartCost
            };
        }

        // المرحلة الثانية: تسجيل الإصلاحات والعمليات المنفذة (عملت كذا وسعرها كذا)
        public async Task<MaintenanceTicketResponse> AddOperationAsync(int ticketId, AddTicketOperationRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.OperationName))
                throw new ArgumentException("اسم أو وصف الإصلاح مطلوب.", nameof(request.OperationName));

            if (request.Cost < 0)
                throw new ArgumentException("سعر العملية لا يمكن أن يكون سالباً.", nameof(request.Cost));

            if (request.PartCost < 0)
                throw new ArgumentException("تكلفة القطعة لا يمكن أن تكون سالبة.", nameof(request.PartCost));

            var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
            if (ticket == null)
                throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticketId} غير موجودة.");

            if (ticket.Status == MaintenanceStatus.Delivered)
                throw new InvalidOperationException("لا يمكن إضافة عمليات لجهاز تم تسليمه للعميل بالفعل.");

            if (ticket.Status == MaintenanceStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن إضافة عمليات لتذكرة ملغية.");

            var operation = new MaintenanceTicketOperation
            {
                MaintenanceTicketId = ticketId,
                OperationName = request.OperationName.Trim(),
                Cost = Math.Round(request.Cost, 2, MidpointRounding.AwayFromZero),
                PartCost = Math.Round(request.PartCost, 2, MidpointRounding.AwayFromZero),
                Notes = request.Notes?.Trim(),
                CreatedAt = DateTime.Now
            };

            ticket.Operations.Add(operation);

            // تحويل الحالة إلى قيد الإصلاح إذا كانت مستلمة فقط
            if (ticket.Status == MaintenanceStatus.Received)
            {
                ticket.Status = MaintenanceStatus.UnderRepair;
            }

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return await ToResponseAsync(ticket, ct);
        }

        public async Task<MaintenanceTicketResponse> DeleteOperationAsync(int ticketId, int operationId, CancellationToken ct = default)
        {
            var ticket = await WithLockedTicketAsync(ticketId, async t =>
            {
                if (t.Status == MaintenanceStatus.Delivered)
                    throw new InvalidOperationException("لا يمكن حذف عمليات من جهاز تم تسليمه.");

                var operation = t.Operations.FirstOrDefault(o => o.Id == operationId)
                                ?? throw new KeyNotFoundException($"العملية برقم {operationId} غير موجودة في هذه التذكرة.");

                // The total can't fall below what the customer already paid (it would show a negative remaining)
                var totalAfter = t.TotalCost - operation.Cost;
                if (t.PaidAmount > totalAfter)
                    throw new InvalidOperationException(
                        $"لا يمكن حذف العملية: المدفوع ({t.PaidAmount:N2}) أكبر من إجمالي التذكرة بعد الحذف ({totalAfter:N2}).");

                _ticketRepository.RemoveOperation(operation);
                t.Operations.Remove(operation);
                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return await ToResponseAsync(ticket, ct);
        }

        public async Task<MaintenanceTicketResponse> CompleteRepairAsync(int ticketId, CompleteRepairRequest? request = null, CancellationToken ct = default)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
            if (ticket == null)
                throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticketId} غير موجودة.");

            if (ticket.Status == MaintenanceStatus.Delivered)
                throw new InvalidOperationException("الجهاز مسلم بالفعل للعميل.");

            if (ticket.Status == MaintenanceStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن إنهاء إصلاح تذكرة ملغية.");

            // إضافة عمليات إضافية إن أُرسلت في الطلب
            if (request?.Operations != null && request.Operations.Any(o => !string.IsNullOrWhiteSpace(o.OperationName) && (o.Cost < 0 || o.PartCost < 0)))
                throw new ArgumentException("سعر العملية وتكلفة القطعة لا يمكن أن يكونا بالسالب.");

            if (request?.Operations != null && request.Operations.Any())
            {
                foreach (var op in request.Operations)
                {
                    if (string.IsNullOrWhiteSpace(op.OperationName))
                        continue;

                    ticket.Operations.Add(new MaintenanceTicketOperation
                    {
                        MaintenanceTicketId = ticketId,
                        OperationName = op.OperationName.Trim(),
                        Cost = Math.Round(op.Cost, 2, MidpointRounding.AwayFromZero),
                        PartCost = Math.Round(op.PartCost, 2, MidpointRounding.AwayFromZero),
                        Notes = op.Notes?.Trim(),
                        CreatedAt = DateTime.Now
                    });
                }
            }

            ticket.RepairedAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(request?.RepairNotes))
            {
                ticket.RepairNotes = request.RepairNotes.Trim();
            }

            ticket.Status = MaintenanceStatus.Completed;

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return await ToResponseAsync(ticket, ct);
        }

        // المرحلة الثالثة: الدفع والتسليم للعميل
        public async Task<MaintenanceTicketResponse> AddPaymentAsync(int ticketId, AddTicketPaymentRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0)
                throw new ArgumentException("مبلغ الدفع يجب أن يكون أكبر من صفر.", nameof(request.Amount));

            // Locked + re-read: two payments at the same moment can't both pass the "remaining" check
            var ticket = await WithLockedTicketAsync(ticketId, async t =>
            {
                if (t.Status == MaintenanceStatus.Cancelled)
                    throw new InvalidOperationException("لا يمكن إضافة دفعات لتذكرة صيانة ملغية.");

                if (t.TotalCost > 0 && amount > t.RemainingAmount)
                    throw new InvalidOperationException(
                        $"مبلغ الدفع ({amount:N2}) أكبر من المتبقي على التذكرة ({t.RemainingAmount:N2}).");

                t.Payments.Add(new MaintenancePayment
                {
                    MaintenanceTicketId = ticketId,
                    Amount = amount,
                    PaidAt = request.PaidAt ?? DateTime.Now,
                    PaymentMethod = request.PaymentMethod,
                    Notes = request.Notes?.Trim()
                });

                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return await ToResponseAsync(ticket, ct);
        }

        public async Task<MaintenanceTicketResponse> DeliverTicketAsync(int ticketId, DeliverTicketRequest? request = null, int? branchStoreId = null, CancellationToken ct = default)
        {
            var delivered = await WithLockedTicketAsync(ticketId, t =>
            {
                if (branchStoreId.HasValue)
                    EnsureBranchCanDeliver(t, branchStoreId.Value, request);
                return DeliverLockedAsync(t, request, ct);
            }, ct);
            return await ToResponseAsync(delivered, ct);
        }

        /// <summary>Branch employee rules for delivery, checked on the locked, freshly read ticket.</summary>
        private static void EnsureBranchCanDeliver(MaintenanceTicket ticket, int branchStoreId, DeliverTicketRequest? request)
        {
            // Another branch's ticket is reported as "not found", like on every other employee endpoint
            if (ticket.StoreId != branchStoreId)
                throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticket.Id} غير موجودة.");

            if (ticket.Status != MaintenanceStatus.Completed)
                throw new InvalidOperationException("لا يمكن تسليم الجهاز قبل أن يصبح جاهزاً.");

            // No amount given = the whole remaining amount is collected (see DeliverLockedAsync)
            if (request?.PaymentAmount is decimal paid
                && Math.Round(paid, 2, MidpointRounding.AwayFromZero) < ticket.RemainingAmount)
                throw new InvalidOperationException($"يجب تحصيل كامل المبلغ المتبقي ({ticket.RemainingAmount:N2}) قبل التسليم.");
        }

        private async Task DeliverLockedAsync(MaintenanceTicket ticket, DeliverTicketRequest? request, CancellationToken ct)
        {
            var ticketId = ticket.Id;

            if (ticket.Status == MaintenanceStatus.Delivered)
                throw new InvalidOperationException("الجهاز مسلّم بالفعل للعميل.");

            if (ticket.Status == MaintenanceStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن تسليم تذكرة صيانة ملغية.");

            // تحديد المبلغ المدفوع عند التسليم
            decimal paymentAmount = 0;
            if (request?.PaymentAmount.HasValue == true)
            {
                paymentAmount = Math.Round(request.PaymentAmount.Value, 2, MidpointRounding.AwayFromZero);
            }
            else if (ticket.RemainingAmount > 0)
            {
                // إذا لم يُحدد مبلغ وكان هناك متبقي، نسدد كامل المتبقي كإجراء افتراضي عند التسليم
                paymentAmount = ticket.RemainingAmount;
            }

            if (ticket.TotalCost > 0 && paymentAmount > ticket.RemainingAmount)
                throw new InvalidOperationException(
                    $"المبلغ المحصَّل ({paymentAmount:N2}) أكبر من المتبقي على التذكرة ({ticket.RemainingAmount:N2}).");

            if (paymentAmount > 0)
            {
                ticket.Payments.Add(new MaintenancePayment
                {
                    MaintenanceTicketId = ticketId,
                    Amount = paymentAmount,
                    PaidAt = DateTime.Now,
                    PaymentMethod = request?.PaymentMethod ?? MaintenancePaymentMethod.Cash,
                    Notes = "دفعة تسليم الجهاز للعميل"
                });
            }

            ticket.DeliveredAt = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(request?.DeliveryNotes))
            {
                ticket.DeliveryNotes = request.DeliveryNotes.Trim();
            }
            if (!string.IsNullOrWhiteSpace(request?.WarrantyNote))
            {
                ticket.WarrantyNote = request.WarrantyNote.Trim();
            }

            ticket.Status = MaintenanceStatus.Delivered;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task<MaintenanceTicketResponse> UpdateStatusAsync(int ticketId, MaintenanceStatus newStatus, CancellationToken ct = default)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
            if (ticket == null)
                throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticketId} غير موجودة.");

            if (!Enum.IsDefined(typeof(MaintenanceStatus), newStatus))
                throw new ArgumentException("حالة التذكرة غير صحيحة.");

            if (newStatus == ticket.Status)
                return await ToResponseAsync(ticket, ct);

            if (ticket.Status == MaintenanceStatus.Delivered)
                throw new InvalidOperationException("لا يمكن تغيير حالة جهاز تم تسليمه.");

            if (ticket.Status == MaintenanceStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن تغيير حالة تذكرة ملغية.");

            if (newStatus == MaintenanceStatus.Delivered)
                throw new InvalidOperationException("التسليم يتم من زر \"تسليم الجهاز\" حتى يُسجَّل تحصيل المبلغ.");

            if (newStatus == MaintenanceStatus.Cancelled && ticket.PaidAmount > 0)
                throw new InvalidOperationException(
                    $"لا يمكن إلغاء تذكرة عليها دفعات ({ticket.PaidAmount:N2}). ردّ المبلغ للعميل أولاً أو سلّم الجهاز.");

            ticket.Status = newStatus;

            if (newStatus == MaintenanceStatus.Completed && !ticket.RepairedAt.HasValue)
            {
                ticket.RepairedAt = DateTime.Now;
            }

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return await ToResponseAsync(ticket, ct);
        }

        private async Task<MaintenanceTicketResponse> ToResponseAsync(MaintenanceTicket ticket, CancellationToken ct)
        {
            var dto = MapToResponse(ticket);
            if (ticket.StoreId.HasValue)
                dto.StoreName = StoreNameOf(await _storeRepository.GetNamesAsync(ct), ticket.StoreId);
            return dto;
        }

        private static string? StoreNameOf(Dictionary<int, string> names, int? storeId)
            => storeId.HasValue ? (names.TryGetValue(storeId.Value, out var n) ? n : $"فرع #{storeId}") : null;

        private static MaintenanceTicketResponse MapToResponse(MaintenanceTicket ticket)
        {
            return new MaintenanceTicketResponse
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                ReceivedBy = ticket.ReceivedBy,
                StoreId = ticket.StoreId,
                CustomerName = ticket.CustomerName,
                CustomerPhone = ticket.CustomerPhone,
                DeviceName = ticket.DeviceName,
                Brand = ticket.Brand,
                Model = ticket.Model,
                Color = ticket.Color,
                SerialNumber = ticket.SerialNumber,
                IssueDescription = ticket.IssueDescription,
                Accessories = ticket.Accessories,
                Notes = ticket.Notes,
                ReceivedAt = ticket.ReceivedAt,
                ExpectedCost = ticket.ExpectedCost,
                Status = ticket.Status,
                RepairedAt = ticket.RepairedAt,
                RepairNotes = ticket.RepairNotes,
                DeliveredAt = ticket.DeliveredAt,
                DeliveryNotes = ticket.DeliveryNotes,
                WarrantyNote = ticket.WarrantyNote,
                TotalCost = ticket.TotalCost,
                TotalPartCost = ticket.TotalPartCost,
                Profit = ticket.TotalCost - ticket.TotalPartCost,
                PaidAmount = ticket.PaidAmount,
                RemainingAmount = ticket.RemainingAmount,
                IsPaid = ticket.IsPaid,
                Operations = ticket.Operations?.Select(o => new MaintenanceTicketOperationResponse
                {
                    Id = o.Id,
                    MaintenanceTicketId = o.MaintenanceTicketId,
                    OperationName = o.OperationName,
                    Cost = o.Cost,
                    PartCost = o.PartCost,
                    Notes = o.Notes,
                    CreatedAt = o.CreatedAt
                }).OrderBy(o => o.CreatedAt).ToList() ?? new List<MaintenanceTicketOperationResponse>(),
                Payments = ticket.Payments?.Select(p => new MaintenancePaymentResponse
                {
                    Id = p.Id,
                    MaintenanceTicketId = p.MaintenanceTicketId,
                    Amount = p.Amount,
                    PaidAt = p.PaidAt,
                    PaymentMethod = p.PaymentMethod,
                    Notes = p.Notes
                }).OrderBy(p => p.PaidAt).ToList() ?? new List<MaintenancePaymentResponse>()
            };
        }

        private static MaintenanceTicketSummaryResponse MapToSummaryResponse(MaintenanceTicket ticket)
        {
            return new MaintenanceTicketSummaryResponse
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                StoreId = ticket.StoreId,
                CustomerName = ticket.CustomerName,
                CustomerPhone = ticket.CustomerPhone,
                DeviceName = ticket.DeviceName,
                ReceivedAt = ticket.ReceivedAt,
                Status = ticket.Status,
                TotalCost = ticket.TotalCost,
                PaidAmount = ticket.PaidAmount,
                RemainingAmount = ticket.RemainingAmount,
                IsPaid = ticket.IsPaid
            };
        }
    }
}
