using System.ComponentModel.DataAnnotations;

namespace CashManagement.Models
{
    public class OtherProfit
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "مبلغ الربح مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "مبلغ الربح يجب أن يكون أكبر من صفر")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "وصف الربح مطلوب")]
        [StringLength(500, ErrorMessage = "الوصف لا يجب أن يتجاوز 500 حرف")]
        public string Description { get; set; }

        [Required(ErrorMessage = "نوع الإيداع مطلوب")]
        public OtherProfitDepositType DepositType { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum OtherProfitDepositType
    {
        [Display(Name = "نقدي في الدرج")]
        PhysicalCash = 1,
        
        [Display(Name = "InstaPay")]
        InstaPay = 2,
        
        [Display(Name = "خط نقدي")]
        CashLine = 3
    }
}