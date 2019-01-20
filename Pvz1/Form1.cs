using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Pvz1
{
    public partial class Form1 : Form
    {
        int gridSize = 1000;
        Random rnd = new Random();
        Point start = new Point();
        Point end = new Point();
        SolidBrush myBrush = new SolidBrush(Color.Black);
        Bitmap bitmap;
        Bitmap originalBitmap;
        Graphics g;

        List<Rectangle> freeRectangles = new List<Rectangle>();
        List<Polygon> polygons = new List<Polygon>();

        public Form1()
        {
            InitializeComponent();
            Initialize();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            button3.Text = trackBar1.Value.ToString();
        }

        // generate original polygon field
        private void button3_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            polygons = new List<Polygon>();
            freeRectangles = new List<Rectangle>(){ new Rectangle(100, 0, gridSize - 2*100, gridSize) };
            bitmap = new Bitmap(gridSize, gridSize);
            g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            start = new Point(rnd.Next(100), rnd.Next(gridSize));
            end = new Point(rnd.Next(100)+gridSize-100, rnd.Next(gridSize));
            
            for(int i=0; i<trackBar1.Value; i++)
            {
                int maxArea = freeRectangles.Max((rec) => rec.Width * rec.Height);
                Rectangle rectangleToUse = freeRectangles.Find((rec) => rec.Width * rec.Height == maxArea);

                List<Point> polygonPoints = generateRandomConvexPolygon(rectangleToUse);
                updateFreeRectangles(rectangleToUse, polygonPoints);
                Polygon polygon = new Polygon(polygonPoints, Color.FromArgb(rnd.Next(155) + 90, rnd.Next(155) + 90, rnd.Next(155) + 90));
                polygon.centroid = findCentroidOfPolygonPoints(polygonPoints);
                polygons.Add(polygon);
            }

            foreach (Polygon polygon in polygons)
            {
                g.FillPolygon(new SolidBrush(polygon.color), polygon.originalPoints.ToArray());
               // g.FillEllipse(myBrush, polygon.centroid.X, polygon.centroid.Y, 10, 10); // polygon centroids
            }

            originalBitmap = (Bitmap)bitmap.Clone();

            Node endNode = getCalculatedPath(bitmap);
            drawPath(endNode, g);

            g.FillEllipse(myBrush, start.X, start.Y, 8, 8);
            g.FillEllipse(myBrush, end.X, end.Y, 8, 8);
            
            panel1.BackgroundImage = bitmap;
            g.Dispose();
            Cursor = Cursors.Arrow;
        }

        private void drawPath(Node endNode, Graphics g)
        {
            if (endNode.Parent == null) return;
            for(Node curr = endNode; curr != null; curr = curr.Parent)
            {
                g.FillRectangle(myBrush, curr.X-1, curr.Y-1, 3, 3);
            }

            textBox2.Text = String.Format("{0:0.00} px", endNode.G);
        }

        private float getPathsLength(Node endNode)
        {
            if (endNode.Parent == null) return 5000;
            return endNode.G;
        }

        private Node getCalculatedPath(Bitmap bitmap)
        {
            List<Node> nodes = new List<Node>();
            nodes.Add(new Node(start.X, start.Y, 0, getEuclideanDistance(start.X, start.Y, end)));  // start
            bool[,] gridClosed = new bool[gridSize, gridSize];
            for(int i=0; i<gridSize; i++)
            {
                for (int j=0; j<gridSize; j++)
                {
                    gridClosed[i, j] = false;
                }
            }
            Point[] neighborDiffs = {
                new Point(-3, -3),
                new Point(0, -3),
                new Point(3, -3),
                new Point(-3, 0),
                new Point(3, 0),
                new Point(-3, 3),
                new Point(0, 3),
                new Point(3, 3)
            };

            while (true)
            {
                float minScore = 1000000;
                int bestIndex = -1;
                for(int i=0; i<nodes.Count; i++)
                {
                    if (nodes[i].getScore() < minScore)
                    {
                        minScore = nodes[i].getScore();
                        bestIndex = i;
                    }
                }

                if (bestIndex == -1) return new Node();
                if (isEndPointFound(nodes[bestIndex])) return nodes[bestIndex];

                foreach (Point neighborDiff in neighborDiffs)
                {
                    float newGBonus = neighborDiff.X == 0 || neighborDiff.Y == 0 ?
                            3 : 4.24f;
                    float newGDistance = nodes[bestIndex].G + newGBonus;
                    int neighborX = nodes[bestIndex].X + neighborDiff.X;
                    int neighborY = nodes[bestIndex].Y + neighborDiff.Y;
                    var existingOpenNodeIndex = nodes.FindIndex((n) =>
                    n.X == neighborX &&
                    n.Y == neighborY);

                    if (existingOpenNodeIndex != -1)
                    {
                        int i = existingOpenNodeIndex;
                        if (nodes[i].G > newGDistance)
                        {
                            nodes[i].G = newGDistance;
                            nodes[i].Parent = nodes[bestIndex];
                        }
                    }
                    else if(neighborX > 0 && neighborX < 1000 && neighborY > 0 && neighborY < 1000)
                    {
                        if (areAllNeighborPixelsWhite(bitmap, neighborX, neighborY) && gridClosed[neighborX, neighborY] == false)
                        {
                            nodes.Add(new Node(neighborX, neighborY,
                                newGDistance,
                                getEuclideanDistance(neighborX, neighborY, end),
                                nodes[bestIndex]));
                        }
                    }
                }
                gridClosed[nodes[bestIndex].X, nodes[bestIndex].Y] = true;
                nodes.RemoveAt(bestIndex);
            }
        }

        private bool areAllNeighborPixelsWhite(Bitmap bitmap, int neighborX, int neighborY)
        {
            Point[] neighboringPixelDiffs = {
                new Point(-1, -1),
                new Point(0, -1),
                new Point(1, -1),
                new Point(-1, 0),
                new Point(1, 0),
                new Point(-1, 1),
                new Point(0, 1),
                new Point(1, 1),
                new Point(0, 0)
            };

            foreach (Point pixel in neighboringPixelDiffs)
            {
                int currentX = neighborX + pixel.X;
                int currentY = neighborY + pixel.Y;
                if ((currentX <= 0 || currentX >= 1000 || currentY <= 0 || currentY >= 1000) ||
                    bitmap.GetPixel(currentX, currentY).Name != "ffffffff")
                {
                    return false;
                }
            }
            return true;
        }

        private bool isEndPointFound(Node bestIndex)
        {
            Point[] neighboringPixelDiffs = {
                new Point(-1, -1),
                new Point(0, -1),
                new Point(1, -1),
                new Point(-1, 0),
                new Point(1, 0),
                new Point(-1, 1),
                new Point(0, 1),
                new Point(1, 1),
                new Point(0, 0)
            };

            foreach (Point pixel in neighboringPixelDiffs)
            {
                int currentX = bestIndex.X + pixel.X;
                int currentY = bestIndex.Y + pixel.Y;
                if ((currentX > 0 && currentX < 1000 && currentY > 0 && currentY < 1000) &&
                        bestIndex.X + pixel.X == end.X && bestIndex.Y + pixel.Y == end.Y)
                {
                    return true;
                }
            }
            return false;
        }

        private float getEuclideanDistance(int x, int y, Point p)
        {
            Point curr = new Point(x, y);
            return (float)Math.Sqrt(Math.Pow(curr.X - p.X, 2) + Math.Pow(curr.Y - p.Y, 2));

        }

        private List<Point> generateRandomConvexPolygon(Rectangle rectangleToUse)
        {           
            int n = rnd.Next(4) + 3;
            // Generate two lists of random X and Y coordinates
            List<int> xPool = new List<int>(n);
            List<int> yPool = new List<int>(n);

            for (int i = 0; i < n; i++)
            {
                xPool.Add(rnd.Next(rectangleToUse.Width) + rectangleToUse.X);
                yPool.Add(rnd.Next(rectangleToUse.Height) + rectangleToUse.Y);
            }

            // Sort them
            xPool.Sort();
            yPool.Sort();

            // Isolate the extreme points
            int minX = xPool[0];
            int maxX = xPool[n - 1];
            int minY = yPool[0];
            int maxY = yPool[n - 1];

            // Divide the interior points into two chains & Extract the vector components
            List<int> xVec = new List<int>(n);
            List<int> yVec = new List<int>(n);

            int lastTop = minX, lastBot = minX;

            for (int i = 1; i < n - 1; i++)
            {
                int ptX = xPool[i];

                if (rnd.Next()%2 == 0)
                {
                    xVec.Add(ptX - lastTop);
                    lastTop = ptX;
                }
                else
                {
                    xVec.Add(lastBot - ptX);
                    lastBot = ptX;
                }
            }

            xVec.Add(maxX - lastTop);
            xVec.Add(lastBot - maxX);

            int lastLeft = minY, lastRight = minY;

            for (int i = 1; i < n - 1; i++)
            {
                int ptY = yPool[i];

                if (rnd.Next() % 2 == 0)
                {
                    yVec.Add(ptY - lastLeft);
                    lastLeft = ptY;
                }
                else
                {
                    yVec.Add(lastRight - ptY);
                    lastRight = ptY;
                }
            }

            yVec.Add(maxY - lastLeft);
            yVec.Add(lastRight - maxY);

            // Combine the paired up components into vectors
            List<Point> vec = new List<Point>(n);

            for (int i = 0; i < n; i++)
            {
                vec.Add(new Point(xVec[i], yVec[i]));
            }

            // Sort the vectors by angle
            vec.Sort((v1, v2) => Math.Atan2(v1.Y, v1.X).CompareTo(Math.Atan2(v2.Y, v2.X)));

            // Lay them end-to-end
            int x = 0, y = 0;
            int minPolygonX = 0;
            int minPolygonY = 0;
            List<Point> points = new List<Point>(n);

            for (int i = 0; i < n; i++)
            {
                points.Add(new Point(x, y));

                x += vec[i].X;
                y += vec[i].Y;

                minPolygonX = Math.Min(minPolygonX, x);
                minPolygonY = Math.Min(minPolygonY, y);
            }

            // Move the polygon to the original min and max coordinates
            int xShift = minX - minPolygonX;
            int yShift = minY - minPolygonY;

            for (int i = 0; i < n; i++)
            {
                Point p = points[i];
                points[i] = new Point(p.X + xShift, p.Y + yShift);
            }

            return points;
        }

        private PointF findCentroidOfPolygonPoints(List<Point> vertices)
        {
            PointF centroid = new PointF() { X = 0.0f, Y = 0.0f };
            float signedArea = 0.0f;
            float x0 = 0.0f; // Current vertex X
            float y0 = 0.0f; // Current vertex Y
            float x1 = 0.0f; // Next vertex X
            float y1 = 0.0f; // Next vertex Y
            float a = 0.0f;  // Partial signed area

            // For all vertices except last
            int i = 0;
            for (i = 0; i < vertices.Count - 1; ++i)
            {
                x0 = vertices[i].X;
                y0 = vertices[i].Y;
                x1 = vertices[i + 1].X;
                y1 = vertices[i + 1].Y;
                a = x0 * y1 - x1 * y0;
                signedArea += a;
                centroid.X += (x0 + x1) * a;
                centroid.Y += (y0 + y1) * a;
            }

            // Do last vertex
            x0 = vertices[i].X;
            y0 = vertices[i].Y;
            x1 = vertices[0].X;
            y1 = vertices[0].Y;
            a = x0 * y1 - x1 * y0;
            signedArea += a;
            centroid.X += (x0 + x1) * a;
            centroid.Y += (y0 + y1) * a;

            signedArea *= 0.5f;
            centroid.X /= (6 * signedArea);
            centroid.Y /= (6 * signedArea);

            return centroid;
        }

        /**
         * Updates the locations where newly generated polygons can be drawn
         */
        private void updateFreeRectangles(Rectangle oldRectangle, List<Point> points)
        {
            int minY = points.Min((p) => p.Y);
            int maxY = points.Max((p) => p.Y);
            int minX = points.Min((p) => p.X);
            int maxX = points.Max((p) => p.X);

            Rectangle topLeft = new Rectangle(oldRectangle.X, oldRectangle.Y, minX - oldRectangle.X, minY - oldRectangle.Y);
            Rectangle top = new Rectangle(minX, oldRectangle.Y, maxX - minX, minY - oldRectangle.Y);
            Rectangle topRight = new Rectangle(maxX, oldRectangle.Y, oldRectangle.X + oldRectangle.Width - maxX, minY - oldRectangle.Y);

            Rectangle midLeft = new Rectangle(oldRectangle.X, minY, minX - oldRectangle.X, maxY - minY);
            Rectangle midRight = new Rectangle(maxX, minY, oldRectangle.X + oldRectangle.Width - maxX, maxY - minY);

            Rectangle bottomLeft = new Rectangle(oldRectangle.X, maxY, minX - oldRectangle.X, oldRectangle.Y + oldRectangle.Height - maxY);
            Rectangle bottom = new Rectangle(minX, maxY, maxX - minX, oldRectangle.Y + oldRectangle.Height - maxY);
            Rectangle bottomRight = new Rectangle(maxX, maxY, oldRectangle.X + oldRectangle.Width - maxX, oldRectangle.Y + oldRectangle.Height - maxY);

            Rectangle[] newRectangles = { topLeft, top, topRight, midLeft, midRight, bottomLeft, bottom, bottomRight };

            freeRectangles.Remove(oldRectangle);
            freeRectangles.AddRange(newRectangles);
        }

        // optimize button
        private void button1_Click(object sender, EventArgs e)
        {
            if (polygons.Count < 1) return;
            Cursor = Cursors.WaitCursor;
            double n = 5;
            double h = 0.1;
            double step = 0.5;
            int directionChangeLimit = 5;

            progressBar1.Maximum = (int)n * directionChangeLimit + 1;
            progressBar1.Step = 1;
            progressBar1.Visible = true;

           
            double[] degrees = new double[polygons.Count];
            for (int i=0; i<polygons.Count; i++)
            {
                degrees[i] = 0;  
            }

            Vector<double> degreeVector = Vector<double>.Build.DenseOfArray(degrees);

            for (int i=0; i< directionChangeLimit; i++)
            {
                var origLength = getCurrentLength();
                var gradient = grad(degreeVector, h, origLength);
                for (int j = 0; j < n; j++)
                {
                    var norm = gradient.L2Norm();
                    if (norm != 0)
                    {
                        Vector<double> deltaX = gradient / norm * step;
                        degreeVector = degreeVector - deltaX;
                        updatePolygonRotatedPoints(degreeVector);
                        double currLength = getCurrentLength();
                        if (currLength > origLength)
                        {
                            degreeVector = degreeVector + deltaX;
                            updatePolygonRotatedPoints(degreeVector);
                            step = step / 10;
                        }
                        else
                        {
                            origLength = currLength;
                        }
                    }
                    progressBar1.PerformStep();
                }
                step = 0.5;
            }

            drawCurrentPath();
            progressBar1.Visible = false;
            progressBar1.Value = 0;
            this.Refresh();
            Cursor = Cursors.Arrow;

        }

        private List<Point> findRotatedPoints(Polygon polygon, float angle)
        {
            List<Point> rotatedPoints = new List<Point>();
            foreach (Point point in polygon.originalPoints)
            {
                float x = point.X;
                float y = point.Y;
                float s = (float) Math.Sin(angle);
                float c = (float) Math.Cos(angle);

                // translate point back to origin:
                x -= polygon.centroid.X;
                y -= polygon.centroid.Y;

                // rotate point
                float xnew = x * c - y * s;
                float ynew = x * s + y * c;

                // translate point back:
                x = xnew + polygon.centroid.X;
                y = ynew + polygon.centroid.Y;

                rotatedPoints.Add(new Point((int)x, (int)y));
            }
            return rotatedPoints;
        }

        private void drawCurrentPath()
        {
            Graphics optimizationGraphics;
            Bitmap angleBitmap;
            Node endNode;

            angleBitmap = new Bitmap(gridSize, gridSize);
            optimizationGraphics = Graphics.FromImage(angleBitmap);
            optimizationGraphics.Clear(Color.White);
            for (int j = 0; j < polygons.Count; j++)
            {
                optimizationGraphics.FillPolygon(new SolidBrush(polygons[j].color), polygons[j].rotatedPoints.ToArray());
            }

            endNode = getCalculatedPath(angleBitmap);
            drawPath(endNode, optimizationGraphics);
            optimizationGraphics.FillEllipse(myBrush, start.X, start.Y, 8, 8);
            optimizationGraphics.FillEllipse(myBrush, end.X, end.Y, 8, 8);
            panel1.BackgroundImage = angleBitmap;
            optimizationGraphics.Dispose();
        }

         
        private double getCurrentLength()
        {
            Graphics optimizationGraphics;
            Bitmap angleBitmap;
            Node endNode;

            angleBitmap = new Bitmap(gridSize, gridSize);
            optimizationGraphics = Graphics.FromImage(angleBitmap);
            optimizationGraphics.Clear(Color.White);
            for (int j = 0; j < polygons.Count; j++)
            {
                optimizationGraphics.FillPolygon(new SolidBrush(polygons[j].color), polygons[j].rotatedPoints.ToArray());
            }

            endNode = getCalculatedPath(angleBitmap);
            float length = getPathsLength(endNode);

            optimizationGraphics.Dispose();

            return length;
        }

        private Vector<double> grad(Vector<double> degrees, double h, double originalLength)
        {
            double[] grad = new double[polygons.Count];
            List<List<Point>> beforeGradPolygonPoints = new List<List<Point>>();
            for(int i=0; i<polygons.Count; i++)
            {
                beforeGradPolygonPoints.Add(polygons[i].rotatedPoints);
            }
            for (int i =0; i< polygons.Count; i++)
            {
                polygons[i].rotatedPoints = findRotatedPoints(polygons[i], (float)(degrees[i] + h));
                grad[i] = (getCurrentLength() - originalLength) / h;
                polygons[i].rotatedPoints = beforeGradPolygonPoints[i];
            }

            return Vector<double>.Build.DenseOfArray(grad);
        }

        private void updatePolygonRotatedPoints(Vector<double> degrees)
        {
            for (int j = 0; j < polygons.Count; j++)
            {
                polygons[j].rotatedPoints = findRotatedPoints(polygons[j], (float)(degrees[j]));
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }

}
