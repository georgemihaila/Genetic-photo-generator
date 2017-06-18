using Genetic_photo_generator;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Utilities
{
    public static class Images
    {
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
        /// <summary>
        /// Convert a Bitmap to an ImageSource.
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static ImageSource ToImageSource(this Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }

        /// <summary>
        /// Returns an ImageSource from a local file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static ImageSource ImageSourceFromFile(string filename)
        {
            Bitmap bmp = (Bitmap)Bitmap.FromFile(filename);
            return bmp.ToImageSource();
        }

        /// <summary>
        /// Returns a chunk of a bitmap
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="chunkNumber"></param>
        /// <param name="totalChunks"></param>
        /// <returns></returns>
        public static Bitmap SplitVertically(this Bitmap bitmap, int chunkNumber, int totalChunks)
        {
            int Width = bitmap.Width / totalChunks;
            int Height = bitmap.Height;
            Bitmap b = new Bitmap(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = (chunkNumber - 1) * Width, c = 0; x < chunkNumber * Width; x++, c++)
                {
                    b.SetPixel(c, y, bitmap.GetPixel(x, y));
                }
            }
            return b;
        }

        public static Bitmap ResizeImage(this Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }
    }
    public static class GeneticPhotoUtilities
    {
        /// <summary>
        /// Returns the stamina of a GeneticPhoto relative to a target Bitmap
        /// </summary>
        /// <param name="photo"></param>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static double GetStaminaRelativeTo(this GeneticPhoto photo, Bitmap bitmap)
        {
            double value = 0;
            for (int y = 0; y < photo.Height; y++)
            {
                for (int x = 0; x < photo.Width; x++)
                {
                    System.Drawing.Color c = bitmap.GetPixel(x, y);
                    Pixel p = new Pixel(c.R, c.G, c.B);
                    value += Math.Abs(p.R - photo.Pixels[x, y].R);
                    value += Math.Abs(p.G - photo.Pixels[x, y].G);
                    value += Math.Abs(p.B - photo.Pixels[x, y].B);
                }
            }
            return (photo.Width * photo.Height) / value;
        }

        public static void Sort(this GeneticPhoto[] photo, Bitmap bitmap)
        {
            double[] values = new double[photo.Length];
            for (int i = 0; i < photo.Length; i++)
            {
                double max = photo[i].GetStaminaRelativeTo(bitmap);
                for (int j = i + 1; j < photo.Length; j++)
                {
                    if (max < photo[j].GetStaminaRelativeTo(bitmap))
                    {
                        max = photo[j].GetStaminaRelativeTo(bitmap);
                        GeneticPhoto t = photo[j];
                        photo[j] = photo[i];
                        photo[i] = t;
                    }
                }
            }
        }

        public static void FillWith(this Bitmap bmp, System.Drawing.Color color)
        {
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    bmp.SetPixel(x, y, color);
                }
            }
        }

        /// <summary>
        /// Evolve a generation of GeneticPhotos
        /// </summary>
        /// <param name="sortedPhotos"></param>
        /// <param name="randomSeed"></param>
        /// <param name="bmp"></param>
        public static void Evolve(this GeneticPhoto[] sortedPhotos, int randomSeed, Bitmap bmp, bool skipTop3, bool skipPerfects, int perfectMutationChance)
        {
            Random random = new Random(randomSeed);
            int top3Mutation = 1;
            if (!skipTop3)
            for (int i = 0; i < sortedPhotos.Length * 1 / 3; i++)
            {
                for (int y = 0; y < sortedPhotos[i].Height; y++)
                {
                    for (int x = 0; x < sortedPhotos[i].Width; x++)
                    {
                        if ((random.Next(100) < perfectMutationChance) && !skipPerfects) //Perfect mutation 
                        {
                            System.Drawing.Color c = bmp.GetPixel(x, y);
                            sortedPhotos[i].Dna[x, y] = new DNA(c.R, c.G, c.B, c.R, c.G, c.B);
                        }
                        else if (random.Next(10) < top3Mutation) //Mutate randomly (10%)
                        {
                            int a = random.Next(byte.MaxValue);
                            int b = 256;
                            while ((b = random.Next(byte.MaxValue + 1)) < a) ;
                            sortedPhotos[i].Dna[x, y].MinR = (byte)a;
                            sortedPhotos[i].Dna[x, y].MaxR = (byte)b;

                            a = random.Next(byte.MaxValue);
                            b = 256;
                            while ((b = random.Next(byte.MaxValue + 1)) < a) ;
                            sortedPhotos[i].Dna[x, y].MinG = (byte)a;
                            sortedPhotos[i].Dna[x, y].MaxG = (byte)b;

                            a = random.Next(byte.MaxValue);
                            b = 256;
                            while ((b = random.Next(byte.MaxValue + 1)) < a) ;
                            sortedPhotos[i].Dna[x, y].MinB = (byte)a;
                            sortedPhotos[i].Dna[x, y].MaxB = (byte)b;
                        }
                    }
                }
            }
            for (int i = sortedPhotos.Length * 1 / 3; i < sortedPhotos.Length; i++)
            {
                for (int y = 0; y < sortedPhotos[i].Height; y++)
                {
                    for (int x = 0; x < sortedPhotos[i].Width; x++)
                    {
                        if (random.Next(10) < 2) //Inherit genes (20%)
                        {
                            sortedPhotos[i].Dna[x, y] = sortedPhotos[0].Dna[x, y];
                        }
                        else if (random.Next(10) < 5) //Mutate randomly (70%)
                        {
                            int a = random.Next(byte.MaxValue);
                            int b = 256;
                            while ((b = random.Next(byte.MaxValue + 1)) < a) ;
                            sortedPhotos[i].Dna[x, y].MinR = (byte)a;
                            sortedPhotos[i].Dna[x, y].MaxR = (byte)b;

                            a = random.Next(byte.MaxValue);
                            b = 256;
                            while ((b = random.Next(byte.MaxValue + 1)) < a) ;
                            sortedPhotos[i].Dna[x, y].MinG = (byte)a;
                            sortedPhotos[i].Dna[x, y].MaxG = (byte)b;

                            a = random.Next(byte.MaxValue);
                            b = 256;
                            while ((b = random.Next(byte.MaxValue + 1)) < a) ;
                            sortedPhotos[i].Dna[x, y].MinB = (byte)a;
                            sortedPhotos[i].Dna[x, y].MaxB = (byte)b;
                        }
                        else if ((random.Next(100) < perfectMutationChance) && (!skipPerfects)) //Perfect mutation
                        {
                            System.Drawing.Color c = bmp.GetPixel(x, y);
                            sortedPhotos[i].Dna[x, y] = new DNA(c.R, c.G, c.B, c.R, c.G, c.B);
                        }
                    }
                }
                sortedPhotos[i].Generate(random.Next());
            }
        }
    }
}
