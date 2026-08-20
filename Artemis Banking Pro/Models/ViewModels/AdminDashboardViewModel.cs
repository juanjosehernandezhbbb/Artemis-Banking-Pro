namespace Artemis_Banking_Pro.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalTransactionsHistorical { get; set; }
        public int TransactionsToday { get; set; }
        public int TotalPaymentsHistorical { get; set; }
        public int PaymentsToday { get; set; }
        public int ActiveClients { get; set; }
        public int InactiveClients { get; set; }
        public int TotalFinancialProducts { get; set; }
        public int ActiveLoans { get; set; }
        public int ActiveCreditCards { get; set; }
        public int ActiveSavingsAccounts { get; set; }
        public decimal AverageClientDebt { get; set; }
    }
}