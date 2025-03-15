// src/CertBox/MainWindow.axaml.cs

using Avalonia.Controls;
using CertBox.ViewModels;

namespace CertBox
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            // Call async initialization after window is shown
            Loaded += async (s, e) => await viewModel.InitializeAsync();
        }
    }
}