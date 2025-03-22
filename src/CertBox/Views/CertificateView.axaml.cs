// src/CertBox/Views/CertificateView.axaml.cs

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CertBox.Models;
using CertBox.ViewModels;
using Microsoft.Extensions.Logging;

namespace CertBox.Views
{
    public partial class CertificateView : UserControl
    {
        private readonly ILogger<CertificateView> _logger;

        public CertificateView(ILogger<CertificateView> logger)
        {
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

        private T FindVisualChild<T>(Visual visual) where T : Visual
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