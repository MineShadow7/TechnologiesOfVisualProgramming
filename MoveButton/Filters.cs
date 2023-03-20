using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Security.Policy;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ScrollBar;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace MoveButton
{
    abstract class Filters
    {
        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                worker.ReportProgress((int)(float)i / resultImage.Width * 100);
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
            }
            return resultImage;
        }

        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);

        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    class InvertFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourcecolor = sourceImage.GetPixel(x, y);
            Color resultcolor = Color.FromArgb(255 - sourcecolor.R,
                                                255 - sourcecolor.G,
                                                255 - sourcecolor.B);
            return resultcolor;
        }
    }

    class MatrixFilter : Filters
    {
        protected float[,] kernel = null;
        protected MatrixFilter() { }
        public MatrixFilter(float[,] kernel)
        {
            this.kernel = kernel;
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;

            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neighborColor = sourceImage.GetPixel(idX, idY);
                    resultR += neighborColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neighborColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neighborColor.B * kernel[k + radiusX, l + radiusY];
                }
            }
            return Color.FromArgb(
                Clamp((int)resultR, 0, 255),
                Clamp((int)resultG, 0, 255),
                Clamp((int)resultB, 0, 255));
        }
    }

    class BlurFilter : MatrixFilter
    {
        public BlurFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
                }
            }
        }
    }

    class GaussianFilter : MatrixFilter
    {
        public GaussianFilter()
        {
            createGaussianFilter(3, 2);
        }
        public void createGaussianFilter(int radius, float sigma)
        {
            int size = 2 * radius + 1;
            kernel = new float[size, size];
            float norm = 0;
            for (int i = -radius; i <= radius; i++)

                for (int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = (float)(Math.Exp(i * i + j * j) / (sigma * sigma));
                    norm += kernel[i + radius, j + radius];
                }
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    kernel[i, j] /= norm;
                }
            }
        }
    }

    class GrayScaleFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourcecolor = sourceImage.GetPixel(x, y);
            float Intensity = (float)(0.36 * sourcecolor.R) + (float)(0.53 * sourcecolor.G) + (float)(0.11 * sourcecolor.B);
            Color resultcolor = Color.FromArgb((int)Intensity, (int)Intensity, (int)Intensity);
            return resultcolor;
        }
    }

    class Scepiah : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourcecolor = sourceImage.GetPixel(x, y);
            float Intensity = (float)(0.36 * sourcecolor.R) + (float)(0.53 * sourcecolor.G) + (float)(0.11 * sourcecolor.B);
            double k = 15;
            int Red = (int)((int)Intensity + 2 * k);
            int Green = (int)((int)Intensity + 0.5 * k);
            int Blue = (int)((int)Intensity - 1 * k);
            Color resultcolor = Color.FromArgb(Clamp(Red, 0, 255), Clamp(Green, 0, 255), Clamp(Blue, 0, 255));
            return resultcolor;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    class Bright : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int k = 100;
            Color sourcecolor = sourceImage.GetPixel(x, y);
            Color resultcolor = Color.FromArgb(Clamp(sourcecolor.R + k, 0, 255),
                Clamp(sourcecolor.G + k, 0, 255),
                Clamp(sourcecolor.B + k, 0, 255));
            return resultcolor;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    class SobelFilter : MatrixFilter
    {
        public SobelFilter()
        {
            createSobelFilter(3);
        }
        public void createSobelFilter(int radius)
        {
            int size = radius;
            kernel = new float[size, size];
            for (int i = 0; i < radius; i++)
                for (int j = 0; j < radius; j++)
                {
                    kernel[i, j] = 0;
                }
            kernel[0, 0] = -1;
            kernel[1, 0] = -2;
            kernel[2, 0] = -1;
            kernel[0, 2] = 1;
            kernel[1, 2] = 2;
            kernel[2, 2] = 1;
            
        }
    }

    class SharpnessFilter : MatrixFilter 
    {
        public SharpnessFilter()
        {
            createSharpnessFilter(3);
        }
        public void createSharpnessFilter(int radius)
        {
            int size = radius;
            kernel = new float[size, size];
            for (int i = 0; i < radius; i++)
                for (int j = 0; j < radius; j++)
                {
                    kernel[i, j] = 0;
                }
            kernel[0, 0] = 0;
            kernel[1, 0] = -1;
            kernel[2, 0] = 0;
            kernel[0, 1] = -1;
            kernel[1, 1] = 5;
            kernel[2, 1] = -1;
            kernel[0, 2] = 0;
            kernel[1, 2] = -1;
            kernel[2, 2] = 0;
        }
    }

    class EmbossingFilter : MatrixFilter
    {
        public EmbossingFilter()
        {
            createEmbossingFilter(3);
        }
        public void createEmbossingFilter(int radius)
        {
            int sizeX = radius;
            int sizeY = radius;
            kernel = new float[sizeX, sizeY];
            kernel[0, 0] = 0;
            kernel[1, 0] = 1;
            kernel[2, 0] = 0;
            kernel[0, 1] = 1;
            kernel[1, 1] = 0;
            kernel[2, 1] = -1;
            kernel[0, 2] = 0;
            kernel[1, 2] = -1;
            kernel[2, 2] = 0;
        }
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            float resR = 0;
            float resG = 0;
            float resB = 0;
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            int i = 100;
            for(int j = -radiusY; j <= radiusY; j++)
            {
                for(int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + j, 0, sourceImage.Height - 1);
                    Color neighborcolor = sourceImage.GetPixel(idX, idY);
                    resR += neighborcolor.R * kernel[k + radiusX, j + radiusY];
                    resG += neighborcolor.R * kernel[k + radiusX, j + radiusY];
                    resB += neighborcolor.R * kernel[k + radiusX, j + radiusY];
                }
            }
            Color resultColor = Color.FromArgb(Clamp((int)resR + i, 0, 255), Clamp((int)resG + i, 0, 255), Clamp((int)resB + i, 0, 255));
            return resultColor;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    class MotionBlurFilter : MatrixFilter
    {
        public MotionBlurFilter()
        {
            createMotionBlurFilter(5);
        }
        public void createMotionBlurFilter(int size)
        {
            kernel = new float[size, size];
            for(int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if(i == j)
                    {
                        kernel[i, j] = 1.0f * (1.0f / (float)size);
                    }
                    else
                    {
                        kernel[i, j] = 0.0f;
                    }
                }
            }
        }
    }

    class MedianFilter
    {
        
        public Bitmap processImage(Bitmap sourceImage)
        {
            int matrixSize = 3;
            BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height),
                                                     ImageLockMode.ReadOnly,
                                                     PixelFormat.Format32bppArgb);

            byte[] pixelBuffer = new byte[sourceData.Stride *
                                  sourceData.Height];


            byte[] resultBuffer = new byte[sourceData.Stride *
                                           sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0,
                                       pixelBuffer.Length);


            sourceImage.UnlockBits(sourceData);

            int filterOffset = (matrixSize - 1) / 2;
            int calcOffset = 0;


            int byteOffset = 0;

            List<int> neighbourPixels = new List<int>();
            byte[] middlePixel;


            for (int offsetY = filterOffset; offsetY <
                sourceImage.Height - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX <
                    sourceImage.Width - filterOffset; offsetX++)
                {
                    byteOffset = offsetY *
                                 sourceData.Stride +
                                 offsetX * 4;


                    neighbourPixels.Clear();


                    for (int filterY = -filterOffset;
                        filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset;
                            filterX <= filterOffset; filterX++)
                        {


                            calcOffset = byteOffset +
                                         (filterX * 4) +
                                (filterY * sourceData.Stride);


                            neighbourPixels.Add(BitConverter.ToInt32(
                                             pixelBuffer, calcOffset));
                        }
                    }


                    neighbourPixels.Sort();

                    middlePixel = BitConverter.GetBytes(
                                       neighbourPixels[filterOffset]);


                    resultBuffer[byteOffset] = middlePixel[0];
                    resultBuffer[byteOffset + 1] = middlePixel[1];
                    resultBuffer[byteOffset + 2] = middlePixel[2];
                    resultBuffer[byteOffset + 3] = middlePixel[3];
                }
            }


            Bitmap resultBitmap = new Bitmap(sourceImage.Width,
                                             sourceImage.Height);


            BitmapData resultData =
                       resultBitmap.LockBits(new Rectangle(0, 0,
                       resultBitmap.Width, resultBitmap.Height),
                       ImageLockMode.WriteOnly,
                       PixelFormat.Format32bppArgb);


            Marshal.Copy(resultBuffer, 0, resultData.Scan0,
                                       resultBuffer.Length);


            resultBitmap.UnlockBits(resultData);


            return resultBitmap;
        }
    }
}
