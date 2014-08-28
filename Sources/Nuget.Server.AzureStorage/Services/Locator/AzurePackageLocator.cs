namespace Nuget.Server.AzureStorage.Services.Locator {
    using NuGet;

    internal sealed class AzurePackageLocator : IPackageLocator {
        public string GetContainerName(IPackage package) {
            return GetAzureFriendlyString(package.Id);
        }

        public string GetContainerName(string packageId) {
            return GetAzureFriendlyString(packageId);
        }

        public string GetItemName(IPackage package) {
            return package.Version.ToString();
        }

        public string GetItemName(string packageId, SemanticVersion version) {
            return version.ToString();
        }

        private static string GetAzureFriendlyString(string packageId) {
            return packageId.ToLower()
                            .Replace(".", "-");
        }
    }
}