using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGIS
{
    public class GISNode
    {
        //结点位置
        public GISVertex location;
        public GISNode(GISVertex v)
        {
            location = v;
        }
    }
    public class GISArc
    {
        //弧段对应的线空间对象
        public GISFeature feature;
        //两个对应结点在列表中的序号
        public int FromNodeIndex;
        public int ToNodeIndex;
        //阻抗
        public double Impedence;
        public GISArc(GISFeature f, int from, int to, double impedence)
        {
            feature = f;
            FromNodeIndex = from;
            ToNodeIndex = to;
            Impedence = impedence;
        }
    }

    public class GISNetwork
    {
        //结点列表
        public List<GISNode> Nodes = new List<GISNode>();
        //弧段列表
        public List<GISArc> Arcs = new List<GISArc>();
        //邻接矩阵
        public GISArc[,] Matrix;
        //原始线图层
        public GISVectorLayer LineLayer;

        public GISNetwork(GISVectorLayer lineLayer, int FieldIndex = -1, double Tolerance = -1)
        {
            LineLayer = lineLayer;
            //如果该图层不是线图层，则返回
            if (LineLayer.ShapeType != SHAPETYPE.line) return;

            //如果用户没有提供，计算Tolerance
            if (Tolerance < 0)
            {
                Tolerance = double.MaxValue;
                for (int i = 0; i < LineLayer.FeatureCount(); i++)
                {
                    GISLine line = (GISLine)(LineLayer.GetFeature(i).spatialpart);
                    Tolerance = Math.Min(Tolerance, line.Length);
                }
                //找出最小的线实体长度，令其缩小100倍，作为Tolerance
                Tolerance /= 100;
            }

            for (int i = 0; i < LineLayer.FeatureCount(); i++)
            {
                GISLine line = (GISLine)(LineLayer.GetFeature(i).spatialpart);
                //获得对应的结点
                int from = FindOrInsertNode(line.FromNode(), Tolerance);
                int to = FindOrInsertNode(line.ToNode(), Tolerance);
                //获得阻抗，可以是已有的一个属性或者是弧段长度
                double impedence = (FieldIndex > 0) ? (double)(LineLayer.GetFeature(i).getAttribute(FieldIndex)) : line.Length;
                //增加到弧段列表
                Arcs.Add(new GISArc(LineLayer.GetFeature(i), from, to, impedence));
            }
            //建立邻接矩阵
            BuildMatrix();
        }

        private void BuildMatrix()
        {
            //初始化邻接矩阵
            Matrix = new GISArc[Nodes.Count, Nodes.Count];
            for (int i = 0; i < Nodes.Count; i++)
                for (int j = 0; j < Nodes.Count; j++)
                    Matrix[i, j] = null;
            //填充邻接矩阵，假定每个弧段都为双向通行，且阻抗相同
            for (int i = 0; i < Arcs.Count; i++)
            {
                Matrix[Arcs[i].FromNodeIndex, Arcs[i].ToNodeIndex] = Arcs[i];
                Matrix[Arcs[i].ToNodeIndex, Arcs[i].FromNodeIndex] = Arcs[i];
            }
        }

        private int FindOrInsertNode(GISVertex vertex, double Tolerance)
        {
            //在Nodes中查看该位置是否已经存在一个结点，如果是就直接返回这个结点
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].location.Distance(vertex) < Tolerance) return i;
            }
            //该位置尚无结点，则新增一个结点。
            Nodes.Add(new GISNode(vertex));
            return Nodes.Count - 1;
        }

        //根据位置找到最近的结点序号
        private int FindNearestNodeIndex(GISVertex vertex)
        {
            double mindist = double.MaxValue;
            int minindex=-1;
            for (int i = 0; i < Nodes.Count; i++)
            {
                double dist=Nodes[i].location.Distance(vertex);
                if (dist < mindist)
                {
                    minindex = i;
                    mindist = dist;
                }                    
            }
            return minindex;
        }

        //根据起止点位置计算最短路径
        public List<GISFeature> FindRoute(GISVertex vfrom, GISVertex vto)
        {
            int FromNodeIndex=FindNearestNodeIndex(vfrom);
            int ToNodeIndex=FindNearestNodeIndex(vto);
            return FindRoute(FromNodeIndex, ToNodeIndex);
        }

        public List<GISFeature> FindRoute(int FromNodeIndex, int ToNodeIndex)
        {
            //初始化路径记录
            List<GISFeature> route = new List<GISFeature>();
            //起点终点相同，所以直接返回空路径
            if (FromNodeIndex == ToNodeIndex) return route;
            //定义并初始化相关变量
            double[] dist = new double[Nodes.Count];
            int[] prev = new int[Nodes.Count];
            List<int> Q = new List<int>();
            for (int i = 0; i < Nodes.Count; i++)
            {
                dist[i] = double.MaxValue;
                prev[i] = -1;
                Q.Add(i);
            }
            dist[FromNodeIndex] = 0;

            bool FindPath = false;
            while (Q.Count > 0)
            {
                //寻找Q中dist值最小的结点
                int minindex = 0;
                for (int i = 1; i < Q.Count; i++)
                    if (dist[Q[i]] < dist[Q[minindex]]) minindex = i;
                //如果结点是终点，则退出循环
                if (Q[minindex] == ToNodeIndex)
                {
                    FindPath = true;
                    break;
                }
                //更新dist及prev
                for (int i = 0; i < Q.Count; i++)
                {
                    if (minindex == i) continue;
                    if (Matrix[Q[minindex], Q[i]]==null) continue;

                    double newdist = dist[Q[minindex]] + Matrix[Q[minindex], Q[i]].Impedence;
                    if (newdist < dist[Q[i]])
                    {
                        dist[Q[i]] = newdist;
                        prev[Q[i]] = Q[minindex];
                    }
                }
                //移除已经确定最短距离的结点
                Q.RemoveAt(minindex);
            }
            //如果有路径存在，通过倒序的方法找到沿路的弧段
            if (FindPath)
            {
                int i = ToNodeIndex;
                while (prev[i] > -1)
                {
                    route.Insert(0, Matrix[prev[i], i].feature);
                    i = prev[i];
                }
            }
            return route;
        }
    }

}
