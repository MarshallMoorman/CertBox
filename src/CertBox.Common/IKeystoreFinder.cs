using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

namespace CertBox.Common
{
    public interface IKeystoreFinder
    {
        Task SearchCommonLocations(ObservableCollection<string> cacertsFiles);

        void SearchFilesystem(ObservableCollection<string> cacertsFiles, Action<string> addToCollection,
            CancellationToken cancellationToken, ILogger logger);
    }
}