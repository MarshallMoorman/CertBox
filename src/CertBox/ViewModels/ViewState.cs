// src/CertBox/ViewModels/ViewState.cs

using CommunityToolkit.Mvvm.ComponentModel;

namespace CertBox.ViewModels
{
    public partial class ViewState : ObservableObject
    {
        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _isErrorPaneVisible;

        [ObservableProperty]
        private bool _isDeepSearchRunning;
    }
}