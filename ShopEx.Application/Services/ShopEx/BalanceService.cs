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

            if (request.Amount <= 0)
                throw new ArgumentException("يجب أن يكون المبلغ المشحون أكبر من الصفر.", nameof(request.Amount));

            var balance = new Balance
            {
                StoreId = request.StoreId,
                BalanceType = request.BalanceType,
                Amount = request.Amount,
                ChargedAt = request.ChargedAt ?? DateTime.UtcNow,
                Notes = request.Notes?.Trim()
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

            var currentUsed = balance.Closures.Sum(c => c.AmountUsed);
            var remaining = balance.Amount - currentUsed;

            if (request.AmountUsed > remaining)
            {
                throw new InvalidOperationException(
                    $"المبلغ المراد تقفيله ({request.AmountUsed:N2}) أكبر من الرصيد المتبقي المتاح ({remaining:N2}).");
            }

            var closure = new BalanceClosure
            {
                BalanceId = balanceId,
                AmountUsed = request.AmountUsed,
                ClosedAt = request.ClosedAt ?? DateTime.UtcNow,
                Notes = request.Notes?.Trim()
            };

            await _balanceRepo.AddClosureAsync(closure, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new BalanceClosureResponse
            {
                Id = closure.Id,
                BalanceId = closure.BalanceId,
                AmountUsed = closure.AmountUsed,
                ClosedAt = closure.ClosedAt,
                Notes = closure.Notes
            };
        }

        public async Task<List<BalanceSummaryResponse>> GetAllAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var charges = await _balanceRepo.GetChargesAsync(storeId, balanceType, fromDate, toDate, ct);

            return charges.Select(b =>
            {
                var used = b.Closures.Sum(c => c.AmountUsed);
                return new BalanceSummaryResponse
                {
                    Id = b.Id,
                    StoreId = b.StoreId,
                    BalanceType = b.BalanceType,
                    BalanceTypeName = GetBalanceTypeName(b.BalanceType),
                    Amount = b.Amount,
                    ChargedAt = b.ChargedAt,
                    Notes = b.Notes,
                    TotalUsedAmount = used,
                    RemainingAmount = b.Amount - used,
                    ClosuresCount = b.Closures.Count
                };
            }).ToList();
        }

        public async Task<BalanceResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var balance = await _balanceRepo.GetChargeByIdAsync(id, ct);
            if (balance == null)
                return null;

            return MapToResponse(balance);
        }

        public async Task<List<BalanceClosureResponse>> GetClosuresAsync(int balanceId, CancellationToken ct = default)
        {
            var closures = await _balanceRepo.GetClosuresByBalanceIdAsync(balanceId, ct);

            return closures.Select(c => new BalanceClosureResponse
            {
                Id = c.Id,
                BalanceId = c.BalanceId,
                AmountUsed = c.AmountUsed,
                ClosedAt = c.ClosedAt,
                Notes = c.Notes
            }).ToList();
        }

        public async Task<List<StoreBalanceGroupDto>> GetGroupedBalancesAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            CancellationToken ct = default)
        {
            var charges = await _balanceRepo.GetChargesAsync(storeId, balanceType, null, null, ct);

            var grouped = charges
                .GroupBy(b => new { b.StoreId, b.BalanceType })
                .Select(g =>
                {
                    var totalCharged = g.Sum(b => b.Amount);
                    var totalUsed = g.SelectMany(b => b.Closures).Sum(c => c.AmountUsed);
                    var closures = g.SelectMany(b => b.Closures).ToList();

                    return new StoreBalanceGroupDto
                    {
                        StoreId = g.Key.StoreId,
                        BalanceType = g.Key.BalanceType,
                        BalanceTypeName = GetBalanceTypeName(g.Key.BalanceType),
                        TotalCharged = totalCharged,
                        TotalUsed = totalUsed,
                        ChargesCount = g.Count(),
                        ClosuresCount = closures.Count,
                        LastChargedAt = g.Max(b => (DateTime?)b.ChargedAt),
                        LastClosedAt = closures.Any() ? closures.Max(c => (DateTime?)c.ClosedAt) : null
                    };
                })
                .OrderBy(x => x.StoreId)
                .ThenBy(x => x.BalanceType)
                .ToList();

            return grouped;
        }

        public async Task<List<BalanceClosureResponse>> CloseByStoreAsync(
            CloseStoreBalanceRequest request,
            CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.StoreId <= 0)
                throw new ArgumentException("يجب تحديد الفرع بصورة صحيحة.", nameof(request.StoreId));

            if (request.AmountUsed <= 0)
                throw new ArgumentException("يجب أن يكون مبلغ الاستهلاك أكبر من الصفر.", nameof(request.AmountUsed));

            var charges = await _balanceRepo.GetChargesAsync(request.StoreId, request.BalanceType, null, null, ct);

            var openCharges = charges
                .Select(b => new
                {
                    Balance = b,
                    Remaining = b.Amount - b.Closures.Sum(c => c.AmountUsed)
                })
                .Where(x => x.Remaining > 0)
                .OrderBy(x => x.Balance.ChargedAt)
                .ToList();

            var totalAvailable = openCharges.Sum(x => x.Remaining);

            if (request.AmountUsed > totalAvailable)
            {
                throw new InvalidOperationException(
                    $"المبلغ المراد تقفيله ({request.AmountUsed:N2}) يتجاوز إجمالي الرصيد المتاح للفرع ({totalAvailable:N2}) في هذه الوسيلة.");
            }

            var remainingToAllocate = request.AmountUsed;
            var createdClosures = new List<BalanceClosureResponse>();

            foreach (var item in openCharges)
            {
                if (remainingToAllocate <= 0) break;

                var allocate = Math.Min(remainingToAllocate, item.Remaining);
                var closure = new BalanceClosure
                {
                    BalanceId = item.Balance.Id,
                    AmountUsed = allocate,
                    ClosedAt = request.ClosedAt ?? DateTime.UtcNow,
                    Notes = request.Notes?.Trim()
                };

                await _balanceRepo.AddClosureAsync(closure, ct);
                remainingToAllocate -= allocate;

                createdClosures.Add(new BalanceClosureResponse
                {
                    Id = closure.Id,
                    BalanceId = closure.BalanceId,
                    AmountUsed = closure.AmountUsed,
                    ClosedAt = closure.ClosedAt,
                    Notes = closure.Notes
                });
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return createdClosures;
        }

        public async Task<List<BalanceTransactionItemDto>> GetLedgerAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var charges = await _balanceRepo.GetChargesAsync(storeId, balanceType, null, null, ct);
            var transactions = new List<BalanceTransactionItemDto>();

            foreach (var b in charges)
            {
                // شحنة الرصيد
                transactions.Add(new BalanceTransactionItemDto
                {
                    Id = b.Id,
                    TransactionType = "شحن رصيد (+)",
                    StoreId = b.StoreId,
                    BalanceType = b.BalanceType,
                    BalanceTypeName = GetBalanceTypeName(b.BalanceType),
                    Amount = b.Amount,
                    TransactionDate = b.ChargedAt,
                    Notes = b.Notes,
                    BalanceId = b.Id
                });

                // تقفيلاتها
                foreach (var c in b.Closures)
                {
                    transactions.Add(new BalanceTransactionItemDto
                    {
                        Id = c.Id,
                        TransactionType = "تقفيل يومي (-)",
                        StoreId = b.StoreId,
                        BalanceType = b.BalanceType,
                        BalanceTypeName = GetBalanceTypeName(b.BalanceType),
                        Amount = c.AmountUsed,
                        TransactionDate = c.ClosedAt,
                        Notes = c.Notes,
                        BalanceId = b.Id
                    });
                }
            }

            // تصفية التاريخ
            var query = transactions.AsEnumerable();
            if (fromDate.HasValue)
                query = query.Where(t => t.TransactionDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(t => t.TransactionDate <= toDate.Value);

            return query.OrderByDescending(t => t.TransactionDate).ToList();
        }

        // ================================================================
        // Helper Mappings
        // ================================================================

        private static BalanceResponse MapToResponse(Balance balance)
        {
            var used = balance.Closures?.Sum(c => c.AmountUsed) ?? 0;
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
                Closures = balance.Closures?.Select(c => new BalanceClosureResponse
                {
                    Id = c.Id,
                    BalanceId = c.BalanceId,
                    AmountUsed = c.AmountUsed,
                    ClosedAt = c.ClosedAt,
                    Notes = c.Notes
                }).OrderByDescending(c => c.ClosedAt).ToList() ?? new List<BalanceClosureResponse>()
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
