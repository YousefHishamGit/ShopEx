using ShopEx.Application.DTOs.Maintenance;
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

        public MaintenanceTicketService(
            IMaintenanceTicketRepository ticketRepository,
            IUnitOfWork unitOfWork)
        {
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

            var ticketNumber = await _ticketRepository.GetNextTicketNumberAsync(ct);

            var ticket = new MaintenanceTicket
            {
                TicketNumber = ticketNumber,
                ReceivedBy = request.ReceivedBy?.Trim() ?? string.Empty,
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

            await _ticketRepository.AddAsync(ticket, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(ticket);
        }

        public async Task<MaintenanceTicketResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var ticket = await _ticketRepository.GetByIdAsync(id, ct);
            return ticket == null ? null : MapToResponse(ticket);
        }

        public async Task<MaintenanceTicketResponse?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber))
                throw new ArgumentException("رقم التذكرة مطلوب.", nameof(ticketNumber));

            var ticket = await _ticketRepository.GetByTicketNumberAsync(ticketNumber.Trim(), ct);
            return ticket == null ? null : MapToResponse(ticket);
        }

        public async Task<List<MaintenanceTicketSummaryResponse>> GetAllAsync(
            MaintenanceStatus? status = null,
            string? customerPhone = null,
            CancellationToken ct = default)
        {
            List<MaintenanceTicket> tickets;

            if (status.HasValue)
            {
                tickets = await _ticketRepository.GetByStatusAsync(status.Value, ct);
            }
            else if (!string.IsNullOrWhiteSpace(customerPhone))
            {
                tickets = await _ticketRepository.GetByCustomerPhoneAsync(customerPhone.Trim(), ct);
            }
            else
            {
                tickets = await _ticketRepository.GetAllAsync(ct);
            }

            return tickets.Select(MapToSummaryResponse).ToList();
        }

        // المرحلة الثانية: تسجيل الإصلاحات والعمليات المنفذة (عملت كذا وسعرها كذا)
        public async Task<MaintenanceTicketResponse> AddOperationAsync(int ticketId, AddTicketOperationRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.OperationName))
                throw new ArgumentException("اسم أو وصف الإصلاح مطلوب.", nameof(request.OperationName));

            if (request.Cost < 0)
                throw new ArgumentException("تكلفة العملية لا يمكن أن تكون سالبة.", nameof(request.Cost));

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

            return MapToResponse(ticket);
        }

        public async Task<MaintenanceTicketResponse> DeleteOperationAsync(int ticketId, int operationId, CancellationToken ct = default)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
            if (ticket == null)
                throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticketId} غير موجودة.");

            if (ticket.Status == MaintenanceStatus.Delivered)
                throw new InvalidOperationException("لا يمكن حذف عمليات من جهاز تم تسليمه.");

            var operation = ticket.Operations.FirstOrDefault(o => o.Id == operationId);
            if (operation == null)
                throw new KeyNotFoundException($"العملية برقم {operationId} غير موجودة في هذه التذكرة.");

            _ticketRepository.RemoveOperation(operation);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(ticket);
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

            return MapToResponse(ticket);
        }

        // المرحلة الثالثة: الدفع والتسليم للعميل
        public async Task<MaintenanceTicketResponse> AddPaymentAsync(int ticketId, AddTicketPaymentRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0)
                throw new ArgumentException("مبلغ الدفع يجب أن يكون أكبر من صفر.", nameof(request.Amount));

            var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
            if (ticket == null)
                throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticketId} غير موجودة.");

            if (ticket.Status == MaintenanceStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن إضافة دفعات لتذكرة صيانة ملغية.");

            ticket.Payments.Add(new MaintenancePayment
            {
                MaintenanceTicketId = ticketId,
                Amount = amount,
                PaidAt = request.PaidAt ?? DateTime.Now,
                PaymentMethod = request.PaymentMethod,
                Notes = request.Notes?.Trim()
            });

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(ticket);
        }

        public async Task<MaintenanceTicketResponse> DeliverTicketAsync(int ticketId, DeliverTicketRequest? request = null, CancellationToken ct = default)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
            if (ticket == null)
                throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticketId} غير موجودة.");

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

            ticket.Status = MaintenanceStatus.Delivered;

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(ticket);
        }

        public async Task<MaintenanceTicketResponse> UpdateStatusAsync(int ticketId, MaintenanceStatus newStatus, CancellationToken ct = default)
        {
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, ct);
            if (ticket == null)
                throw new KeyNotFoundException($"تذكرة الصيانة برقم {ticketId} غير موجودة.");

            ticket.Status = newStatus;

            if (newStatus == MaintenanceStatus.Completed && !ticket.RepairedAt.HasValue)
            {
                ticket.RepairedAt = DateTime.Now;
            }
            else if (newStatus == MaintenanceStatus.Delivered && !ticket.DeliveredAt.HasValue)
            {
                ticket.DeliveredAt = DateTime.Now;
            }

            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(ticket);
        }

        private static MaintenanceTicketResponse MapToResponse(MaintenanceTicket ticket)
        {
            return new MaintenanceTicketResponse
            {
                Id = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                ReceivedBy = ticket.ReceivedBy,
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
                TotalCost = ticket.TotalCost,
                PaidAmount = ticket.PaidAmount,
                RemainingAmount = ticket.RemainingAmount,
                IsPaid = ticket.IsPaid,
                Operations = ticket.Operations?.Select(o => new MaintenanceTicketOperationResponse
                {
                    Id = o.Id,
                    MaintenanceTicketId = o.MaintenanceTicketId,
                    OperationName = o.OperationName,
                    Cost = o.Cost,
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
