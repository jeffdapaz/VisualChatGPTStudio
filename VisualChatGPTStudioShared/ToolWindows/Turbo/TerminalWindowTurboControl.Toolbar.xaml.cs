using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenAI_API.Chat;
using VisualChatGPTStudioShared.ToolWindows.Turbo;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using MessageBox = System.Windows.MessageBox;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    public partial class TerminalWindowTurboControl
    {
        private void NewChat_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CreateNewChat();
            ClearChat();
        }

        private void DeleteChat_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteChat(_viewModel.ChatId);
            ClearChat();
        }

        private void ClearChat()
        {
            _viewModel.apiChat.ClearConversation();
            _ = _webView?.ExecuteScriptAsync(WebFunctions.ClearChat);
            ToggleApi.IsChecked = ToggleSql.IsChecked = false;

            _viewModel.ApiDefinitions = [];
            _viewModel.SqlServerConnections = [];
            _viewModel.AttachedImage = null;
            EnableDisableButtons(true);
        }

        private void ToggleHistory_Click(object sender, RoutedEventArgs e)
            => ToggleHistory(HistorySidebar.Visibility != Visibility.Visible);

        private void ToggleWebViewVisibility()
        {
#if !COPILOT_ENABLED // VS 2019
            WebViewHost.Visibility = Overlay.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
#endif
        }

        private void ToggleHistory(bool showHistory)
        {
            if (showHistory)
            {
                _viewModel.ForceDownloadChats();
                HistorySidebar.Visibility = Overlay.Visibility = Visibility.Visible;
                ToggleWebViewVisibility();
                HistorySearch.Focus();
            }
            else
            {
                HistorySidebar.Visibility = Overlay.Visibility = Visibility.Collapsed;
                ToggleWebViewVisibility();
                txtRequest.Focus();
            }
        }

        private void HistoryList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
            => ToggleHistory(false);

        private void HistoryPagingButton_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue && ((Button)sender).IsFocused)
            {
                HistorySearch.Focus();
            }
        }

        private async void LoadChat(string chatId)
        {
            try
            {
                CancelRequest(null, null);
                await _webView!.ExecuteScriptAsync(WebFunctions.ClearChat);
                _viewModel.LoadChat(chatId);
                grdCommands.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                MessageBox.Show(ex.Message, Constants.EXTENSION_NAME, MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel != null && sender is ListBox { SelectedItem: ChatEntity selectedItem } && Mouse.LeftButton != MouseButtonState.Released)
            {
                _viewModel.LoadChat(selectedItem.Id);
                LoadChat(selectedItem.Id);
            }
        }

        private void HistoryList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up && sender is ListBox { SelectedIndex: 0 } listBox)
            {
                listBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
                e.Handled = true;
            }
        }

        private void HistoryListItem_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space or Key.Enter when sender is ListBoxItem { DataContext: ChatEntity selectedItem } lbi:
                    lbi.IsSelected = true;
                    e.Handled = true;
                    _viewModel.LoadChat(selectedItem.Id);
                    LoadChat(selectedItem.Id);
                    ToggleHistory(false);
                    break;
                case Key.Left when _viewModel.CanGoPrev:
                    _viewModel.PrevCmd.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Right when _viewModel.CanGoNext:
                    _viewModel.NextCmd.Execute(null);
                    e.Handled = true;
                    break;
                case Key.R when sender is ListBoxItem { DataContext: ChatEntity selectedItem }:
                    _viewModel.StartRenameCmd.Execute(selectedItem);
                    e.Handled = true;
                    break;
                case Key.Delete when sender is ListBoxItem { DataContext: ChatEntity selectedItem }:
                    _viewModel.DeleteCmd.Execute(selectedItem);
                    e.Handled = true;
                    break;
            }
        }

        private void HistorySearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && sender is TextBox textBox)
            {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                e.Handled = true;
            }
        }

        private void CloseHistoryButton_Click(object sender, RoutedEventArgs e)
            => ToggleHistory(false);

        private void ToggleSettings_Click(object sender, RoutedEventArgs e)
            => ToggleSettings(SettingsSidebar.Visibility != Visibility.Visible);

        private void ToggleSettings(bool showSettings)
        {
            SettingsSidebar.Visibility = showSettings
                ? Overlay.Visibility = Visibility.Visible
                : Overlay.Visibility = Visibility.Collapsed;

            ToggleWebViewVisibility();
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
            => ToggleSettings(false);

        private void Box_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
                return;
            var tb = (TextBox)sender;
            tb.Focus();
            tb.SelectAll();
        }

        private void Overlay_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            ToggleHistory(false);
            ToggleSettings(false);
        }
    }
}
