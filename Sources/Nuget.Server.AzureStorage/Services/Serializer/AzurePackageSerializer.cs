namespace Nuget.Server.AzureStorage.Services.Serializer {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using AutoMapper;

    using Microsoft.WindowsAzure.Storage.Blob;

    using Newtonsoft.Json;

    using NuGet;

    using Nuget.Server.AzureStorage.Domain;

    internal sealed class AzurePackageSerializer : IAzurePackageSerializer {
        internal const string ReleaseNotesEnc64 = "ReleaseNotesEnc64";

        internal const string PackageIsListed = "Listed";

        public AzurePackage ReadFromMetadata(CloudBlockBlob blob) {
            blob.FetchAttributes();

            var package = new AzurePackage {
                Id = blob.Metadata["Id"],
                RequireLicenseAcceptance = bool.Parse(blob.Metadata["RequireLicenseAcceptance"]),
                DevelopmentDependency = bool.Parse(blob.Metadata["DevelopmentDependency"]),
                IsAbsoluteLatestVersion = bool.Parse(blob.Metadata["IsAbsoluteLatestVersion"]),
                IsLatestVersion = bool.Parse(blob.Metadata["IsLatestVersion"]),
                Listed = bool.Parse(blob.Metadata[PackageIsListed]),
                Version = new SemanticVersion(blob.Metadata["Version"]),
                DependencySets = JsonConvert.DeserializeObject<IEnumerable<AzurePackageDependencySet>>(Base64Decode(blob.Metadata["Dependencies"]))
                                            .Select(x => new PackageDependencySet(x.TargetFramework, x.Dependencies))
            };

            if (blob.Metadata.ContainsKey("Title")) {
                package.Title = blob.Metadata["Title"];
            }

            if (blob.Metadata.ContainsKey("Authors")) {
                package.Authors = blob.Metadata["Authors"].Split(',');
            }

            if (blob.Metadata.ContainsKey("Owners")) {
                package.Owners = blob.Metadata["Owners"].Split(',');
            }

            if (blob.Metadata.ContainsKey("IconUrl")) {
                package.IconUrl = new Uri(blob.Metadata["IconUrl"]);
            }

            if (blob.Metadata.ContainsKey("LicenseUrl")) {
                package.LicenseUrl = new Uri(blob.Metadata["LicenseUrl"]);
            }

            if (blob.Metadata.ContainsKey("ProjectUrl")) {
                package.ProjectUrl = new Uri(blob.Metadata["ProjectUrl"]);
            }
            
            if (blob.Metadata.ContainsKey("Description")) {
                package.Description = blob.Metadata["Description"];
            }

            if (blob.Metadata.ContainsKey("Summary")) {
                package.Summary = blob.Metadata["Summary"];
            }

            if (blob.Metadata.ContainsKey("ReleaseNotes")) {
                package.ReleaseNotes = blob.Metadata["ReleaseNotes"];
            }

            if (blob.Metadata.ContainsKey(ReleaseNotesEnc64)) {
                package.ReleaseNotes = Base64Decode(blob.Metadata[ReleaseNotesEnc64]);
            }

            if (blob.Metadata.ContainsKey("Tags")) {
                package.Tags = blob.Metadata["Tags"];
            }

            if (blob.Metadata.ContainsKey("MinClientVersion")) {
                package.MinClientVersion = new Version(blob.Metadata["MinClientVersion"]);
            }

            return package;
        }

        public void SaveToMetadata(AzurePackage package, CloudBlockBlob blob) {
            if (package.Version == null) {
                throw new ArgumentException("Package must not have a null Version");
            }

            if (package.DependencySets == null) {
                throw new ArgumentException("Package must not have a null DependencySets");
            }

            blob.Metadata["Id"] = package.Id;
            blob.Metadata["Version"] = package.Version.ToString();
            blob.Metadata["RequireLicenseAcceptance"] = package.RequireLicenseAcceptance.ToString();
            blob.Metadata["DevelopmentDependency"] = package.DevelopmentDependency.ToString();
            blob.Metadata["IsAbsoluteLatestVersion"] = package.IsAbsoluteLatestVersion.ToString();
            blob.Metadata["IsLatestVersion"] = package.IsLatestVersion.ToString();
            blob.Metadata[PackageIsListed] = package.Listed.ToString();
            blob.Metadata["Dependencies"] = Base64Encode(JsonConvert.SerializeObject(package.DependencySets.Select(Mapper.Map<AzurePackageDependencySet>)));

            if (package.IconUrl != null) {
                blob.Metadata["IconUrl"] = package.IconUrl.AbsoluteUri;
            }

            if (package.LicenseUrl != null) {
                blob.Metadata["LicenseUrl"] = package.LicenseUrl.AbsoluteUri;
            }

            if (package.ProjectUrl != null) {
                blob.Metadata["ProjectUrl"] = package.ProjectUrl.AbsoluteUri;
            }

            if (package.MinClientVersion != null) {
                blob.Metadata["MinClientVersion"] = package.MinClientVersion.ToString();
            }

            if (!string.IsNullOrEmpty(package.Title)) {
                blob.Metadata["Title"] = package.Title;
            }

            if (!string.IsNullOrEmpty(package.Description)) {
                blob.Metadata["Description"] = package.Description;
            }

            if (!string.IsNullOrEmpty(package.Summary)) {
                blob.Metadata["Summary"] = package.Summary;
            }

            if (!string.IsNullOrEmpty(package.ReleaseNotes)) {
                blob.Metadata[ReleaseNotesEnc64] = Base64Encode(package.ReleaseNotes);
            }

            if (!string.IsNullOrEmpty(package.Tags)) {
                blob.Metadata["Tags"] = package.Tags;
            }

            if (package.Authors != null) {
                blob.Metadata["Authors"] = string.Join(",", package.Authors);
            }

            if (package.Owners != null) {
                blob.Metadata["Owners"] = string.Join(",", package.Owners);
            }

            blob.SetMetadata();
        }

        private static string Base64Encode(string plainText) {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        private static string Base64Decode(string base64EncodedData) {
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64EncodedData));
        }
    }
}