using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_6
{
    public partial class Form1 : Form
    {
        GISLayer layer = null;
        GISView view = null;
        public Form1()
        {
            InitializeComponent();
            view = new GISView(new GISExtent(new GISVertex(0, 0), new GISVertex(100, 100)),
                    ClientRectangle);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Shapefile文件|*.shp";
            openFileDialog.RestoreDirectory = false;
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            layer = GISShapefile.ReadShapeFile(openFileDialog.FileName);
            layer.DrawAttributeOrNot = false;
            MessageBox.Show("read " + layer.FeatureCount() +  " objects.");
            view.UpdateExtent(layer.Extent);
            UpdateMap();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            view.UpdateExtent(layer.Extent);
            UpdateMap();
        }

        private void UpdateMap()
        {
            Graphics graphics = CreateGraphics();
            graphics.FillRectangle(new SolidBrush(Color.Black), ClientRectangle);
            layer.draw(graphics, view);
        }

        private void MapButtonClick(object sender, EventArgs e)
        {
            GISMapActions action = GISMapActions.zoomin;
            if ((Button)sender == button3) action = GISMapActions.zoomin;
            else if ((Button)sender == button4) action = GISMapActions.zoomout;
            else if ((Button)sender == button5) action = GISMapActions.moveup;
            else if ((Button)sender == button6) action = GISMapActions.movedown;
            else if ((Button)sender == button7) action = GISMapActions.moveleft;
            else if ((Button)sender == button8) action = GISMapActions.moveright;
            view.ChangeView(action);
            UpdateMap();
        }
    }
}
