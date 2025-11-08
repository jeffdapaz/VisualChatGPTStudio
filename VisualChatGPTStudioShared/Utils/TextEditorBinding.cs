using System.Windows;

namespace JeffPires.VisualChatGPTStudio.Utils;

public static class TextEditorBinding
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.RegisterAttached(
            "Text", typeof(string), typeof(TextEditorBinding),
            new FrameworkPropertyMetadata(string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextChanged));

    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ICSharpCode.AvalonEdit.TextEditor editor)
        {
            if (editor.Document == null)
                editor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
            if (editor.Document.Text != (string)e.NewValue)
                editor.Document.Text = (string)e.NewValue;
        }
    }
}

