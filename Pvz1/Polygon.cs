using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Pvz1
{
    class Polygon
    {
        public List<Point> originalPoints = new List<Point>();
        public List<Point> rotatedPoints { get; set; }
        public PointF centroid { get; set; }
        public Color color { get; set; }

        public Polygon(List<Point> points, Color color)
        {
            this.originalPoints = points;
            this.rotatedPoints = points;
            this.color = color;
        }
    }
}
