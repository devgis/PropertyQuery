using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_23
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GISVectorLayer layer = (GISVectorLayer)gisPanel1.document.layers[0];
            RTree rtree = layer.rtree;
            GISVectorLayer treelayer = rtree.GetTreeLayer();
            if (gisPanel1.document.getLayer(treelayer.Name) != null)
                gisPanel1.document.RemoveLayer(treelayer.Name);
            gisPanel1.document.AddLayer(treelayer);
            gisPanel1.UpdateMap();
        }

        int index = 0;
        private void button2_Click(object sender, EventArgs e)
        {
            GISVectorLayer layer = (GISVectorLayer)gisPanel1.document.layers[0];
            layer.rtree = new RTree(layer);
            index = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            GISVectorLayer layer = (GISVectorLayer)gisPanel1.document.layers[0];
            if (index == layer.FeatureCount()) return;
            layer.rtree.InsertData(index);
            index++;
            GISVectorLayer treelayer = layer.rtree.GetTreeLayer();
            if (gisPanel1.document.getLayer(treelayer.Name) != null)
                gisPanel1.document.RemoveLayer(treelayer.Name);
            gisPanel1.document.AddLayer(treelayer);
            gisPanel1.UpdateMap();
        }

    }
}
