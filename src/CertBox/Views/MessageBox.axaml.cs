// src/CertBox/Views/MessageBox.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using CertBox.Models;

namespace CertBox.Views
{
    public partial class MessageBox : Window
    {
        public string Message { get; set; } = string.Empty;
        public MessageBoxButtons Buttons { get; set; }
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public MessageBox()
        {
            InitializeComponent();
        }

        public static async Task<MessageBoxResult> Show(Window owner, string title, string message, MessageBoxButtons buttons)
        {
            var messageBox = new MessageBox
            {
                Title = title,
                Message = message,
                Buttons = buttons
            };

            // Set MaxWidth and MaxHeight based on owner window size (80% of owner's dimensions)
            if (owner != null)
            {
                messageBox.MaxWidth = owner.Bounds.Width * 0.8;
                messageBox.MaxHeight = owner.Bounds.Height * 0.8;

                // Also set the ScrollViewer's MaxHeight to 60% of the owner's height to leave room for buttons
                var scrollViewer = messageBox.FindControl<ScrollViewer>("ScrollViewer");
                if (scrollViewer != null)
                {
                    scrollViewer.MaxHeight = owner.Bounds.Height * 0.6;
                }
            }

            messageBox.DataContext = messageBox; // Bind to itself for Message property
            messageBox.InitializeButtons();

            // Size to content within MaxWidth/MaxHeight constraints
            messageBox.SizeToContent = SizeToContent.WidthAndHeight;

            await messageBox.ShowDialog(owner);
            return messageBox.Result;
        }

        private void InitializeButtons()
        {
            var buttonPanel = this.FindControl<StackPanel>("ButtonPanel") ?? new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 10
            };

            switch (Buttons)
            {
                case MessageBoxButtons.Ok:
                    AddButton(buttonPanel, "OK", MessageBoxResult.Ok, isDefault: true);
                    break;
                case MessageBoxButtons.OkCancel:
                    AddButton(buttonPanel, "OK", MessageBoxResult.Ok, isDefault: true);
                    AddButton(buttonPanel, "Cancel", MessageBoxResult.Cancel);
                    break;
                case MessageBoxButtons.YesNo:
                    AddButton(buttonPanel, "Yes", MessageBoxResult.Yes, isDefault: true);
                    AddButton(buttonPanel, "No", MessageBoxResult.No);
                    break;
                case MessageBoxButtons.YesNoCancel:
                    AddButton(buttonPanel, "Yes", MessageBoxResult.Yes, isDefault: true);
                    AddButton(buttonPanel, "No", MessageBoxResult.No);
                    AddButton(buttonPanel, "Cancel", MessageBoxResult.Cancel);
                    break;
            }

            var stackPanel = this.FindControl<StackPanel>("MainStackPanel");
            if (stackPanel != null && !stackPanel.Children.Contains(buttonPanel))
            {
                stackPanel.Children.Add(buttonPanel);
            }
        }

        private void AddButton(StackPanel panel, string content, MessageBoxResult result, bool isDefault = false)
        {
            var button = new Button
            {
                Content = content,
                Width = 75,
                IsDefault = isDefault,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            button.Click += (s, e) =>
            {
                Result = result;
                Close();
            };
            panel.Children.Add(button);
        }
    }
}