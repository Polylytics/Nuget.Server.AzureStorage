﻿namespace Nuget.Server.AzureStorage.Services.Locator {
    using NuGet;

    /// <summary>
    ///     Helps to get azure names.
    /// </summary>
    public interface IPackageLocator {
        /// <summary>
        ///     Gets the name of the container.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns>Azure redable name of the container</returns>
        string GetContainerName(IPackage package);

        /// <summary>
        ///     Gets the name of the item.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <returns>Azrure redable name for item in the container</returns>
        string GetItemName(IPackage package);
    }
}