using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualChatGPTStudioShared.Agents.ApiAgent;
using VisualChatGPTStudioShared.Utils.Repositories;
using UserControl = System.Windows.Controls.UserControl;

namespace JeffPires.VisualChatGPTStudio.Options.ApiAgent
{
    /// <summary>
    /// Window option to persist the parametrized APIs definitions.
    /// </summary>
    public partial class OptionApiAgentWindow : UserControl
    {
        #region Properties

        /// <summary>
        /// Gets or sets the collection of API items.
        /// </summary>
        public ObservableCollection<ApiItem> Apis { get; set; } = [];

        /// <summary>
        /// Represents a collection of API tag items, initialized as an empty observable collection.
        /// </summary>
        public ObservableCollection<ApiTagItem> Tags { get; set; } = [];

        private string apiIdOnEdit;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the OptionApiAgentWindow class, sets up the UI components, 
        /// initializes the database, retrieves API items, and binds data sources to UI elements.
        /// </summary>
        public OptionApiAgentWindow()
        {
            InitializeComponent();

            ApiAgentRepository.CreateDataBase();

            List<ApiItem> apis = ApiAgentRepository.GetAPIs();

            foreach (ApiItem api in apis)
            {
                Apis.Add(api);
            }

            grdApis.ItemsSource = Apis;
            grdTags.ItemsSource = Tags;

            cbTypeColumn.ItemsSource = Enum.GetValues(typeof(ApiTagType));
        }

        #endregion Constructors

        #region Event Handlers

        /// <summary>
        /// Handles the click event for the "Insert Tag" button, adding a new ApiTagItem with default values to the Tags collection.
        /// </summary>
        private void btnInsertTag_Click(object sender, RoutedEventArgs e)
        {
            Tags.Add(new ApiTagItem { Key = string.Empty, Value = string.Empty, Type = ApiTagType.Header });
        }

        /// <summary>
        /// Handles the click event for the delete button in the tags grid. Prompts the user for confirmation before removing the selected tag from the collection.
        /// </summary>
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

        /// <summary>
        /// Handles the click event for the Insert API button. Validates input fields, checks for duplicate API definitions, 
        /// and inserts or updates the API in the repository. Clears input fields and resets state after successful operation.
        /// </summary>
        private void btnInsertApi_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtIdentification.Text) || string.IsNullOrWhiteSpace(txtBaseUrl.Text))
            {
                MessageBox.Show("Please fill in both Identification and Base URL fields before adding an API.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (!IsValidUrl(txtBaseUrl.Text))
            {
                MessageBox.Show("The Base URL is not valid. Please enter a valid URL (e.g., https://example.com).", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(txtDefinition.Text))
            {
                MessageBox.Show("Please paste the API's definition.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (Apis.Any(a => a.Name.Equals(txtIdentification.Text.Trim(), System.StringComparison.InvariantCultureIgnoreCase)))
            {
                MessageBox.Show($"The API '{txtIdentification.Text}' definition already exists.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            ApiItem api = new()
            {
                Id = apiIdOnEdit,
                Name = txtIdentification.Text.Trim(),
                BaseUrl = txtBaseUrl.Text.Trim(),
                Tags = Tags.ToList(),
                Definition = txtDefinition.Text.Trim()
            };

            api.Id = ApiAgentRepository.InsertOrUpdate(api);

            Apis.Add(api);

            apiIdOnEdit = null;
            txtIdentification.Clear();
            txtBaseUrl.Clear();
            txtDefinition.Clear();
            Tags.Clear();
        }

        /// <summary>
        /// Handles the click event for the Edit API button, populates the form fields with the selected API's details, 
        /// and removes the selected API from the list for editing purposes.
        /// </summary>
        private void btnEditApi_Click(object sender, MouseButtonEventArgs e)
        {
            if (grdApis.SelectedItem is ApiItem selectedApi)
            {
                apiIdOnEdit = selectedApi.Id;
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

        /// <summary>
        /// Handles the click event for the delete API button. Prompts the user for confirmation before deleting the selected API from the list and repository.
        /// </summary>
        private void btnDeleteApi_Click(object sender, MouseButtonEventArgs e)
        {
            if (grdApis.SelectedItem is ApiItem selectedApi)
            {
                MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the API '{selectedApi.Name}'?", Utils.Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Apis.Remove(selectedApi);

                    ApiAgentRepository.DeleteApi(selectedApi.Id);
                }
            }
        }

        #endregion Event Handlers

        /// <summary>
        /// Validates whether the provided URL is a well-formed absolute URL with an HTTP or HTTPS scheme.
        /// </summary>
        /// <param name="url">The URL string to validate.</param>
        /// <returns>
        /// True if the URL is valid and uses the HTTP or HTTPS scheme; otherwise, false.
        /// </returns>
        private bool IsValidUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
            {
                return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
            }

            return false;
        }
    }
}