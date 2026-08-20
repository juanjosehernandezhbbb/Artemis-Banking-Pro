using System.ComponentModel.DataAnnotations;

namespace Artemis_Banking_Pro.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        [Display(Name = "Nombre de usuario")]
        public string UserName { get; set; }
    }
}