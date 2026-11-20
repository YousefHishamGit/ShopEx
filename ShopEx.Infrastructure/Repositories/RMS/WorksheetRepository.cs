using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Domain.RMSEntity;
using ShopEx.Infrastructure.Persistence.RMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.RMS
{
    public class WorksheetRepository : IWorksheetRepository
    {
        private readonly RMSContext _context;

        public WorksheetRepository(RMSContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddLinesAsync(IEnumerable<Worksheet_PurchaseOrder> lines, CancellationToken cancellationToken = default)
        {
            if (lines != null && lines.Any())
            {
                await _context.Worksheet_PurchaseOrders.AddRangeAsync(lines, cancellationToken);
            }
        }

        public async Task AddWorkSheetAsync(Worksheet worksheet, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(worksheet);
            await _context.Worksheet.AddAsync(worksheet, cancellationToken);
        }

        public async Task<Worksheet?> GetByIdAsync(int worksheetId, CancellationToken cancellationToken = default)
        {
            return await _context.Worksheet
                .Include(w => w.Worksheet_PurchaseOrders)
                .FirstOrDefaultAsync(w => w.ID == worksheetId, cancellationToken);
        }

        public async Task<List<Worksheet>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Worksheet
                .Include(w => w.Worksheet_PurchaseOrders)
                .OrderByDescending(w => w.ID)
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
