using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils.Repositories;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VisualChatGPTStudioShared.ToolWindows.Turbo;

namespace JeffPires.VisualChatGPTStudio.ToolWindows.Turbo
{
    /// <summary>
    /// Interaction logic for TerminalWindowTurboControl.
    /// </summary>
    public partial class TerminalWindowTurboControl : UserControl
    {
        #region Properties

        private OptionPageGridGeneral options;
        private Package package;
        private readonly List<ChatUserControlsItem> chatUserControlsItems;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowTurboControl"/> class.
        /// </summary>
        public TerminalWindowTurboControl()
        {
            this.InitializeComponent();

            this.PreviewKeyUp += TerminalWindowTurboControl_PreviewKeyUp;

            ChatRepository.CreateDataBase();

            chatUserControlsItems = [];
        }

        #endregion Constructors

        #region Event Handlers

        /// <summary>
        /// Handles the PreviewKeyUp event for the TerminalWindowTurboControl.
        /// Switches the selected tab in the tabChats control when the user releases a number key (1-9) while holding the Control key.
        /// </summary>
        private void TerminalWindowTurboControl_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }

            int tabIndex = -1;

            if (e.Key == Key.D1) tabIndex = 0;
            else if (e.Key == Key.D2) tabIndex = 1;
            else if (e.Key == Key.D3) tabIndex = 2;
            else if (e.Key == Key.D4) tabIndex = 3;
            else if (e.Key == Key.D5) tabIndex = 4;
            else if (e.Key == Key.D6) tabIndex = 5;
            else if (e.Key == Key.D7) tabIndex = 6;
            else if (e.Key == Key.D8) tabIndex = 7;
            else if (e.Key == Key.D9) tabIndex = 8;

            if (tabIndex >= 0 && tabIndex < tabChats.Items.Count)
            {
                tabChats.SelectedIndex = tabIndex;
            }
        }

        /// <summary>
        /// Event handler for the "New Chat" button click. Creates a new chat header and chat user control, adds them to a new tab item, and selects the new tab item.
        /// </summary>
        private void btnNewChat_Click(object sender, RoutedEventArgs e)
        {
            ucChatHeader ucChatHeader = new(this, "New Chat");

            string chatId = Guid.NewGuid().ToString();

            ucChat ucChat = new(this, options, package, ucChatHeader, [], chatId);

            TabItem newChatTab = new() { Header = ucChatHeader, Content = ucChat };

            tabChats.Items.Add(newChatTab);

            tabChats.SelectedItem = newChatTab;

            chatUserControlsItems.Add(new() { Chat = new() { Id = chatId, Name = string.Empty }, Header = ucChatHeader, TabItem = newChatTab });
        }

        /// <summary>
        /// Event handler for the double click on a chat item in the ListView.
        /// </summary>
        private void lvChats_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenChat();
        }

        /// <summary>
        /// Handles the KeyDown event for the lvChats ListView. When the Enter key is pressed,
        /// it opens the selected chat and marks the event as handled to prevent further processing.
        /// </summary>
        private void lvChats_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement is TextBox)
                {
                    return;
                }

                OpenChat();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Event handler for previewing mouse wheel scrolling on a ScrollViewer control.
        /// Scrolls the content of the ScrollViewer horizontally based on the direction of the mouse wheel.
        /// </summary>
        /// <param name="sender">The ScrollViewer control that raised the event.</param>
        /// <param name="e">The MouseWheelEventArgs containing information about the mouse wheel scrolling.</param>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;

            if (e.Delta < 0)
            {
                scrollViewer.LineRight();
            }
            else
            {
                scrollViewer.LineLeft();
            }

            e.Handled = true;
        }

        /// <summary>
        /// Ensures that when the ListView 'lvChats' receives focus, if no item is selected and the list contains items,
        /// the first item is selected and focused to improve keyboard navigation and user experience.
        /// </summary>
        private void lvChats_GotFocus(object sender, RoutedEventArgs e)
        {
            if (lvChats.SelectedIndex == -1 && lvChats.Items.Count > 0)
            {
                lvChats.SelectedIndex = 0;
                ListViewItem item = lvChats.ItemContainerGenerator.ContainerFromIndex(0) as ListViewItem;
                item?.Focus();
            }
        }

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

            List<ChatEntity> chats = ChatRepository.GetChats();

            foreach (ChatEntity chat in chats.OrderByDescending(c => c.Date))
            {
                ucChatItem ucChatItem = new(this, chat.Name);

                ChatUserControlsItem chatItem = new()
                {
                    Chat = chat,
                    Header = new ucChatHeader(this, chat.Name),
                    ListItem = ucChatItem
                };

                lvChats.Items.Add(ucChatItem);

                chatUserControlsItems.Add(chatItem);
            }
        }

        /// <summary>
        /// Notifies that a new chat has been created with the given chat header and chat name.
        /// </summary>
        /// <param name="header">The chat header.</param>
        /// <param name="chatName">The chat name.</param>
        /// <param name="firstMessages">The chat first messages.</param>
        public void NotifyNewChatCreated(ucChatHeader header, string chatName, List<MessageEntity> firstMessages)
        {
            int sameNameQtd = chatUserControlsItems.Count(c => c.Chat.Name.StartsWith(chatName));

            if (sameNameQtd > 0)
            {
                chatName = string.Concat(chatName, " ", sameNameQtd + 1);
            }

            ucChatItem chatItem = new(this, chatName);

            lvChats.Items.Insert(0, chatItem);

            ChatUserControlsItem chatUserControlItem = chatUserControlsItems.First(c => c.Header == header);

            chatUserControlItem.Chat.Name = chatName;
            chatUserControlItem.Chat.Date = DateTime.Now;
            chatUserControlItem.Chat.Messages = firstMessages.OrderBy(m => m.Order).ToList();
            chatUserControlItem.ListItem = chatItem;
            chatUserControlItem.OpenedBefore = true;

            ChatRepository.AddChat(chatUserControlItem.Chat);
        }

        /// <summary>
        /// Notifies that new chat messages have been added to the chat.
        /// </summary>
        /// <param name="header">The chat header.</param>
        /// <param name="messages">The list of new messages.</param>
        public void NotifyNewChatMessagesAdded(ucChatHeader header, List<MessageEntity> messages)
        {
            ChatUserControlsItem chatUserControlItem = chatUserControlsItems.First(c => c.Header == header);

            chatUserControlItem.Chat.Date = DateTime.Now;
            chatUserControlItem.Chat.Messages = messages.OrderBy(m => m.Order).ToList();

            ChatRepository.UpdateChat(chatUserControlItem.Chat);
        }

        /// <summary>
        /// Closes a tab in a chat interface based on the provided ucChatHeader.
        /// </summary>
        /// <param name="chatHeader">The ucChatHeader of the tab to be closed.</param>
        public void CloseTab(ucChatHeader chatHeader)
        {
            ChatUserControlsItem chatItem = chatUserControlsItems.First(c => c.Header == chatHeader);

            tabChats.Items.Remove(chatItem.TabItem);
        }

        /// <summary>
        /// Closes a tab in a chat interface based on the provided ucChat.
        /// </summary>
        /// <param name="chat">The chat user control whose tab should be closed.</param>
        public void CloseTab(ucChat chat)
        {
            ChatUserControlsItem chatItem = chatUserControlsItems.First(c => c.TabItem?.Content != null && (ucChat)c.TabItem.Content == chat);

            tabChats.Items.Remove(chatItem.TabItem);
        }

        /// <summary>
        /// Deletes a chat item from the chat list and repository.
        /// </summary>
        /// <param name="ucChatItem">The chat item to be deleted.</param>
        public void DeleteChat(ucChatItem ucChatItem)
        {
            ChatUserControlsItem chatItem = chatUserControlsItems.First(c => c.ListItem == ucChatItem);

            lvChats.Items.Remove(chatItem.ListItem);

            tabChats.Items.Remove(chatItem.TabItem);

            chatUserControlsItems.Remove(chatItem);

            ChatRepository.DeleteChat(chatItem.Chat.Id);
        }

        public bool SetChatNewName(ucChatItem ucChatItem, string newName)
        {
            int sameNameQtd = chatUserControlsItems.Count(c => c.Chat.Name.StartsWith(newName));

            if (sameNameQtd > 0)
            {
                return false;
            }

            ChatUserControlsItem chatItem = chatUserControlsItems.First(c => c.ListItem == ucChatItem);

            chatItem.Chat.Name = newName;
            chatItem.Header.UpdateChatName(newName);

            ChatRepository.UpdateChatName(chatItem.Chat.Id, chatItem.Chat.Name);

            return true;
        }

        /// <summary>
        /// Opens the selected chat from the chat list. If the chat is already opened in a tab, it selects that tab; otherwise, it opens a new tab for the chat.
        /// </summary>
        private void OpenChat()
        {
            ChatUserControlsItem chatItem = GetChatItem();

            if (chatItem?.TabItem != null && tabChats.Items.Contains(chatItem.TabItem))
            {
                tabChats.SelectedItem = chatItem.TabItem;
            }
            else
            {
                OpenTab(chatItem);
            }
        }

        /// <summary>
        /// Opens a new tab for the specified chat item. If the chat item has not been opened before, it creates a new header, retrieves the chat messages from the repository, creates a new chat user control, and sets the tab item's header and content.
        /// </summary>
        /// <param name="chatItem">The chat item to open a tab for.</param>
        private void OpenTab(ChatUserControlsItem chatItem)
        {
            if (!chatItem.OpenedBefore)
            {
                chatItem.Header = new(this, chatItem.Chat.Name);

                chatItem.Chat.Messages = ChatRepository.GetMessages(chatItem.Chat.Id);

                ucChat ucChat = new(this, options, package, chatItem.Header, chatItem.Chat.Messages, chatItem.Chat.Id);

                chatItem.TabItem = new() { Header = chatItem.Header, Content = ucChat };

                chatItem.OpenedBefore = true;
            }

            tabChats.Items.Add(chatItem.TabItem);

            tabChats.SelectedItem = chatItem.TabItem;
        }

        /// <summary>
        /// Deletes the currently selected chat by invoking its delete event handler.
        /// </summary>
        public async void DeleteChat(Object sender, ExecutedRoutedEventArgs e)
        {
            ChatUserControlsItem chatItem = GetChatItem();

            if (chatItem?.ListItem == null)
            {
                return;
            }

            chatItem.ListItem.imgDelete_Click(sender, null);
        }

        /// <summary>
        /// Handles the EditChat command execution by retrieving the current chat item and invoking its edit action.
        /// </summary>
        public async void EditChat(Object sender, ExecutedRoutedEventArgs e)
        {
            ChatUserControlsItem chatItem = GetChatItem();

            if (chatItem?.ListItem == null)
            {
                return;
            }

            chatItem.ListItem.imgEdit_Click(sender, null);
        }

        /// <summary>
        /// Retrieves the ChatUserControlsItem associated with the currently selected chat item in the list view.
        /// </summary>
        /// <returns>
        /// The ChatUserControlsItem corresponding to the selected ucChatItem; returns null if no item is selected or the selected item is not a ucChatItem.
        /// </returns>
        private ChatUserControlsItem GetChatItem()
        {
            if (lvChats.SelectedItem is not ucChatItem listItem)
            {
                return null;
            }

            return chatUserControlsItems.First(c => c.ListItem == listItem);
        }

        #endregion Methods          
    }
}