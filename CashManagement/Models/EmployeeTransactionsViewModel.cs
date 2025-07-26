namespace CashManagement.Models
{
    public class EmployeeTransactionsViewModel
    {
        public string UserName { get; set; }

        // الإحصائيات اليومية
        public decimal TodayPhysicalDeposits { get; set; }
        public decimal TodayPhysicalWithdrawals { get; set; }
        public decimal TodayCashDeposits { get; set; }
        public decimal TodayCashWithdrawals { get; set; }
        public decimal TodayInstaPayDeposits { get; set; }
        public decimal TodayInstaPayWithdrawals { get; set; }
        public decimal TodaySupplierCredits { get; set; }
        public decimal TodaySupplierDebits { get; set; }

        // الإحصائيات الشهرية
        public decimal MonthlyPhysicalDeposits { get; set; }
        public decimal MonthlyPhysicalWithdrawals { get; set; }
        public decimal MonthlyCashDeposits { get; set; }
        public decimal MonthlyCashWithdrawals { get; set; }
        public decimal MonthlyInstaPayDeposits { get; set; }
        public decimal MonthlyInstaPayWithdrawals { get; set; }
        public decimal MonthlySupplierCredits { get; set; }
        public decimal MonthlySupplierDebits { get; set; }

        // الإحصائيات السنوية
        public decimal YearlyPhysicalDeposits { get; set; }
        public decimal YearlyPhysicalWithdrawals { get; set; }
        public decimal YearlyCashDeposits { get; set; }
        public decimal YearlyCashWithdrawals { get; set; }
        public decimal YearlyInstaPayDeposits { get; set; }
        public decimal YearlyInstaPayWithdrawals { get; set; }
        public decimal YearlySupplierCredits { get; set; }
        public decimal YearlySupplierDebits { get; set; }

        // آخر المعاملات
        public List<CashTransaction_Physical> RecentPhysicalTransactions { get; set; }
        public List<CashTransaction> RecentCashTransactions { get; set; }
        public List<InstaPayTransaction> RecentInstaPayTransactions { get; set; }
        public List<SupplierTransaction> RecentSupplierTransactions { get; set; }

        // خصائص محسوبة
        public decimal TodayTotalDeposits => TodayPhysicalDeposits + TodayCashDeposits + TodayInstaPayDeposits + TodaySupplierCredits;
        public decimal TodayTotalWithdrawals => TodayPhysicalWithdrawals + TodayCashWithdrawals + TodayInstaPayWithdrawals + TodaySupplierDebits;
        public decimal MonthlyTotalDeposits => MonthlyPhysicalDeposits + MonthlyCashDeposits + MonthlyInstaPayDeposits + MonthlySupplierCredits;
        public decimal MonthlyTotalWithdrawals => MonthlyPhysicalWithdrawals + MonthlyCashWithdrawals + MonthlyInstaPayWithdrawals + MonthlySupplierDebits;
        public decimal YearlyTotalDeposits => YearlyPhysicalDeposits + YearlyCashDeposits + YearlyInstaPayDeposits + YearlySupplierCredits;
        public decimal YearlyTotalWithdrawals => YearlyPhysicalWithdrawals + YearlyCashWithdrawals + YearlyInstaPayWithdrawals + YearlySupplierDebits;
    }
}