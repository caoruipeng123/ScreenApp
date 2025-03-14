using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreenApp
{
    public enum ScreenState
    {
        Start,//开始
        Select,//开始框选
        Selecting,//框选中
        Selected,//框选结束
        Move,//开始移动
        Moving,//移动中
        Moved,//移动结束
        End//结束
    }

    //状态上下文
    public class StateContext
    {
        public IScreentState _currentState;

        public SnapShotWindow _window;
        public MouseEventArgs _args;
        //框选开始坐标
        public Point _startPoint;
        //框选结束坐标
        public Point _endPoint;
        //目前状态
        public ScreenState _state;
        //开始拖拽时,鼠标按下的位置
        public Point _mouseDownPosition;
        //开始拖拽时,鼠标按下控件的Margin
        public Thickness _mouseDownMargin;
        public bool _allowMove = false;

        private readonly IScreentState _startState;
        private readonly IScreentState _selectState;
        private readonly IScreentState _selectingState;
        private readonly IScreentState _selectedState;
        private readonly IScreentState _moveState;
        private readonly IScreentState _movingState;
        private readonly IScreentState _movedState;
        private readonly IScreentState _endState;

        public StateContext(SnapShotWindow window)
        {
            _window = window;
            _startState = new StartState();
            _selectState = new SelectState();
            _selectingState = new SelectingState();
            _selectedState = new SelectedState();
            _moveState = new MoveState();
            _movingState = new MovingState();
            _movedState = new MovedState();
            _endState = new EndState();

            _state = ScreenState.Start;
            _currentState = _startState;
            SetNewState(_state);
        }

        public void SetNewState(ScreenState state)
        {
            _state = state;
            switch (state)
            {
                case ScreenState.Start:
                    _currentState = _startState;
                    break;
                case ScreenState.Select:
                    _currentState = _selectState;
                    break;
                case ScreenState.Selecting:
                    _currentState = _selectingState;
                    break;
                case ScreenState.Selected:
                    _currentState = _selectedState;
                    break;
                case ScreenState.Move:
                    _currentState = _moveState;
                    break;
                case ScreenState.Moving:
                    _currentState = _movingState;
                    break;
                case ScreenState.Moved:
                    _currentState = _movedState;
                    break;
                case ScreenState.End:
                    _currentState = _endState;
                    break;
            }
            _currentState.ProcessState(this);
        }

        public void SetNewState(ScreenState state, MouseEventArgs args)
        {
            _args = args;
            var point = args.GetPosition(_window);
            SetNewState(state);
        }

        public string GetText(double offsetX = 0, double offsetY = 0)
        {
            Point leftTop = _window.clipRect.PointToScreen(new Point(0, 0));
            Point rightBottom = _window.clipRect.PointToScreen(new Point(_window.clipRect.ActualWidth, _window.clipRect.ActualHeight));

            double width = Math.Round(Math.Abs(rightBottom.X - leftTop.X) + offsetX);
            double height = Math.Round(Math.Abs(rightBottom.Y - leftTop.Y) + offsetY);

            return $"{leftTop}    {width}×{height}";
        }

        public bool IsInClipRect(Point point)
        {
            var relativePosition = point;
            if (relativePosition.X >= 0 && relativePosition.X <= _window.clipRect.ActualWidth &&
                relativePosition.Y >= 0 && relativePosition.Y <= _window.clipRect.ActualHeight)
            {
                return true;
            }
            return false;
        }
    }

    //状态接口
    public interface IScreentState
    {
        void ProcessState(StateContext context);
    }

    //......下面是一些状态的实现类
    public class StartState : IScreentState
    {
        public void ProcessState(StateContext context)
        {
            SnapShotWindow win = context._window;
            win.clipRect.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8000"));
            win.Cursor = Cursors.Arrow;
            win.leftPanel.Width = 0;
            win.topPanel.Height = 0;
            win.rightPanel.Width = 0;
            win.bottomPanel.Height = 0;
        }
    }

    public class SelectState : IScreentState
    {
        public void ProcessState(StateContext context)
        {
            context._startPoint = context._args.GetPosition(context._window);
        }
    }

    public class SelectingState : IScreentState
    {
        public void ProcessState(StateContext context)
        {
            SnapShotWindow win = context._window;
            win.clipRect.Background = Brushes.Transparent;
            context._endPoint = context._args.GetPosition(win);
            win.leftPanel.Width = context._startPoint.X;
            win.topPanel.Height = context._startPoint.Y;
            win.rightPanel.Width = win.ActualWidth - context._endPoint.X;
            win.bottomPanel.Height = win.ActualHeight - context._endPoint.Y;
            win.snapShotInfo.Text = context.GetText();
        }
    }

    public class SelectedState : IScreentState
    {
        public void ProcessState(StateContext context)
        {
            context._endPoint = context._args.GetPosition(context._window);
        }
    }

    public class MoveState : IScreentState
    {
        public void ProcessState(StateContext context)
        {
            SnapShotWindow win = context._window;
            context._mouseDownPosition = context._args.GetPosition(win);
            context._mouseDownMargin = new Thickness(win.leftPanel.ActualWidth, win.topPanel.ActualHeight, win.rightPanel.ActualWidth, win.bottomPanel.Height); ;

            var relativePosition = context._args.GetPosition(win);
            if (relativePosition.X >= 0 && relativePosition.X <= win.clipRect.ActualWidth &&
                relativePosition.Y >= 0 && relativePosition.Y <= win.clipRect.ActualHeight)
            {
                context._allowMove = true;
                win.Cursor = Cursors.SizeAll;
            }
        }
    }

    public class MovingState : IScreentState
    {
        public void ProcessState(StateContext context)
        {
            SnapShotWindow win = context._window;
            win.Cursor = Cursors.SizeAll;
            //todo 隐藏操作按钮
            var pos = context._args.GetPosition(win);  //拖拽后鼠标的位置
            var dp = pos - context._mouseDownPosition; //鼠标移动的偏移量
            Thickness newThickness = new Thickness(context._mouseDownMargin.Left + dp.X,
                context._mouseDownMargin.Top + dp.Y,
                context._mouseDownMargin.Right - dp.X,
                context._mouseDownMargin.Bottom - dp.Y);
            win.leftPanel.Width = newThickness.Left < 0 ? 0 : newThickness.Left;
            win.topPanel.Height = newThickness.Top < 0 ? 0 : newThickness.Top;
            win.rightPanel.Width = newThickness.Right < 0 ? 0 : newThickness.Right;
            win.bottomPanel.Height = newThickness.Bottom < 0 ? 0 : newThickness.Bottom;
            win.snapShotInfo.Text = context.GetText();
        }
    }

    public class MovedState : IScreentState
    {
        public void ProcessState(StateContext context)
        {
            //todo 显示操作按钮
        }
    }

    public class EndState : IScreentState
    {
        public void ProcessState(StateContext context)
        {
        }
    }
}
