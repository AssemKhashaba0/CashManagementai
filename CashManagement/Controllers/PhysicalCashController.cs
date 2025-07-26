using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CashManagement.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using CashManagement.Data;
using Microsoft.AspNetCore.Authorization;

namespace CashManagement.Controllers
{
    public class PhysicalCashController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PhysicalCashController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: PhysicalCash/Index
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string searchString, DateTime? startDate, DateTime? endDate, TransactionType? type, TransactionStatus? status)
        {
            var user = await _userManager.GetUserAsync(User);
            var isManager = await _userManager.IsInRoleAsync(user, "Admin");

            var query = _context.CashTransactionsPhysical.AsQueryable();

            // تصفية حسب البحث
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.Description.Contains(searchString));
            }

            // تصفية حسب التاريخ
            if (startDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= endDate.Value.AddDays(1));
            }

            // تصفية حسب نوع العملية
            if (type.HasValue)
            {
                query = query.Where(t => t.TransactionType == type.Value);
            }

            // إذا لم يكن مديرًا، عرض العمليات الخاصة بالمستخدم فقط
            if (!isManager)
            {
                query = query.Where(t => t.UserId == user.Id);
            }

            var transactions = await query
                .Select(t => new CashTransactionPhysicalViewModel
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    UserName = t.UserId,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            // تمرير بيانات الفلترة إلى العرض
            ViewBag.SearchString = searchString;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.TransactionType = type;
            ViewBag.Status = status;
            ViewBag.IsEmployee = false;

            var lastDepositId = await _context.CashTransactionsPhysical
                .Where(t => t.TransactionType == TransactionType.Deposit)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            ViewBag.LastDepositId = lastDepositId;

            return View(transactions);
        }

        // GET: PhysicalCash/Deposit
        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        // POST: PhysicalCash/Deposit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(CashTransactionPhysicalViewModel model)
        {
            if (ModelState.IsValid)
            {
                return View(model);
            }

            // التحقق من أن المبلغ صالح
            if (model.Amount <= 0)
            {
                TempData["ErrorMessage"] = "المبلغ يجب أن يكون أكبر من صفر.";
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            var transaction = new CashTransaction_Physical
            {
                Amount = model.Amount,
                TransactionType = TransactionType.Deposit,
                Description = model.Description,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            // تحديث الرصيد النقدي في SystemBalance
            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            if (systemBalance == null)
            {
                systemBalance = new SystemBalance
                {
                    TotalPhysicalCash = 0,
                    LastUpdated = DateTime.UtcNow
                };
                _context.SystemBalances.Add(systemBalance);
            }

            systemBalance.TotalPhysicalCash += model.Amount;
            systemBalance.TotalSystemBalance = systemBalance.TotalPhysicalCash + systemBalance.TotalCashLineBalance + systemBalance.TotalInstaPayBalance;
            systemBalance.LastUpdated = DateTime.UtcNow;

            _context.CashTransactionsPhysical.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تسجيل عملية الإيداع بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        // GET: PhysicalCash/Withdraw
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Withdraw()
        {
            return View();
        }

        // POST: PhysicalCash/Withdraw
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Withdraw(CashTransactionPhysicalViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            
            var expense = new CashTransaction_Physical
            {
                Amount = model.Amount,
                Description = $"مصروفات: {model.Description}",
                TransactionType = TransactionType.Withdraw,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashTransactionsPhysical.Add(expense);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تسجيل المصروفات بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        // GET: PhysicalCash/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var transaction = await _context.CashTransactionsPhysical
                .Include(t => t.User)
                .Select(t => new CashTransactionPhysicalViewModel
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    UserName = t.User.FullName,
                    CreatedAt = t.CreatedAt
                })
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // GET: PhysicalCash/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var transaction = await _context.CashTransactionsPhysical
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (transaction == null)
                return NotFound();

            var viewModel = new CashTransactionPhysicalViewModel
            {
                Id = transaction.Id,
                Amount = transaction.Amount,
                TransactionType = transaction.TransactionType,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            };

            return View(viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.UtcNow.Date;
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var currentYear = new DateTime(DateTime.UtcNow.Year, 1, 1);

            // جمع بيانات النظام
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
                await _context.SaveChangesAsync();
            }

            // جمع بيانات العمليات النقدية
            var transactions = await _context.CashTransactionsPhysical.ToListAsync();

            var model = new PhysicalCashDashboardViewModel
            {
                // الرصيد النقدي الكلي والنظام
                TotalPhysicalCash = systemBalance.TotalPhysicalCash,
                TotalSystemBalance = systemBalance.TotalSystemBalance,
                LastUpdated = systemBalance.LastUpdated,

                // الإيداع والسحب اليومي
                DailyDeposits = transactions
                    .Where(t => t.TransactionType == TransactionType.Deposit && t.CreatedAt.Date == today)
                    .Sum(t => t.Amount),
                DailyWithdrawals = transactions
                    .Where(t => t.TransactionType == TransactionType.Withdraw && t.CreatedAt.Date == today)
                    .Sum(t => t.Amount),
                DailyTransactions = transactions
                    .Count(t => t.CreatedAt.Date == today),

                // الإيداع والسحب الشهري
                MonthlyDeposits = transactions
                    .Where(t => t.TransactionType == TransactionType.Deposit && t.CreatedAt >= currentMonth)
                    .Sum(t => t.Amount),
                MonthlyWithdrawals = transactions
                    .Where(t => t.TransactionType == TransactionType.Withdraw && t.CreatedAt >= currentMonth)
                    .Sum(t => t.Amount),
                MonthlyTransactions = transactions
                    .Count(t => t.CreatedAt >= currentMonth),

                // الإيداع والسحب السنوي
                YearlyDeposits = transactions
                    .Where(t => t.TransactionType == TransactionType.Deposit && t.CreatedAt >= currentYear)
                    .Sum(t => t.Amount),
                YearlyWithdrawals = transactions
                    .Where(t => t.TransactionType == TransactionType.Withdraw && t.CreatedAt >= currentYear)
                    .Sum(t => t.Amount),
                YearlyTransactions = transactions
                    .Count(t => t.CreatedAt >= currentYear),

                //// العمليات المكتملة والمجمدة
                //CompletedTransactions = transactions.Count(t => t. == TransactionStatus.Completed),
                //FrozenTransactions = transactions.Count(t => t.Status == TransactionStatus.Frozen)
            };

            return View(model);
        }

        // POST: PhysicalCash/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CashTransactionPhysicalViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                return View(model);
            }

            // التحقق من أن المبلغ صالح
            if (model.Amount <= 0)
            {
                TempData["ErrorMessage"] = "المبلغ يجب أن يكون أكبر من صفر.";
                return View(model);
            }

            var transaction = await _context.CashTransactionsPhysical.FindAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            // التحقق من كفاية الرصيد إذا كانت عملية سحب
            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            if (transaction.TransactionType == TransactionType.Withdraw && (systemBalance == null || systemBalance.TotalPhysicalCash + transaction.Amount < model.Amount))
            {
                TempData["ErrorMessage"] = "الرصيد النقدي غير كافٍ لإتمام عملية السحب.";
                return View(model);
            }

            // حساب الفرق في المبلغ لتحديث الرصيد
            var amountDifference = model.Amount - transaction.Amount;

            // تحديث بيانات العملية
            transaction.Amount = model.Amount;
            transaction.Description = model.Description;

            // تحديث الرصيد النقدي في SystemBalance
            if (systemBalance != null)
            {
                if (transaction.TransactionType == TransactionType.Deposit)
                {
                    systemBalance.TotalPhysicalCash += amountDifference;
                }
                else if (transaction.TransactionType == TransactionType.Withdraw)
                {
                    systemBalance.TotalPhysicalCash -= amountDifference;
                }
                systemBalance.TotalSystemBalance = systemBalance.TotalPhysicalCash + systemBalance.TotalCashLineBalance + systemBalance.TotalInstaPayBalance;
                systemBalance.LastUpdated = DateTime.UtcNow;
            }

            try
            {
                _context.Update(transaction);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "تم تعديل العملية النقدية بنجاح.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.CashTransactionsPhysical.AnyAsync(t => t.Id == id))
                {
                    return NotFound();
                }
                throw;
            }
        }
    }

    // ViewModel للعمليات النقدية
    public class CashTransactionPhysicalViewModel
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public TransactionType TransactionType { get; set; }
        public string Description { get; set; }
        public TransactionStatus Status { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class PhysicalCashDashboardViewModel
    {
        public decimal TotalPhysicalCash { get; set; } // الرصيد النقدي الكلي
        public decimal TotalSystemBalance { get; set; } // إجمالي رصيد النظام
        public decimal DailyDeposits { get; set; } // إجمالي الإيداع اليومي
        public decimal DailyWithdrawals { get; set; } // إجمالي السحب اليومي
        public decimal MonthlyDeposits { get; set; } // إجمالي الإيداع الشهري
        public decimal MonthlyWithdrawals { get; set; } // إجمالي السحب الشهري
        public decimal YearlyDeposits { get; set; } // إجمالي الإيداع السنوي
        public decimal YearlyWithdrawals { get; set; } // إجمالي السحب السنوي
        public int DailyTransactions { get; set; } // عدد العمليات اليومية
        public int MonthlyTransactions { get; set; } // عدد العمليات الشهرية
        public int YearlyTransactions { get; set; } // عدد العمليات السنوية
        public int CompletedTransactions { get; set; } // العمليات المكتملة
        public int FrozenTransactions { get; set; } // العمليات المجمدة
        public DateTime LastUpdated { get; set; } // آخر تحديث
    }
}




