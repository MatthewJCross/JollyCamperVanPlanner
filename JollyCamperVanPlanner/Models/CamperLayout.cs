
using System.Collections.Generic;

namespace JollyCamperVanPlanner
{
    public sealed class CamperLayout
    {
        public double VanLengthMm { get; set; } = 5986;
        public double VanWidthMm { get; set; } = 2427;
        public List<FurnitureData> Objects { get; set; } = new();
    }
}

