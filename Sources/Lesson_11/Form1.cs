using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_11
{
    public partial class Form1 : Form
    {
        GISLayer layer = null;
        GISView view = null;
        Form2 AtributeWindow = null;

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

        public void UpdateMap()
        {
            Graphics graphics = CreateGraphics();
            graphics.FillRectangle(new SolidBrush(Color.Black), ClientRectangle);
            layer.draw(graphics, view);
            UpdateStatusBar();
        }

        public void UpdateStatusBar()
        {
            toolStripStatusLabel1.Text = layer.Selection.Count.ToString();
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

        private void button9_Click(object sender, EventArgs e)
        {
            OpenAttributeWindow();
        }

        private void OpenAttributeWindow()
        {
            //如果图层为空就返回
            if (layer == null) return;
            //如果属性窗口还没有初始化，则初始化
            if (AtributeWindow == null)
                AtributeWindow = new Form2(layer, this);
            //如果属性窗口资源被释放了，则初始化
            if (AtributeWindow.IsDisposed)
                AtributeWindow = new Form2(layer, this);
            //显示属性窗口
            AtributeWindow.Show();
            //如果属性窗口最小化了，令它正常显示
            if (AtributeWindow.WindowState == FormWindowState.Minimized)
                AtributeWindow.WindowState = FormWindowState.Normal;
            //把属性窗口放到桌面最前端显示
            AtributeWindow.BringToFront();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            GISMyFile.WriteFile(layer, @"E:\mygisfile.data");
            MessageBox.Show("done.");
        }

        private void button11_Click(object sender, EventArgs e)
        {
            layer = GISMyFile.ReadFile(@"E:\mygisfile.data");
            MessageBox.Show("read " + layer.FeatureCount() + " objects.");
            view.UpdateExtent(layer.Extent);
            UpdateMap();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (layer==null) return;
            GISVertex v = view.ToMapVertex(new Point(e.X, e.Y));
            SelectResult sr = layer.Select(v, view);
            if (sr == SelectResult.OK)
            {
                UpdateMap();
                UpdateAttributeWindow();
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (layer == null) return;
            layer.ClearSelection();
            UpdateMap();
            UpdateAttributeWindow();
        }

        private void UpdateAttributeWindow()
        {
            //如果图层为空，则返回
            if (layer == null) return;
            //如果属性窗口为空，则返回
            if (AtributeWindow == null) return;
            //如果属性窗口资源已经释放，则返回
            if (AtributeWindow.IsDisposed) return;
            //调用属性窗口的数据更新函数
            AtributeWindow.UpdateData();
        }
    }
}
