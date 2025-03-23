// src/CertBox/Views/CertificateView.axaml.cs

using System.Security.Cryptography.X509Certificates;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CertBox.Models;
using CertBox.Services;
using CertBox.ViewModels;
using Microsoft.Extensions.Logging;

namespace CertBox.Views
{
    public partial class CertificateView : UserControl
    {
        private readonly CertificateService _certificateService;
        private readonly ILogger<CertificateView> _logger;

        // Parameterless constructor for Avalonia's runtime loader
        public CertificateView()
        {
            throw new InvalidOperationException("CertificateView must be instantiated via dependency injection.");
        }

        public CertificateView(CertificateService certificateService, ILogger<CertificateView> logger)
        {
            _certificateService = certificateService;
            _logger = logger;

            InitializeComponent();

            // Handle DataGrid row styling for expired certificates
            var certificateList = this.FindControl<DataGrid>("CertificateList");
            if (certificateList != null)
            {
                // Update row classes when the DataGrid is attached to the visual tree
                certificateList.AttachedToVisualTree += (s, e) =>
                {
                    ScheduleUpdateDataGridRowClasses(certificateList);

                    // Find the ScrollViewer in the visual tree and attach the ScrollChanged handler
                    var scrollViewer = FindVisualChild<ScrollViewer>(certificateList);
                    if (scrollViewer != null)
                    {
                        scrollViewer.ScrollChanged += (s, e) => { ScheduleUpdateDataGridRowClasses(certificateList); };
                    }
                    else
                    {
                        _logger.LogWarning("ScrollViewer not found in DataGrid visual tree");
                    }
                };

                // Update row classes when DataContext or its properties change
                PropertyChanged += (s, e) =>
                {
                    if (e.Property == DataContextProperty && DataContext is MainWindowViewModel vm)
                    {
                        // Subscribe to changes in AllCertificates
                        vm.Certificates.CollectionChanged += (s, e) =>
                        {
                            ScheduleUpdateDataGridRowClasses(certificateList);
                        };

                        // Initial update
                        ScheduleUpdateDataGridRowClasses(certificateList);
                    }
                };

                // Add drag-and-drop handler
                AddHandler(DragDrop.DropEvent, OnCertificateListDrop);
                AddHandler(DragDrop.DragOverEvent, OnCertificateListDragOver);
                AddHandler(DragDrop.DragLeaveEvent, OnCertificateListDragLeave);
            }
        }

        private void OnCertificateListDragOver(object? sender, DragEventArgs e)
        {
            var certificateList = this.FindControl<DataGrid>("CertificateList");
            if (certificateList != null)
            {
                if (e.Data.Contains(DataFormats.Files))
                {
                    e.DragEffects = DragDropEffects.Copy;
                    // Highlight the CertificateList during drag-over
                    certificateList.BorderBrush = Brushes.Green;
                    certificateList.BorderThickness = new Avalonia.Thickness(2);
                }
                else
                {
                    e.DragEffects = DragDropEffects.None;
                    // Reset the border when not a valid drop
                    certificateList.BorderBrush = Brushes.Transparent;
                    certificateList.BorderThickness = new Avalonia.Thickness(0);
                }
            }

            e.Handled = true;
        }

        private void OnCertificateListDragLeave(object? sender, DragEventArgs e)
        {
            var certificateList = this.FindControl<DataGrid>("CertificateList");
            if (certificateList != null)
            {
                // Reset the border when the drag leaves
                certificateList.BorderBrush = Brushes.Transparent;
                certificateList.BorderThickness = new Avalonia.Thickness(0);
            }
        }

        private async void OnCertificateListDrop(object? sender, DragEventArgs e)
        {
            var certificateList = this.FindControl<DataGrid>("CertificateList");
            if (certificateList != null)
            {
                certificateList.BorderBrush = Brushes.Transparent;
                certificateList.BorderThickness = new Avalonia.Thickness(0);
            }

            if (DataContext is MainWindowViewModel vm && e.Data.Contains(DataFormats.Files))
            {
                if (string.IsNullOrEmpty(vm.SelectedFilePath) || !File.Exists(vm.SelectedFilePath))
                {
                    _logger.LogWarning("No keystore loaded for import");
                    vm.ShowError("No keystore loaded for import.");
                    return;
                }

                var files = e.Data.GetFiles();
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        var certPath = file.Path.LocalPath;
                        _logger.LogDebug("Dropped file on CertificateList: {Path}", certPath);

                        if (!File.Exists(certPath))
                        {
                            _logger.LogWarning("Dropped certificate file does not exist: {Path}", certPath);
                            vm.ShowError($"Dropped certificate file does not exist: {certPath}");
                            continue;
                        }

                        try
                        {
                            var cert = X509CertificateLoader.LoadCertificate(File.ReadAllBytes(certPath));
                            var alias = Path.GetFileNameWithoutExtension(certPath);
                            _certificateService.ImportCertificate(alias, cert);
                            await _certificateService.LoadCertificatesAsync(vm.SelectedFilePath);
                            _logger.LogInformation("Imported certificate with alias: {Alias}", alias);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error importing certificate from dropped file {Path}", certPath);
                            vm.ShowError($"Error importing certificate from {certPath}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void ScheduleUpdateDataGridRowClasses(DataGrid dataGrid)
        {
            // Schedule the update on the UI thread to ensure the visual tree is fully constructed
            Dispatcher.UIThread.Post(() => { UpdateDataGridRowClasses(dataGrid); }, DispatcherPriority.Background);
        }

        private void UpdateDataGridRowClasses(DataGrid dataGrid)
        {
            // Find the DataGridRowsPresenter in the visual tree
            var rowsPresenter = FindVisualChild<DataGridRowsPresenter>(dataGrid);
            if (rowsPresenter == null)
            {
                _logger.LogDebug("DataGridRowsPresenter not found in DataGrid visual tree");
                return;
            }

            // Find all DataGridRow elements in the DataGridRowsPresenter's visual tree
            var rows = FindVisualChildren<DataGridRow>(rowsPresenter);
            foreach (var row in rows)
            {
                if (row.DataContext is CertificateModel cert)
                {
                    // Clear existing classes related to expiration
                    row.Classes.Remove("expired");

                    // Apply the 'expired' class if the certificate is expired
                    if (cert.IsExpired)
                    {
                        row.Classes.Add("expired");
                    }
                }
            }

            _logger.LogDebug("Updated classes for {RowCount} DataGridRow elements", rows.Count());
        }

        private T? FindVisualChild<T>(Visual visual) where T : Visual
        {
            if (visual == null) return null;

            if (visual is T target)
            {
                return target;
            }

            foreach (var child in visual.GetVisualChildren())
            {
                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private IEnumerable<T> FindVisualChildren<T>(Visual visual) where T : Visual
        {
            var results = new List<T>();

            if (visual == null) return results;

            if (visual is T target)
            {
                results.Add(target);
            }

            foreach (var child in visual.GetVisualChildren())
            {
                results.AddRange(FindVisualChildren<T>(child));
            }

            return results;
        }
    }
}