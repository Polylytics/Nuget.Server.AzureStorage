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
                return (this.SerializableDependencies == null
                            ? new PackageDependency[0]
                            : this.SerializableDependencies.Select(SerializePackageDependency)).ToList();
            }

            set {
                this.SerializableDependencies = value.Select(x => x.Id + " " + JsonConvert.SerializeObject(new AzureVersionSpec(x.VersionSpec)));
            }
        }

        [JsonIgnore]
        public IEnumerable<FrameworkName> SupportedFrameworks {
            get {
                return this.SerializableSupportedFrameworks == null
                           ? new FrameworkName[0]
                           : this.SerializableSupportedFrameworks.Select(x => new FrameworkName(x));
            }

            set {
                this.SerializableSupportedFrameworks = value.Select(x => x.FullName);
            }
        }

        [JsonIgnore]
        public FrameworkName TargetFramework {
            get {
                return string.IsNullOrWhiteSpace(this.SerializableTargetFramework)
                           ? null
                           : new FrameworkName(this.SerializableTargetFramework);
            }

            set {
                if (value != null) {
                    this.SerializableTargetFramework = value.FullName;
                }
            }
        }

        public string SerializableTargetFramework { get; set; }

        public IEnumerable<string> SerializableDependencies { get; set; }

        public IEnumerable<string> SerializableSupportedFrameworks { get; set; }

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