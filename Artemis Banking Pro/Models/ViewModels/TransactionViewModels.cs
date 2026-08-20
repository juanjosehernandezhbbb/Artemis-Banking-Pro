using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtemisBankingPro.WebApp.ViewModels
{
    public class ExpressTransactionViewModel
    {
        [Required(ErrorMessage = "El número de cuenta destino es requerido.")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe tener exactamente 9 dígitos.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "El número de cuenta solo debe contener números.")]
        public string TargetAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        public int SourceAccountId { get; set; }

        // Para poblar el selector en la vista
        public IEnumerable<SelectListItem> ActiveAccounts { get; set; } = new List<SelectListItem>();
    }

    public class ExpressConfirmationViewModel
    {
        public string TargetAccountNumber { get; set; } = string.Empty;
        public string TargetClientName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int SourceAccountId { get; set; }
        public string SourceAccountNumber { get; set; } = string.Empty;
    }

    public class PayCreditCardViewModel
    {
        [Required(ErrorMessage = "La tarjeta de crédito destino es requerida.")]
        public int CreditCardId { get; set; }

        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        public int SourceAccountId { get; set; }

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        public decimal Amount { get; set; }

        public IEnumerable<SelectListItem> ActiveCards { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ActiveAccounts { get; set; } = new List<SelectListItem>();
    }

    public class PayLoanViewModel
    {
        [Required(ErrorMessage = "El préstamo a pagar es requerido.")]
        public int LoanId { get; set; }

        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        public int SourceAccountId { get; set; }

        [Required(ErrorMessage = "El monto a pagar es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor que cero.")]
        public decimal Amount { get; set; }

        public IEnumerable<SelectListItem> ActiveLoans { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ActiveAccounts { get; set; } = new List<SelectListItem>();
    }

    public class TransferToBeneficiaryViewModel
    {
        [Required(ErrorMessage = "El beneficiario es requerido.")]
        public int BeneficiaryId { get; set; }

        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        public int SourceAccountId { get; set; }

        [Required(ErrorMessage = "El monto a transferir es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a transferir debe ser mayor que cero.")]
        public decimal Amount { get; set; }

        public IEnumerable<SelectListItem> Beneficiaries { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> ActiveAccounts { get; set; } = new List<SelectListItem>();

        public bool HasBeneficiaries { get; set; } = true;
    }

    public class ConfirmTransferViewModel
    {
        public int BeneficiaryId { get; set; }
        public int SourceAccountId { get; set; }
        public decimal Amount { get; set; }

        public string BeneficiaryName { get; set; } = string.Empty;
        public string BeneficiaryAccountNumber { get; set; } = string.Empty;
        public string SourceAccountNumber { get; set; } = string.Empty;
    }
}