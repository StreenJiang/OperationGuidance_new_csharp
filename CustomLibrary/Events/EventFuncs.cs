using System.Runtime.InteropServices;
using CustomLibrary.Forms;
using CustomWidgets.Structs;

namespace CustomLibrary.Events {
    public class EventFuncs {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        private static List<Control> _autoActivatingControls = new();
        public static List<Action> _clickActions = new();

        public static POINT RealTimePoint { get; set; }
        public static Form? MainForm { get; set; }
        public static CustomPopUpForm? CurrentPopUpForm { get; set; }
        public static Control? CurrentActiveControl { get; set; }
        public static Control? AutoCloseControl { get; set; }

        public static void AddAutoActivatingControl(Control control) {
            _autoActivatingControls.Add(control);
        }

        public static void AddClickAction(Action action) {
            _clickActions.Add(action);
        }

        public static void GlobalMouseClick(object? sender, EventArgs eventArgs) {
            if (GetCursorPos(out POINT point)) {
                RealTimePoint = point;
                if (MainForm != null && !MainForm.IsDisposed) {
                    Rectangle mainFormRectangleToScreen = new(MainForm.PointToScreen(Point.Empty), MainForm.ClientSize);
                    if (mainFormRectangleToScreen.Contains(point)) {
                        // 点击在Control范围外时，直接销毁该Control
                        if (AutoCloseControl != null && !AutoCloseControl.IsDisposed && AutoCloseControl.Visible) {
                            Rectangle rectangleToScreen = new(AutoCloseControl.PointToScreen(Point.Empty), AutoCloseControl.Size);
                            // 判断鼠标点击的坐标是否在弹框范围外
                            if (!rectangleToScreen.Contains(point)) {
                                AutoCloseControl.Controls.Clear();
                                AutoCloseControl.Dispose();
                                AutoCloseControl = null;
                            }
                        }

                        // 执行Actions
                        foreach (Action action in _clickActions) {
                            action();
                        }

                        // 点击在弹出框范围外时，关闭弹出框
                        if (CurrentPopUpForm != null && !CurrentPopUpForm.IsDisposed && CurrentPopUpForm.Visible && CurrentPopUpForm.BackForm.Focused) {
                            Rectangle rectangleToScreen = new(CurrentPopUpForm.PointToScreen(Point.Empty), CurrentPopUpForm.Size);
                            // 判断鼠标点击的坐标是否在弹框范围外
                            if (!rectangleToScreen.Contains(point)) {
                                CurrentPopUpForm.HideForm();
                                CurrentPopUpForm = null;
                            }
                        }

                        // Control失去焦点
                        if (CurrentActiveControl != null && !CurrentActiveControl.IsDisposed && CurrentActiveControl.Visible) {
                            Rectangle rectangleToScreen = new(CurrentActiveControl.PointToScreen(Point.Empty), CurrentActiveControl.Size);
                            // 判断鼠标点击的坐标是否在弹框范围外
                            if (!rectangleToScreen.Contains(point)) {
                                EventFuncs.CurrentActiveControl = null;
                                // 判断当前control是否在mainForm上获取的焦点
                                if (MainForm != null && MainForm.ActiveControl != null) {
                                    MainForm.ActiveControl = null;
                                }
                                // 检查当前是否有弹窗，如果有的话检查control是否也在弹窗上有焦点
                                if (CurrentPopUpForm != null && CurrentPopUpForm.ActiveControl != null) {
                                    CurrentPopUpForm.ActiveControl = null;
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void GlobalMouseMove(object? sender, EventArgs eventArgs) {
            if (GetCursorPos(out POINT point)) {
                if (MainForm != null && !MainForm.IsDisposed) {
                    try {
                        Point e = Point.Empty;
                        Size s = MainForm.ClientSize;
                        Point p = MainForm.PointToScreen(Point.Empty);
                        Rectangle mainFormRectangleToScreen = new(MainForm.PointToScreen(Point.Empty), MainForm.ClientSize);
                        if (mainFormRectangleToScreen.Contains(point)) {
                            // 鼠标进入时自动获取焦点
                            if (CurrentActiveControl == null || CurrentActiveControl.IsDisposed && _autoActivatingControls.Count > 0) {
                                foreach (Control control in _autoActivatingControls) {
                                    if (control != null && !control.IsDisposed && control.Visible && control.CanFocus && control != MainForm.ActiveControl) {
                                        Rectangle rectangleToScreen = new(control.PointToScreen(Point.Empty), control.Size);
                                        if (rectangleToScreen.Contains(point)) {
                                            MainForm.ActiveControl = control;
                                        } else if (!rectangleToScreen.Contains(point)) {
                                            MainForm.ActiveControl = null;
                                        }
                                    }
                                }
                            }
                        }
                    } catch (Exception e) {
                        Console.WriteLine("e: " + e);
                        throw e;
                    } finally {
                    }
                }
            }
        }
    }
}
