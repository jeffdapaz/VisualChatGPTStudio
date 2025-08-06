using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Provides methods for capturing screenshots of the Visual Studio.
    /// </summary>
    public static class ScreenCapturer
    {
        /// <summary>
        /// Imports the user32.dll function to retrieve a handle to the window that is currently in the foreground (has focus).
        /// </summary>
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window, identified by its handle.
        /// The dimensions are given in screen coordinates, and the function returns true if successful.
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]

        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Represents a rectangle defined by the coordinates of its upper-left and lower-right corners.
        /// </summary>
        /// <remarks>
        /// Used for interoperability with native Windows APIs that require a RECT structure.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Captures a screenshot of the entire screen that contains the currently focused (Visual Studio) window.
        /// </summary>
        /// <param name="screenBounds">Outputs the bounds of the screen where the focused window is located.</param>
        /// <returns>A byte array containing the screenshot image in PNG format.</returns>
        public static byte[] CaptureFocusedScreenScreenshot(out Rectangle screenBounds)
        {
            // 1. Get the handle of the foreground (focused) window
            IntPtr hWnd = GetForegroundWindow();

            if (hWnd == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to get the Visual Studio window.");
            }

            // 2. Get the window's position and size
            if (!GetWindowRect(hWnd, out RECT rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "GetWindowRect failed.");
            }

            // 3. Convert RECT to .NET Rectangle
            Rectangle windowRect = new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

            // 4. Determine which screen contains most of the window
            Screen targetScreen = Screen.FromRectangle(windowRect);

            screenBounds = targetScreen.Bounds;

            // 5. Capture the entire screen as a bitmap
            using Bitmap bmp = new(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size, CopyPixelOperation.SourceCopy);
            }

            // 6. Save the image to a memory stream as PNG and return the byte array
            using MemoryStream ms = new();

            bmp.Save(ms, ImageFormat.Png);

            return ms.ToArray();
        }
    }
}