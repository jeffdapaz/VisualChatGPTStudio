using System.Windows;using System.Windows.Controls;using System.Windows.Input;namespace VisualChatGPTStudioShared.Utils{
    /// <summary>
    /// Represents an image control that can be used as a command button.
    /// </summary>
    public class CommandImage : Image    {        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command", typeof(ICommand), typeof(CommandImage), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command associated with this control.
        /// </summary>
        public ICommand Command        {            get { return (ICommand)GetValue(CommandProperty); }            set { SetValue(CommandProperty, value); }        }

        /// <summary>
        /// Overrides the OnMouseLeftButtonDown method to execute the Command if it is not null and can be executed.
        /// </summary>
        /// <param name="e">The MouseButtonEventArgs parameter.</param>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)        {            base.OnMouseLeftButtonDown(e);            if (Command != null && Command.CanExecute(null))            {                Command.Execute(null);            }        }    }}