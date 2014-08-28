namespace Nuget.Server.AzureStorage.Domain {
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Versioning;

    using Newtonsoft.Json;

    using NuGet;

    internal sealed class AzurePackageDependencySet {
        [JsonIgnore]
        public ICollection<PackageDependency> Dependencies {
            get {
                return (this.SeriazlizableDependencies == null
                            ? new PackageDependency[0]
                            : this.SeriazlizableDependencies.Select(SerializePackageDependency)).ToList();
            }

            set {
                this.SeriazlizableDependencies = value.Select(x => x.Id + " " + JsonConvert.SerializeObject(new AzureVersionSpec(x.VersionSpec)));
            }
        }

        [JsonIgnore]
        public IEnumerable<FrameworkName> SupportedFrameworks {
            get {
                return this.SeriazlizableSupportedFrameworks == null
                           ? new FrameworkName[0]
                           : this.SeriazlizableSupportedFrameworks.Select(x => new FrameworkName(x));
            }

            set {
                this.SeriazlizableSupportedFrameworks = value.Select(x => x.FullName);
            }
        }

        [JsonIgnore]
        public FrameworkName TargetFramework {
            get {
                return string.IsNullOrWhiteSpace(this.SeriazlizableTargetFramework)
                           ? null
                           : new FrameworkName(this.SeriazlizableTargetFramework);
            }

            set {
                if (value != null) {
                    this.SeriazlizableTargetFramework = value.FullName;
                }
            }
        }

        public string SeriazlizableTargetFramework { get; set; }

        public IEnumerable<string> SeriazlizableDependencies { get; set; }

        public IEnumerable<string> SeriazlizableSupportedFrameworks { get; set; }

        private static PackageDependency SerializePackageDependency(string x) {
            var firstSpace = x.IndexOf(' ');
            var id = x.Substring(0, firstSpace);
            var serializedVersion = x.Substring(firstSpace);

            if (string.IsNullOrWhiteSpace(serializedVersion)) {
                return new PackageDependency(id);
            }

            var version = JsonConvert.DeserializeObject<AzureVersionSpec>(serializedVersion);
            return new PackageDependency(id, version.ToVersionSpec());
        }
    }
}