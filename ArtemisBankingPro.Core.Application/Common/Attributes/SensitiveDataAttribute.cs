using System;

namespace ArtemisBankingPro.Core.Application.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SensitiveDataAttribute : Attribute
    {
    }
}