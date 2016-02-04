using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Numerics
{

    public static class SpecialFunctions
    {

        // Normal //
        public static double NormalPDF(double x)
        {

            // Variables //
            double t = Math.Exp(-x * x * 0.50);
            t = t / Math.Sqrt(2 * Math.PI);

            // Return //
            return t;

        }

        public static double NormalPDF(double x, double mu, double sigma)
        {
            double z = (x - mu) / sigma;
            return NormalPDF(z);
        }

        public static double NormalCDF(double x)
        {

            // Variables //
            double[] b = { 0.2316419, 0.319381530, -0.356563782, 1.781477937, -1.821255978, 1.330274429 };
            double t = 1 / (1 + b[0] * x);

            // Set c //
            return 1 - NormalPDF(x) * (b[1] * t + b[2] * t * t + b[3] * t * t * t + b[4] * t * t * t * t + b[5] * t * t * t * t * t);

        }

        public static double NormalCDF(double x, double mu, double sigma)
        {
            double z = (x - mu) / sigma;
            return NormalCDF(z);
        }

        public static double NormalINV(double p)
        {

            // Handle out of bounds //
            if (p >= 1) return double.PositiveInfinity;
            if (p <= 0) return double.NegativeInfinity;

            // Variables //
            double x = 0;
            double dx = 0;
            double ep = 0;
            double e = 0.0001;
            int maxitter = 10;

            for (int i = 0; i < maxitter; i++)
            {
                dx = NormalPDF(x);
                ep = (p - NormalCDF(x));
                if (Math.Abs(ep) <= e) break;
                x += (ep) / (dx);

            }

            return x;

        }

        public static double NormalINV(double p, double mu, double sigma)
        {
            return NormalINV(p) * sigma + mu;
        }

    }

}
