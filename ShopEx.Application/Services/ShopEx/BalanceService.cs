using ShopEx.Application.DTOs.Balance;
using ShopEx.Application.DTOs.Expense;
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
    public class BalanceService : IBalanceService
    {
        /// <summary>أقصى عدد حركات يعرضها سجل التحويلات</summary>
        public const int LedgerPageSize = 30;
        /// <summary>Each page merges the first page×size rows of two tables, so very deep pages are refused (use date filters).</summary>
        public const int MaxLedgerPage = 500;

        private const int MaxNotesLength = 500;

        private readonly IBalanceRepository _balanceRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStoreRepository _storeRepo;

        public BalanceService(
            IBalanceRepository balanceRepo,
            IUnitOfWork unitOfWork,
            IStoreRepository storeRepo)
        {
            _storeRepo = storeRepo ?? throw new ArgumentNullException(nameof(storeRepo));
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

            if (!await _storeRepo.ExistsAsync(request.StoreId, ct))
                throw new ArgumentException("الفرع المحدد غير موجود.", nameof(request.StoreId));

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

        /// <summary>بند قديم كانت تُسجَّل عليه شحنات الفرع الذاتية كمصروف؛ لم يعد يُستخدم (يُنظَّف عند التشغيل)</summary>
        public const string SelfChargeExpenseCategory = "شحن رصيد";

        public async Task<BalanceResponse> ChargeOwnBranchAsync(
            int storeId, string userName, ChargeOwnBalanceRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var user = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
            var note = request.Notes?.Trim();

            // شحن الرصيد شراء رصيد للبيع وليس مصروفاً: يُسجَّل كشحنة فقط (ويظهر ربحه عند التقفيل)
            return await ChargeAsync(new ChargeBalanceRequest
            {
                StoreId = storeId,
                BalanceType = request.BalanceType,
                Amount = request.Amount,
                Notes = string.IsNullOrEmpty(note) ? $"شحن من الفرع [{user}]" : $"شحن من الفرع [{user}] {note}"
            }, ct);
        }

        public async Task<BalanceClosureResponse> CloseAsync(int balanceId, CloseBalanceRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.AmountUsed <= 0)
                throw new ArgumentException("يجب أن يكون المبلغ المستخدم أكبر من الصفر.", nameof(request.AmountUsed));

            if (request.Profit < 0)
                throw new ArgumentException("الربح لا يمكن أن يكون سالباً.", nameof(request.Profit));

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
                    Profit = Math.Round(request.Profit, 2),
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

            if (request.Profit < 0)
                throw new ArgumentException("الربح لا يمكن أن يكون سالباً.", nameof(request.Profit));

            var amount = Math.Round(request.AmountUsed, 2);
            var profit = Math.Round(request.Profit, 2);
            var closedAt = request.ClosedAt ?? DateTime.Now;
            var notes = NormalizeNotes(request.Notes);
            var created = new List<BalanceClosure>();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // القفل أولاً ثم قراءة المتبقي، حتى لا يتجاوز تقفيلان متزامنان الرصيد المتاح
                await _balanceRepo.AcquireStoreLockAsync(request.StoreId, request.BalanceType, ct);

                var openCharges = await _balanceRepo.GetOpenChargesAsync(request.StoreId, request.BalanceType, ct);
                foreach (var (balanceId, used, share) in AllocateFifo(openCharges, amount, profit))
                {
                    var closure = new BalanceClosure
                    {
                        BalanceId = balanceId,
                        AmountUsed = used,
                        Profit = share,
                        ClosedAt = closedAt,
                        Notes = notes
                    };
                    await _balanceRepo.AddClosureAsync(closure, ct);
                    created.Add(closure);
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return created.Select(MapClosure).ToList();
        }

        /// <summary>
        /// صفحة من كشف الحركات (شحن وتقفيل) مرتبة من الأحدث للأقدم.
        /// </summary>
        public async Task<BalanceLedgerPageDto> GetLedgerAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = LedgerPageSize,
            CancellationToken ct = default)
        {
            page = Math.Clamp(page, 1, MaxLedgerPage);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, total) = await _balanceRepo.GetTransactionsPageAsync(
                storeId, balanceType, fromDate, toDate, page, pageSize, ct);

            foreach (var t in items)
                t.BalanceTypeName = GetBalanceTypeName(t.BalanceType);

            return new BalanceLedgerPageDto { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
        }

        // ================================================================
        // Helper Mappings
        // ================================================================

        /// <summary>
        /// Splits a closing over the open charges, oldest first (FIFO). The profit is shared in proportion to the
        /// amount taken from each charge; rounding leftovers go to the last charge so the shares add up exactly.
        /// Throws <see cref="InvalidOperationException"/> when the amount exceeds what is available.
        /// </summary>
        public static List<(int BalanceId, decimal Amount, decimal Profit)> AllocateFifo(
            IReadOnlyList<OpenChargeRow> openCharges, decimal amount, decimal profit)
        {
            var totalAvailable = openCharges.Sum(x => x.Remaining);
            if (amount > totalAvailable)
            {
                throw new InvalidOperationException(
                    $"المبلغ المراد تقفيله ({amount:N2}) يتجاوز إجمالي الرصيد المتاح للفرع ({totalAvailable:N2}) في هذه الوسيلة.");
            }

            var result = new List<(int, decimal, decimal)>();
            var toAllocate = amount;
            var profitLeft = profit;
            foreach (var charge in openCharges)
            {
                if (toAllocate <= 0) break;
                if (charge.Remaining <= 0) continue;

                var allocate = Math.Min(toAllocate, charge.Remaining);
                var isLast = toAllocate - allocate <= 0;
                var share = isLast ? profitLeft : Math.Round(profit * allocate / amount, 2);

                result.Add((charge.Id, allocate, share));
                toAllocate -= allocate;
                profitLeft -= share;
            }
            return result;
        }

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
            Profit = c.Profit,
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
