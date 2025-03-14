using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenApp
{
    public partial class SnapShotWindow : Window
    {
        public SnapShotWindow()
        {
            InitializeComponent();
            _context = new StateContext(this);
        }

        StateContext _context;
        public System.Drawing.Bitmap _bitmap;

        //鼠标左键按下事件
        private void PanelMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.ClickCount)
            {
                case 1:
                    if (_context._state == ScreenState.Start)
                    {
                        _context.SetNewState(ScreenState.Select, e);
                    }
                    if (_context._state == ScreenState.Selected || _context._state == ScreenState.Moved)
                    {
                        if(_context.IsInClipRect(e.GetPosition(this.clipRect)))
                        {
                            _context.SetNewState(ScreenState.Move, e);
                            var pos = e.GetPosition(this);  //拖拽后鼠标的位置
                            var dp = pos - _context._mouseDownPosition; //鼠标移动的偏移量
                        }
                    }
                    break;
                case 2:
                    _bitmap?.Dispose();

                    Point leftTop = clipRect.PointToScreen(new Point(0, 0));
                    Point rightBottom = clipRect.PointToScreen(new Point(clipRect.ActualWidth, clipRect.ActualHeight));

                    int width = Math.Abs((int)(rightBottom.X - leftTop.X));
                    int height = Math.Abs((int)(rightBottom.Y - leftTop.Y));
                    if (width > 0 && height > 0)
                    {
                        _bitmap = Snapshot((int)leftTop.X, (int)leftTop.Y, width, height);
                        Clipboard.SetImage(ConvertToBitmapSource(_bitmap));
                        this.DialogResult = true;
                    }
                    break;
            }
        }

        //鼠标左键抬起事件  
        private void PanelMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_context._state == ScreenState.Selecting)
            {
                //框选太小 直接恢复到开始状态
                if ((_context._endPoint.X - _context._startPoint.X <= 5) && (_context._endPoint.Y - _context._startPoint.Y < 5))
                {
                    _context.SetNewState(ScreenState.Start, e);
                }
                else
                {
                    _context.SetNewState(ScreenState.Selected, e);
                }
            }
            if (_context._state == ScreenState.Moving)
            {
                _context.SetNewState(ScreenState.Moved, e);
            }
        }

        //鼠标移动事件
        private void PanelMouseMove(object sender, MouseEventArgs e)
        {
            //如果鼠标左键未按下
            if (e.LeftButton == MouseButtonState.Released &&
                (_context._state == ScreenState.Selected || _context._state == ScreenState.Moved))
            {
                if (_context.IsInClipRect(e.GetPosition(this.clipRect)))
                {
                    _context._allowMove = true;
                    this.Cursor = Cursors.SizeAll;
                }
                else
                {
                    _context._allowMove = false;
                    this.Cursor = Cursors.Arrow;
                }
            }

            //如果鼠标左键被按下
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_context._state == ScreenState.Select || _context._state == ScreenState.Selecting)
                {
                    _context.SetNewState(ScreenState.Selecting, e);
                }

                if (
                    (_context._state == ScreenState.Move || _context._state == ScreenState.Moving))
                {
                    _context.SetNewState(ScreenState.Moving, e);
                }
            }
        }

        //鼠标右键抬起事件:恢复到开始状态
        private new void MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _context.SetNewState(ScreenState.Start, e);
        }

        //截图生成bitmap图片
        public static System.Drawing.Bitmap Snapshot(int x, int y, int width, int height)
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), System.Drawing.CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        //格式转换
        public static BitmapSource ConvertToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }
    }
}
