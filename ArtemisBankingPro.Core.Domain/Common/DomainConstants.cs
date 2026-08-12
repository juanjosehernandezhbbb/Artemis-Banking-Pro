namespace ArtemisBankingPro.Core.Domain.Common;

public static class DomainConstants
{
    public const string RolAdministrador = "Administrador";
    public const string RolCajero = "Cajero";
    public const string RolCliente = "Cliente";
    public const string RolComercio = "Comercio";

    public const int LongitudNumeroCuenta = 9;
    public const int LongitudNumeroPrestamo = 9;
    public const int LongitudNumeroTarjeta = 16;
    public const int LongitudCvc = 3;

    public static readonly int[] PlazosPrestamoPermitidos = { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 };

    public const decimal TasaInteresAvanceEfectivo = 0.0625m;

    public const int TamanoPaginaPorDefecto = 20;
    public const int TamanoPaginaMaximo = 20;
}
