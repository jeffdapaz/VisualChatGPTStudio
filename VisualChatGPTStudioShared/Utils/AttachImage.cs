using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
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
        /// Handles the PreviewKeyDown event for a text editor to detect and process Ctrl+V when an image is in the clipboard.
        /// Converts the clipboard image to a byte array and triggers the OnImagePaste event with the image data.
        /// </summary>
        public static void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.V ||
                (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control ||
                !System.Windows.Clipboard.ContainsImage())
            {
                return;
            }

            System.Windows.Media.Imaging.BitmapSource image = System.Windows.Clipboard.GetImage();

            if (image == null)
            {
                return;
            }

            byte[] imageBytes = ConvertBitmapSourceToByteArray(image);

            OnImagePaste?.Invoke(imageBytes, "image_attached.png");

            e.Handled = true;
        }

        /// <summary>
        /// Converts a BitmapSource object to a byte array by encoding it as a PNG image.
        /// </summary>
        /// <returns>
        /// A byte array representing the encoded PNG image of the provided BitmapSource.
        /// </returns>
        private static byte[] ConvertBitmapSourceToByteArray(System.Windows.Media.Imaging.BitmapSource bitmapSource)
        {
            using (MemoryStream stream = new())
            {
                System.Windows.Media.Imaging.PngBitmapEncoder encoder = new();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
                encoder.Save(stream);

                return stream.ToArray();
            }
        }
    }
}