using ShopEx.Application.DTOs.Balance;
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
    public class BalanceService : IBalanceService
    {
        /// <summary>أقصى عدد حركات يعرضها سجل التحويلات</summary>
        public const int LedgerLimit = 30;

        private const int MaxNotesLength = 500;

        private readonly IBalanceRepository _balanceRepo;
        private readonly IUnitOfWork _unitOfWork;

        public BalanceService(
            IBalanceRepository balanceRepo,
            IUnitOfWork unitOfWork)
        {
            _balanceRepo = balanceRepo ?? throw new ArgumentNullException(nameof(balanceRepo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<BalanceResponse> ChargeAsync(ChargeBalanceRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.StoreId <= 0)
                throw new ArgumentException("يجب تحديد الفرع بصورة صحيحة.", nameof(request.StoreId));

            ValidateBalanceType(request.BalanceType);

            if (request.Amount <= 0)
                throw new ArgumentException("يجب أن يكون المبلغ المشحون أكبر من الصفر.", nameof(request.Amount));

            var balance = new Balance
            {
                StoreId = request.StoreId,
                BalanceType = request.BalanceType,
                Amount = Math.Round(request.Amount, 2),
                ChargedAt = request.ChargedAt ?? DateTime.Now,
                Notes = NormalizeNotes(request.Notes)
            };

            await _balanceRepo.AddChargeAsync(balance, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(balance);
        }

        public async Task<BalanceClosureResponse> CloseAsync(int balanceId, CloseBalanceRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.AmountUsed <= 0)
                throw new ArgumentException("يجب أن يكون المبلغ المستخدم أكبر من الصفر.", nameof(request.AmountUsed));

            var balance = await _balanceRepo.GetChargeByIdAsync(balanceId, ct);
            if (balance == null)
                throw new KeyNotFoundException($"شحنة الرصيد برقم {balanceId} غير موجودة.");

            var amount = Math.Round(request.AmountUsed, 2);
            BalanceClosure? closure = null;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // قفل على الفرع/الوسيلة ثم إعادة حساب المتبقي لمنع التقفيل المتزامن من تجاوز الرصيد
                await _balanceRepo.AcquireStoreLockAsync(balance.StoreId, balance.BalanceType, ct);

                var remaining = balance.Amount - await _balanceRepo.GetUsedAmountAsync(balanceId, ct);
                if (amount > remaining)
                {
                    throw new InvalidOperationException(
                        $"المبلغ المراد تقفيله ({amount:N2}) أكبر من الرصيد المتبقي المتاح ({remaining:N2}).");
                }

                closure = new BalanceClosure
                {
                    BalanceId = balanceId,
                    AmountUsed = amount,
                    ClosedAt = request.ClosedAt ?? DateTime.Now,
                    Notes = NormalizeNotes(request.Notes)
                };

                await _balanceRepo.AddClosureAsync(closure, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return MapClosure(closure!);
        }

        public async Task<List<BalanceSummaryResponse>> GetAllAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var list = await _balanceRepo.GetChargeSummariesAsync(storeId, balanceType, fromDate, toDate, ct);
            foreach (var item in list)
                item.BalanceTypeName = GetBalanceTypeName(item.BalanceType);
            return list;
        }

        public async Task<BalanceResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var balance = await _balanceRepo.GetChargeByIdAsync(id, ct);
            return balance == null ? null : MapToResponse(balance);
        }

        public async Task<List<BalanceClosureResponse>> GetClosuresAsync(int balanceId, CancellationToken ct = default)
        {
            var closures = await _balanceRepo.GetClosuresByBalanceIdAsync(balanceId, ct);
            return closures.Select(MapClosure).ToList();
        }

        public async Task<List<StoreBalanceGroupDto>> GetGroupedBalancesAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            CancellationToken ct = default)
        {
            var groups = await _balanceRepo.GetGroupedAsync(storeId, balanceType, ct);
            foreach (var g in groups)
                g.BalanceTypeName = GetBalanceTypeName(g.BalanceType);
            return groups;
        }

        public async Task<List<BalanceClosureResponse>> CloseByStoreAsync(
            CloseStoreBalanceRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.StoreId <= 0)
                throw new ArgumentException("يجب تحديد الفرع بصورة صحيحة.", nameof(request.StoreId));

            ValidateBalanceType(request.BalanceType);

            if (request.AmountUsed <= 0)
                throw new ArgumentException("يجب أن يكون مبلغ الاستهلاك أكبر من الصفر.", nameof(request.AmountUsed));

            var amount = Math.Round(request.AmountUsed, 2);
            var closedAt = request.ClosedAt ?? DateTime.Now;
            var notes = NormalizeNotes(request.Notes);
            var created = new List<BalanceClosure>();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // القفل أولاً ثم قراءة المتبقي، حتى لا يتجاوز تقفيلان متزامنان الرصيد المتاح
                await _balanceRepo.AcquireStoreLockAsync(request.StoreId, request.BalanceType, ct);

                var openCharges = await _balanceRepo.GetOpenChargesAsync(request.StoreId, request.BalanceType, ct);
                var totalAvailable = openCharges.Sum(x => x.Remaining);

                if (amount > totalAvailable)
                {
                    throw new InvalidOperationException(
                        $"المبلغ المراد تقفيله ({amount:N2}) يتجاوز إجمالي الرصيد المتاح للفرع ({totalAvailable:N2}) في هذه الوسيلة.");
                }

                // توزيع المبلغ على الشحنات الأقدم أولاً (FIFO)
                var toAllocate = amount;
                foreach (var charge in openCharges)
                {
                    if (toAllocate <= 0) break;

                    var allocate = Math.Min(toAllocate, charge.Remaining);
                    var closure = new BalanceClosure
                    {
                        BalanceId = charge.Id,
                        AmountUsed = allocate,
                        ClosedAt = closedAt,
                        Notes = notes
                    };

                    await _balanceRepo.AddClosureAsync(closure, ct);
                    created.Add(closure);
                    toAllocate -= allocate;
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return created.Select(MapClosure).ToList();
        }

        /// <summary>
        /// آخر <see cref="LedgerLimit"/> حركة فقط (شحن وتقفيل) مرتبة من الأحدث للأقدم.
        /// </summary>
        public async Task<List<BalanceTransactionItemDto>> GetLedgerAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var items = await _balanceRepo.GetLatestTransactionsAsync(
                storeId, balanceType, fromDate, toDate, LedgerLimit, ct);

            foreach (var t in items)
                t.BalanceTypeName = GetBalanceTypeName(t.BalanceType);

            return items;
        }

        // ================================================================
        // Helper Mappings
        // ================================================================

        private static void ValidateBalanceType(BalanceType type)
        {
            if (!Enum.IsDefined(typeof(BalanceType), type))
                throw new ArgumentException("وسيلة الدفع غير صحيحة.", nameof(type));
        }

        private static string? NormalizeNotes(string? notes)
        {
            var trimmed = notes?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return null;
            return trimmed.Length > MaxNotesLength ? trimmed[..MaxNotesLength] : trimmed;
        }

        private static BalanceClosureResponse MapClosure(BalanceClosure c) => new()
        {
            Id = c.Id,
            BalanceId = c.BalanceId,
            AmountUsed = c.AmountUsed,
            ClosedAt = c.ClosedAt,
            Notes = c.Notes
        };

        private static BalanceResponse MapToResponse(Balance balance)
        {
            var closures = balance.Closures ?? new List<BalanceClosure>();
            var used = closures.Sum(c => c.AmountUsed);
            return new BalanceResponse
            {
                Id = balance.Id,
                StoreId = balance.StoreId,
                BalanceType = balance.BalanceType,
                BalanceTypeName = GetBalanceTypeName(balance.BalanceType),
                Amount = balance.Amount,
                ChargedAt = balance.ChargedAt,
                Notes = balance.Notes,
                TotalUsedAmount = used,
                RemainingAmount = balance.Amount - used,
                Closures = closures.OrderByDescending(c => c.ClosedAt).Select(MapClosure).ToList()
            };
        }

        private static string GetBalanceTypeName(BalanceType type) => type switch
        {
            BalanceType.VodafoneCash => "Vodafone Cash (فودافون كاش)",
            BalanceType.OrangeCash => "Orange Cash (أورنج كاش)",
            BalanceType.EtisalatCash => "Etisalat Cash (اتصالات كاش)",
            BalanceType.instapay => "Instapay (انستاباي)",
            BalanceType.Cards => "Cards (فيزا / كروت)",
            BalanceType.Fawry => "Fawry (فوري)",
            _ => type.ToString()
        };
    }
}
