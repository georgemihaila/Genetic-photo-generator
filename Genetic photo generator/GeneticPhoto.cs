using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Genetic_photo_generator
{
    public class GeneticPhoto
    {
        /// <summary>
        /// Creates an instance of the GeneticPhoto class based on a source bitmap
        /// </summary>
        /// <param name="bitmap"></param>
        public GeneticPhoto(Bitmap bitmap)
        {
            Pixels = new Pixel[bitmap.Width, bitmap.Height];
            Dna = new DNA[bitmap.Width, bitmap.Height];
            Width = bitmap.Width;
            Height = bitmap.Height;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var px = bitmap.GetPixel(x, y);
                    Pixel p = new Pixel(px.R, px.G, px.B);
                    Pixels[x, y] = p;
                }
            }
        }

        /// <summary>
        /// Creates an empty instance of the GeneticPhoto class.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public GeneticPhoto(int width, int height)
        {
            Pixels = new Pixel[width, height];
            Dna = new DNA[width, height];
            Width = width;
            Height = height;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Dna[x, y] = new DNA(byte.MinValue, byte.MinValue, byte.MinValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
                }
            }
        }

        public Pixel[,] Pixels;
        public DNA[,] Dna;
        public int Width;
        public int Height;

        public void Generate(int randomSeed)
        {
            Random random = new Random(randomSeed);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Pixels[x, y] = new Pixel((byte)random.Next(Dna[x, y].MinR, Dna[x, y].MaxR + 1), (byte)random.Next(Dna[x, y].MinG, Dna[x, y].MaxG + 1), (byte)random.Next(Dna[x, y].MinB, Dna[x, y].MaxB + 1));
                }
            }
        }

        public Bitmap ToBitmap()
        {
            Bitmap b = new Bitmap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    b.SetPixel(x, y, System.Drawing.Color.FromArgb(Pixels[x, y].R, Pixels[x, y].G, Pixels[x, y].B));
                }
            }
            return b;
        }

        public ImageSource ToImageSource()
        {
            return Utilities.Images.ToImageSource(ToBitmap());
        }
    }
}
