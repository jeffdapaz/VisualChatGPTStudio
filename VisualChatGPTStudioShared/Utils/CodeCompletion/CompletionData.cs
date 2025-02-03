using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
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
        /// Gets the type of the completion item.
        /// </summary>
        public CompletionItemType CompletionItemType { get; private set; }

        /// <summary>
        /// Gets the file path as a string.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        public string ClassName { get; private set; }

        /// <summary>
        /// Gets the name of the method.
        /// </summary>
        public string MethodName { get; private set; }

        /// <summary>
        /// Gets the list of method parameter types as strings.
        /// </summary>
        public List<string> MethodParameterTypes { get; private set; }

        /// <summary>
        /// Gets or sets the method signature.
        /// </summary>
        public string MethodSignature { get; set; }

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
        /// Initializes a new instance of the <see cref="CompletionData"/> class.
        /// </summary>
        /// <param name="text">The text of the completion item.</param>
        /// <param name="description">A description of the completion item.</param>
        /// <param name="image">An image associated with the completion item.</param>
        /// <param name="completionItemType">The type of the completion item.</param>
        /// <param name="filePath">The file path where the completion item is located.</param>
        /// <param name="className">The name of the class associated with the completion item.</param>
        /// <returns>
        /// A new instance of the <see cref="CompletionData"/> class.
        /// </returns>
        public CompletionData(string text,
                              string description,
                              ImageSource image,
                              CompletionItemType completionItemType,
                              string filePath,
                              string className)
        {
            this.Text = text;
            this.Description = description;
            this.Image = image;
            this.CompletionItemType = completionItemType;
            this.FilePath = filePath;
            this.ClassName = className;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletionData"/> class.
        /// </summary>
        /// <param name="text">The text of the completion item.</param>
        /// <param name="description">A description of the completion item.</param>
        /// <param name="image">An image associated with the completion item.</param>
        /// <param name="completionItemType">The type of the completion item.</param>
        /// <param name="filePath">The file path where the completion item is located.</param>
        /// <param name="className">The name of the class associated with the completion item.</param>
        /// <param name="methodName">The name of the method associated with the completion item.</param>
        /// <param name="methodParameterTypes">A list of parameter types for the method.</param>
        /// <param name="methodSignature">The method signature.</param>
        /// <returns>
        /// A new instance of the <see cref="CompletionData"/> class.
        /// </returns>
        public CompletionData(string text,
                              string description,
                              ImageSource image,
                              CompletionItemType completionItemType,
                              string filePath,
                              string className,
                              string methodName,
                              List<string> methodParameterTypes,
                              string methodSignature)
        {
            this.Text = text;
            this.Description = description;
            this.Image = image;
            this.CompletionItemType = completionItemType;
            this.FilePath = filePath;
            this.ClassName = className;
            this.MethodName = methodName;
            this.MethodParameterTypes = methodParameterTypes;
            this.MethodSignature = methodSignature;
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
            string text = this.Text + " ";

            if (this.CompletionItemType == CompletionItemType.CSharpMethod)
            {
                text = this.Text + this.MethodSignature + " ";
            }

            textArea.Document.Replace(completionSegment, text);
        }
    }

    /// <summary>
    /// Represents the different types of completion items available.
    /// </summary>
    public enum CompletionItemType
    {
        File = 0,
        CSharpClass = 1,
        CSharpMethod = 2
    }
}