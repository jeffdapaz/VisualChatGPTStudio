using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using VisualChatGPTStudioShared.Agents.McpAgent;
using VisualChatGPTStudioShared.Utils.Repositories;
using UserControl = System.Windows.Controls.UserControl;

namespace JeffPires.VisualChatGPTStudio.Options.McpAgent
{
    /// <summary>
    /// Window option to persist the parametrized MCP server definitions.
    /// </summary>
    public partial class OptionMcpAgentWindow : UserControl
    {
        #region Constants

        #endregion Constants

        #region Properties

        /// <summary>
        /// Gets or sets the MCP servers collection.
        /// </summary>
        public ObservableCollection<McpServerItem> McpServers { get; set; } = [];

        private string mcpServerIdOnEdit;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionMcpAgentWindow"/> class.
        /// </summary>
        public OptionMcpAgentWindow()
        {
            InitializeComponent();

            McpAgentRepository.CreateDataBase();

            List<McpServerItem> servers = McpAgentRepository.GetMcpServers();

            foreach (McpServerItem server in servers.OrderBy(s => s.Name))
            {
                McpServers.Add(server);
            }

            grdMcpServers.ItemsSource = McpServers;
            cbTransport.ItemsSource = Enum.GetValues(typeof(McpTransportType));
            cbTransport.SelectedItem = McpTransportType.Stdio;

            UpdateFieldsByTransport();
        }

        #endregion Constructors

        #region Public Methods

        #endregion Public Methods

        #region Private Methods

        /// <summary>
        /// Handles transport selection changes and updates visible fields.
        /// </summary>
        private void cbTransport_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFieldsByTransport();
        }

        /// <summary>
        /// Handles MCP server insert/update action.
        /// </summary>
        private void btnInsertMcp_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
            {
                return;
            }

            string name = txtIdentification.Text.Trim();

            if (McpServers.Any(s => s.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) && s.Id != mcpServerIdOnEdit))
            {
                MessageBox.Show($"The MCP server '{name}' already exists.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            McpTransportType transportType = (McpTransportType)cbTransport.SelectedItem;

            McpServerItem server = new()
            {
                Id = mcpServerIdOnEdit,
                Name = name,
                TransportType = transportType,
                Command = txtCommand.Text.Trim(),
                Arguments = txtArguments.Text.Trim(),
                WorkingDirectory = txtWorkingDirectory.Text.Trim(),
                Endpoint = txtEndpoint.Text.Trim(),
                EnvironmentVariablesJson = txtEnvironmentVariables.Text.Trim(),
                Enabled = chkEnabled.IsChecked.GetValueOrDefault(true)
            };

            server.Id = McpAgentRepository.InsertOrUpdate(server);

            McpServerItem oldItem = McpServers.FirstOrDefault(s => s.Id == server.Id);

            if (oldItem != null)
            {
                McpServers.Remove(oldItem);
            }

            McpServers.Add(server);

            SortMcpServers();
            ResetForm();
        }

        /// <summary>
        /// Handles MCP server edit action from list.
        /// </summary>
        private void btnEditMcp_Click(object sender, MouseButtonEventArgs e)
        {
            if (grdMcpServers.SelectedItem is not McpServerItem selectedServer)
            {
                return;
            }

            mcpServerIdOnEdit = selectedServer.Id;
            txtIdentification.Text = selectedServer.Name;
            cbTransport.SelectedItem = selectedServer.TransportType;
            txtCommand.Text = selectedServer.Command;
            txtArguments.Text = selectedServer.Arguments;
            txtWorkingDirectory.Text = selectedServer.WorkingDirectory;
            txtEndpoint.Text = selectedServer.Endpoint;
            txtEnvironmentVariables.Text = selectedServer.EnvironmentVariablesJson;
            chkEnabled.IsChecked = selectedServer.Enabled;

            UpdateFieldsByTransport();
        }

        /// <summary>
        /// Handles MCP server delete action from list.
        /// </summary>
        private void btnDeleteMcp_Click(object sender, MouseButtonEventArgs e)
        {
            if (grdMcpServers.SelectedItem is not McpServerItem selectedServer)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete the MCP server '{selectedServer.Name}'?", Utils.Constants.EXTENSION_NAME, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            McpServers.Remove(selectedServer);
            McpAgentRepository.DeleteMcpServer(selectedServer.Id);

            if (mcpServerIdOnEdit == selectedServer.Id)
            {
                ResetForm();
            }
        }

        /// <summary>
        /// Validates fields according to selected transport type.
        /// </summary>
        /// <returns>True when fields are valid; otherwise false.</returns>
        private bool ValidateFields()
        {
            if (string.IsNullOrWhiteSpace(txtIdentification.Text))
            {
                MessageBox.Show("Please fill in Identification field.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            McpTransportType transportType = (McpTransportType)cbTransport.SelectedItem;

            if (transportType == McpTransportType.Stdio && string.IsNullOrWhiteSpace(txtCommand.Text))
            {
                MessageBox.Show("For stdio transport, command is required.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (transportType == McpTransportType.Sse && !IsValidUrl(txtEndpoint.Text))
            {
                MessageBox.Show("For sse transport, endpoint must be a valid HTTP/HTTPS URL.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtEnvironmentVariables.Text))
            {
                try
                {
                    _ = JObject.Parse(txtEnvironmentVariables.Text);
                }
                catch
                {
                    MessageBox.Show("Environment Variables must be a valid JSON object.", Utils.Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Updates fields visibility and enabled state according to selected transport.
        /// </summary>
        private void UpdateFieldsByTransport()
        {
            if (cbTransport.SelectedItem is not McpTransportType transportType)
            {
                return;
            }

            bool isStdio = transportType == McpTransportType.Stdio;
            bool isSse = transportType == McpTransportType.Sse;

            lblCommand.IsEnabled = isStdio;
            txtCommand.IsEnabled = isStdio;
            lblArguments.IsEnabled = isStdio;
            txtArguments.IsEnabled = isStdio;
            lblWorkingDirectory.IsEnabled = isStdio;
            txtWorkingDirectory.IsEnabled = isStdio;

            lblEndpoint.IsEnabled = isSse;
            txtEndpoint.IsEnabled = isSse;
        }

        /// <summary>
        /// Clears the form state after insert/update operations.
        /// </summary>
        private void ResetForm()
        {
            mcpServerIdOnEdit = null;
            txtIdentification.Clear();
            cbTransport.SelectedItem = McpTransportType.Stdio;
            txtCommand.Clear();
            txtArguments.Clear();
            txtWorkingDirectory.Clear();
            txtEndpoint.Clear();
            txtEnvironmentVariables.Clear();
            chkEnabled.IsChecked = true;

            UpdateFieldsByTransport();
        }

        /// <summary>
        /// Sorts MCP servers by name after changes.
        /// </summary>
        private void SortMcpServers()
        {
            List<McpServerItem> ordered = McpServers.OrderBy(s => s.Name).ToList();

            McpServers.Clear();

            foreach (McpServerItem server in ordered)
            {
                McpServers.Add(server);
            }
        }

        /// <summary>
        /// Validates whether a URL is a valid absolute HTTP or HTTPS URL.
        /// </summary>
        /// <param name="url">The URL text.</param>
        /// <returns>True when valid; otherwise false.</returns>
        private static bool IsValidUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
            {
                return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
            }

            return false;
        }

        #endregion Private Methods
    }
}
