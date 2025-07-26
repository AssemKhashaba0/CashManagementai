using CashManagement.Data;
using CashManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OtherProfitsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OtherProfitsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: OtherProfits
        public async Task<IActionResult> Index()
        {
            var profits = await _context.OtherProfits
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var today = DateTime.UtcNow.Date;
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var currentYear = new DateTime(DateTime.UtcNow.Year, 1, 1);

            var viewModel = new OtherProfitsIndexViewModel
            {
                OtherProfits = profits,
                TodayTotal = profits.Where(p => p.CreatedAt.Date == today).Sum(p => p.Amount),
                MonthlyTotal = profits.Where(p => p.CreatedAt >= currentMonth).Sum(p => p.Amount),
                YearlyTotal = profits.Where(p => p.CreatedAt >= currentYear).Sum(p => p.Amount),
                TotalCount = profits.Count,
                PhysicalCashTotal = profits.Where(p => p.DepositType == OtherProfitDepositType.PhysicalCash).Sum(p => p.Amount),
                InstaPayTotal = profits.Where(p => p.DepositType == OtherProfitDepositType.InstaPay).Sum(p => p.Amount),
                CashLineTotal = profits.Where(p => p.DepositType == OtherProfitDepositType.CashLine).Sum(p => p.Amount)
            };

            return View(viewModel);
        }

        // GET: OtherProfits/Create
        public async Task<IActionResult> Create()
        {
            var model = new OtherProfit();
            
            // جلب الخطوط النقدية النشطة للاختيار
            ViewBag.ActiveCashLines = await _context.CashLines
                .Where(cl => cl.Status == AccountStatus.Active)
                .Select(cl => new { cl.Id, cl.PhoneNumber })
                .ToListAsync();

            // جلب حسابات InstaPay النشطة
            ViewBag.ActiveInstaPayAccounts = await _context.InstaPays
                .Where(ip => ip.Status == AccountStatus.Active)
                .Select(ip => new { ip.Id, ip.PhoneNumber })
                .ToListAsync();

            return View(model);
        }

        // POST: OtherProfits/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OtherProfit model, int? selectedCashLineId, int? selectedInstaPayId)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                model.UserId = user.Id;
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                _context.OtherProfits.Add(model);
                await _context.SaveChangesAsync();

                // تحديث الأرصدة حسب نوع الإيداع
                await UpdateBalances(model, selectedCashLineId, selectedInstaPayId);

                // تحديث الأرباح اليومية
                await UpdateDailyProfit(model.Amount);

                // تسجيل النشاط
                await LogActivity(user.Id, "Add", "OtherProfit", model.Id, 
                    $"تم إضافة ربح آخر بمبلغ {model.Amount:N2} - {model.Description}");

                TempData["Success"] = "تم إضافة الربح الآخر بنجاح وتحديث الأرصدة.";
                return RedirectToAction(nameof(Index));
            }

            // إعادة تحميل البيانات في حالة الخطأ
            ViewBag.ActiveCashLines = await _context.CashLines
                .Where(cl => cl.Status == AccountStatus.Active)
                .Select(cl => new { cl.Id, cl.PhoneNumber })
                .ToListAsync();

            ViewBag.ActiveInstaPayAccounts = await _context.InstaPays
                .Where(ip => ip.Status == AccountStatus.Active)
                .Select(ip => new { ip.Id, ip.PhoneNumber })
                .ToListAsync();

            return View(model);
        }

        // GET: OtherProfits/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var profit = await _context.OtherProfits
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profit == null)
            {
                return NotFound();
            }

            return View(profit);
        }

        // GET: OtherProfits/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var profit = await _context.OtherProfits
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profit == null)
            {
                return NotFound();
            }

            return View(profit);
        }

        // POST: OtherProfits/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var profit = await _context.OtherProfits.FindAsync(id);
            if (profit != null)
            {
                var user = await _userManager.GetUserAsync(User);
                
                _context.OtherProfits.Remove(profit);
                await _context.SaveChangesAsync();

                // تسجيل النشاط
                await LogActivity(user.Id, "Delete", "OtherProfit", profit.Id, 
                    $"تم حذف ربح آخر بمبلغ {profit.Amount:N2} - {profit.Description}");

                TempData["Success"] = "تم حذف الربح الآخر بنجاح.";
            }

            return RedirectToAction(nameof(Index));
        }

        // تحديث الأرصدة حسب نوع الإيداع
        private async Task UpdateBalances(OtherProfit profit, int? cashLineId, int? instaPayId)
        {
            switch (profit.DepositType)
            {
                case OtherProfitDepositType.PhysicalCash:
                    await UpdatePhysicalCashBalance(profit.Amount);
                    break;

                case OtherProfitDepositType.InstaPay:
                    if (instaPayId.HasValue)
                    {
                        await UpdateInstaPayBalance(instaPayId.Value, profit.Amount);
                    }
                    break;

                case OtherProfitDepositType.CashLine:
                    if (cashLineId.HasValue)
                    {
                        await UpdateCashLineBalance(cashLineId.Value, profit.Amount);
                    }
                    break;
            }

            // تحديث رصيد النظام العام
            await UpdateSystemBalance();
        }

        private async Task UpdatePhysicalCashBalance(decimal amount)
        {
            var transaction = new CashTransaction_Physical
            {
                Amount = amount,
                Description = "ربح آخر - إيداع نقدي",
                TransactionType = TransactionType.Deposit,
                UserId = (await _userManager.GetUserAsync(User)).Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashTransactionsPhysical.Add(transaction);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateInstaPayBalance(int instaPayId, decimal amount)
        {
            var instaPay = await _context.InstaPays.FindAsync(instaPayId);
            if (instaPay != null)
            {
                instaPay.CurrentBalance += amount;
                instaPay.UpdatedAt = DateTime.UtcNow;
                _context.InstaPays.Update(instaPay);
                await _context.SaveChangesAsync();
            }
        }

        private async Task UpdateCashLineBalance(int cashLineId, decimal amount)
        {
            var cashLine = await _context.CashLines.FindAsync(cashLineId);
            if (cashLine != null)
            {
                cashLine.CurrentBalance += amount;
                cashLine.UpdatedAt = DateTime.UtcNow;
                _context.CashLines.Update(cashLine);
                await _context.SaveChangesAsync();
            }
        }

        private async Task UpdateSystemBalance()
        {
            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            if (systemBalance == null)
            {
                systemBalance = new SystemBalance();
                _context.SystemBalances.Add(systemBalance);
            }

            systemBalance.TotalPhysicalCash = await _context.CashTransactionsPhysical
                .SumAsync(t => t.TransactionType == TransactionType.Deposit ? t.Amount : -t.Amount);

            systemBalance.TotalCashLineBalance = await _context.CashLines
                .Where(cl => cl.Status == AccountStatus.Active)
                .SumAsync(cl => cl.CurrentBalance);

            systemBalance.TotalInstaPayBalance = await _context.InstaPays
                .Where(ip => ip.Status == AccountStatus.Active)
                .SumAsync(ip => ip.CurrentBalance);

            systemBalance.TotalSystemBalance = systemBalance.TotalPhysicalCash + 
                                             systemBalance.TotalCashLineBalance + 
                                             systemBalance.TotalInstaPayBalance;
            systemBalance.LastUpdated = DateTime.UtcNow;

            _context.SystemBalances.Update(systemBalance);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateDailyProfit(decimal amount)
        {
            var today = DateTime.UtcNow.Date;
            var dailyProfit = await _context.DailyProfits
                .FirstOrDefaultAsync(dp => dp.Date == today);

            if (dailyProfit == null)
            {
                dailyProfit = new CashManagement.Models.DailyProfit
                {
                    Date = today,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DailyProfits.Add(dailyProfit);
                await _context.SaveChangesAsync();
            }

            dailyProfit.TotalProfit += amount;
            dailyProfit.UpdatedAt = DateTime.UtcNow;

            _context.DailyProfits.Update(dailyProfit);
            await _context.SaveChangesAsync();
        }

        private async Task LogActivity(string userId, string action, string entityType, int entityId, string description)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                ActionType = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }

    public class OtherProfitsIndexViewModel
    {
        public List<OtherProfit> OtherProfits { get; set; }
        public decimal TodayTotal { get; set; }
        public decimal MonthlyTotal { get; set; }
        public decimal YearlyTotal { get; set; }
        public int TotalCount { get; set; }
        public decimal PhysicalCashTotal { get; set; }
        public decimal InstaPayTotal { get; set; }
        public decimal CashLineTotal { get; set; }
    }
}

