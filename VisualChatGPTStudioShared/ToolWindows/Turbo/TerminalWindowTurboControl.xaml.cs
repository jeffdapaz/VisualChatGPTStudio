using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
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

            ChatRepository.CreateDataBase();

            chatUserControlsItems = [];
        }

        #endregion Constructors

        #region Event Handlers

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
            if (lvChats.SelectedItem is not ucChatItem listItem)
            {
                return;
            }

            ChatUserControlsItem chatItem = chatUserControlsItems.First(c => c.ListItem == listItem);

            if (chatItem.TabItem != null && tabChats.Items.Contains(chatItem.TabItem))
            {
                tabChats.SelectedItem = chatItem.TabItem;
            }
            else
            {
                OpenTab(chatItem);
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

        #endregion Methods                            
    }

    /// <summary>
    /// Represents the different types of commands that can be used.
    /// </summary>
    enum CommandType
    {
        Code = 0,
        Request = 1
    }
}