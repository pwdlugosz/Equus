using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Equus.Horse;

namespace Equus.Lipizzan
{

    public static class Exchange
    {

        public static Record MonoChromeBitmapToRecord(Bitmap Map)
        {

            RecordBuilder factory = new RecordBuilder();
            for (int x = 0; x < Map.Height; x++)
            {

                for (int y = 0; y < Map.Width; y++)
                {

                    if (Map.GetPixel(x, y).ToArgb() == Color.Black.ToArgb())
                    {
                        factory.Add(1D);
                    }
                    else
                    {
                        factory.Add(0D);
                    }

                }

            }
            return factory.ToRecord();

        }

        public static Bitmap RecordToMonoChromeBitmap(int X, int Y, double Threshold, Record Datum)
        {

            if (Datum.Count != X * Y)
                throw new Exception(string.Format("Bitmap dimensions are invalid for record lenght"));

            Bitmap map = new Bitmap(X, Y);

            int index = 0;

            for (int x = 0; x < X; x++)
            {

                for (int y = 0; y < Y; y++)
                {

                    if (Datum[index].DOUBLE >= Threshold)
                    {
                        map.SetPixel(x, y, Color.Black);
                    }

                    index++;

                }

            }

            return map;

        }

    }

}
