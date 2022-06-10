using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.Threading;
///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }



        /////////////////////////////////// Our Code ///////////////////////////////////

        static bool[,,] distinct;
        static RGBPixel[] distinctColors;
        static int k;
        static double MST;
        static int NumberOfDistinct;

        public class edge : IComparable<edge>
        {
            public int src { get; set; }
            public int dest { get; set; }
            public double weight { get; set; }

            public int CompareTo(edge obj)
            {
                return this.weight.CompareTo(obj.weight);
            }
        }

        static int[,,] finalImage;
        public static void getDistinctColors(RGBPixel[,] ImageMatrix, int kClusters)   // Time: Ɵ(N^2) - Total Space: Ɵ(D)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            distinct = new bool[256, 256, 256];      //Space: O(1) constant and independent of D
            finalImage = new int[256, 256, 256];     //Space: O(1) constant and independent of D
            int nDistinct = 0;
            for (int i = 0; i < GetHeight(ImageMatrix); i++)      //Time: Ɵ(N) x Ɵ(N) --> Ɵ(N^2)
            {
                for (int j = 0; j < GetWidth(ImageMatrix); j++)   //Time: Ɵ(N)
                {
                    if (!distinct[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue])
                    {
                        finalImage[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue] = nDistinct;  //Ɵ(1)
                        nDistinct++;
                        distinct[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue] = true;   //Ɵ(1)
                    }
                }
            }

            distinctColors = new RGBPixel[nDistinct];            //Space: Ɵ(D)
            for (int i = 0; i < GetHeight(ImageMatrix); i++)     //Time: Ɵ(N^2)
            {
                for (int j = 0; j < GetWidth(ImageMatrix); j++)  //Time: Ɵ(N)
                {
                    if (distinct[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue])
                    {
                        distinctColors[finalImage[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue]] = ImageMatrix[i, j];   //Ɵ(1)
                    }
                }
            }

            k = kClusters;
            NumberOfDistinct = nDistinct;

            stopwatch.Stop();
            Console.WriteLine("Execution Time of getDistinctColors: {0} s", stopwatch.ElapsedMilliseconds * 0.001);

            MinimumSpanningTree();
        }

        static edge[] edges;
        static bool[] visited;
        public static void MinimumSpanningTree()     //Total Time: Ɵ(E x D)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            edges = new edge[NumberOfDistinct];
            visited = new bool[NumberOfDistinct];

            for (int i = 0; i < NumberOfDistinct; i++)    //Time: Ɵ(D)
            {
                edges[i] = new edge();
                edges[i].dest = i;
                edges[i].weight = double.MaxValue;
            }
            edges[0].weight = 0;
            visited[0] = true;

            double dist1, dist2, dist3, minWeight;
            int vertex = 0, minIndex = 0;

            for (int i = 0; i < NumberOfDistinct - 1; i++)   // Time: Ɵ(E x D)
            {
                minWeight = double.MaxValue;
                for (int j = 0; j < NumberOfDistinct; j++)   //Time: Ɵ(D)
                {
                    if (visited[j])
                        continue;
                    dist1 = (distinctColors[vertex].red - distinctColors[j].red);
                    dist1 *= dist1;
                    dist2 = (distinctColors[vertex].green - distinctColors[j].green);
                    dist2 *= dist2;
                    dist3 = (distinctColors[vertex].blue - distinctColors[j].blue);
                    dist3 *= dist3;
                    double weight = Math.Sqrt(dist1 + dist2 + dist3);
                    if (weight < edges[j].weight)
                    {
                        edges[j].weight = weight;
                        edges[j].src = vertex;
                    }
                    if (edges[j].weight < minWeight)
                    {
                        minWeight = edges[j].weight;
                        minIndex = j;
                    }
                }
                vertex = minIndex;
                visited[vertex] = true;
            }

            stopwatch.Stop();
            Console.WriteLine("Execution Time of MinimumSpanningTree: {0} s", stopwatch.ElapsedMilliseconds * 0.001);

            Clustering(edges);
        }

        public static double GetMST()    //Time: O(1)
        {
            return Math.Round(MST,2);
        }

        public static double GetDistinct()   //Time: O(1)
        {
            return NumberOfDistinct;
        }

        public static int findSet(int[] clusters, int vertex)  //Total Time: O(Log(D)) 
        {
            if (clusters[vertex] == vertex)  //Time: Ɵ(1)
                return vertex;
            return clusters[vertex] = findSet(clusters, clusters[vertex]);  //Time: O(Log(D))
        }

        public static void union(int[] clusters, int src, int dest, ref int numOfSets) // Total Time: O(Log(D)) 
        {
            src = findSet(clusters, src);       // O(Log(D)) 
            dest = findSet(clusters, dest);     // O(Log(D)) 
            if (src != dest)   //O(1)
            {
                clusters[dest] = clusters[src];
                numOfSets--;
            }
        }

        static int[] clusters;
        public static void Clustering(edge[] edges)    //Time: O(D Log(D)) 
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            clusters = new int[NumberOfDistinct];
            for (int i = 0; i < NumberOfDistinct; i++)   //Time: Ɵ(D)
            {
                clusters[i] = i;
            }

            Array.Sort(edges);  // E Log(E)

            int numOfSets = NumberOfDistinct;
            for (int i = 0; i < NumberOfDistinct; i++) //Total Time: Ɵ(D-k) x O(Log(D)) --> O(D Log(D)) 
            {
                if (numOfSets == k)
                {
                    break;
                }
                union(clusters, edges[i].src, edges[i].dest, ref numOfSets);   //Time: O(Log(D)) 
            }

            for (int j = 0; j < NumberOfDistinct; j++)     // Ɵ(D) x O(Log(D)) -->  O(D Log(D))
            {
                MST += edges[j].weight;
                clusters[j] = findSet(clusters, clusters[j]);    // O(Log(D)) 
            }

            stopwatch.Stop();
            Console.WriteLine("Execution Time of Clustering: {0} s", stopwatch.ElapsedMilliseconds * 0.001);

            getRepresentativeColor();
        }

        static int[] clusterCount;
        static RGBPixel[] clusterRepresentative;
        static RGBPixelD[] temp;
        public static void getRepresentativeColor()    //Time: Ɵ(D) 
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            clusterCount = new int[NumberOfDistinct];
            clusterRepresentative = new RGBPixel[NumberOfDistinct];
            temp = new RGBPixelD[NumberOfDistinct];

            for (int i = 0; i < NumberOfDistinct; i++)    //Time: Ɵ(D) 
            {
                temp[clusters[i]].red += distinctColors[i].red;
                temp[clusters[i]].green += distinctColors[i].green;
                temp[clusters[i]].blue += distinctColors[i].blue;
                clusterCount[clusters[i]] += 1;
            }

            for (int j = 0; j < NumberOfDistinct; j++)    //Time: Ɵ(D) 
            {
                clusterRepresentative[j].red = (byte)Math.Round(temp[j].red / clusterCount[j]);
                clusterRepresentative[j].green = (byte)Math.Round(temp[j].green / clusterCount[j]);
                clusterRepresentative[j].blue = (byte)Math.Round(temp[j].blue / clusterCount[j]);
            }

            stopwatch.Stop();
            Console.WriteLine("Execution Time of getRepresentativeColor: {0} s", stopwatch.ElapsedMilliseconds * 0.001);
        }

        public static RGBPixel[,] replacingColors(RGBPixel[,] originalImg)    //Time: Ɵ(N^2) 
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            int temp;
            for (int i = 0; i < GetHeight(originalImg); i++)    //Time: Ɵ(N^2) 
            {
                for (int j = 0; j < GetWidth(originalImg); j++)    //Time: Ɵ(N)
                {
                    temp = clusters[finalImage[originalImg[i, j].red, originalImg[i, j].green, originalImg[i, j].blue]];
                    originalImg[i, j] = clusterRepresentative[temp];
                }
            }

            stopwatch.Stop();
            Console.WriteLine("Execution Time of replacingColors {0} s", stopwatch.ElapsedMilliseconds * 0.001);

            return originalImg;
        }
    }
}