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
            viewModel.OpenFilePickerRequested += async () =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select cacerts File",
                    Filters = new()
                    {
                        new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                    },
                    AllowMultiple = false,
#if DEBUG
                    InitialFileName = "/Library/Java/JavaVirtualMachines/zulu-11.jdk/Contents/Home/lib/security/cacerts"
#endif
                };

                var result = await dialog.ShowAsync(this);
                return result != null && result.Length > 0 ? result[0] : null;
            };
            Loaded += async (s, e) => await viewModel.InitializeAsync();
        }
    }
}