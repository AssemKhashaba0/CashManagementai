using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CashManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using CashManagement.Data;

namespace CashManagement.Controllers
{
    [Authorize]
    public class CashLinesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CashLinesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(decimal? amount, TransactionType? transactionType)
        {
            var user = await _userManager.GetUserAsync(User);
            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            var query = _context.CashLines.AsQueryable();
            if (!isManager)
            {
                query = query.Where(cl => cl.Status == AccountStatus.Active &&
                                         cl.DailyWithdrawUsed < cl.DailyWithdrawLimit &&
                                         cl.DailyDepositUsed < cl.DailyDepositLimit &&
                                         cl.MonthlyWithdrawUsed < cl.MonthlyWithdrawLimit &&
                                         cl.MonthlyDepositUsed < cl.MonthlyDepositLimit);
            }

            // Filter by amount and transaction type if provided
            if (amount.HasValue && amount.Value > 0 && transactionType.HasValue)
            {
                if (transactionType == TransactionType.Withdraw)
                {
                    query = query.Where(cl => cl.CurrentBalance >= amount.Value &&
                                             (cl.DailyWithdrawLimit - cl.DailyWithdrawUsed) >= amount.Value &&
                                             (cl.MonthlyWithdrawLimit - cl.MonthlyWithdrawUsed) >= amount.Value);
                }
                else if (transactionType == TransactionType.Deposit)
                {
                    query = query.Where(cl => (cl.DailyDepositLimit - cl.DailyDepositUsed) >= amount.Value &&
                                             (cl.MonthlyDepositLimit - cl.MonthlyDepositUsed) >= amount.Value);
                }
            }

            var cashLines = await query.Select(cl => new CashLineViewModel
            {
                Id = cl.Id,
                PhoneNumber = cl.PhoneNumber,
                OwnerName = cl.OwnerName,
                NationalId = cl.NationalId,
                NetworkType = cl.NetworkType,
                CurrentBalance = cl.CurrentBalance,
                DailyWithdrawLimit = cl.DailyWithdrawLimit,
                DailyDepositLimit = cl.DailyDepositLimit,
                MonthlyWithdrawLimit = cl.MonthlyWithdrawLimit,
                MonthlyDepositLimit = cl.MonthlyDepositLimit,
                DailyWithdrawUsed = cl.DailyWithdrawUsed,
                DailyDepositUsed = cl.DailyDepositUsed,
                MonthlyWithdrawUsed = cl.MonthlyWithdrawUsed,
                MonthlyDepositUsed = cl.MonthlyDepositUsed,
                Status = cl.Status,
                DailyWithdrawRemaining = cl.DailyWithdrawLimit - cl.DailyWithdrawUsed,
                DailyDepositRemaining = cl.DailyDepositLimit - cl.DailyDepositUsed,
                MonthlyWithdrawRemaining = cl.MonthlyWithdrawLimit - cl.MonthlyWithdrawUsed,
                MonthlyDepositRemaining = cl.MonthlyDepositLimit - cl.MonthlyDepositUsed,
                DailyWithdrawRemainingPercentage = (cl.DailyWithdrawLimit > 0) ? Math.Min(100, Math.Max(0, (1 - (cl.DailyWithdrawUsed / cl.DailyWithdrawLimit)) * 100)) : 0,
                DailyDepositRemainingPercentage = (cl.DailyDepositLimit > 0) ? Math.Min(100, Math.Max(0, (1 - (cl.DailyDepositUsed / cl.DailyDepositLimit)) * 100)) : 0,
                MonthlyWithdrawRemainingPercentage = (cl.MonthlyWithdrawLimit > 0) ? Math.Min(100, Math.Max(0, (1 - (cl.MonthlyWithdrawUsed / cl.MonthlyWithdrawLimit)) * 100)) : 0,
                MonthlyDepositRemainingPercentage = (cl.MonthlyDepositLimit > 0) ? Math.Min(100, Math.Max(0, (1 - (cl.MonthlyDepositUsed / cl.MonthlyDepositLimit)) * 100)) : 0
            }).ToListAsync();

            ViewBag.Amount = amount;
            ViewBag.TransactionType = transactionType;
            return View(cashLines);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CashLine cashLine)
        {
            if (!ModelState.IsValid)
            {
                return View(cashLine);
            }

            if (await _context.CashLines.AnyAsync(cl => cl.PhoneNumber == cashLine.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "رقم الهاتف مسجل مسبقًا.");
                return View(cashLine);
            }

            if (await _context.CashLines.AnyAsync(cl => cl.NationalId == cashLine.NationalId))
            {
                ModelState.AddModelError("NationalId", "الرقم القومي مسجل مسبقًا.");
                return View(cashLine);
            }

            if (cashLine.MonthlyWithdrawLimit < cashLine.DailyWithdrawLimit)
            {
                ModelState.AddModelError("MonthlyWithdrawLimit", "الحد الشهري للسحب يجب أن يكون أكبر من أو يساوي الحد اليومي للسحب.");
                return View(cashLine);
            }

            if (cashLine.MonthlyDepositLimit < cashLine.DailyDepositLimit)
            {
                ModelState.AddModelError("MonthlyDepositLimit", "الحد الشهري للإيداع يجب أن يكون أكبر من أو يساوي الحد اليومي للإيداع.");
                return View(cashLine);
            }

            cashLine.CreatedAt = DateTime.UtcNow;
            cashLine.UpdatedAt = DateTime.UtcNow;

            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            if (systemBalance != null && cashLine.CurrentBalance > 0)
            {
                systemBalance.TotalPhysicalCash -= cashLine.CurrentBalance;
                systemBalance.TotalSystemBalance -= cashLine.CurrentBalance;
                systemBalance.LastUpdated = DateTime.UtcNow;
            }

            _context.CashLines.Add(cashLine);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إضافة الخط بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var cashLine = await _context.CashLines.FindAsync(id);
            if (cashLine == null)
            {
                return NotFound("الخط غير موجود.");
            }
            return View(cashLine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CashLine cashLine)
        {
            if (id != cashLine.Id)
            {
                return BadRequest("معرف الخط غير متطابق.");
            }

            if (!ModelState.IsValid)
            {
                return View(cashLine);
            }

            var existingCashLine = await _context.CashLines.FindAsync(id);
            if (existingCashLine == null)
            {
                return NotFound("الخط غير موجود.");
            }

            if (cashLine.PhoneNumber != existingCashLine.PhoneNumber &&
                await _context.CashLines.AnyAsync(cl => cl.PhoneNumber == cashLine.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", "رقم الهاتف مسجل لخط آخر.");
                return View(cashLine);
            }

            if (cashLine.NationalId != existingCashLine.NationalId &&
                await _context.CashLines.AnyAsync(cl => cl.NationalId == cashLine.NationalId))
            {
                ModelState.AddModelError("NationalId", "الرقم القومي مسجل لخط آخر.");
                return View(cashLine);
            }

            if (cashLine.MonthlyWithdrawLimit < cashLine.DailyWithdrawLimit)
            {
                ModelState.AddModelError("MonthlyWithdrawLimit", "الحد الشهري للسحب يجب أن يكون أكبر من أو يساوي الحد اليومي للسحب.");
                return View(cashLine);
            }

            if (cashLine.MonthlyDepositLimit < cashLine.DailyDepositLimit)
            {
                ModelState.AddModelError("MonthlyDepositLimit", "الحد الشهري للإيداع يجب أن يكون أكبر من أو يساوي الحد اليومي للإيداع.");
                return View(cashLine);
            }

            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            if (systemBalance != null)
            {
                systemBalance.TotalPhysicalCash -= (cashLine.CurrentBalance - existingCashLine.CurrentBalance);
                systemBalance.TotalSystemBalance -= (cashLine.CurrentBalance - existingCashLine.CurrentBalance);
                systemBalance.LastUpdated = DateTime.UtcNow;
            }

            existingCashLine.PhoneNumber = cashLine.PhoneNumber;
            existingCashLine.OwnerName = cashLine.OwnerName;
            existingCashLine.NationalId = cashLine.NationalId;
            existingCashLine.NetworkType = cashLine.NetworkType;
            existingCashLine.CurrentBalance = cashLine.CurrentBalance;
            existingCashLine.DailyWithdrawLimit = cashLine.DailyWithdrawLimit;
            existingCashLine.DailyDepositLimit = cashLine.DailyDepositLimit;
            existingCashLine.MonthlyWithdrawLimit = cashLine.MonthlyWithdrawLimit;
            existingCashLine.MonthlyDepositLimit = cashLine.MonthlyDepositLimit;
            existingCashLine.Status = cashLine.Status;
            existingCashLine.UpdatedAt = DateTime.UtcNow;

            _context.Entry(existingCashLine).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تعديل الخط بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cashLine = await _context.CashLines.FindAsync(id);
            if (cashLine == null)
            {
                return NotFound("الخط غير موجود.");
            }

            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            if (systemBalance != null && cashLine.CurrentBalance > 0)
            {
                systemBalance.TotalPhysicalCash += cashLine.CurrentBalance;
                systemBalance.TotalSystemBalance += cashLine.CurrentBalance;
                systemBalance.LastUpdated = DateTime.UtcNow;
            }

            cashLine.Status = AccountStatus.Deleted;
            cashLine.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف الخط بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Freeze(int id)
        {
            var cashLine = await _context.CashLines.FindAsync(id);
            if (cashLine == null)
            {
                return NotFound("الخط غير موجود.");
            }

            if (cashLine.Status == AccountStatus.Frozen)
            {
                TempData["ErrorMessage"] = "الخط مجمد بالفعل.";
                return RedirectToAction(nameof(Index));
            }

            cashLine.Status = AccountStatus.Frozen;
            cashLine.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تجميد الخط بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfreeze(int id)
        {
            var cashLine = await _context.CashLines.FindAsync(id);
            if (cashLine == null)
            {
                return NotFound("الخط غير موجود.");
            }

            if (cashLine.Status == AccountStatus.Active)
            {
                TempData["ErrorMessage"] = "الخط نشط بالفعل.";
                return RedirectToAction(nameof(Index));
            }

            cashLine.Status = AccountStatus.Active;
            cashLine.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تفعيل الخط بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var cashLine = await _context.CashLines
                .Include(cl => cl.CashTransactions)
                .Select(cl => new CashLineViewModel
                {
                    Id = cl.Id,
                    PhoneNumber = cl.PhoneNumber,
                    OwnerName = cl.OwnerName,
                    NationalId = cl.NationalId,
                    NetworkType = cl.NetworkType,
                    CurrentBalance = cl.CurrentBalance,
                    DailyWithdrawLimit = cl.DailyWithdrawLimit,
                    DailyDepositLimit = cl.DailyDepositLimit,
                    MonthlyWithdrawLimit = cl.MonthlyWithdrawLimit,
                    MonthlyDepositLimit = cl.MonthlyDepositLimit,
                    DailyWithdrawUsed = cl.DailyWithdrawUsed,
                    DailyDepositUsed = cl.DailyDepositUsed,
                    MonthlyWithdrawUsed = cl.MonthlyWithdrawUsed,
                    MonthlyDepositUsed = cl.MonthlyDepositUsed,
                    Status = cl.Status,
                    DailyWithdrawRemainingPercentage = (cl.DailyWithdrawLimit > 0) ? (1 - (cl.DailyWithdrawUsed / cl.DailyWithdrawLimit)) * 100 : 0,
                    DailyDepositRemainingPercentage = (cl.DailyDepositLimit > 0) ? (1 - (cl.DailyDepositUsed / cl.DailyDepositLimit)) * 100 : 0,
                    MonthlyWithdrawRemainingPercentage = (cl.MonthlyWithdrawLimit > 0) ? (1 - (cl.MonthlyWithdrawUsed / cl.MonthlyWithdrawLimit)) * 100 : 0,
                    MonthlyDepositRemainingPercentage = (cl.MonthlyDepositLimit > 0) ? (1 - (cl.MonthlyDepositUsed / cl.MonthlyDepositLimit)) * 100 : 0,
                    Transactions = cl.CashTransactions.Select(t => new CashTransactionViewModel
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        Fees = t.Fees,
                        NetAmount = t.NetAmount,
                        TransactionType = t.TransactionType,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt
                    }).ToList()
                })
                .FirstOrDefaultAsync(cl => cl.Id == id);

            if (cashLine == null)
            {
                return NotFound("الخط غير موجود.");
            }

            return View(cashLine);
        }

        [HttpPost]
        public async Task<IActionResult> SearchAvailableLines(decimal amount, TransactionType transactionType)
        {
            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "المبلغ يجب أن يكون أكبر من صفر.";
                return RedirectToAction(nameof(Index));
            }

            var cashLines = await _context.CashLines
                .AsNoTracking()
                .Where(cl => cl.Status == AccountStatus.Active &&
                             cl.CurrentBalance >= (transactionType == TransactionType.Withdraw ? amount : 0) &&
                             (transactionType == TransactionType.Withdraw ?
                                (cl.DailyWithdrawLimit - cl.DailyWithdrawUsed >= amount &&
                                 cl.MonthlyWithdrawLimit - cl.MonthlyWithdrawUsed >= amount) :
                                (cl.DailyDepositLimit - cl.DailyDepositUsed >= amount &&
                                 cl.MonthlyDepositLimit - cl.MonthlyDepositUsed >= amount)))
                .Select(cl => new CashLineViewModel
                {
                    Id = cl.Id,
                    PhoneNumber = cl.PhoneNumber,
                    OwnerName = cl.OwnerName,
                    NationalId = cl.NationalId,
                    CurrentBalance = cl.CurrentBalance,
                    DailyWithdrawLimit = cl.DailyWithdrawLimit,
                    DailyDepositLimit = cl.DailyDepositLimit,
                    MonthlyWithdrawLimit = cl.MonthlyWithdrawLimit,
                    MonthlyDepositLimit = cl.MonthlyDepositLimit,
                    DailyWithdrawUsed = cl.DailyWithdrawUsed,
                    DailyDepositUsed = cl.DailyDepositUsed,
                    MonthlyWithdrawUsed = cl.MonthlyWithdrawUsed,
                    MonthlyDepositUsed = cl.MonthlyDepositUsed,
                    DailyWithdrawRemaining = cl.DailyWithdrawLimit - cl.DailyWithdrawUsed,
                    DailyDepositRemaining = cl.DailyDepositLimit - cl.DailyDepositUsed,
                    MonthlyWithdrawRemaining = cl.MonthlyWithdrawLimit - cl.MonthlyWithdrawUsed,
                    MonthlyDepositRemaining = cl.MonthlyDepositLimit - cl.MonthlyDepositUsed,
                    DailyWithdrawRemainingPercentage = (cl.DailyWithdrawLimit > 0) ? (1 - (cl.DailyWithdrawUsed / cl.DailyWithdrawLimit)) * 100 : 0,
                    DailyDepositRemainingPercentage = (cl.DailyDepositLimit > 0) ? (1 - (cl.DailyDepositUsed / cl.DailyDepositLimit)) * 100 : 0,
                    MonthlyWithdrawRemainingPercentage = (cl.MonthlyWithdrawLimit > 0) ? (1 - (cl.MonthlyWithdrawUsed / cl.MonthlyWithdrawLimit)) * 100 : 0,
                    MonthlyDepositRemainingPercentage = (cl.MonthlyDepositLimit > 0) ? (1 - (cl.MonthlyDepositUsed / cl.MonthlyDepositLimit)) * 100 : 0
                })
                .ToListAsync();

            if (!cashLines.Any())
            {
                TempData["ErrorMessage"] = "لم يتم العثور على خطوط نقدية متاحة لهذا المبلغ.";
            }

            ViewBag.Amount = amount;
            ViewBag.TransactionType = transactionType;
            return View(cashLines);
        }
        [HttpPost]
        public async Task<IActionResult> ResetLimits()
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

                if (egyptTime.Date > lastResetEgyptTime.Date)
                {
                    cashLine.DailyWithdrawUsed = 0;
                    cashLine.DailyDepositUsed = 0;
                    cashLine.LastResetDate = DateTime.UtcNow;
                }

                if (egyptTime.Day == 1 && egyptTime.Hour == 0)
                {
                    cashLine.MonthlyWithdrawUsed = 0;
                    cashLine.MonthlyDepositUsed = 0;
                    cashLine.LastResetDate = DateTime.UtcNow;
                    if (cashLine.Status == AccountStatus.Frozen)
                    {
                        cashLine.Status = AccountStatus.Active;
                    }
                }

                cashLine.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إعادة تعيين الحدود اليومية والشهرية بنجاح.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> FrozenLines()
        {
            var user = await _userManager.GetUserAsync(User);
            var isManager = await _userManager.IsInRoleAsync(user, "Manager");

            var query = _context.CashLines
                .Where(cl => cl.Status == AccountStatus.Frozen)
                .AsQueryable();

            if (!isManager)
            {
                query = query.Where(cl => cl.CashTransactions.Any(t => t.UserId == user.Id));
            }

            var frozenLines = await query.Select(cl => new CashLineViewModel
            {
                Id = cl.Id,
                PhoneNumber = cl.PhoneNumber,
                OwnerName = cl.OwnerName,
                NationalId = cl.NationalId,
                NetworkType = cl.NetworkType,
                CurrentBalance = cl.CurrentBalance,
                DailyWithdrawLimit = cl.DailyWithdrawLimit,
                DailyDepositLimit = cl.DailyDepositLimit,
                MonthlyWithdrawLimit = cl.MonthlyWithdrawLimit,
                MonthlyDepositLimit = cl.MonthlyDepositLimit,
                DailyWithdrawUsed = cl.DailyWithdrawUsed,
                DailyDepositUsed = cl.DailyDepositUsed,
                MonthlyWithdrawUsed = cl.MonthlyWithdrawUsed,
                MonthlyDepositUsed = cl.MonthlyDepositUsed,
                Status = cl.Status,
                DailyWithdrawRemainingPercentage = (cl.DailyWithdrawLimit > 0) ? (1 - (cl.DailyWithdrawUsed / cl.DailyWithdrawLimit)) * 100 : 0,
                DailyDepositRemainingPercentage = (cl.DailyDepositLimit > 0) ? (1 - (cl.DailyDepositUsed / cl.DailyDepositLimit)) * 100 : 0,
                MonthlyWithdrawRemainingPercentage = (cl.MonthlyWithdrawLimit > 0) ? (1 - (cl.MonthlyWithdrawUsed / cl.MonthlyWithdrawLimit)) * 100 : 0,
                MonthlyDepositRemainingPercentage = (cl.MonthlyDepositLimit > 0) ? (1 - (cl.MonthlyDepositUsed / cl.MonthlyDepositLimit)) * 100 : 0
            }).ToListAsync();

            return View(frozenLines);
        }
    }

    public class CashLineViewModel
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public string OwnerName { get; set; }
        public string NationalId { get; set; }
        public NetworkType NetworkType { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal DailyWithdrawLimit { get; set; }
        public decimal DailyDepositLimit { get; set; }
        public decimal MonthlyWithdrawLimit { get; set; }
        public decimal MonthlyDepositLimit { get; set; }
        public decimal DailyWithdrawUsed { get; set; }
        public decimal DailyDepositUsed { get; set; }
        public decimal MonthlyWithdrawUsed { get; set; }
        public decimal MonthlyDepositUsed { get; set; }
        public AccountStatus Status { get; set; }
        public decimal DailyWithdrawRemaining { get; set; } // المبلغ المتبقي للسحب اليومي
        public decimal DailyDepositRemaining { get; set; } // المبلغ المتبقي للإيداع اليومي
        public decimal MonthlyWithdrawRemaining { get; set; } // المبلغ المتبقي للسحب الشهري
        public decimal MonthlyDepositRemaining { get; set; } // المبلغ المتبقي للإيداع الشهري
        public decimal DailyWithdrawRemainingPercentage { get; set; }
        public decimal DailyDepositRemainingPercentage { get; set; }
        public decimal MonthlyWithdrawRemainingPercentage { get; set; }
        public decimal MonthlyDepositRemainingPercentage { get; set; }
        public List<CashTransactionViewModel> Transactions { get; set; }
    }
}