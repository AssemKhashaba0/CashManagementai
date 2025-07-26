using CashManagement.Data;
using CashManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Services
{
    public class DailyResetService
    {
        private readonly ApplicationDbContext _context;

        public DailyResetService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ResetDailyLimitsAsync()
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
            
            var cashLines = await _context.CashLines
                .Where(cl => cl.Status != AccountStatus.Deleted)
                .ToListAsync();

            foreach (var cashLine in cashLines)
            {
                var lastReset = cashLine.LastResetDate ?? cashLine.CreatedAt;
                var lastResetEgyptTime = TimeZoneInfo.ConvertTimeFromUtc(lastReset, egyptTimeZone);

                // إعادة تعيين الحدود اليومية إذا مر يوم جديد
                if (egyptTime.Date > lastResetEgyptTime.Date)
                {
                    cashLine.DailyWithdrawUsed = 0;
                    cashLine.DailyDepositUsed = 0;
                    cashLine.LastResetDate = DateTime.UtcNow;
                    cashLine.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task ResetMonthlyLimitsAsync()
        {
            var egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var egyptTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);
            
            // التأكد أنه أول يوم في الشهر
            if (egyptTime.Day == 1)
            {
                var cashLines = await _context.CashLines
                    .Where(cl => cl.Status != AccountStatus.Deleted)
                    .ToListAsync();

                foreach (var cashLine in cashLines)
                {
                    // إعادة تعيين الحدود الشهرية
                    cashLine.MonthlyWithdrawUsed = 0;
                    cashLine.MonthlyDepositUsed = 0;
                    
                    // فك التجميد للخطوط المجمدة
                    if (cashLine.Status == AccountStatus.Frozen)
                    {
                        cashLine.Status = AccountStatus.Active;
                    }
                    
                    cashLine.LastResetDate = DateTime.UtcNow;
                    cashLine.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
        }
    }
}
