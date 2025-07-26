using System.ComponentModel.DataAnnotations;

namespace CashManagement.Models
{
    public class FawryTransaction
    {
        public int Id { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        
        [Required]
        public TransactionType TransactionType { get; set; }
        
        public decimal FeesAmount { get; set; }
        
        public decimal? ManualFees { get; set; }
        
        public decimal NetAmount { get; set; }
        
        public string? Description { get; set; }
        
        public int? ServiceId { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        public TransactionStatus Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        // Navigation Properties
        public FawryService? FawryService { get; set; }
        public ApplicationUser User { get; set; }
    }

    public enum FawryServiceType
    {
        Regular = 0,    // فوري عادي
        Purchases = 1   // فوري مشتريات
    }

    public enum FawrySubServiceType
    {
        Cash = 0,           // سحب كاش
        Visa = 1,           // سحب فيزا
        VodafoneCash = 2    // سحب فودافون كاش
    }
}





