using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MyGIS;

namespace Lesson_14
{
    public partial class Form1 : Form
    {
        GISLayer layer = null;
        GISView view = null;
        Form2 AtributeWindow = null;
        Bitmap backwindow;

        MOUSECOMMAND MouseCommand = MOUSECOMMAND.Pan;
        int MouseStartX = 0;
        int MouseStartY = 0;
        int MouseMovingX = 0;
        int MouseMovingY = 0;

        bool MouseOnMap = false;
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
            MessageBox.Show("read " + layer.FeatureCount() + " objects.");
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
            layer.draw(g, view);
            //把背景窗口绘制到前景窗口上
            Graphics graphics = CreateGraphics();
            graphics.DrawImage(backwindow, 0, 0);
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
            if (layer == null) return;
            if (MouseOnMap == false) return;
            MouseOnMap = false;
            switch (MouseCommand)
            {
                case MOUSECOMMAND.Select:
                    //如果ctrl键没被按住，就清空选择集
                    if (Control.ModifierKeys != Keys.Control) layer.ClearSelection();
                    //初始化选择结果
                    SelectResult sr = SelectResult.UnknownType;
                    if (e.X == MouseStartX && e.Y == MouseStartY)
                    {
                        //点选
                        GISVertex v = view.ToMapVertex(new Point(e.X, e.Y));
                        sr = layer.Select(v, view);
                    }
                    else
                    {
                        //框选
                        GISExtent extent = view.RectToExtent(e.X, MouseStartX, e.Y, MouseStartY);
                        sr = layer.Select(extent);
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
            if (layer == null) return;            
            if (sender.Equals(fullExtentToolStripMenuItem))
            {
                view.UpdateExtent(layer.Extent);
                UpdateMap();
            }
            else
            {
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
