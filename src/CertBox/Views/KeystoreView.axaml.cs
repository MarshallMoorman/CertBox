using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using CertBox.Services;
using CertBox.ViewModels;
using Microsoft.Extensions.Logging;

namespace CertBox.Views
{
    public partial class KeystoreView : UserControl
    {
        private readonly CertificateService _certificateService;
        private readonly IKeystoreSearchService _searchService;
        private readonly ILogger<KeystoreView> _logger;

        // Parameterless constructor for Avalonia's runtime loader
        public KeystoreView()
        {
            throw new InvalidOperationException("KeystoreView must be instantiated via dependency injection.");
        }

        public KeystoreView(CertificateService certificateService, IKeystoreSearchService searchService,
            ILogger<KeystoreView> logger)
        {
            _certificateService = certificateService;
            _searchService = searchService;
            _logger = logger;

            InitializeComponent();

            // Attach SelectionChanged handler to the ListBox
            var keystoreList = this.FindControl<ListBox>("KeystoreList");
            if (keystoreList != null)
            {
                keystoreList.SelectionChanged += async (s, e) =>
                {
                    _logger.LogDebug("KeystoreList SelectionChanged event fired");
                    if (DataContext is MainWindowViewModel vm && keystoreList.SelectedItem is string selectedPath)
                    {
                        _logger.LogDebug("Selected keystore path: {Path}", selectedPath);
                        try
                        {
                            await _certificateService.LoadCertificatesAsync(selectedPath);
                            _logger.LogDebug("Certificates loaded successfully, count: {Count}", vm.Certificates.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error loading certificates from {Path}", selectedPath);
                            vm.ShowError($"Error loading certificates: {ex.Message}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "SelectionChanged: DataContext or SelectedItem invalid. DataContext: {DataContext}, SelectedItem: {SelectedItem}",
                            DataContext?.GetType().Name ?? "null",
                            keystoreList.SelectedItem?.ToString() ?? "null");
                    }
                };

                // Add drag-and-drop handlers
                AddHandler(DragDrop.DropEvent, OnKeystoreListDrop);
                AddHandler(DragDrop.DragOverEvent, OnKeystoreListDragOver);
                AddHandler(DragDrop.DragLeaveEvent, OnKeystoreListDragLeave);
            }
            else
            {
                _logger.LogWarning("KeystoreList not found in KeystoreView");
            }
        }

        private void OnKeystoreListDragOver(object? sender, DragEventArgs e)
        {
            var keystoreList = this.FindControl<ListBox>("KeystoreList");
            if (keystoreList != null)
            {
                if (e.Data.Contains(DataFormats.Files))
                {
                    e.DragEffects = DragDropEffects.Copy;
                    // Highlight the KeystoreList during drag-over
                    keystoreList.BorderBrush = Brushes.Green;
                    keystoreList.BorderThickness = new Avalonia.Thickness(2);
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                    // Reset the border when not a valid drop
                    keystoreList.BorderBrush = Brushes.Transparent;
                    keystoreList.BorderThickness = new Avalonia.Thickness(0);
                }
            }

            e.Handled = true;
        }

        private void OnKeystoreListDragLeave(object? sender, DragEventArgs e)
        {
            var keystoreList = this.FindControl<ListBox>("KeystoreList");
            if (keystoreList != null)
            {
                // Reset the border when the drag leaves
                keystoreList.BorderBrush = Brushes.Transparent;
                keystoreList.BorderThickness = new Avalonia.Thickness(0);
            }
        }

        private async void OnKeystoreListDrop(object? sender, DragEventArgs e)
        {
            var keystoreList = this.FindControl<ListBox>("KeystoreList");
            if (keystoreList != null)
            {
                // Reset the border after drop
                keystoreList.BorderBrush = Brushes.Transparent;
                keystoreList.BorderThickness = new Avalonia.Thickness(0);
            }

            if (DataContext is MainWindowViewModel vm && e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        var filePath = file.Path.LocalPath;
                        _logger.LogDebug("Dropped file on KeystoreList: {Path}", filePath);

                        if (!File.Exists(filePath))
                        {
                            _logger.LogWarning("Dropped file does not exist: {Path}", filePath);
                            vm.ShowError($"Dropped file does not exist: {filePath}");
                            continue;
                        }

                        try
                        {
                            // Add the keystore path to the list if not already present
                            _searchService.AddKeystorePath(filePath);
                            await _certificateService.LoadCertificatesAsync(filePath);
                            vm.SelectedFilePath = filePath; // Update the selected path on success
                            _logger.LogDebug("Certificates loaded successfully from dropped file, count: {Count}",
                                vm.Certificates.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error loading certificates from dropped file {Path}", filePath);
                            vm.ShowError($"Error loading certificates from {filePath}: {ex.Message}");
                            vm.SelectedFilePath = string.Empty; // Clear the selected path on failure
                        }
                    }
                }
            }
        }
    }
}