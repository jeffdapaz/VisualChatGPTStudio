using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JeffPires.VisualChatGPTStudio.Utils.CodeCompletion
{
    /// <summary>
    /// Represents the data for a completion item in an auto-completion system.
    /// Implements the ICompletionData interface to provide necessary properties and methods
    /// for displaying and interacting with completion suggestions.
    /// </summary>
    public class CompletionData : ICompletionData
    {
        /// <summary>
        /// Gets the text value.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Gets the description of the object.
        public object Description { get; private set; }

        /// <summary>
        /// Gets the image source associated with this instance.
        /// </summary>
        public ImageSource Image { get; private set; }

        /// <summary>
        /// Use this property if you want to show a fancy UIElement in the list.
        /// </summary>
        /// <returns>The text associated with the UIElement.</returns>
        public object Content { get { return this.Text; } }

        /// <summary>
        /// Gets the priority value, which is always set to 0.
        /// </summary>
        public double Priority => 0;

        /// <summary>
        /// To store auxiliar data
        /// </summary>
        public string Aux { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletionData"/> class.
        /// </summary>
        /// <param name="text">The text associated with the completion data.</param>
        /// <param name="description">A description providing additional information about the completion data.</param>
        /// <param name="image">An image source representing an icon or visual associated with the completion data.</param>
        /// <param name="aux">Auxiliar data if necssary.</param>
        public CompletionData(string text, string description, ImageSource image, string aux)
        {
            this.Text = text;
            this.Description = description;
            this.Image = image;
            this.Aux = aux;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletionData"/> class with the specified text, description, and image.
        /// </summary>
        /// <param name="text">The text associated with the completion data.</param>
        /// <param name="description">The description associated with the completion data.</param>
        /// <param name="image">The URI of the image to be associated with the completion data.</param>
        public CompletionData(string text, string description, string image)
        {
            this.Text = text;
            this.Description = description;

            BitmapImage imageSource = new();

            imageSource.BeginInit();
            imageSource.UriSource = new Uri(image);
            imageSource.EndInit();

            this.Image = imageSource;
        }

        /// <summary>
        /// Completes the text in the specified text area by replacing the content 
        /// within the given completion segment with the provided text.
        /// </summary>
        /// <param name="textArea">The text area where the completion will occur.</param>
        /// <param name="completionSegment">The segment of the document to be replaced.</param>
        /// <param name="insertionRequestEventArgs">Event arguments related to the insertion request.</param>
        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment, this.Text + " ");
        }
    }
}