using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_15    
{
    public partial class Form1 : Form
    {
        GISView view = null;
        Dictionary<GISLayer, Form2> AllAttWnds = new Dictionary<GISLayer, Form2>();
        Bitmap backwindow;

        MOUSECOMMAND MouseCommand = MOUSECOMMAND.Pan;
        int MouseStartX = 0;
        int MouseStartY = 0;
        int MouseMovingX = 0;
        int MouseMovingY = 0;

        bool MouseOnMap = false;
        GISDocument document = new GISDocument();

        public Form1()
        {
            InitializeComponent();
        }


        public void UpdateMap()
        {
            if (view == null)
            {
                if (document.IsEmpty()) return;
                view = new GISView(new GISExtent(document.Extent), ClientRectangle);
            }
            //如果地图窗口被最小化了，就不用绘制了
            if (ClientRectangle.Width * ClientRectangle.Height == 0) return;
            //确保当前view的地图窗口尺寸是正确的
            view.UpdateRectangle(ClientRectangle);
            //根据最新的地图窗口尺寸建立背景窗口
            if (backwindow != null) backwindow.Dispose();
            backwindow = new Bitmap(ClientRectangle.Width, ClientRectangle.Height);
            //在背景窗口上绘图
            Graphics g = Graphics.FromImage(backwindow);
            g.FillRectangle(new SolidBrush(Color.Black), ClientRectangle);
            document.draw(g, view);
            //把背景窗口绘制到前景窗口上
            Graphics graphics = CreateGraphics();
            graphics.DrawImage(backwindow, 0, 0);
            UpdateStatusBar();
        }

        public void UpdateStatusBar()
        {
            toolStripStatusLabel1.Text = document.layers.Count.ToString();
        }

        public void OpenAttributeWindow(GISLayer layer)
        {
            Form2 AttributeWindow = null;
            //如果属性窗口之前已经存在了，就找到它，然后移除记录，稍后统一添加
            if (AllAttWnds.ContainsKey(layer))
            {
                AttributeWindow = AllAttWnds[layer];
                AllAttWnds.Remove(layer);
            }
            //初始化属性窗口
            if (AttributeWindow == null)
                AttributeWindow = new Form2(layer, this);
            if (AttributeWindow.IsDisposed)
                AttributeWindow = new Form2(layer, this);
            //添加属性窗口与图层的关联记录
            AllAttWnds.Add(layer, AttributeWindow);
            //显示属性窗口
            AttributeWindow.Show();
            if (AttributeWindow.WindowState == FormWindowState.Minimized)
                AttributeWindow.WindowState = FormWindowState.Normal;
            AttributeWindow.BringToFront();
        }


        private void UpdateAttributeWindow()
        {
            //如果图层为空，则返回
            if (document.IsEmpty()) return;
            foreach (Form2 AttributeWindow in AllAttWnds.Values)
            {
                //如果属性窗口为空，则返回
                if (AttributeWindow == null) continue;
                //如果属性窗口资源已经释放，则返回
                if (AttributeWindow.IsDisposed) continue;
                //调用属性窗口的数据更新函数
                AttributeWindow.UpdateData();
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            if (backwindow != null)
            {
                //是鼠标操作引起的窗口重绘
                if (MouseOnMap)
                {
                    //是由于移动地图造成的，就移动背景图片
                    if (MouseCommand == MOUSECOMMAND.Pan)
                    {
                        e.Graphics.DrawImage(backwindow, MouseMovingX - MouseStartX, MouseMovingY - MouseStartY);
                    }
                    //是由于选择或缩放操作造成的，就画一个框
                    else if (MouseCommand != MOUSECOMMAND.Unused)
                    {
                        e.Graphics.DrawImage(backwindow, 0, 0);
                        e.Graphics.FillRectangle(new SolidBrush(GISConst.ZoomSelectBoxColor), new Rectangle(
                            Math.Min(MouseStartX, MouseMovingX), Math.Min(MouseStartY, MouseMovingY),
                            Math.Abs(MouseStartX - MouseMovingX), Math.Abs(MouseStartY - MouseMovingY)));
                    }
                }
                //如果不是鼠标引起的，就直接复制背景窗口
                else e.Graphics.DrawImage(backwindow, 0, 0);
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            UpdateMap();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            MouseStartX = e.X;
            MouseStartY = e.Y;
            MouseOnMap = (e.Button == MouseButtons.Left && MouseCommand != MOUSECOMMAND.Unused);
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            MouseMovingX = e.X;
            MouseMovingY = e.Y;
            if (MouseOnMap) Invalidate();
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (document.IsEmpty()) return;
            if (MouseOnMap == false) return;
            MouseOnMap = false;
            switch (MouseCommand)
            {
                case MOUSECOMMAND.Select:
                    //如果ctrl键没被按住，就清空选择集
                    if (Control.ModifierKeys != Keys.Control) document.ClearSelection();
                    //初始化选择结果
                    SelectResult sr = SelectResult.UnknownType;
                    if (e.X == MouseStartX && e.Y == MouseStartY)
                    {
                        //点选
                        GISVertex v = view.ToMapVertex(new Point(e.X, e.Y));
                        sr = document.Select(v, view);
                    }
                    else
                    {
                        //框选
                        GISExtent extent = view.RectToExtent(e.X, MouseStartX, e.Y, MouseStartY);
                        sr = document.Select(extent);
                    }
                    //仅当选择集最可能发生变化时，才更新地图和属性窗口
                    if (sr == SelectResult.OK || Control.ModifierKeys != Keys.Control)
                    {
                        UpdateMap();
                        UpdateAttributeWindow();
                    }
                    break;

                case MOUSECOMMAND.ZoomIn:
                    if (e.X == MouseStartX && e.Y == MouseStartY)
                    {
                        //单点放大
                        GISVertex MouseLocation = view.ToMapVertex(new Point(e.X, e.Y));
                        GISExtent E1 = view.getRealExtent();
                        double newwidth = E1.getWidth() * GISConst.ZoomInFactor;
                        double newheight = E1.getHeight() * GISConst.ZoomInFactor;
                        double newminx = MouseLocation.x - (MouseLocation.x - E1.getMinX()) * GISConst.ZoomInFactor;
                        double newminy = MouseLocation.y - (MouseLocation.y - E1.getMinY()) * GISConst.ZoomInFactor;
                        view.UpdateExtent(new GISExtent(newminx, newminx + newwidth, newminy, newminy + newheight));
                    }
                    else
                    {
                        //拉框放大
                        view.UpdateExtent(view.RectToExtent(e.X, MouseStartX, e.Y, MouseStartY));
                    }
                    UpdateMap();
                    break;
                case MOUSECOMMAND.ZoomOut:
                    if (e.X == MouseStartX && e.Y == MouseStartY)
                    {
                        //点击缩小
                        GISExtent E1 = view.getRealExtent();
                        GISVertex MouseLocation = view.ToMapVertex(new Point(e.X, e.Y));
                        double newwidth = E1.getWidth() / GISConst.ZoomOutfactor;
                        double newheight = E1.getHeight() / GISConst.ZoomOutfactor;
                        double newminx = MouseLocation.x - (MouseLocation.x - E1.getMinX()) / GISConst.ZoomOutfactor;
                        double newminy = MouseLocation.y - (MouseLocation.y - E1.getMinY()) / GISConst.ZoomOutfactor;
                        view.UpdateExtent(new GISExtent(newminx, newminx + newwidth, newminy, newminy + newheight));
                    }
                    else
                    {
                        //拉框缩小
                        GISExtent E3 = view.RectToExtent(e.X, MouseStartX, e.Y, MouseStartY);
                        GISExtent E1 = view.getRealExtent();
                        double newwidth = E1.getWidth() * E1.getWidth() / E3.getWidth();
                        double newheight = E1.getHeight() * E1.getHeight() / E3.getHeight();
                        double newminx = E3.getMinX() - (E3.getMinX() - E1.getMinX()) * newwidth / E1.getWidth();
                        double newminy = E3.getMinY() - (E3.getMinY() - E1.getMinY()) * newheight / E1.getHeight();
                        view.UpdateExtent(new GISExtent(newminx, newminx + newwidth, newminy, newminy + newheight));
                    }
                    UpdateMap();
                    break;
                case MOUSECOMMAND.Pan:
                    if (e.X != MouseStartX || e.Y != MouseStartY)
                    {
                        GISExtent E1 = view.getRealExtent();
                        GISVertex M1 = view.ToMapVertex(new Point(MouseStartX, MouseStartY));
                        GISVertex M2 = view.ToMapVertex(new Point(e.X, e.Y));
                        double newwidth = E1.getWidth();
                        double newheight = E1.getHeight();
                        double newminx = E1.getMinX() - (M2.x - M1.x);
                        double newminy = E1.getMinY() - (M2.y - M1.y);
                        view.UpdateExtent(new GISExtent(newminx, newminx + newwidth, newminy, newminy + newheight));
                        UpdateMap();
                    }
                    break;
            }
        }

        private void toolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender.Equals(openDocumentToolStripMenuItem))
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "GIS Document (*." + GISConst.MYDOC + ")|*." + GISConst.MYDOC;
                openFileDialog.RestoreDirectory = false;
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = false;
                if (openFileDialog.ShowDialog() != DialogResult.OK) return;
                document.Read(openFileDialog.FileName);
                if (document.IsEmpty() == false)
                    UpdateMap();
            }
            else if (sender.Equals(layerControlToolStripMenuItem))
            {
                Form3 LayerControl = new Form3(document, this);
                LayerControl.ShowDialog();
            }
            else if (sender.Equals(fullExtentToolStripMenuItem))
            {
                if (document.IsEmpty() || view == null) return;
                view.UpdateExtent(document.Extent);
                UpdateMap();
            }
            else
            {
                if (document.IsEmpty() || view == null) return;
                selectToolStripMenuItem.Checked = false;
                zoomInToolStripMenuItem.Checked = false;
                zoomOutToolStripMenuItem.Checked = false;
                panToolStripMenuItem.Checked = false;
                ((ToolStripMenuItem)sender).Checked = true;
                if (sender.Equals(selectToolStripMenuItem))
                    MouseCommand = MOUSECOMMAND.Select;
                else if (sender.Equals(zoomInToolStripMenuItem))
                    MouseCommand = MOUSECOMMAND.ZoomIn;
                else if (sender.Equals(zoomOutToolStripMenuItem))
                    MouseCommand = MOUSECOMMAND.ZoomOut;
                else if (sender.Equals(panToolStripMenuItem))
                    MouseCommand = MOUSECOMMAND.Pan;
            }
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                contextMenuStrip1.Show(this.PointToScreen(new Point(e.X, e.Y)));
        }

    }
}
