using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualChatGPTStudioShared.ToolWindows.Turbo;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using MessageBox = System.Windows.MessageBox;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    public partial class TerminalWindowTurboControl
    {
        private const string ClearChatScript = "clearChat()";

        private void NewChat_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CreateNewChat();
            apiChat.ClearConversation();
            _ = _webView?.ExecuteScriptAsync(ClearChatScript);
        }

        private void DeleteChat_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteChat(_viewModel.ChatId);
            apiChat.ClearConversation();
            _ = _webView?.ExecuteScriptAsync(ClearChatScript);
        }

        private void ToggleHistory_Click(object sender, RoutedEventArgs e)
        {
            if (HistorySidebar.Visibility == Visibility.Visible)
            {
                CloseHistory();
            }
            else
            {
                OpenHistory();
            }
        }

        private void ToggleWebViewVisibility()
        {
#if !COPILOT_ENABLED // VS 2019
            WebViewHost.Visibility = Overlay.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
#endif
        }

        private void OpenHistory()
        {
            _viewModel.ForceReloadChats();
            HistorySidebar.Visibility = Overlay.Visibility = Visibility.Visible;
            ToggleWebViewVisibility();
        }

        private void CloseHistory()
        {
            HistorySidebar.Visibility = Overlay.Visibility = Visibility.Collapsed;
            ToggleWebViewVisibility();
        }

        private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel != null && sender is ListBox listBox)
            {
                if (listBox.SelectedItem is ChatEntity selectedItem)
                {
                    try
                    {
                        CloseHistory();
                        _viewModel.LoadChat(selectedItem.Id);
                        apiChat.ClearConversation();
                        _ = _webView?.ExecuteScriptAsync(ClearChatScript);
                        AddMessagesFromModel();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            }
        }

        private void CloseHistoryButton_Click(object sender, RoutedEventArgs e) => CloseHistory();

        private void ToggleSettings_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsSidebar.Visibility == Visibility.Visible)
            {
                CloseSettings();
            }
            else
            {
                OpenSettings();
            }
        }

        private void OpenSettings()
        {
            SettingsSidebar.Visibility = Overlay.Visibility = Visibility.Visible;
            ToggleWebViewVisibility();
        }

        private void CloseSettings()
        {
            SettingsSidebar.Visibility = Overlay.Visibility = Visibility.Collapsed;
            ToggleWebViewVisibility();
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e) => CloseSettings();

        private void Box_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
                return;
            var tb = (TextBox)sender;
            tb.Focus();
            tb.SelectAll();
        }
    }
}
