using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.VisualStudio.Shell;
using UserControl = System.Windows.Controls.UserControl;namespace JeffPires.VisualChatGPTStudio.ToolWindows{
    /// <summary>
    /// Represents a user control for the Terminal Window Solution Context.
    /// </summary>
    public partial class TerminalWindowCodeReviewControl : UserControl    {
        #region Properties
        private OptionPageGridGeneral options;
        private Package package;

        #endregion Properties
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the TerminalWindowSolutionContextControl class.
        /// </summary>
        public TerminalWindowCodeReviewControl()        {            this.InitializeComponent();


        }

        #endregion Constructors
        #region Event Handlers



        #endregion Event Handlers
        #region Methods

        /// <summary>
        /// Starts the control with the given options and package.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="package">The package.</param>
        public void StartControl(OptionPageGridGeneral options, Package package)
        {
            this.options = options;
            this.package = package;
        }

        #endregion Methods     
    }}