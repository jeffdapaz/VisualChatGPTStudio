using OpenAI_API.ResponsesAPI.Models.Response;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    public static class ComputerUse
    {
        private static readonly InputSimulator inputSimulator = new();
        private static Rectangle _screenBounds;

        public static void DoAction(ComputerUseAction action, Rectangle screenBounds)
        {
            _screenBounds = screenBounds;

            switch (action.Type)
            {
                case ComputerUseActionType.Click:
                    MouseClick(action.X, action.Y, action.Button);
                    break;
                case ComputerUseActionType.DoubleClick:
                    MouseClick(action.X, action.Y, action.Button, doubleClick: true);
                    break;
                case ComputerUseActionType.Scroll:
                    MouseScroll(action.X, action.Y, action.ScrollX ?? 0, action.ScrollY ?? 0);
                    break;
                case ComputerUseActionType.KeyPress:
                    KeyPress(action.Text);
                    break;
                case ComputerUseActionType.Type:
                    TypeText(action.Text);
                    break;
                case ComputerUseActionType.Move:
                    SetCursorPosition(action.X, action.Y);
                    break;
                case ComputerUseActionType.Wait:
                    Thread.Sleep(2000);
                    break;
                case ComputerUseActionType.Screenshot:
                    // No action needed, screenshot will be taken after
                    break;
                default:
                    Logger.Log("Unknown or unsupported action: " + action.Type);
                    break;
            }
        }

        private static void MouseClick(int? x, int? y, ComputerUseButton button, bool doubleClick = false)
        {
            SetCursorPosition(x, y);

            switch (button)
            {
                case ComputerUseButton.Left:
                    if (doubleClick)
                    {
                        inputSimulator.Mouse.LeftButtonDoubleClick();
                    }
                    else
                    {
                        inputSimulator.Mouse.LeftButtonClick();
                    }
                    break;
                case ComputerUseButton.Right:
                    if (doubleClick)
                    {
                        inputSimulator.Mouse.RightButtonDoubleClick();
                    }
                    else
                    {
                        inputSimulator.Mouse.RightButtonClick();
                    }
                    break;
                default:
                    // Unknown or unsupported action
                    break;
            }
        }

        private static void MouseScroll(int? x, int? y, int scrollX, int scrollY)
        {
            SetCursorPosition(x, y);

            if (scrollY != 0)
            {
                inputSimulator.Mouse.VerticalScroll(scrollY);
            }

            if (scrollX != 0)
            {
                inputSimulator.Mouse.HorizontalScroll(scrollX);
            }
        }

        private static void KeyPress(string keys)
        {
            if (string.IsNullOrWhiteSpace(keys))
            {
                return;
            }

            // Map common keys
            if (keys.ToUpper().Contains("ENTER"))
            {
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }
            else if (keys.ToUpper().Contains("SPACE"))
            {
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);
            }
            else
            {
                foreach (char c in keys)
                {
                    inputSimulator.Keyboard.TextEntry(c);
                }
            }
        }

        private static void TypeText(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                inputSimulator.Keyboard.TextEntry(text);
            }
        }

        private static void SetCursorPosition(int? x, int? y)
        {
            if (x.HasValue && y.HasValue)
            {
                int globalX = _screenBounds.X + x.Value;
                int globalY = _screenBounds.Y + y.Value;

                Cursor.Position = new Point(globalX, globalY);

                Thread.Sleep(50);
            }
        }
    }
}