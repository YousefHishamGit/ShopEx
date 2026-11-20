using ShopEx.Application.DTOs.Consignment;
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
    public class DealerService : IDealerService
    {
        private readonly IDealerRepository _dealerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DealerService(IDealerRepository dealerRepository, IUnitOfWork unitOfWork)
        {
            _dealerRepository = dealerRepository ?? throw new ArgumentNullException(nameof(dealerRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<List<DealerResponse>> GetAllAsync(bool? activeOnly = null, string? search = null, CancellationToken ct = default)
        {
            var dealers = await _dealerRepository.GetAllAsync(activeOnly, search, ct);
            return dealers.Select(MapToResponse).ToList();
        }

        public async Task<DealerResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var dealer = await _dealerRepository.GetByIdAsync(id, ct);
            return dealer == null ? null : MapToResponse(dealer);
        }

        public async Task<DealerResponse> CreateAsync(CreateDealerRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("اسم التاجر مطلوب.", nameof(request.Name));

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new ArgumentException("رقم الهاتف مطلوب.", nameof(request.Phone));

            var phoneExists = await _dealerRepository.ExistsByPhoneAsync(request.Phone.Trim(), null, ct);
            if (phoneExists)
                throw new InvalidOperationException($"يوجد تاجر آخر مسجل بنفس رقم الهاتف ({request.Phone}).");

            var dealer = new Dealer
            {
                Name = request.Name.Trim(),
                Phone = request.Phone.Trim(),
                Address = request.Address?.Trim(),
                Notes = request.Notes?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _dealerRepository.AddAsync(dealer, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(dealer);
        }

        public async Task<DealerResponse> UpdateAsync(int id, UpdateDealerRequest request, CancellationToken ct = default)
        {
            var dealer = await _dealerRepository.GetByIdAsync(id, ct);
            if (dealer == null)
                throw new KeyNotFoundException($"التاجر برقم {id} غير موجود.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("اسم التاجر مطلوب.", nameof(request.Name));

            if (string.IsNullOrWhiteSpace(request.Phone))
                throw new ArgumentException("رقم الهاتف مطلوب.", nameof(request.Phone));

            var phoneExists = await _dealerRepository.ExistsByPhoneAsync(request.Phone.Trim(), id, ct);
            if (phoneExists)
                throw new InvalidOperationException($"يوجد تاجر آخر مسجل بنفس رقم الهاتف ({request.Phone}).");

            dealer.Name = request.Name.Trim();
            dealer.Phone = request.Phone.Trim();
            dealer.Address = request.Address?.Trim();
            dealer.Notes = request.Notes?.Trim();
            dealer.IsActive = request.IsActive;

            _dealerRepository.Update(dealer);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(dealer);
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var dealer = await _dealerRepository.GetByIdAsync(id, ct);
            if (dealer == null)
                throw new KeyNotFoundException($"التاجر برقم {id} غير موجود.");

            if (dealer.Consignments != null && dealer.Consignments.Any(c => c.Status == ConsignmentStatus.Open))
            {
                throw new InvalidOperationException("لا يمكن حذف التاجر لوجود أذونات أمانة مفتوحة مرتبطة به.");
            }

            _dealerRepository.Delete(dealer);
            await _unitOfWork.SaveChangesAsync(ct);
        }

        private static DealerResponse MapToResponse(Dealer d)
        {
            var consignments = d.Consignments ?? new List<Consignment>();
            var openConsignments = consignments.Where(c => c.Status == ConsignmentStatus.Open).ToList();

            return new DealerResponse
            {
                ID = d.ID,
                Name = d.Name,
                Phone = d.Phone,
                Address = d.Address,
                Notes = d.Notes,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt,
                ActiveConsignmentsCount = openConsignments.Count,
                TotalOutstandingValue = openConsignments.Sum(c => c.OutstandingValue),
                TotalUnpaidAmount = openConsignments.Sum(c => c.RemainingAmount)
            };
        }
    }
}
