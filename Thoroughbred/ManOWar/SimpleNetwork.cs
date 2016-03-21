using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equus.Thoroughbred.ManOWar
{

    class SimpleNetwork
    {

        public double[,] _weights;
        public double[,] _gradients;
        public double sse = 0;
        public Numerics.ScalarFunction _act = new Numerics.BinarySigmoid();

        public SimpleNetwork()
        {

            this._weights = new double[3, 3];
            this._gradients = new double[3, 3];

            this._weights[0, 0] = 0.25;
            this._weights[1, 0] = -0.25;
            this._weights[2, 0] = 0.25;

            this._weights[0, 1] = -0.25;
            this._weights[1, 1] = 0.25;
            this._weights[2, 1] = -0.25;

            this._weights[0, 2] = 0.25;
            this._weights[1, 2] = -0.25;
            this._weights[2, 2] = 0.25;

        }

        public void TrainOne(Gidran.Matrix Data)
        {

            this._gradients = new double[3, 3];
            this.sse = 0;
            for (int i = 0; i < Data.RowCount; i++)
            {

                double[] values = Data[i];
                
                double nu1 = 1 * this._weights[0, 0] + values[0] * this._weights[0, 1] + values[1] * this._weights[0, 2];
                double h1 = this._act.Evaluate(nu1);
                
                double nu2 = 1 * this._weights[1, 0] + values[0] * this._weights[1, 1] + values[1] * this._weights[1, 2];
                double h2 = this._act.Evaluate(nu2);

                double nuf = 1 * this._weights[2, 0] + h1 * this._weights[2, 1] + h2 * this._weights[2, 2];
                double y = this._act.Evaluate(nuf);
                Console.WriteLine("H1 : {0}", h1);
                Console.WriteLine("H2 : {0}", h2);
                Console.WriteLine("Y : {0}", y);

                double dx = y * (1 - y) * (values[2] - y);
                this.sse += (y - values[2]) * (y - values[2]);
                //Console.WriteLine(y);
                
                this._gradients[2, 0] += 1D * dx;
                this._gradients[2, 1] += h1 * dx;
                this._gradients[2, 2] += h2 * dx;

                this._gradients[0, 0] += h1 * (1 - h1) * dx * this._weights[2, 1] * 1;
                this._gradients[0, 1] += h1 * (1 - h1) * dx * this._weights[2, 1] * values[0];
                this._gradients[0, 2] += h1 * (1 - h1) * dx * this._weights[2, 1] * values[1];

                this._gradients[1, 0] += h2 * (1 - h2) * dx * this._weights[2, 2] * 1;
                this._gradients[1, 1] += h2 * (1 - h2) * dx * this._weights[2, 2] * values[0];
                this._gradients[1, 2] += h2 * (1 - h2) * dx * this._weights[2, 2] * values[1];

            }

            for (int i = 0; i < 3; i++)
            {

                for (int j = 0; j < 3; j++)
                {
                    this._weights[i, j] += this._gradients[i, j] * 0.3;
                }

            }

        }

        public void TrainN(Gidran.Matrix Data, int N)
        {
            for (int i = 0; i < N; i++)
            {
                TrainOne(Data);
                if (i % 100 == 0)
                {
                    Console.WriteLine("{0} : {1}", i, sse);
                }
            }
        }

        public void PrintGradients()
        {

            for (int i = 0; i < 3; i++)
            {

                for (int j = 0; j < 3; j++)
                {
                    Console.WriteLine(this._gradients[i, j]);
                }

            }

        }

    }

}
