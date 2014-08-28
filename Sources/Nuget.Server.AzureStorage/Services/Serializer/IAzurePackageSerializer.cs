namespace Nuget.Server.AzureStorage.Services.Serializer {
    using Microsoft.WindowsAzure.Storage.Blob;

    using Nuget.Server.AzureStorage.Domain;

    public interface IAzurePackageSerializer {
        AzurePackage ReadFromMetadata(CloudBlockBlob container);

        void SaveToMetadata(AzurePackage package, CloudBlockBlob container);
    }
}