using ArtemisBankingPro.Core.Application.Common.Interfaces;
using System;

namespace ArtemisBankingPro.Infrastructure.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}