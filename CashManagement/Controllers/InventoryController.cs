using CashManagement.Data;
using CashManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CashManagement.Controllers
{
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string period = "daily")
        {
            DateTime inventoryStartDate, inventoryEndDate;
            var today = DateTime.UtcNow.Date;
            var currentMonth = new DateTime(today.Year, today.Month, 1);
            var currentYear = new DateTime(today.Year, 1, 1);

            switch (period.ToLower())
            {
                case "daily":
                    inventoryStartDate = startDate?.Date ?? today;
                    inventoryEndDate = inventoryStartDate.AddDays(1).AddTicks(-1);
                    break;
                case "monthly":
                    inventoryStartDate = startDate?.Date ?? currentMonth;
                    inventoryEndDate = inventoryStartDate.AddMonths(1).AddTicks(-1);
                    break;
                case "yearly":
                    inventoryStartDate = startDate?.Date ?? currentYear;
                    inventoryEndDate = inventoryStartDate.AddYears(1).AddTicks(-1);
                    break;
                default:
                    inventoryStartDate = today;
                    inventoryEndDate = today.AddDays(1).AddTicks(-1);
                    break;
            }

            if (startDate.HasValue && endDate.HasValue)
            {
                inventoryStartDate = startDate.Value.Date;
                inventoryEndDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
            }

            var inventory = await GetInventoryData(inventoryStartDate, inventoryEndDate);

            ViewBag.Period = period;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(inventory);
        }

        private async Task<InventoryViewModel> GetInventoryData(DateTime startDate, DateTime endDate)
        {
            var systemBalance = await _context.SystemBalances
                .AsNoTracking()
                .FirstOrDefaultAsync() ?? new SystemBalance
                {
                    TotalPhysicalCash = 0,
                    TotalCashLineBalance = 0,
                    TotalInstaPayBalance = 0,
                    TotalSystemBalance = 0
                };

            var instaPayTransactions = await _context.InstaPayTransactions
                .Include(t => t.InstaPay)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == TransactionStatus.Completed)
                .ToListAsync();

            var cashTransactions = await _context.CashTransactions
                .Include(t => t.CashLine)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate && t.Status == TransactionStatus.Completed)
                .ToListAsync();

            var physicalCashTransactions = await _context.CashTransactionsPhysical
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            var supplierTransactions = await _context.SupplierTransactions
                .Include(t => t.Supplier)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .ToListAsync();

            var instaPaySummary = new InventorySummary
            {
                TotalDeposits = instaPayTransactions
                    .Where(t => t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.NetAmount),
                TotalWithdrawals = instaPayTransactions
                    .Where(t => t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.NetAmount),
                TotalFees = instaPayTransactions.Sum(t => t.FeesAmount),
                TotalTransactions = instaPayTransactions.Count,
                CurrentBalance = systemBalance.TotalInstaPayBalance
            };

            var cashLineSummary = new InventorySummary
            {
                TotalDeposits = cashTransactions
                    .Where(t => t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.NetAmount),
                TotalWithdrawals = cashTransactions
                    .Where(t => t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.NetAmount),
                TotalFees = cashTransactions.Sum(t => t.Fees),
                TotalTransactions = cashTransactions.Count,
                CurrentBalance = systemBalance.TotalCashLineBalance
            };

            var physicalCashSummary = new InventorySummary
            {
                TotalDeposits = physicalCashTransactions
                    .Where(t => t.TransactionType == TransactionType.Deposit)
                    .Sum(t => t.Amount),
                TotalWithdrawals = physicalCashTransactions
                    .Where(t => t.TransactionType == TransactionType.Withdraw)
                    .Sum(t => t.Amount),
                TotalFees = 0,
                TotalTransactions = physicalCashTransactions.Count,
                CurrentBalance = systemBalance.TotalPhysicalCash
            };

            var suppliers = await _context.Suppliers.ToListAsync();
            var supplierSummary = new InventorySummary
            {
                TotalDeposits = supplierTransactions
                    .Where(t => t.DebitCreditType == DebitCreditType.Debit)
                    .Sum(t => t.Amount),
                TotalWithdrawals = supplierTransactions
                    .Where(t => t.DebitCreditType == DebitCreditType.Credit)
                    .Sum(t => t.Amount),
                TotalFees = 0,
                TotalTransactions = supplierTransactions.Count,
                CurrentBalance = suppliers.Sum(s => s.CurrentBalance),
                AmountOwedToCompany = suppliers
                    .Where(s => s.CurrentBalance > 0)
                    .Sum(s => s.CurrentBalance), // المستحق للشركة (ليه)
                AmountOwedByCompany = suppliers
                    .Where(s => s.CurrentBalance < 0)
                    .Sum(s => Math.Abs(s.CurrentBalance)) // المستحق على الشركة (بره)
            };

            return new InventoryViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                InstaPaySummary = instaPaySummary,
                CashLineSummary = cashLineSummary,
                PhysicalCashSummary = physicalCashSummary,
                SupplierSummary = supplierSummary,
                TotalSystemBalance = systemBalance.TotalSystemBalance,
                TotalFees = instaPaySummary.TotalFees + cashLineSummary.TotalFees,
                TotalTransactions = instaPaySummary.TotalTransactions + cashLineSummary.TotalTransactions + physicalCashSummary.TotalTransactions + supplierSummary.TotalTransactions
            };
        }
    }

    public class InventoryViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public InventorySummary InstaPaySummary { get; set; }
        public InventorySummary CashLineSummary { get; set; }
        public InventorySummary PhysicalCashSummary { get; set; }
        public InventorySummary SupplierSummary { get; set; }
        public decimal TotalSystemBalance { get; set; }
        public decimal TotalFees { get; set; }
        public int TotalTransactions { get; set; }
    }

    public class InventorySummary
    {
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal TotalFees { get; set; }
        public int TotalTransactions { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal AmountOwedToCompany { get; set; } // المستحق للشركة (ليه)
        public decimal AmountOwedByCompany { get; set; } // المستحق على الشركة (بره)
    }
}