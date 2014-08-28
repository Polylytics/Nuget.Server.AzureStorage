namespace Nuget.Server.AzureStorage.Services.Repository {
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Threading.Tasks;
    using System.Web.Configuration;

    using AutoMapper;

    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    using Ninject;

    using NuGet;

    using Nuget.Server.AzureStorage.Domain;
    using Nuget.Server.AzureStorage.Services.Locator;
    using Nuget.Server.AzureStorage.Services.Serializer;

    using NuGet.Server.DataServices;
    using NuGet.Server.Infrastructure;

    /// <summary>
    ///     An class used to provide diffrent FileSystem to <see cref="ServerPackageRepository" />
    /// </summary>
    public class AzureServerPackageRepository : IServerPackageRepository {
        private readonly CloudBlobClient blobClient;

        private readonly IPackageLocator packageLocator;

        private readonly IAzurePackageSerializer packageSerializer;

        private readonly IHashProvider hashProvider;

        private static readonly ConcurrentDictionary<string, DerivedPackageData> DerivedDataCache = new ConcurrentDictionary<string, DerivedPackageData>();

        public PackageSaveModes PackageSaveMode { get; set; }

        public string Source {
            get {
                return "/";
            }
        }

        public bool SupportsPrereleasePackages {
            get {
                return false;
            }
        }

        private static bool EnableDelisting {
            get {
                bool value;
                return bool.TryParse(WebConfigurationManager.AppSettings["enableDelisting"], out value) && value;
            }
        }
        
        public AzureServerPackageRepository(IPackageLocator packageLocator, IAzurePackageSerializer packageSerializer, IHashProvider hashProvider, CloudBlobClient blobClient) {
            this.packageLocator = packageLocator;
            this.packageSerializer = packageSerializer;
            this.hashProvider = hashProvider;
            this.blobClient = blobClient;
        }

        public Package GetMetadataPackage(IPackage package) {
            return new Package(package, this.CalculateDerivedData(package));
        }

        private DerivedPackageData CalculateDerivedData(IPackage package) {
            DerivedPackageData derivedPackageData;

            if (DerivedDataCache.TryGetValue(package.Id, out derivedPackageData)) {
                return derivedPackageData;
            }

            var blob = this.GetLatestBlobForPackage(package);

            long length;
            byte[] inArray;
            using (var stream = blob.OpenRead()) {
                length = stream.Length;
                inArray = this.hashProvider.CalculateHash(stream);
            }

            derivedPackageData = new DerivedPackageData {
                PackageSize = length, 
                PackageHash = Convert.ToBase64String(inArray), 
                LastUpdated = blob.Properties.LastModified ?? default(DateTimeOffset), 
                Created = blob.Properties.LastModified ?? default(DateTimeOffset), 
                Path = blob.Uri.ToString(), 
                FullPath = blob.Uri.ToString(), 
                IsAbsoluteLatestVersion = package.IsAbsoluteLatestVersion, 
                IsLatestVersion = package.IsLatestVersion
            };

            DerivedDataCache.AddOrUpdate(package.Id, derivedPackageData, (key, old) => derivedPackageData);

            return derivedPackageData;
        }

        public CloudBlockBlob GetLatestBlobForPackage(IPackage package) {
            var containerName = this.packageLocator.GetContainerName(package);
            var container = this.blobClient.GetContainerReference(containerName);
            container.FetchAttributes();
            var latest = container.Metadata[AzurePropertiesConstants.LastUploadedVersion];
            var blob = container.GetBlockBlobReference(latest);
            return blob;
        }

        /// <summary>
        ///     Gets the packages.
        /// </summary>
        /// <returns></returns>
        public IQueryable<IPackage> GetPackages() {
            return this.GetPackagesInner().Result.AsQueryable();
        }

        private async Task<IEnumerable<IPackage>> GetPackagesInner() {
            var containers = this.blobClient.ListContainers().ToArray();
            
            // fetch all the container metadata
            await Task.WhenAll(containers.Select(c => c.FetchAttributesAsync()));

            // ReadFromMetadata could be async too
            return containers.Select(c => this.packageSerializer.ReadFromMetadata(c.GetBlockBlobReference(c.Metadata[AzurePropertiesConstants.LastUploadedVersion])))
                .Where(p => p.Listed);
        }

        /// <summary>
        ///     Searches the specified search term.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="targetFrameworks">The target frameworks.</param>
        /// <param name="allowPrereleaseVersions">if set to <c>true</c> [allow prerelease versions].</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IQueryable<IPackage> Search(string searchTerm, IEnumerable<string> targetFrameworks, bool allowPrereleaseVersions) {
            return (from p in this.GetPackages()
                                  .Find(searchTerm)
                                  .FilterByPrerelease(allowPrereleaseVersions)
                    where p.Listed
                    select p).AsQueryable<IPackage>();
        }

        /// <summary>
        ///     Gets the updates.
        /// </summary>
        /// <param name="packages">The packages.</param>
        /// <param name="includePrerelease">if set to <c>true</c> [include prerelease].</param>
        /// <param name="includeAllVersions">if set to <c>true</c> [include all versions].</param>
        /// <param name="targetFrameworks">The target frameworks.</param>
        /// <param name="versionConstraints">The version constraints.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerable<IPackage> GetUpdates(IEnumerable<IPackageName> packages, bool includePrerelease, bool includeAllVersions, IEnumerable<FrameworkName> targetFrameworks, IEnumerable<IVersionSpec> versionConstraints) {
            return this.GetUpdatesCore(packages, includePrerelease, includeAllVersions, targetFrameworks, versionConstraints);
        }

        /// <summary>
        ///     Adds the package.
        /// </summary>
        /// <param name="package">The package.</param>
        public void AddPackage(IPackage package) {
            var name = this.packageLocator.GetContainerName(package);
            var container = this.blobClient.GetContainerReference(name);

            var exists = container.CreateIfNotExists();

            container.Metadata[AzurePropertiesConstants.LatestModificationDate] = DateTimeOffset.Now.ToString();
            if (!exists) {
                container.Metadata[AzurePropertiesConstants.Created] = DateTimeOffset.Now.ToString();
            }

            container.Metadata[AzurePropertiesConstants.LastUploadedVersion] = package.Version.ToString();
            container.Metadata[AzurePropertiesConstants.PackageId] = package.Id;
            container.SetMetadata();

            var blobName = package.Version.ToString();

            // this.packageLocator.GetItemName(package);
            var blob = container.GetBlockBlobReference(blobName);
            blob.UploadFromStream(package.GetStream());
            blob.Metadata[AzurePropertiesConstants.LatestModificationDate] = DateTimeOffset.Now.ToString();
            var azurePackage = Mapper.Map<AzurePackage>(package);

            // blob.Metadata[AzurePropertiesConstants.Package] = JsonConvert.SerializeObject(azurePackage);
            this.packageSerializer.SaveToMetadata(azurePackage, blob);
            blob.SetMetadata();
        }

        /// <summary>
        ///     Removes the package.
        /// </summary>
        /// <param name="package">The package.</param>
        public void RemovePackage(IPackage package) {
            var name = this.packageLocator.GetContainerName(package);
            var container = this.blobClient.GetContainerReference(name);

            if (container.Exists()) {
                var blobName = package.Version.ToString(); // TODO: this should use this.packageLocator.GetItemName(package), but a) that isn't backwards compatible and b) isn't what AddPackage() does
                var blob = container.GetBlockBlobReference(blobName);

                if (EnableDelisting) {
                    blob.FetchAttributes();
                    blob.Metadata[AzurePackageSerializer.PackageIsListed] = "false";
                    blob.SetMetadata();
                }
                else {
                    blob.DeleteIfExists();
                }
            }
        }

        /// <summary>
        ///     Removes the package.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="version">The version.</param>
        public void RemovePackage(string packageId, SemanticVersion version) {
            this.RemovePackage(new AzurePackage {
                Id = packageId, 
                Version = version, 
            });
        }
    }
}