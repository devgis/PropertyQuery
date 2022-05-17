using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyGIS
{
    //其可用于三种对象：叶结点，非叶结点，数据结点
    public class NodeEntry
    {
        public GISExtent MBR=null;
        public int FeatureIndex;
        public GISFeature Feature = null;
        public List<NodeEntry> Entries = null;
        public NodeEntry Parent = null;
        public int Level;

        //专用于叶结点和非叶结点的生成
        public NodeEntry(int _level)
        {
            Entries = new List<NodeEntry>();
            Level = _level;
        }

        //专用于数据结点的生成
        public NodeEntry(GISFeature _feature, int _index)
        {
            Feature = _feature;
            FeatureIndex = _index;
            MBR = Feature.spatialpart.extent;
            Level = 0;
        }

        //向当前树结点增加一个子结点
        public void AddEntry(NodeEntry node)
        {
            //如果子结点为空，就返回
            if (node == null) return;
            //增加该子结点
            Entries.Add(node);
            //更新MBR
            if (MBR == null) MBR = new GISExtent(node.MBR);
            else MBR.Merge(node.MBR);
            //指定子结点的父结点
            node.Parent = this;
        }
    }

    public class RTree
    {
        //根结点
        NodeEntry Root;
        //每个结点的最大入口数
        int MaxEntries;
        //每个结点的最少入口数
        int MinEntries;
        //与此树关联的图层
        GISVectorLayer layer;

        public RTree(GISVectorLayer _layer, int maxEntries = 4)
        {
            Root = new NodeEntry(1);
            MaxEntries = Math.Max(maxEntries, 2);
            MinEntries = MaxEntries / 2;
            layer = _layer;
        }

        public GISVectorLayer GetTreeLayer()
        {
            List<NodeEntry> nodes = new List<NodeEntry>();
            NodeList(nodes, Root);
            List<GISField> fields = new List<GISField>();
            fields.Add(new GISField(typeof(Int32), "Level"));
            GISVectorLayer treelayer = new GISVectorLayer("treelayer", SHAPETYPE.line, null, fields);

            for (int i = 0; i < nodes.Count; i++)
            {
                List<GISVertex> vs = new List<GISVertex>();
                vs.Add(new GISVertex(nodes[i].MBR.getMaxX(), nodes[i].MBR.getMaxY()));
                vs.Add(new GISVertex(nodes[i].MBR.getMaxX(), nodes[i].MBR.getMinY()));
                vs.Add(new GISVertex(nodes[i].MBR.getMinX(), nodes[i].MBR.getMinY()));
                vs.Add(new GISVertex(nodes[i].MBR.getMinX(), nodes[i].MBR.getMaxY()));
                vs.Add(new GISVertex(nodes[i].MBR.getMaxX(), nodes[i].MBR.getMaxY()));
                GISLine line = new GISLine(vs);
                GISAttribute a = new GISAttribute();
                a.AddValue(nodes[i].Level);
                treelayer.AddFeature(new GISFeature(line, a), true);
            }
            return treelayer;
        }

        private void NodeList(List<NodeEntry> nodes, NodeEntry node)
        {
            nodes.Add(node);
            if (node.Entries == null) return;
            for (int i = 0; i < node.Entries.Count; i++)
                NodeList(nodes, node.Entries[i]);
        }
        //仅用于插入数据
        public void InsertData(int index)
        {
            GISFeature feature = layer.GetFeature(index);
            //生成数据结点
            NodeEntry DataEntry = new NodeEntry(feature, index);
            //从树根开始，找到一个叶结点
            NodeEntry LeafNode = ChooseLeaf(Root, DataEntry);
            //把数据入口插入叶结点
            InsertNode(LeafNode, DataEntry);
        }

        //将子树结点插入到一个父结点的入口列表中
        private void InsertNode(NodeEntry ParentNode, NodeEntry ChildNode)
        {
            ParentNode.AddEntry(ChildNode);
            //如果父结点的入口数量超限，则需要分割出一个叔叔结点
            NodeEntry UncleNode = (ParentNode.Entries.Count > MaxEntries) ? SplitNode(ParentNode) : null;
            //调整上层树结构
            AdjustTree(ParentNode, UncleNode);
        }

        //调整上层结点，其中OneNode为已经在树中的结点，SplitNode是新分割出来的
        private void AdjustTree(NodeEntry OneNode, NodeEntry SplitNode)
        {
            //OneNode是根结点
            if (OneNode.Parent == null)
            {
                //出现了一个兄弟，则需要向上生长
                if (SplitNode != null)
                {
                    //新生长的根结点，肯定不是叶结点
                    NodeEntry newroot = new NodeEntry(OneNode.Level+1);
                    newroot.AddEntry(OneNode);
                    newroot.AddEntry(SplitNode);
                    Root = newroot;
                }
                return;
            }
            //找到原有结点的父结点
            NodeEntry Parent = OneNode.Parent;
            //调整父结点的MBR
            Parent.MBR.Merge(OneNode.MBR);
            //将被分割出来的结点插入父结点的入口列表
            InsertNode(Parent, SplitNode);
        }

        //入口数量超限，需要分割
        private NodeEntry SplitNode(NodeEntry OneNode)
        {
            //找到两个种子的Entries序号，seed2>seed1
            int seed1 = 0;
            int seed2 = 1;
            //寻找可以最大化未重叠面积的，即两个种子间隔最远的
            double MaxArea = double.MinValue;
            for (int i = 0; i < OneNode.Entries.Count - 1; i++)
                for (int j = i + 1; j < OneNode.Entries.Count; j++)
                {
                    //计算未覆盖面积
                    double area = new GISExtent(OneNode.Entries[i].MBR, OneNode.Entries[j].MBR).area -
                        OneNode.Entries[i].MBR.area - OneNode.Entries[j].MBR.area;
                    if (area > MaxArea)
                    {
                        seed1 = i;
                        seed2 = j;
                        MaxArea = area;
                    }
                }
            //待分割所有入口，包括两个种子入口
            List<NodeEntry> leftEntries = OneNode.Entries;
            //生成原有结点的兄弟结点，两个结点Level相同
            NodeEntry SplitNode = new NodeEntry(OneNode.Level);
            //给分割结点一个种子
            SplitNode.AddEntry(leftEntries[seed2]);
            //清空原有结点的入口
            OneNode.Entries = new List<NodeEntry>();
            //清空其MBR
            OneNode.MBR = null;
            //给原有结点一个种子
            OneNode.AddEntry(leftEntries[seed1]);
            //从待分割入口中移除两个种子入口，因为他们已经分配过了，先移除seed2，因为seed2>seed1，移除后也不会影响seed1
            leftEntries.RemoveAt(seed2);
            leftEntries.RemoveAt(seed1);
            //将每个待分割入口分给两个结点
            while (leftEntries.Count > 0)
            {
                //如果有一个结点的入口数太少，就把剩余的入口全分配给它
                if (OneNode.Entries.Count + leftEntries.Count == MinEntries)
                {
                    AssignAllEntries(OneNode, leftEntries);
                    break;
                }
                else if (SplitNode.Entries.Count + leftEntries.Count == MinEntries)
                {
                    AssignAllEntries(SplitNode, leftEntries);
                    break;
                }
                double diffArea = 0;
                //获得diffArea绝对值最大的入口
                int index = PickNext(OneNode, SplitNode, leftEntries, ref diffArea);
                if (diffArea < 0) OneNode.AddEntry(leftEntries[index]);
                else if (diffArea > 0) SplitNode.AddEntry(leftEntries[index]);
                else
                {
                    //分配给原有结点后的合并面积
                    double merge1 = new GISExtent(leftEntries[index].MBR, OneNode.MBR).area;
                    //分配给分割结点后的合并面积
                    double merge2 = new GISExtent(leftEntries[index].MBR, SplitNode.MBR).area;
                    //分配给何必跟面积最小的结点
                    if (merge1 < merge2) OneNode.AddEntry(leftEntries[index]);
                    else if (merge1 > merge2) SplitNode.AddEntry(leftEntries[index]);
                    else
                    {
                        //分配给目前入口数量最少的结点
                        if (OneNode.Entries.Count < SplitNode.Entries.Count)
                            OneNode.AddEntry(leftEntries[index]);
                        else
                            SplitNode.AddEntry(leftEntries[index]);
                    }
                }
                //将已经分配好的入口移除
                leftEntries.RemoveAt(index);
                //如果有一个结点的入口数太少，就把剩余的入口全分配给它
                if (OneNode.Entries.Count + leftEntries.Count == MinEntries) 
                    AssignAllEntries(OneNode, leftEntries);
                else if (SplitNode.Entries.Count + leftEntries.Count == MinEntries) 
                    AssignAllEntries(SplitNode, leftEntries);
            }
            return SplitNode;
        }

        private void AssignAllEntries(NodeEntry node, List<NodeEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
                node.AddEntry(entries[i]);
            entries.Clear();
        }

        //选择面积差异最大的入口序号
        private int PickNext(NodeEntry FirstNode, NodeEntry SecondNode, List<NodeEntry> entries, ref double maxDiffArea)
        {
            maxDiffArea = double.MinValue;
            int index=-1;
            for (int i = 0; i < entries.Count;i++ )
            {
                double diffArea = EnlargedArea(FirstNode, entries[i]) - EnlargedArea(SecondNode, entries[i]);
                if (Math.Abs(diffArea)>maxDiffArea)
                {
                    maxDiffArea = Math.Abs(diffArea);
                    index = i;
                }
            }
            maxDiffArea = EnlargedArea(FirstNode, entries[index]) - EnlargedArea(SecondNode, entries[index]);
            return index;
        }

        //计算增加一个入口后，MBR扩大的面积
        private double EnlargedArea(NodeEntry node, NodeEntry entry)
        {
            return new GISExtent(entry.MBR, node.MBR).area - node.MBR.area;
        }
        
        //寻找放入新入口的叶结点
        private NodeEntry ChooseLeaf(NodeEntry node, NodeEntry entry)
        {
            //如果达到叶结点，就返回
            if (node.Level == 1) return node;
            //寻找扩大面积最小的子结点序号index
            double MinEnlargement = double.MaxValue;
            int MinIndex = -1;
            for (int i = 0; i < node.Entries.Count; i++)
            {
                double Enlargement = EnlargedArea(node.Entries[i], entry);
                if (Enlargement < MinEnlargement)
                {
                    MinIndex = i;
                    MinEnlargement = Enlargement;
                }
            }
            //递归方法，继续调用查找下一级子结点
            return ChooseLeaf(node.Entries[MinIndex], entry);
        }
    }
}
