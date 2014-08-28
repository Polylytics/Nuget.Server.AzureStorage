namespace Nuget.Server.AzureStorage.Domain.Services {
    using Microsoft.WindowsAzure.Storage.Blob;

    using Nuget.Server.AzureStorage.Doman.Entities;

    public interface IAzurePackageSerializer {
        AzurePackage ReadFromMetadata(CloudBlockBlob container);

        void SaveToMetadata(AzurePackage package, CloudBlockBlob container);
    }
}