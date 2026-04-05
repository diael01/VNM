using Microsoft.Extensions.Options;
using Infrastructure.Options;

namespace EnergyManagement.Tests.Services.Analytics
{
    public class FakeMeteringOptions : IOptions<MeteringOptions>
    {
        public MeteringOptions Value { get; set; }
        public FakeMeteringOptions(MeteringOptions value)
        {
            Value = value;
        }
    }
}
