using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace JeffPires.VisualChatGPTStudio.Utils.API
{
    /// <summary>
    /// Provides functionality to attach images.
    /// </summary>
    public static class AttachImage
    {
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
    }
}