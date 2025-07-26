using CashManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace CashManagement.ViewModels
{
    public class FawryIndexViewModel
    {
        public List<FawryTransaction> FawryTransactions { get; set; } = new List<FawryTransaction>();
        public decimal RegularFawryBalance { get; set; }
        public decimal RegularFawryTodayTotal { get; set; }
        public decimal PurchasesFawryBalance { get; set; }
        public decimal PurchasesFawryTodayTotal { get; set; }
    }

    public class FawryTransactionViewModel
    {
        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "المبلغ يجب أن يكون أكبر من صفر")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "نوع العملية مطلوب")]
        public TransactionType TransactionType { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "مبلغ الرسوم يجب أن يكون قيمة موجبة")]
        public decimal? ManualFees { get; set; }

        public string? Description { get; set; }
        
        public int? ServiceId { get; set; }
    }
}





