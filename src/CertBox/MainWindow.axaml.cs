// src/CertBox/MainWindow.axaml.cs

using Avalonia.Controls;
using CertBox.ViewModels;

namespace CertBox
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.OpenFilePickerRequested += async () =>
            {
                return await ShowFilePickerAsync("Select Java Keystore File",
                    new FileDialogFilter { Name = "All Files", Extensions = { "*" } },
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "../../../../../../tests/resources/test_cacerts")));
            };
            viewModel.ImportCertificateRequested += async () =>
            {
                return await ShowFilePickerAsync("Select Certificate File",
                    new FileDialogFilter
                        { Name = "Certificate Files", Extensions = { "pem", "crt", "cer", "der" } },
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "../../../../../../tests/resources/sample_certs")));
            };
            Loaded += async (s, e) => await viewModel.InitializeAsync();
        }

        private async Task<string> ShowFilePickerAsync(string title, FileDialogFilter filter,
            string initialFileName = null)
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filters = new() { filter },
                AllowMultiple = false,
                InitialFileName = initialFileName
            };

            var result = await dialog.ShowAsync(this);
            return result != null && result.Length > 0 ? result[0] : null;
        }
    }
}