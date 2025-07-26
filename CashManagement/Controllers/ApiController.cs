using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CashManagement.Data;
using CashManagement.Models;

[Route("api")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("balances")]
    public async Task<IActionResult> GetBalances()
    {
        try
        {
            // النقد الفعلي - من SystemBalance
            var systemBalance = await _context.SystemBalances.FirstOrDefaultAsync();
            var physicalCash = systemBalance?.TotalPhysicalCash ?? 0;

            // CashLines - مجموع الأرصدة الحالية للخطوط النشطة
            var cashLineBalance = await _context.CashLines
                .Where(c => c.Status == AccountStatus.Active)
                .SumAsync(c => c.CurrentBalance);

            // InstaPay - مجموع الأرصدة الحالية للحسابات النشطة
            var instaPayBalance = await _context.InstaPays
                .Where(i => i.Status == AccountStatus.Active)
                .SumAsync(i => i.CurrentBalance);

            // Fawry - حساب الرصيد من المعاملات
            var fawryDeposits = await _context.FawryTransactions
                .Where(f => f.TransactionType == TransactionType.Deposit && f.Status == TransactionStatus.Completed)
                .SumAsync(f => f.NetAmount);
            
            var fawryWithdrawals = await _context.FawryTransactions
                .Where(f => f.TransactionType == TransactionType.Withdraw && f.Status == TransactionStatus.Completed)
                .SumAsync(f => f.NetAmount);
            
            var fawryBalance = fawryDeposits - fawryWithdrawals;

            // Suppliers - حساب الرصيد مع تحديد ليك أو عليك
            var supplierBalance = await _context.Suppliers
                .SumAsync(s => s.CurrentBalance);

            // تحديد إذا كان الرصيد ليك أو عليك
            var supplierStatus = supplierBalance >= 0 ? "ليك" : "عليك";
            var supplierAbsBalance = Math.Abs(supplierBalance);

            var result = new
            {
                physical = physicalCash,
                cashLine = cashLineBalance,
                instaPay = instaPayBalance,
                fawry = fawryBalance,
                supplier = supplierBalance,
                supplierStatus = supplierStatus,
                supplierAbsBalance = supplierAbsBalance,
                total = physicalCash + cashLineBalance + instaPayBalance + fawryBalance + supplierBalance
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}





