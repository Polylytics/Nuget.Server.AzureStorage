namespace Nuget.Server.AzureStorage.Services.Locator {
    using NuGet;

    internal sealed class AzurePackageLocator : IPackageLocator {
        public string GetContainerName(IPackage package) {
            return this.GetAzureFriendlyString(package.Id);
        }

        public string GetItemName(IPackage package) {
            return this.GetAzureFriendlyString(package.Version.ToString());
        }

        private string GetAzureFriendlyString(string packageId) {
            return packageId.ToLower()
                            .Replace(".", "-");
        }
    }
}