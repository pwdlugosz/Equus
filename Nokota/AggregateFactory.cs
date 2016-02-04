using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;
using Equus.Calabrese;

namespace Equus.Nokota
{


    public static class CellReductions
    {

        public static Aggregate Average(FNode M)
        {
            return new AggregateAverage(M);
        }

        public static Aggregate Average(FNode M, FNode W)
        {
            return new AggregateAverage(M, W);
        }

        public static Aggregate Correl(FNode M, FNode N)
        {
            return new AggregateCorrelation(M, N);
        }

        public static Aggregate Correl(FNode M, FNode N, FNode W)
        {
            return new AggregateCorrelation(M, N, W);
        }

        public static Aggregate Count(FNode M)
        {
            return new AggregateCount(M);
        }

        public static Aggregate CountAll()
        {
            return new AggregateCountAll(new FNodeValue(null, new Cell((long)0)));
        }

        public static Aggregate CountNull(FNode M)
        {
            return new AggregateCountNull(M);
        }

        public static Aggregate Covar(FNode M, FNode N)
        {
            return new AggregateCovariance(M, N);
        }

        public static Aggregate Covar(FNode M, FNode N, FNode W)
        {
            return new AggregateCovariance(M, N, W);
        }

        public static Aggregate Frequency(Predicate P)
        {
            return new AggregateFreq(P);
        }

        public static Aggregate Frequency(Predicate P, FNode W)
        {
            return new AggregateFreq(W, P);
        }

        public static Aggregate Intercept(FNode M, FNode N)
        {
            return new AggregateIntercept(M, N);
        }

        public static Aggregate Intercept(FNode M, FNode N, FNode W)
        {
            return new AggregateIntercept(M, N, W);
        }

        public static Aggregate Max(FNode M)
        {
            return new AggregateMax(M);
        }

        public static Aggregate Min(FNode M)
        {
            return new AggregateMin(M);
        }

        public static Aggregate Slope(FNode M, FNode N)
        {
            return new AggregateSlope(M, N);
        }

        public static Aggregate Slope(FNode M, FNode N, FNode W)
        {
            return new AggregateSlope(M, N, W);
        }

        public static Aggregate Stdev(FNode M)
        {
            return new AggregateStdevP(M);
        }

        public static Aggregate Stdev(FNode M, FNode W)
        {
            return new AggregateStdevP(M, W);
        }

        public static Aggregate Sum(FNode M)
        {
            return new AggregateSum(M);
        }

        public static Aggregate Var(FNode M)
        {
            return new AggregateVarianceP(M);
        }

        public static Aggregate Var(FNode M, FNode W)
        {
            return new AggregateVarianceP(M, W);
        }

    }


}
