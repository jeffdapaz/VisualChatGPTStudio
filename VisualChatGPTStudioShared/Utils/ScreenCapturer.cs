using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

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
        /// Captures a screenshot of the currently active (foreground) window (supposed to be the Visual Studio) and returns it as a PNG byte array.
        /// Also outputs the width and height of the captured window.
        /// </summary>
        /// <param name="displayWidth">The width of the captured window in pixels.</param>
        /// <param name="displayHeight">The height of the captured window in pixels.</param>
        /// <returns>
        /// A byte array containing the PNG-encoded screenshot of the active window.
        /// </returns>
        public static byte[] CaptureActiveWindowScreenshot(out int displayWidth, out int displayHeight)
        {
            IntPtr hWnd = GetForegroundWindow();

            if (hWnd == IntPtr.Zero)
            {
                throw new InvalidOperationException("It was not possible to get the active window.");
            }

            if (!GetWindowRect(hWnd, out RECT rect))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failure in GetWindowRect");
            }

            displayWidth = rect.Right - rect.Left;
            displayHeight = rect.Bottom - rect.Top;

            using Bitmap bmp = new(displayWidth, displayHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(displayWidth, displayHeight), CopyPixelOperation.SourceCopy);
            }

            using MemoryStream ms = new();

            bmp.Save(ms, ImageFormat.Png);

            return ms.ToArray();
        }
    }
}