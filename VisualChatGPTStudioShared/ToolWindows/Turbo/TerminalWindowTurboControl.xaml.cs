using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using JeffPires.VisualChatGPTStudio.Utils.API;
using JeffPires.VisualChatGPTStudio.Utils.CodeCompletion;
using JeffPires.VisualChatGPTStudio.Utils.Repositories;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using VisualChatGPTStudioShared.ToolWindows.Turbo;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using JsonElement = System.Text.Json.JsonElement;
using MessageBox = System.Windows.MessageBox;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo;

/// <summary>
/// Interaction logic for TerminalWindowTurboControl.
/// </summary>
public partial class TerminalWindowTurboControl
{
    private bool _webView2Installed;
    private readonly TerminalTurboViewModel _viewModel;
    private IWebView2 _webView;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalWindowTurboControl"/> class.
    /// </summary>
    public TerminalWindowTurboControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        _viewModel = (TerminalTurboViewModel)DataContext;
    }

    /// <summary>
    /// Starts the control with the given options and package.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="package">The package.</param>
    public async void StartControl(OptionPageGridGeneral options, Package package)
    {
        _viewModel.Options = options;
        _viewModel.Package = package;

        if (!_webView2Installed)
        {
            _webView2Installed = await WebView2BootstrapperHelper.EnsureRuntimeAvailableAsync();

            if (!_webView2Installed)
            {
                return;
            }
        }

        txtRequest.MaxHeight = rowRequest.MaxHeight - 10;
        txtRequest.TextArea.TextEntering += txtRequest_TextEntering;
        txtRequest.TextArea.TextEntered += txtRequest_TextEntered;
        txtRequest.PreviewKeyDown += OnTxtRequestOnPreviewKeyDown;

        AttachImage.OnImagePaste += AttachImage_OnImagePaste;

        _viewModel.CompletionManager = new CompletionManager(package, txtRequest);

        VSColorTheme.ThemeChanged += _ =>
        {
            WebAsset.DeployTheme();
            SafeExecuteJs(WebFunctions.ReloadThemeCss(WebAsset.IsDarkTheme));
        };

        _viewModel.ScriptRequested += async script =>
        {
            try
            {
                if (_webView != null)
                    return await _webView.ExecuteScriptAsync(script);
            }
            catch (Exception exception)
            {
                Logger.Log($"WebView error: {exception.Message}");
            }

            return string.Empty;
        };
    }

    private void SafeExecuteJs(string script)
    {
        AsyncEventHandler.SafeFireAndForget(
            async () =>
            {
                if (_webView != null)
                    await _webView.ExecuteScriptAsync(script);
            },
            Logger.Log);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_webView?.CoreWebView2 != null)
            {
                _ = _webView.CoreWebView2.BrowserProcessId;
                return;
            }
        }
        catch (Exception)
        {
            WebViewHost.Content = null;
            _webView?.Dispose();
            _webView = null;
        }

        if (!_webView2Installed)
        {
            _webView2Installed = await WebView2BootstrapperHelper.EnsureRuntimeAvailableAsync();

            if (!_webView2Installed)
            {
                return;
            }
        }

#if COPILOT_ENABLED //VS2022
        _webView = new WebView2CompositionControl();
#else //VS2019
            _webView = new WebView2();
#endif
        _webView.CoreWebView2InitializationCompleted += CoreWebView2InitializationCompleted;
        _webView.NavigationCompleted += (_, _) =>
        {
            SafeExecuteJs(WebFunctions.ReloadThemeCss(WebAsset.IsDarkTheme));
            _viewModel.ForceDownloadChats();
            _viewModel.LoadChat();
        };

        WebViewHost.Content = _webView;

        var env = await CoreWebView2Environment.CreateAsync(null,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        await _webView.EnsureCoreWebView2Async(env);

        _webView.WebMessageReceived += WebViewOnWebMessageReceived;
    }

    private async void WebViewOnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs webMessage)
    {
        if (webMessage?.WebMessageAsJson == null)
            return;

        try
        {
            var msg = JsonSerializer.Deserialize<JsonElement>(webMessage.WebMessageAsJson);
            var action = msg.GetProperty("action").GetString()?.ToLower();
            var data = msg.GetProperty("data").GetString();
            if (data == null)
                return;

            switch (action)
            {
                case "png":
                    var pngBytes = Convert.FromBase64String(data);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(pngBytes);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    Clipboard.SetImage(bitmap);
                    break;
                case "copy":
                    Clipboard.SetText(data);
                    break;
                case "apply":
                    await TerminalWindowHelper.ApplyCodeToActiveDocumentAsync(data);
                    break;
            }
        }
        catch (Exception e)
        {
            Logger.Log($"WebView error: {e.Message}");
        }
    }

    private void CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess)
        {
            Logger.Log($"WebView error: {e.InitializationException}");
            return;
        }
        _webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
#if !DEBUG
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
#else
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
#endif
        _webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
        _webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
        _webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
        _webView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = true;
        _webView.CoreWebView2.Settings.IsScriptEnabled = true;
        _webView.CoreWebView2.Settings.IsZoomControlEnabled = true;
        _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        _webView.DefaultBackgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundBrushKey);
        _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
        _webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
        try
        {
            _webView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
        }
        catch (Exception ex)
        {
            Logger.Log($"WebView MemoryUsageTargetLevel error: {ex}");
        }

        UpdateBrowser();
    }

    private void OnTxtRequestOnPreviewKeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !_viewModel.CompletionManager.IsShowed)
        {
            if (_viewModel.Options.UseEnter)
            {
                switch (Keyboard.Modifiers)
                {
                    case ModifierKeys.Control:
                        // add new line
                        txtRequest.AppendText(Environment.NewLine);
                        e.Handled = true;
                        break;
                    case ModifierKeys.None:
                        // send Request by Enter
                        e.Handled = true;
                        _ = _viewModel.RequestAsync(RequestType.Request);
                        break;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                // send Request by Ctrl+Enter
                e.Handled = true;
                _ = _viewModel.RequestAsync(RequestType.Request);
            }
        }
        else
        {
            AttachImage.TextEditor_PreviewKeyDown(s, e);
        }
    }

    /// <summary>
    /// Updates the embedded web browser control with dynamically generated HTML content.
    /// </summary>
    private void UpdateBrowser()
    {
        if (!_webView2Installed)
        {
            return;
        }

        WebAsset.DeployTheme();
        _webView?.CoreWebView2.Navigate(WebAsset.GetTurboPath());
    }

    #region Event Handlers

    /// <summary>
    /// Handles the text entered event for the request text box,
    /// passing the entered text to the CompletionManager for processing.
    /// </summary>
    /// <param name="sender">The source of the event, typically the text box.</param>
    /// <param name="e">The event data containing the text that was entered.</param>
    private async void txtRequest_TextEntered(object sender, TextCompositionEventArgs e)
    {
        await _viewModel.CompletionManager.HandleTextEnteredAsync(e);
    }

    /// <summary>
    /// Handles the text entering event for the request text box, delegating the processing to the CompletionManager.
    /// </summary>
    /// <param name="sender">The source of the event, typically the text box.</param>
    /// <param name="e">The event data containing information about the text composition.</param>
    private void txtRequest_TextEntering(object sender, TextCompositionEventArgs e)
    {
        _viewModel.CompletionManager.HandleTextEntering(e);
    }

    /// <summary>
    /// Handles the click event of the btnComputerUse button.
    /// Captures a screenshot of the focused screen, sends a request asynchronously to the API with the captured image and user input.
    /// </summary>
    private void btnComputerUse_Click(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.ComputerUseAsync();
    }

    /// <summary>
    /// Handles the Click event of the btnRequestCode control.
    /// </summary>
    private void SendCode(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.RequestAsync(RequestType.Code);
    }

    /// <summary>
    /// Handles the Click event of the btnRequestSend control.
    /// </summary>
    private void SendRequest(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.RequestAsync(RequestType.Request);
    }

    /// <summary>
    /// Cancels the request.
    /// </summary>
    private void CancelRequest(object sender, RoutedEventArgs e)
    {
        _viewModel.IsReadyToRequest = false;
        _viewModel.CancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// Handles the click event of the button to attach an image.
    /// Opens a file dialog to select an image file, validates the file extension,
    /// and reads the selected image file into a byte array if valid.
    /// </summary>
    private void btnAttachImage_Click(object sender, RoutedEventArgs e)
    {
        if (AttachImage.ShowDialog(out _viewModel.AttachedImage, out var imageName))
        {
            _viewModel.AttachedImageText = imageName;
        }
    }

    /// <summary>
    /// Handles the click event for the delete image button.
    /// Hides the image display and the attach image button,
    /// and clears the attached image reference.
    /// </summary>
    private void btnDeleteImage_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AttachedImage = null;
        _viewModel.AttachedImageText = string.Empty;
    }

    /// <summary>
    /// Handles the event when an image is pasted, attaching the image and updating the UI with the file name.
    /// </summary>
    /// <param name="attachedImage">The byte array representing the pasted image.</param>
    /// <param name="fileName">The name of the pasted image file.</param>
    private void AttachImage_OnImagePaste(byte[] attachedImage, string fileName)
    {
        _viewModel.AttachedImage = attachedImage;
        _viewModel.AttachedImageText = fileName;
    }

    #endregion Event Handlers

    #region Toolbar
    private void NewChat_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CreateNewChat(clearTools: true);
    }

    private void DeleteChat_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DeleteChat(_viewModel.ChatId);
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

    private void LoadChat(string chatId)
    {
        try
        {
            CancelRequest(null, null);
            SafeExecuteJs(WebFunctions.ClearChat);
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
        var lbi = sender as ListBoxItem;
        var chat = lbi?.DataContext as ChatEntity;
        if (chat is { IsEditing: true })
        {
            switch (e.Key)
            {
                // Apply to rename chat
                case Key.Enter:
                    if (chat.Name == null || chat.EditName == null)
                    {
                        return;
                    }
                    if (!string.Equals(chat.Name, chat.EditName, StringComparison.Ordinal))
                    {
                        chat.Name = chat.EditName;
                        ChatRepository.UpdateChatName(chat.Id, chat.Name);
                    }
                    chat.IsEditing = false;
                    e.Handled = true;
                    break;
                // Cancel rename chat
                case Key.Escape:
                    chat.IsEditing = false;
                    e.Handled = true;
                    break;
            }
            return;
        }

        // No editing name
        switch (e.Key)
        {
            case Key.Left when _viewModel.CanGoPrev:
                _viewModel.PrevCmd.Execute(null);
                e.Handled = true;
                break;
            case Key.Right when _viewModel.CanGoNext:
                _viewModel.NextCmd.Execute(null);
                e.Handled = true;
                break;
            case Key.Space or Key.Enter when chat != null:
                lbi.IsSelected = true;
                _viewModel.LoadChat(chat.Id);
                LoadChat(chat.Id);
                ToggleHistory(false);
                e.Handled = true;
                break;
            case Key.R when chat != null:
                _viewModel.StartRenameCmd.Execute(null);
                e.Handled = true;
                break;
            case Key.Delete when chat != null:
                _viewModel.DeleteCmd.Execute(chat);
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

    #endregion
}
