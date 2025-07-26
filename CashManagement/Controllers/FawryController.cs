using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CashManagement.Data;
using CashManagement.Models;
using Microsoft.AspNetCore.Authorization;
using CashManagement.ViewModels;

namespace CashManagement.Controllers
{
    [Authorize]
    public class FawryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FawryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Fawry
        public async Task<IActionResult> Index()
        {
            var fawryTransactions = await _context.FawryTransactions
                .Include(f => f.User)
                .Include(f => f.FawryService)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var viewModel = new FawryIndexViewModel
            {
                FawryTransactions = fawryTransactions,
                // فوري عادي
                RegularFawryBalance = await CalculateRegularFawryBalance(),
                RegularFawryTodayTotal = await CalculateRegularFawryTodayTotal(),
                
                // فوري مشتريات
                PurchasesFawryBalance = await CalculatePurchasesFawryBalance(),
                PurchasesFawryTodayTotal = await CalculatePurchasesFawryTodayTotal()
            };

            return View(viewModel);
        }

        // GET: Fawry/CreateRegular
        public async Task<IActionResult> CreateRegular()
        {
            return View();
        }

        // POST: Fawry/CreateRegular
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRegular(FawryTransactionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(User);
                
                // جيب أي خدمة موجودة أو اعمل واحدة جديدة
                var service = await _context.FawryServices.FirstOrDefaultAsync();
                if (service == null)
                {
                    service = new FawryService
                    {
                        ServiceName = "خدمة افتراضية",
                        Description = "خدمة افتراضية",
                        ServiceType = FawryServiceType.Regular,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.FawryServices.Add(service);
                    await _context.SaveChangesAsync();
                }

                var feesAmount = model.ManualFees ?? 0m;
                
                var transaction = new FawryTransaction
                {
                    Amount = model.Amount,
                    TransactionType = model.TransactionType,
                    UserId = user.Id,
                    ServiceId = service.Id,
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Completed,
                    FeesAmount = feesAmount,
                    ManualFees = model.ManualFees,
                    NetAmount = model.TransactionType == TransactionType.Deposit 
                        ? model.Amount - feesAmount
                        : model.Amount + feesAmount,
                    Description = model.Description
                };

                _context.FawryTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تنفيذ العملية بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ: {ex.Message}";
                return View(model);
            }
        }

        // GET: Fawry/CreatePurchases
        public async Task<IActionResult> CreatePurchases()
        {
            return View();
        }

        // POST: Fawry/CreatePurchases
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchases(FawryTransactionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(User);
                
                var feesAmount = model.ManualFees ?? 0m;
                
                var transaction = new FawryTransaction
                {
                    Amount = model.Amount,
                    TransactionType = model.TransactionType,
                    UserId = user.Id,
                    ServiceId = null, // مش محتاجين خدمة
                    CreatedAt = DateTime.UtcNow,
                    Status = TransactionStatus.Completed,
                    FeesAmount = feesAmount,
                    ManualFees = model.ManualFees,
                    NetAmount = model.TransactionType == TransactionType.Deposit 
                        ? model.Amount - feesAmount
                        : model.Amount + feesAmount,
                    Description = model.Description
                };

                _context.FawryTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تنفيذ العملية بنجاح";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ في تنفيذ العملية: {ex.Message}";
                return View(model);
            }
        }

        private async Task UpdateSystemBalance(FawryTransaction transaction)
        {
            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            if (systemBalance == null)
            {
                systemBalance = new SystemBalance
                {
                    TotalPhysicalCash = 0,
                    TotalCashLineBalance = 0,
                    TotalInstaPayBalance = 0,
                    TotalSystemBalance = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.SystemBalances.Add(systemBalance);
            }

            if (transaction.TransactionType == TransactionType.Withdraw)
            {
                // السحب: نقص من النقدية + إضافة الأرباح
                systemBalance.TotalPhysicalCash -= transaction.Amount;
                systemBalance.TotalPhysicalCash += transaction.FeesAmount;
            }
            else
            {
                // الإيداع: زيادة في النقدية
                systemBalance.TotalPhysicalCash += transaction.Amount;
            }

            systemBalance.TotalSystemBalance = systemBalance.TotalPhysicalCash + 
                                            systemBalance.TotalCashLineBalance + 
                                            systemBalance.TotalInstaPayBalance;
            systemBalance.LastUpdated = DateTime.UtcNow;

            _context.SystemBalances.Update(systemBalance);
        }

        private async Task UpdateDailyProfit(decimal profit, FawryServiceType serviceType)
        {
            var today = DateTime.UtcNow.Date;
            var dailyProfit = await _context.DailyProfits
                .FirstOrDefaultAsync(dp => dp.Date == today);

            if (dailyProfit == null)
            {
                dailyProfit = new DailyProfit
                {
                    Date = today,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DailyProfits.Add(dailyProfit);
                await _context.SaveChangesAsync();
            }

            dailyProfit.FawryProfit += profit;
            dailyProfit.TotalProfit = dailyProfit.CashLineProfit + dailyProfit.InstaPayProfit + dailyProfit.FawryProfit;
            dailyProfit.UpdatedAt = DateTime.UtcNow;

            _context.DailyProfits.Update(dailyProfit);
            await _context.SaveChangesAsync();
        }

        private async Task<decimal> CalculateRegularFawryBalance()
        {
            return await _context.FawryTransactions
                .Where(f => f.FawryService.ServiceType == FawryServiceType.Regular)
                .SumAsync(f => f.TransactionType == TransactionType.Deposit ? f.Amount : -f.Amount);
        }

        private async Task<decimal> CalculateRegularFawryTodayTotal()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.FawryTransactions
                .Where(f => f.FawryService.ServiceType == FawryServiceType.Regular && f.CreatedAt.Date == today)
                .SumAsync(f => f.Amount);
        }

        private async Task<decimal> CalculatePurchasesFawryBalance()
        {
            return await _context.FawryTransactions
                .Where(f => f.FawryService.ServiceType == FawryServiceType.Purchases)
                .SumAsync(f => f.TransactionType == TransactionType.Deposit ? f.Amount : -f.Amount);
        }

        private async Task<decimal> CalculatePurchasesFawryTodayTotal()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.FawryTransactions
                .Where(f => f.FawryService.ServiceType == FawryServiceType.Purchases && f.CreatedAt.Date == today)
                .SumAsync(f => f.Amount);
        }

        // GET: Fawry/AddDefaultServices
        // public async Task<IActionResult> AddDefaultServices() { ... }
    }
}





