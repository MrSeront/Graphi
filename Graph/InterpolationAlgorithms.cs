using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{
    class InterpolationAlgorithms
    {
        public static int Linear(int[] xarray, int[] yarray, int x)
        {
            int y = 0;
            for (int i = 0; i < xarray.Length - 1; i++)
            {
                if (x >= xarray[i] && x < xarray[i + 1])
                {
                    y = yarray[i] + (x - xarray[i]) * (yarray[i + 1] - yarray[i]) /
                    (xarray[i + 1] - xarray[i]);
                }
            }
            return y;
        }

        public static int[] Linear(int[] xarray, int[] yarray, int[] x)
        {
            int[] y = new int[x.Length];
            for (int i = 0; i < x.Length; i++)
                y[i] = Linear(xarray, yarray, x[i]);
            return y;
        }

        public static int Sinus(int[] xarray, int[] yarray, int x)
        {
            int y = 0;
            for (int i = 0; i < xarray.Length - 1; i++)
            {
                if ( yarray[i]>yarray[i + 1])
                {
                    y = Convert.ToInt32((float)(yarray[i]- yarray[i+1]) * Math.Sin(x*3.14/(float)(2*(xarray[i + 1]-(xarray[i])))+3.14/2)+ yarray[i+1]);
                }
                else
                {
                    y = Convert.ToInt32((float)(yarray[i + 1] - yarray[i]) * Math.Sin(x * 3.14 / (float)(2 * (xarray[i + 1] - (xarray[i])))) + yarray[i]);
                    
                }
            }
            return y;
        }
        public static int[] Sinus(int[] xarray, int[] yarray, int[] x)
        {
            int[] y = new int[x.Length];
            for (int i = 0; i < x.Length; i++)
                y[i] = Sinus(xarray, yarray, x[i]);
            return y;
        }

        public static int Sinus1(int[] xarray, int[] yarray, int x)
        {
            int y = 0;
            for (int i = 0; i < xarray.Length - 1; i++)
            {
                if (yarray[i] > yarray[i + 1])
                {
                    y = Convert.ToInt32((float)(yarray[i+1] - yarray[i]) * Math.Sin(x * 3.14 / (float)(2 * (xarray[i + 1] - (xarray[i])))) + yarray[i]);
                }
                else
                {
                    y = Convert.ToInt32((float)(yarray[i] - yarray[i + 1]) * Math.Sin(x * 3.14 / (float)(2 * (xarray[i + 1] - (xarray[i]))) + 3.14 / 2) + yarray[i + 1]);
                }
            }
            return y;
        }
        public static int[] Sinus1(int[] xarray, int[] yarray, int[] x)
        {
            int[] y = new int[x.Length];
            for (int i = 0; i < x.Length; i++)
                y[i] = Sinus1(xarray, yarray, x[i]);
            return y;
        }

    }
}
