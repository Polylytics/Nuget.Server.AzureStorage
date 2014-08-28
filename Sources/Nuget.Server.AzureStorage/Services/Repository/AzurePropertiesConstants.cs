namespace Nuget.Server.AzureStorage.Services.Repository {
    /// <summary>
    ///     Constants for the Azure properties metadata
    /// </summary>
    internal sealed class AzurePropertiesConstants {
        /// <summary>
        ///     The name of the property holding the created date
        /// </summary>
        public const string Created = "Creted";

        /// <summary>
        ///     The name of the property holding the latest modification date
        /// </summary>
        public const string LatestModificationDate = "LastModified";

        /// <summary>
        ///     The name of the property holding the last uploaded version
        /// </summary>
        public const string LastUploadedVersion = "LastVersion";

        /// <summary>
        ///     The name of the property holding the last accessed
        /// </summary>
        public const string LastAccessed = "LastAccessed";
        
        /// <summary>
        ///     The name of the property holding the package id
        /// </summary>
        public const string PackageId = "PackageId";
    }
}