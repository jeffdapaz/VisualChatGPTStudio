using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace JeffPires.VisualChatGPTStudio.Utils.API
{
    /// <summary>
    /// Provides functionality to attach images.
    /// </summary>
    public static class AttachImage
    {
        public delegate void OnImagePasteHandler(byte[] attachedImage, string fileName);
        public static event OnImagePasteHandler OnImagePaste;

        /// <summary>
        /// Displays an open file dialog to select an image file and reads the selected file into a byte array.
        /// </summary>
        /// <param name="attachedImage">An output parameter that will contain the byte array of the selected image.</param>
        /// <param name="fileName">An output parameter that will contain the name of the selected file.</param>
        /// <returns>Returns true if an image file is successfully selected and read; otherwise, false.</returns>
        public static bool ShowDialog(out byte[] attachedImage, out string fileName)
        {
            attachedImage = null;
            fileName = null;

            HashSet<string> validExtensions = [".jpeg", ".png", ".jpg", ".webp"];

            OpenFileDialog openFileDialog = new()
            {
                Multiselect = false,
                Filter = "All Files (*.*)|*.*|JPEG (*.jpeg)|*.jpeg|JPG (*.jpg)|*.jpg|PNG (*.png)|*.png|WEBP (*.webp)|*.webp",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return false;
            }

            string fileExtension = Path.GetExtension(openFileDialog.FileName).ToLowerInvariant();

            if (!validExtensions.Contains(fileExtension))
            {
                System.Windows.MessageBox.Show("You can only select image files.", Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            attachedImage = File.ReadAllBytes(openFileDialog.FileName);

            fileName = Path.GetFileName(openFileDialog.FileName);

            return true;
        }

        /// <summary>
        /// Handles the PreviewKeyDown event for a TextEditor control to customize behavior for Tab key navigation and image pasting.
        /// When Tab is pressed without Shift, moves focus to the next control instead of inserting a tab character.
        /// When Ctrl+V is pressed and the clipboard contains an image, converts the image to a byte array and triggers the OnImagePaste event.
        /// Marks the event as handled to prevent default processing in these cases.
        /// </summary>
        public static void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check if the pressed key is Tab and neither LeftShift nor RightShift is pressed
            if (e.Key == Key.Tab)
            {
                // Mark the event as handled to prevent default tab behavior
                e.Handled = true;

                TraversalRequest request;

                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    // Create a request to move focus to the previous control
                    request = new(FocusNavigationDirection.Previous);
                }
                else
                {
                    // Create a request to move focus to the next control
                    request = new(FocusNavigationDirection.Next);
                }

                // Move focus to the next UI element
                (sender as UIElement)?.MoveFocus(request);

                // Exit the method after handling tab key
                return;
            }

            // Check if the key is not 'V' or Ctrl is not pressed or clipboard does not contain an image
            if (e.Key != Key.V || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control || !System.Windows.Clipboard.ContainsImage())
            {
                // If any condition is true, do nothing and return
                return;
            }

            // Get the image from the clipboard
            System.Windows.Media.Imaging.BitmapSource image = System.Windows.Clipboard.GetImage();

            // If no image is found, return
            if (image == null)
            {
                return;
            }

            // Convert the BitmapSource image to a byte array
            byte[] imageBytes = ConvertBitmapSourceToByteArray(image);

            // Invoke the OnImagePaste event with the image bytes and a default file name
            OnImagePaste?.Invoke(imageBytes, "image_attached.png");

            // Mark the event as handled to prevent further processing
            e.Handled = true;
        }

        public static string GetImageMimeType(byte[] imageBytes)
        {
            if (imageBytes.Length < 4) return "image/jpeg"; // fallback

            // PNG
            if (imageBytes[0] == 0x89 && imageBytes[1] == 0x50 && imageBytes[2] == 0x4E && imageBytes[3] == 0x47)
                return "image/png";

            // JPEG
            if (imageBytes[0] == 0xFF && imageBytes[1] == 0xD8 && imageBytes[2] == 0xFF)
                return "image/jpeg";

            // GIF
            if (imageBytes[0] == 0x47 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46)
                return "image/gif";

            // WEBP
            if (imageBytes[0] == 0x52 && imageBytes[1] == 0x49 && imageBytes[2] == 0x46 && imageBytes[3] == 0x46)
                return "image/webp";

            return "image/jpeg"; // fallback
        }

        /// <summary>
        /// Converts a BitmapSource object to a byte array by encoding it as a PNG image.
        /// </summary>
        /// <returns>
        /// A byte array representing the encoded PNG image of the provided BitmapSource.
        /// </returns>
        private static byte[] ConvertBitmapSourceToByteArray(System.Windows.Media.Imaging.BitmapSource bitmapSource)
        {
            var convertedBitmap = new FormatConvertedBitmap();
            convertedBitmap.BeginInit();
            convertedBitmap.Source = bitmapSource;
            convertedBitmap.DestinationFormat = System.Windows.Media.PixelFormats.Bgr32; // Remove alpha-cannel
            convertedBitmap.EndInit();

            using (MemoryStream stream = new())
            {
                System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(convertedBitmap));
                encoder.Save(stream);

                return stream.ToArray();
            }
        }
    }
}
