using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Text;
using OpenAI_API.ResponsesAPI.Models.Response;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Provides computer usage automations.
    /// </summary>
    public static class ComputerUse
    {
        private static readonly InputSimulator inputSimulator = new();
        private static Rectangle _screenBounds;

        /// <summary>
        /// Executes the specified computer use action asynchronously within the given screen bounds.
        /// </summary>
        /// <param name="action">The computer use action to perform.</param>
        /// <param name="screenBounds">The bounds of the screen where the action will be performed.</param>
        public static async Task DoActionAsync(ComputerUseAction action, Rectangle screenBounds)
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
                    await KeyPressAsync(action.Keys);
                    break;
                case ComputerUseActionType.Type:
                    await TypeTextAsync(action.Text);
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

        /// <summary>
        /// Simulates a mouse click at the specified screen coordinates with the specified mouse button.
        /// </summary>
        /// <param name="x">The x-coordinate of the cursor position. If null, the current position is used.</param>
        /// <param name="y">The y-coordinate of the cursor position. If null, the current position is used.</param>
        /// <param name="button">The mouse button to click (left or right).</param>
        /// <param name="doubleClick">Indicates whether to perform a double click instead of a single click.</param>
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

        /// <summary>
        /// Simulates mouse scrolling at a specified cursor position.
        /// </summary>
        /// <param name="x">The optional x-coordinate to set the cursor position before scrolling. If null, the cursor position is not changed horizontally.</param>
        /// <param name="y">The optional y-coordinate to set the cursor position before scrolling. If null, the cursor position is not changed vertically.</param>
        /// <param name="scrollX">The amount to scroll horizontally. Positive values scroll right, negative values scroll left.</param>
        /// <param name="scrollY">The amount to scroll vertically. Positive values scroll up, negative values scroll down.</param>
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

        /// <summary>
        /// Simulates asynchronous key presses for a sequence of keys.
        /// </summary>
        /// <param name="keys">An enumerable collection of key strings to be pressed. Supports modifiers and normal keys.</param>
        public static async Task KeyPressAsync(IEnumerable<string> keys)
        {
            if (keys == null)
            {
                return;
            }

            // Normalize keys to upper-case
            List<string> keyList = keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim().ToUpperInvariant()).ToList();

            if (keyList.Count == 0)
            {
                return;
            }

            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync().ConfigureAwait(true);
            ITextBuffer textBuffer = docView?.TextBuffer;
            Microsoft.VisualStudio.Text.Editor.IWpfTextView textView = docView?.TextView;
            int caretPos = textView?.Caret.Position.BufferPosition.Position ?? -1;
            bool handledInEditor = false;

            // If it's a simple text insertion (no modifiers) we can insert directly
            if (textBuffer != null && caretPos >= 0 && keyList.Count == 1 && !IsModifierKey(keyList[0]))
            {
                if (keyList[0] == "ENTER")
                {
                    textBuffer.Insert(caretPos, Environment.NewLine);
                    handledInEditor = true;
                }
                else if (keyList[0] == "SPACE")
                {
                    textBuffer.Insert(caretPos, " ");
                    handledInEditor = true;
                }
                else if (keyList[0].Length == 1)
                {
                    textBuffer.Insert(caretPos, keyList[0]);
                    handledInEditor = true;
                }
            }

            if (handledInEditor)
            {
                return;
            }

            // Fallback to InputSimulator for any other keypress scenario
            // Press all modifier keys down first
            List<string> modifiers = keyList.Where(IsModifierKey).Distinct().ToList();
            List<string> normalKeys = keyList.Except(modifiers).ToList();

            // Map strings to VirtualKeyCode
            VirtualKeyCode ToVk(string k) => k switch
            {
                "CTRL" or "CONTROL" => VirtualKeyCode.CONTROL,
                "SHIFT" => VirtualKeyCode.SHIFT,
                "ALT" => VirtualKeyCode.MENU,
                "ENTER" => VirtualKeyCode.RETURN,
                "SPACE" => VirtualKeyCode.SPACE,
                "TAB" => VirtualKeyCode.TAB,
                // Add more named keys as needed
                _ when k.Length == 1 && char.IsLetterOrDigit(k[0]) => (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), "VK_" + k),
                _ => VirtualKeyCode.NONAME
            };

            // Hold modifiers
            foreach (string mod in modifiers)
            {
                VirtualKeyCode vk = ToVk(mod);
                if (vk != VirtualKeyCode.NONAME)
                {
                    inputSimulator.Keyboard.KeyDown(vk);
                }
            }

            // Press normal keys
            foreach (string normal in normalKeys)
            {
                VirtualKeyCode vk = ToVk(normal);
                if (vk != VirtualKeyCode.NONAME)
                {
                    inputSimulator.Keyboard.KeyPress(vk);
                }
            }

            // Release modifiers
            foreach (string mod in modifiers.AsEnumerable().Reverse())
            {
                VirtualKeyCode vk = ToVk(mod);
                if (vk != VirtualKeyCode.NONAME)
                {
                    inputSimulator.Keyboard.KeyUp(vk);
                }
            }
        }

        /// <summary>
        /// Determines whether the specified key is a modifier key (CTRL, CONTROL, SHIFT, or ALT).
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key is a modifier key; otherwise, false.</returns>
        private static bool IsModifierKey(string key)
        {
            return key == "CTRL" || key == "CONTROL" || key == "SHIFT" || key == "ALT";
        }

        /// <summary>
        /// Types the specified text into the active document view if available, otherwise, simulates keyboard text entry.
        /// </summary>
        /// <param name="text">The text to be typed or simulated.</param>
        private static async Task TypeTextAsync(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync().ConfigureAwait(true);

            if (docView?.TextBuffer != null)
            {
                docView.TextBuffer.Insert(docView.TextView.Caret.Position.BufferPosition.Position, text);
            }
            else
            {
                inputSimulator.Keyboard.TextEntry(text);
            }
        }

        /// <summary>
        /// Sets the cursor position relative to the screen bounds if both x and y coordinates are provided.
        /// </summary>
        /// <param name="x">The x-coordinate relative to the screen bounds. If null, the cursor position is not changed.</param>
        /// <param name="y">The y-coordinate relative to the screen bounds. If null, the cursor position is not changed.</param>
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