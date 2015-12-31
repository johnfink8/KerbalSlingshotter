using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KerbalSlingshotter
{
    public static class OrbitExtensions
    {
        public static bool ContainsUT(this Orbit o, double UT)
        {
            return  UT > o.StartUT && (o.patchEndTransition == Orbit.PatchTransitionType.FINAL|| UT < o.EndUT);
        }
    }
}
