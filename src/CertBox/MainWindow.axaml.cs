using Avalonia;
using Avalonia.Controls;
using CertBox.Common;
using CertBox.Services;
using CertBox.ViewModels;
using CertBox.Views;
using Microsoft.Extensions.Logging;

namespace CertBox
{
    public partial class MainWindow : Window
    {
        private readonly IApplicationContext _applicationContext;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly UserConfigService _userConfigService;
        private readonly CertificateService _certificateService;
        private readonly KeystoreView _keystoreView;
        private readonly HeaderView _headerView;
        private readonly ErrorPaneView _errorPaneView;
        private readonly CertificateView _certificateView;
        private readonly DetailsPaneView _detailsPaneView;
        private readonly StatusBarView _statusBarView;
        private Grid _certificateGrid;

        public MainWindow(MainWindowViewModel viewModel, IApplicationContext applicationContext,
            ILogger<MainWindowViewModel> logger, UserConfigService userConfigService, CertificateService certificateService,
            KeystoreView keystoreView, HeaderView headerView, ErrorPaneView errorPaneView, CertificateView certificateView,
            DetailsPaneView detailsPaneView, StatusBarView statusBarView)
        {
            _applicationContext = applicationContext;
            _logger = logger;
            _userConfigService = userConfigService;
            _certificateService = certificateService;
            _keystoreView = keystoreView;
            _headerView = headerView;
            _errorPaneView = errorPaneView;
            _certificateView = certificateView;
            _detailsPaneView = detailsPaneView;
            _statusBarView = statusBarView;

            InitializeComponent();

            // Set the user control instances in their placeholders
            SetUserControl("HeaderViewPlaceholder", _headerView);
            SetUserControl("ErrorPaneViewPlaceholder", _errorPaneView);
            SetUserControl("KeystoreViewPlaceholder", _keystoreView);
            SetUserControl("CertificateViewPlaceholder", _certificateView);
            SetUserControl("DetailsPaneViewPlaceholder", _detailsPaneView);
            SetUserControl("StatusBarViewPlaceholder", _statusBarView);

            DataContext = viewModel;
            viewModel.OpenFilePickerRequested += async () =>
            {
                return await ShowFilePickerAsync("Select Java Keystore File",
                    new FileDialogFilter { Name = "All Files", Extensions = { "*" } },
                    _applicationContext.DefaultKeystorePath);
            };
            viewModel.ImportCertificateRequested += async () =>
            {
                return await ShowFilePickerAsync("Select Certificate File",
                    new FileDialogFilter
                        { Name = "Certificate Files", Extensions = { "pem", "crt", "cer", "der" } },
                    _applicationContext.DefaultSampleCertsPath);
            };
            Loaded += async (s, e) => await viewModel.InitializeAsync();

            // Load window size from user config
            Width = _userConfigService.Config.WindowWidth;
            Height = _userConfigService.Config.WindowHeight;

            // Save window size when it changes
            PropertyChanged += (s, e) =>
            {
                if (e.Property == BoundsProperty && WindowState == WindowState.Normal) // Only save size in normal state
                {
                    var bounds = Bounds;
                    _userConfigService.Config.WindowWidth = bounds.Width;
                    _userConfigService.Config.WindowHeight = bounds.Height;
                    _userConfigService.SaveConfig();
                }
            };

            // Attach PointerPressed handler to the window
            PointerPressed += (s, e) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    // Get the source of the click
                    var source = e.Source as Control;

                    // Find the CertificateView and its controls
                    var certificateView =
                        this.FindControl<ContentControl>("CertificateViewPlaceholder")?.Content as CertificateView;
                    var certificateList = certificateView?.FindControl<DataGrid>("CertificateList");
                    bool isWithinDataGrid =
                        source != null && certificateList != null && IsDescendantOf(source, certificateList);

                    // Find the Remove button within CertificateView
                    var removeButton = certificateView?.FindControl<Button>("RemoveButton");
                    bool isRemoveButton = source is Button button && button == removeButton;

                    // Find the DetailsPaneView and its DetailsPane Border
                    var detailsPaneView =
                        this.FindControl<ContentControl>("DetailsPaneViewPlaceholder")?.Content as DetailsPaneView;
                    var detailsPane = detailsPaneView?.FindControl<Border>("DetailsPane");
                    bool isWithinDetailsPane = source != null && detailsPane != null && IsDescendantOf(source, detailsPane);

                    // Clear selection if the click is outside the DataGrid, DetailsPane, and not on the Remove button
                    if (!isWithinDataGrid && !isWithinDetailsPane && !isRemoveButton)
                    {
                        vm.SelectedCertificate = null;
                    }
                }
            };

            // Find the Grid containing the DataGrid and details pane
            _certificateGrid = this.FindControl<Grid>("CertificateGrid");
            if (_certificateGrid != null && DataContext is MainWindowViewModel vm)
            {
                // Subscribe to changes in SelectedCertificate
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainWindowViewModel.SelectedCertificate))
                    {
                        UpdateCertificateGridColumns(vm.SelectedCertificate != null);
                    }
                };

                // Initial setup
                UpdateCertificateGridColumns(vm.SelectedCertificate != null);
            }
        }

        private void SetUserControl(string placeholderName, Control control)
        {
            var placeholder = this.FindControl<ContentControl>(placeholderName);
            if (placeholder != null)
            {
                placeholder.Content = control;
            }
            else
            {
                _logger.LogWarning("{PlaceholderName} not found in MainWindow", placeholderName);
            }
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

        private bool IsDescendantOf(Control control, Control ancestor)
        {
            var current = control;
            while (current != null)
            {
                if (current == ancestor)
                    return true;

                current = current.Parent as Control;
            }

            return false;
        }

        private void UpdateCertificateGridColumns(bool hasSelection)
        {
            if (_certificateGrid == null) return;

            _logger.LogDebug("Updating CertificateGrid columns: hasSelection={HasSelection}", hasSelection);

            if (hasSelection)
            {
                // When a certificate is selected: 3:1 ratio with splitter
                _certificateGrid.ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                };

                // Reset the actual widths to enforce the 3:1 ratio
                foreach (var column in _certificateGrid.ColumnDefinitions)
                {
                    column.MinWidth = 0; // Reset any user resizing
                    column.MaxWidth = double.PositiveInfinity;
                }

                _certificateGrid.Margin = new Thickness(0, 0, 10, 0);
            }
            else
            {
                // When no certificate is selected: DataGrid takes all space, splitter and details pane hidden
                _certificateGrid.ColumnDefinitions = new ColumnDefinitions
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(0) },
                    new ColumnDefinition { Width = new GridLength(0) }
                };

                _certificateGrid.Margin = new Thickness(0, 0, 0, 0);
            }

            // Log the actual widths after updating
            _logger.LogDebug("CertificateGrid Column 0 ActualWidth={Width0}, Column 2 ActualWidth={Width2}",
                _certificateGrid.ColumnDefinitions[0].ActualWidth,
                _certificateGrid.ColumnDefinitions[2].ActualWidth);
        }
    }
}