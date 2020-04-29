using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopolEvo.NEAT;

namespace TopolEvo.Utilities
{
    public static class Utils
    {
        /// <summary>
        /// Create a Random Normal variable using Box Muller Transform
        /// from https://stackoverflow.com/questions/218060/random-gaussian-variables
        /// </summary>
        internal static double Gaussian(double mean, double std)
        {
            double u1 = 1.0 - Config.globalRandom.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - Config.globalRandom.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal =
                         mean + std * randStdNormal; //random normal(mean,stdDev^2)

            return randNormal;
        }
    }
}
