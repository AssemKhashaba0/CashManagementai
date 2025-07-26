using System.ComponentModel.DataAnnotations;

namespace CashManagement.Models
{
    public class FawryService
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ServiceName { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        public FawryServiceType ServiceType { get; set; }
        
        public FawrySubServiceType? SubServiceType { get; set; }
        
        [Range(0, 100)]
        public decimal? FeesPercentage { get; set; } // الرسوم على الألف
        
        public bool IsManualFees { get; set; } // هل الرسوم يدوية
        
        public bool IsActive { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        // Navigation Properties
        public ICollection<FawryTransaction> FawryTransactions { get; set; }
    }
}