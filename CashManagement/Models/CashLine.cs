using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashManagement.Models
{
    public class CashLine
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [StringLength(15, ErrorMessage = "رقم الهاتف لا يجب أن يتجاوز 15 رقم")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "رقم الهاتف يجب أن يحتوي على أرقام فقط")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "اسم صاحب الخط مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم لا يجب أن يتجاوز 100 حرف")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "الرقم القومي مطلوب")]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "الرقم القومي يجب أن يكون 14 رقمًا")]
        [RegularExpression(@"^[0-9]{14}$", ErrorMessage = "الرقم القومي يجب أن يحتوي على أرقام فقط")]
        public string NationalId { get; set; }

        [Required(ErrorMessage = "نوع الشبكة مطلوب")]
        public NetworkType NetworkType { get; set; }

        [Required(ErrorMessage = "الرصيد الحالي مطلوب")]
        [Range(0, double.MaxValue, ErrorMessage = "الرصيد يجب أن يكون قيمة موجبة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; }

        [Required(ErrorMessage = "الحد اليومي للسحب مطلوب")]
        [Range(0, double.MaxValue, ErrorMessage = "الحد اليومي للسحب يجب أن يكون قيمة موجبة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyWithdrawLimit { get; set; }

        [Required(ErrorMessage = "الحد اليومي للإيداع مطلوب")]
        [Range(0, double.MaxValue, ErrorMessage = "الحد اليومي للإيداع يجب أن يكون قيمة موجبة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyDepositLimit { get; set; }

        [Required(ErrorMessage = "الحد الشهري للسحب مطلوب")]
        [Range(0, double.MaxValue, ErrorMessage = "الحد الشهري للسحب يجب أن يكون قيمة موجبة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyWithdrawLimit { get; set; }

        [Required(ErrorMessage = "الحد الشهري للإيداع مطلوب")]
        [Range(0, double.MaxValue, ErrorMessage = "الحد الشهري للإيداع يجب أن يكون قيمة موجبة")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyDepositLimit { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyWithdrawUsed { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DailyDepositUsed { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyWithdrawUsed { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyDepositUsed { get; set; } = 0;

        public AccountStatus Status { get; set; } = AccountStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastResetDate { get; set; }

        // العلاقات
        public virtual ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
    }

    public enum NetworkType
    {
        [Display(Name = "اتصالات")]
        Etisalat = 1,
        [Display(Name = "فودافون")]
        Vodafone = 2,
        [Display(Name = "أورانج")]
        Orange = 3,
        [Display(Name = "وي")]
        WE = 4
    }
}