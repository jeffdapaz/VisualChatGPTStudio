using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualChatGPTStudioShared.Agents.ApiAgent;
using UserControl = System.Windows.Controls.UserControl;

namespace JeffPires.VisualChatGPTStudio.Options.ApiAgent
{
    /// <summary>
    /// Represents a user control for displaying and interacting with option commands.
    /// </summary>
    public partial class OptionApiAgentWindow : UserControl
    {
        #region Properties

        public ObservableCollection<ApiTagItem> Tags { get; set; } = [];
        public ObservableCollection<ApiItem> Apis { get; set; } = [];

        #endregion Properties

        #region Constructors

        public OptionApiAgentWindow()
        {
            InitializeComponent();

            grdTags.ItemsSource = Tags;
            lstApis.ItemsSource = Apis;
        }

        #endregion Constructors

        #region Event Handlers

        private void btnInsertTag_Click(object sender, RoutedEventArgs e)
        {
            Tags.Add(new ApiTagItem { Key = string.Empty, Value = string.Empty, Type = ApiTagType.Header });
        }

        private void btnGrdTagsDelete_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image deleteButton)
            {
                if (deleteButton.DataContext is ApiTagItem tagToDelete)
                {
                    MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the tag with key '{tagToDelete.Key}'?", Utils.Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        Tags.Remove(tagToDelete);
                    }
                }
            }
        }

        private void btnInsertApi_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtIdentification.Text) || string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                MessageBox.Show("Please fill in both Identification and Base URL fields before adding an API.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(txtDefinition.Text))
            {
                MessageBox.Show("Please paste the API's definition.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            Apis.Add(new ApiItem
            {
                Name = txtIdentification.Text,
                BaseUrl = txtBaseUrl.Text,
                Tags = Tags.ToList(),
                Definition = txtDefinition.Text
            });

            txtIdentification.Clear();
            txtBaseUrl.Clear();
            txtDefinition.Clear();
            Tags.Clear();
        }

        private void btnEditApi_Click(object sender, MouseButtonEventArgs e)
        {
            if (lstApis.SelectedItem is ApiItem selectedApi)
            {
                txtIdentification.Text = selectedApi.Name;
                txtBaseUrl.Text = selectedApi.BaseUrl;
                txtDefinition.Text = selectedApi.Definition;
                Tags.Clear();

                foreach (ApiTagItem tag in selectedApi.Tags)
                {
                    Tags.Add(tag);
                }

                Apis.Remove(selectedApi);
            }
        }

        private void btnDeleteApi_Click(object sender, MouseButtonEventArgs e)
        {
            if (lstApis.SelectedItem is ApiItem selectedApi)
            {
                MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the API '{selectedApi.Name}'?", Utils.Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Apis.Remove(selectedApi);
                }
            }
        }        

        #endregion Event Handlers
    }    
}