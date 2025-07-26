using CashManagement.Data;
using CashManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeTransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeTransactionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: EmployeeTransactions/MyTransactions
        public async Task<IActionResult> MyTransactions()
        {
            var user = await _userManager.GetUserAsync(User);
            var today = DateTime.UtcNow.Date;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var currentYear = new DateTime(today.Year, 1, 1);

            // جلب معاملات النقد الفعلي
            var physicalTransactions = await _context.CashTransactionsPhysical
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // جلب معاملات الخطوط النقدية
            var cashTransactions = await _context.CashTransactions
                .Include(t => t.CashLine)
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // جلب معاملات InstaPay
            var instaPayTransactions = await _context.InstaPayTransactions
                .Include(t => t.InstaPay)
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // جلب معاملات الموردين
            var supplierTransactions = await _context.SupplierTransactions
                .Include(t => t.Supplier)
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var model = new EmployeeTransactionsViewModel
            {
                UserName = user.FullName,
                
                // الإحصائيات اليومية
                TodayPhysicalDeposits = physicalTransactions
                    .Where(t => t.CreatedAt.Date == today && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                TodayPhysicalWithdrawals = physicalTransactions
                    .Where(t => t.CreatedAt.Date == today && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                TodayCashDeposits = cashTransactions
                    .Where(t => t.CreatedAt.Date == today && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                TodayCashWithdrawals = cashTransactions
                    .Where(t => t.CreatedAt.Date == today && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                TodayInstaPayDeposits = instaPayTransactions
                    .Where(t => t.CreatedAt.Date == today && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                TodayInstaPayWithdrawals = instaPayTransactions
                    .Where(t => t.CreatedAt.Date == today && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                TodaySupplierCredits = supplierTransactions
                    .Where(t => t.TransactionDate.Date == today && t.DebitCreditType == DebitCreditType.Credit)
                    .Sum(t => t.Amount),
                TodaySupplierDebits = supplierTransactions
                    .Where(t => t.TransactionDate.Date == today && t.DebitCreditType == DebitCreditType.Debit)
                    .Sum(t => t.Amount),

                // الإحصائيات الشهرية
                MonthlyPhysicalDeposits = physicalTransactions
                    .Where(t => t.CreatedAt >= currentMonth && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                MonthlyPhysicalWithdrawals = physicalTransactions
                    .Where(t => t.CreatedAt >= currentMonth && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                MonthlyCashDeposits = cashTransactions
                    .Where(t => t.CreatedAt >= currentMonth && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                MonthlyCashWithdrawals = cashTransactions
                    .Where(t => t.CreatedAt >= currentMonth && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                MonthlyInstaPayDeposits = instaPayTransactions
                    .Where(t => t.CreatedAt >= currentMonth && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                MonthlyInstaPayWithdrawals = instaPayTransactions
                    .Where(t => t.CreatedAt >= currentMonth && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                MonthlySupplierCredits = supplierTransactions
                    .Where(t => t.TransactionDate >= currentMonth && t.DebitCreditType == DebitCreditType.Credit)
                    .Sum(t => t.Amount),
                MonthlySupplierDebits = supplierTransactions
                    .Where(t => t.TransactionDate >= currentMonth && t.DebitCreditType == DebitCreditType.Debit)
                    .Sum(t => t.Amount),

                // الإحصائيات السنوية
                YearlyPhysicalDeposits = physicalTransactions
                    .Where(t => t.CreatedAt >= currentYear && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                YearlyPhysicalWithdrawals = physicalTransactions
                    .Where(t => t.CreatedAt >= currentYear && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                YearlyCashDeposits = cashTransactions
                    .Where(t => t.CreatedAt >= currentYear && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                YearlyCashWithdrawals = cashTransactions
                    .Where(t => t.CreatedAt >= currentYear && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                YearlyInstaPayDeposits = instaPayTransactions
                    .Where(t => t.CreatedAt >= currentYear && t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                YearlyInstaPayWithdrawals = instaPayTransactions
                    .Where(t => t.CreatedAt >= currentYear && t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                YearlySupplierCredits = supplierTransactions
                    .Where(t => t.TransactionDate >= currentYear && t.DebitCreditType == DebitCreditType.Credit)
                    .Sum(t => t.Amount),
                YearlySupplierDebits = supplierTransactions
                    .Where(t => t.TransactionDate >= currentYear && t.DebitCreditType == DebitCreditType.Debit)
                    .Sum(t => t.Amount),

                // آخر المعاملات
                RecentPhysicalTransactions = physicalTransactions.Take(10).ToList(),
                RecentCashTransactions = cashTransactions.Take(10).ToList(),
                RecentInstaPayTransactions = instaPayTransactions.Take(10).ToList(),
                RecentSupplierTransactions = supplierTransactions.Take(10).ToList()
            };

            return View(model);
        }
    }
}