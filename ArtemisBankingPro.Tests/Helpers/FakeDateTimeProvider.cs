using System;
using ArtemisBankingPro.Core.Application.Common.Interfaces;

namespace ArtemisBankingPro.Tests.Helpers
{
    public class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    }
}