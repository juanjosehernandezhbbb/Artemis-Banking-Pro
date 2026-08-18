namespace ArtemisBankingPro.Core.Domain.Common.Exceptions
{
    public class HighRiskClientException : DomainException
    {
        public string RiskType { get; }
        public decimal CurrentDebt { get; }
        public decimal ProjectedDebt { get; }
        public decimal AverageDebt { get; }

        public HighRiskClientException(string riskType, decimal currentDebt, decimal projectedDebt, decimal averageDebt)
            : base("Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.")
        {
            RiskType = riskType;
            CurrentDebt = currentDebt;
            ProjectedDebt = projectedDebt;
            AverageDebt = averageDebt;
        }
    }
}