using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pvz1
{
    public class Node
    {
        public int X { get; set; }
        public int Y { get; set; }
        public float G { get; set; }
        public float H { get; set; }
        public NodeStatus Status { get; set; }
        public Node Parent { get; set; }

        public Node()
        {

        }

        public Node(int x, int y, float G, float H, Node parent = null)
        {
            X = x;
            Y = y;
            this.G = G;
            this.H = H;
            Parent = parent;
        }

        public float getScore()
        {
            return (G + H)*10 + H;
        }
    }

    public enum NodeStatus
    {
        TARGET,
        OPEN,
        CLOSED
    }


   
}
