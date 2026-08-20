using System.ComponentModel.DataAnnotations;

namespace ArtemisBankingPro.WebApp.ViewModels
{
    public class BeneficiaryListItemViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string NumeroCuenta { get; set; } = string.Empty;
    }

    public class AddBeneficiaryViewModel
    {
        [Required(ErrorMessage = "El número de cuenta es requerido.")]
        [StringLength(9, MinimumLength = 9, ErrorMessage = "El número de cuenta debe contener exactamente 9 dígitos.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "El número de cuenta solo debe contener números.")]
        public string NumeroCuenta { get; set; } = string.Empty;
    }
}