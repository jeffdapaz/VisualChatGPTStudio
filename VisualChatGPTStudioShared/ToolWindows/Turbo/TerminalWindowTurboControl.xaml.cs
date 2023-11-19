using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
        private readonly List<ChatItem> chats;

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TerminalWindowTurboControl"/> class.
        /// </summary>
        public TerminalWindowTurboControl()
        {
            this.InitializeComponent();

            chats = new();
        }

        #endregion Constructors

        #region Event Handlers

        /// <summary>
        /// Event handler for the "New Chat" button click. Creates a new chat header and chat user control, adds them to a new tab item, and selects the new tab item.
        /// </summary>
        private void btnNewChat_Click(object sender, RoutedEventArgs e)
        {
            ucChatHeader ucChatHeader = new(this, "New Chat");

            ucChat ucChat = new(this, options, package, ucChatHeader);

            TabItem newChatTab = new() { Header = ucChatHeader, Content = ucChat };

            tabChats.Items.Add(newChatTab);

            tabChats.SelectedItem = newChatTab;

            chats.Add(new() { Id = Guid.NewGuid().ToString(), Header = ucChatHeader, TabItem = newChatTab, Name = string.Empty });
        }

        /// <summary>
        /// Event handler for the double click on a chat item in the ListView.
        /// </summary>
        private void lvChats_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lvChats.SelectedItem is not ucChatItem listItem)
            {
                return;
            }

            TabItem tabItem = chats.First(c => c.ListItem == listItem).TabItem;

            if (tabChats.Items.Contains(tabItem))
            {
                tabChats.SelectedItem = tabItem;
            }
            else
            {
                tabChats.Items.Add(tabItem); 
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
        }

        public void NotifyNewChatCreated(ucChatHeader header, string chatName)
        {
            int sameNameQtd = chats.Count(c => c.Name.StartsWith(chatName));

            if (sameNameQtd > 0)
            {
                chatName = string.Concat(chatName, " ", sameNameQtd + 1);
            }

            ucChatItem chatItem = new(this, chatName);

            lvChats.Items.Add(chatItem);

            ChatItem chat = chats.First(c => c.Header == header);

            chat.Name = chatName;
            chat.ListItem = chatItem;
        }

        /// <summary>
        /// Closes a tab in a chat interface based on the provided ucChatHeader.
        /// </summary>
        /// <param name="chatHeader">The ucChatHeader of the tab to be closed.</param>
        public void CloseTab(ucChatHeader chatHeader)
        {
            ChatItem chatItem = chats.First(c => c.Header == chatHeader);

            tabChats.Items.Remove(chatItem.TabItem);
        }

        public void DeleteChat(ucChatItem ucChatItem)
        {
            ChatItem chatItem = chats.First(c => c.ListItem == ucChatItem);

            lvChats.Items.Remove(chatItem.ListItem);

            tabChats.Items.Remove(chatItem.TabItem);

            chats.Remove(chatItem);
        }

        public bool SetChatNewName(ucChatItem ucChatItem, string newName)
        {
            int sameNameQtd = chats.Count(c => c.Name.StartsWith(newName));

            if (sameNameQtd > 0)
            {
                return false;
            }

            ChatItem chatItem = chats.First(c => c.ListItem == ucChatItem);

            chatItem.Name = newName;    
            chatItem.Header.UpdateChatName(newName);

            return true;
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