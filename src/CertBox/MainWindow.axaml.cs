using Avalonia.Controls;
using CertBox.ViewModels;

namespace CertBox
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        // Avalonia XAML compilation handles InitializeComponent()
    }
}