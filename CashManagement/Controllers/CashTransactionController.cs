using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CashManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using OfficeOpenXml;
using System.IO;
using CashManagement.Data;
using CashManagement.Models;

namespace CashManagement.Controllers
{
    [Authorize]
    public class CashTransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CashTransactionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, DateTime? startDate, DateTime? endDate, TransactionType? type, TransactionStatus? status, int? cashLineId)
        {
            var query = _context.CashTransactions
                .Include(t => t.CashLine)
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.RecipientNumber.Contains(searchString) || t.Description.Contains(searchString));
            }
            if (startDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= endDate.Value.AddDays(1));
            }
            if (type.HasValue)
            {
                query = query.Where(t => t.TransactionType == type.Value);
            }
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }
            if (cashLineId.HasValue)
            {
                query = query.Where(t => t.CashLineId == cashLineId.Value);
            }

            var transactions = await query.Select(t => new CashTransactionViewModel
            {
                Id = t.Id,
                CashLineId = t.CashLineId,
                CashLinePhoneNumber = t.CashLine.PhoneNumber,
                Amount = t.Amount,
                Fees = t.Fees,
                NetAmount = t.NetAmount,
                CommissionRate = t.CommissionRate,
                TransactionType = t.TransactionType,
                DepositType = t.DepositType,
                RecipientNumber = t.RecipientNumber,
                Description = t.Description,
                Status = t.Status,
                UserId = t.UserId,
                UserName = t.User.FullName,
                CreatedAt = t.CreatedAt
            }).ToListAsync();

            ViewBag.CashLines = await _context.CashLines.Select(cl => new { cl.Id, cl.PhoneNumber }).ToListAsync();
            return View(transactions);
        }

        [HttpGet]
        public async Task<IActionResult> Withdraw(int cashLineId)
        {
            var cashLine = await _context.CashLines.FindAsync(cashLineId);
            if (cashLine == null || cashLine.Status != AccountStatus.Active)
            {
                TempData["ErrorMessage"] = "الخط غير موجود أو غير نشط.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CashTransaction
            {
                CashLineId = cashLineId,
                TransactionType = TransactionType.Withdraw
            };
            ViewBag.CashLine = cashLine;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(CashTransaction transaction)
        {
            if (ModelState.IsValid)
            {
                ViewBag.CashLine = await _context.CashLines.FindAsync(transaction.CashLineId);
                return View(transaction);
            }

            var cashLine = await _context.CashLines.FindAsync(transaction.CashLineId);
            if (cashLine == null || cashLine.Status != AccountStatus.Active)
            {
                TempData["ErrorMessage"] = "الخط غير موجود أو غير نشط.";
                return RedirectToAction(nameof(Index));
            }

            if (cashLine.CurrentBalance < transaction.Amount)
            {
                ModelState.AddModelError("Amount", "الرصيد غير كافٍ.");
                ViewBag.CashLine = cashLine;
                return View(transaction);
            }

            var dailyWithdrawRemaining = cashLine.DailyWithdrawLimit - cashLine.DailyWithdrawUsed;
            var monthlyWithdrawRemaining = cashLine.MonthlyWithdrawLimit - cashLine.MonthlyWithdrawUsed;
            bool willFreeze = dailyWithdrawRemaining < transaction.Amount || monthlyWithdrawRemaining < transaction.Amount;

            transaction.Fees = transaction.Amount * (transaction.CommissionRate / 100);
            transaction.NetAmount = transaction.Amount - transaction.Fees;
            transaction.UserId = _userManager.GetUserId(User);
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.Status = willFreeze ? TransactionStatus.Frozen : TransactionStatus.Completed;
            transaction.TransactionReference = Guid.NewGuid().ToString();

            if (!willFreeze)
            {
                cashLine.CurrentBalance -= transaction.Amount;
                cashLine.DailyWithdrawUsed += transaction.Amount;
                cashLine.MonthlyWithdrawUsed += transaction.Amount;
                cashLine.UpdatedAt = DateTime.UtcNow;

                var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
                if (systemBalance != null)
                {
                    systemBalance.TotalPhysicalCash -= transaction.Amount; // تقليل النقدية بالمبلغ الكلي
                    systemBalance.TotalSystemBalance -= transaction.Amount; // تقليل إجمالي رصيد النظام
                    systemBalance.LastUpdated = DateTime.UtcNow;
                }

                var dailyProfit = await _context.DailyProfits
                    .FirstOrDefaultAsync(dp => dp.Date.Date == DateTime.UtcNow.Date);
                if (dailyProfit == null)
                {
                    dailyProfit = new DailyProfit { Date = DateTime.UtcNow.Date };
                    _context.DailyProfits.Add(dailyProfit);
                }
                dailyProfit.CashLineProfit += transaction.Fees;
                dailyProfit.TotalProfit += transaction.Fees;
                dailyProfit.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                cashLine.Status = AccountStatus.Frozen;
                cashLine.UpdatedAt = DateTime.UtcNow;
            }

            _context.CashTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = willFreeze
                ? "تم تسجيل عملية السحب كمجمدة بسبب تجاوز الحد، وسيتم فك تجميدها تلقائيًا عند إعادة تعيين الحدود."
                : "تم إجراء عملية السحب بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Deposit(int cashLineId)
        {
            var cashLine = await _context.CashLines.FindAsync(cashLineId);
            if (cashLine == null || cashLine.Status != AccountStatus.Active)
            {
                TempData["ErrorMessage"] = "الخط غير موجود أو غير نشط.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CashTransaction
            {
                CashLineId = cashLineId,
                TransactionType = TransactionType.Deposit
            };
            ViewBag.CashLine = cashLine;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(CashTransaction transaction)
        {
            if (ModelState.IsValid)
            {
                ViewBag.CashLine = await _context.CashLines.FindAsync(transaction.CashLineId);
                return View(transaction);
            }

            var cashLine = await _context.CashLines.FindAsync(transaction.CashLineId);
            if (cashLine == null || cashLine.Status != AccountStatus.Active)
            {
                TempData["ErrorMessage"] = "الخط غير موجود أو غير نشط.";
                return RedirectToAction(nameof(Index));
            }

            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            if (systemBalance == null || systemBalance.TotalPhysicalCash < transaction.Amount)
            {
                ModelState.AddModelError("Amount", "رصيد النقدي غير كافٍ.");
                ViewBag.CashLine = cashLine;
                return View(transaction);
            }

            var dailyDepositRemaining = cashLine.DailyDepositLimit - cashLine.DailyDepositUsed;
            var monthlyDepositRemaining = cashLine.MonthlyDepositLimit - cashLine.MonthlyDepositUsed;
            bool willFreeze = dailyDepositRemaining < transaction.Amount || monthlyDepositRemaining < transaction.Amount;

            if (transaction.DepositType != DepositType.NoDeduction && transaction.CommissionRate <= 0)
            {
                ModelState.AddModelError("CommissionRate", "نسبة الرسوم يجب أن تكون أكبر من صفر.");
                ViewBag.CashLine = cashLine;
                return View(transaction);
            }

            transaction.Fees = (transaction.DepositType == DepositType.NoDeduction) ? 0 : transaction.Amount * (transaction.CommissionRate / 100);
            transaction.NetAmount = transaction.Amount + transaction.Fees;
            transaction.UserId = _userManager.GetUserId(User);
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.Status = willFreeze ? TransactionStatus.Frozen : TransactionStatus.Completed;
            transaction.TransactionReference = Guid.NewGuid().ToString();

            if (!willFreeze)
            {
                cashLine.CurrentBalance += transaction.Amount;
                cashLine.DailyDepositUsed += transaction.Amount;
                cashLine.MonthlyDepositUsed += transaction.Amount;
                cashLine.UpdatedAt = DateTime.UtcNow;

                if (systemBalance != null)
                {
                    systemBalance.TotalPhysicalCash += transaction.Amount; // زيادة النقدية بالمبلغ الكلي
                    systemBalance.TotalSystemBalance += transaction.Amount; // زيادة إجمالي رصيد النظام
                    systemBalance.LastUpdated = DateTime.UtcNow;
                }

                var dailyProfit = await _context.DailyProfits
                    .FirstOrDefaultAsync(dp => dp.Date.Date == DateTime.UtcNow.Date);
                if (dailyProfit == null)
                {
                    dailyProfit = new DailyProfit { Date = DateTime.UtcNow.Date };
                    _context.DailyProfits.Add(dailyProfit);
                }
                dailyProfit.CashLineProfit += transaction.Fees;
                dailyProfit.TotalProfit += transaction.Fees;
                dailyProfit.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                cashLine.Status = AccountStatus.Frozen;
                cashLine.UpdatedAt = DateTime.UtcNow;
            }

            _context.CashTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = willFreeze
                ? "تم تسجيل عملية الإيداع كمجمدة بسبب تجاوز الحد، وسيتم فك تجميدها تلقائيًا عند إعادة تعيين الحدود."
                : "تم إجراء عملية الإيداع بنجاح.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmFreeze(CashTransaction transaction)
        {
            var cashLine = await _context.CashLines.FindAsync(transaction.CashLineId);
            if (cashLine == null)
            {
                return NotFound("الخط غير موجود.");
            }

            transaction.Status = TransactionStatus.Frozen;
            transaction.UserId = _userManager.GetUserId(User);
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.Fees = transaction.Amount * (transaction.CommissionRate / 100);
            transaction.NetAmount = transaction.Amount - transaction.Fees;

            _context.CashTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تجميد المبلغ للشهر التالي.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> FrozenAmounts()
        {
            var frozenTransactions = await _context.CashTransactions
                .Where(t => t.Status == TransactionStatus.Frozen)
                .Include(t => t.CashLine)
                .Select(t => new CashTransactionViewModel
                {
                    Id = t.Id,
                    CashLineId = t.CashLineId,
                    CashLinePhoneNumber = t.CashLine.PhoneNumber,
                    Amount = t.Amount,
                    Fees = t.Fees,
                    NetAmount = t.NetAmount,
                    TransactionType = t.TransactionType,
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return View(frozenTransactions);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var transaction = await _context.CashTransactions
                .Include(t => t.CashLine)
                .Include(t => t.User)
                .Select(t => new CashTransactionViewModel
                {
                    Id = t.Id,
                    CashLineId = t.CashLineId,
                    CashLinePhoneNumber = t.CashLine.PhoneNumber,
                    Amount = t.Amount,
                    Fees = t.Fees,
                    NetAmount = t.NetAmount,
                    CommissionRate = t.CommissionRate,
                    TransactionType = t.TransactionType,
                    DepositType = t.DepositType,
                    RecipientNumber = t.RecipientNumber,
                    Description = t.Description,
                    Status = t.Status,
                    UserId = t.UserId,
                    UserName = t.User.FullName,
                    CreatedAt = t.CreatedAt
                })
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null)
            {
                return NotFound("العملية غير موجودة.");
            }

            return View(transaction);
        }

        [HttpPost]
        public async Task<IActionResult> ExportTransactions(DateTime? startDate, DateTime? endDate, int? cashLineId)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var query = _context.CashTransactions
                .Include(t => t.CashLine)
                .Include(t => t.User)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= endDate.Value.AddDays(1));
            }
            if (cashLineId.HasValue)
            {
                query = query.Where(t => t.CashLineId == cashLineId.Value);
            }

            var transactions = await query.ToListAsync();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Transactions");
                worksheet.Cells[1, 1].Value = "رقم العملية";
                worksheet.Cells[1, 2].Value = "رقم الخط";
                worksheet.Cells[1, 3].Value = "المبلغ";
                worksheet.Cells[1, 4].Value = "الرسوم";
                worksheet.Cells[1, 5].Value = "المبلغ الصافي";
                worksheet.Cells[1, 6].Value = "نوع العملية";
                worksheet.Cells[1, 7].Value = "الحالة";
                worksheet.Cells[1, 8].Value = "التاريخ";

                for (int i = 0; i < transactions.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = transactions[i].Id;
                    worksheet.Cells[i + 2, 2].Value = transactions[i].CashLine.PhoneNumber;
                    worksheet.Cells[i + 2, 3].Value = transactions[i].Amount;
                    worksheet.Cells[i + 2, 4].Value = transactions[i].Fees;
                    worksheet.Cells[i + 2, 5].Value = transactions[i].NetAmount;
                    worksheet.Cells[i + 2, 6].Value = transactions[i].TransactionType.ToString();
                    worksheet.Cells[i + 2, 7].Value = transactions[i].Status.ToString();
                    worksheet.Cells[i + 2, 8].Value = transactions[i].CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Transactions_{DateTime.UtcNow:yyyyMMdd}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }

        [HttpGet]
        public async Task<IActionResult> TransactionsDashboard()
        {
            var today = DateTime.UtcNow.Date;
            var transactions = await _context.CashTransactions
                .Where(t => t.CreatedAt.Date == today)
                .Include(t => t.CashLine)
                .GroupBy(t => t.CashLineId)
                .Select(g => new
                {
                    CashLineId = g.Key,
                    PhoneNumber = g.First().CashLine.PhoneNumber,
                    TotalWithdrawals = g.Where(t => t.TransactionType == TransactionType.Withdraw).Sum(t => t.Amount),
                    TotalDeposits = g.Where(t => t.TransactionType == TransactionType.Deposit).Sum(t => t.Amount),
                    TotalFees = g.Sum(t => t.Fees)
                })
                .ToListAsync();

            var cashLines = await _context.CashLines
                .Select(cl => new CashLineViewModel
                {
                    Id = cl.Id,
                    PhoneNumber = cl.PhoneNumber,
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
                })
                .ToListAsync();

            var model = new TransactionsDashboardViewModel
            {
                TotalTransactions = transactions.Count,
                TotalWithdrawals = transactions.Sum(t => t.TotalWithdrawals),
                TotalDeposits = transactions.Sum(t => t.TotalDeposits),
                TotalFees = transactions.Sum(t => t.TotalFees),
                CashLines = cashLines,
                ActiveLinesCount = cashLines.Count(cl => cl.Status == AccountStatus.Active),
                FrozenLinesCount = cashLines.Count(cl => cl.Status == AccountStatus.Frozen)
            };

            return View(model);
        }
    }

    public class CashTransactionViewModel
    {
        public int Id { get; set; }
        public int CashLineId { get; set; }
        public string CashLinePhoneNumber { get; set; }
        public decimal Amount { get; set; }
        public decimal Fees { get; set; }
        public decimal NetAmount { get; set; }
        public decimal CommissionRate { get; set; }
        public TransactionType TransactionType { get; set; }
        public DepositType? DepositType { get; set; }
        public string RecipientNumber { get; set; }
        public string Description { get; set; }
        public TransactionStatus Status { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TransactionsDashboardViewModel
    {
        public int TotalTransactions { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalFees { get; set; }
        public int ActiveLinesCount { get; set; }
        public int FrozenLinesCount { get; set; }
        public List<CashLineViewModel> CashLines { get; set; }
    }
}