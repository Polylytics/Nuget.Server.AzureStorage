namespace Nuget.Server.AzureStorage.Services.Package {
    using System.Net;
    using System.Web;
    using System.Web.Routing;

    using NuGet;
    using NuGet.Server;

    using Nuget.Server.AzureStorage.Services.Repository;

    using NuGet.Server.Infrastructure;

    /// <summary>
    ///     Wrapper around the PackageService to provide download from Azure Storage
    /// </summary>
    internal class AzurePackageService : IPackageService {
        /// <summary>
        ///     The package repository.
        /// </summary>
        private readonly AzureServerPackageRepository repository;

        /// <summary>
        ///     The package service.
        /// </summary>
        private readonly IPackageService packageService;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AzurePackageService" /> class.
        /// </summary>
        /// <param name="repository">
        ///     Repository of packages
        /// </param>
        /// <param name="authenticationService">
        ///     Authentication service
        /// </param>
        public AzurePackageService(AzureServerPackageRepository repository, IPackageAuthenticationService authenticationService) {
            this.repository = repository;
            this.packageService = new PackageService(repository, authenticationService);
        }

        /// <summary>
        ///     Create a package
        /// </summary>
        /// <param name="context">
        ///     The context.
        /// </param>
        public void CreatePackage(HttpContextBase context) {
            this.packageService.CreatePackage(context);
        }

        /// <summary>
        ///     Publish a package
        /// </summary>
        /// <param name="context">
        ///     The context.
        /// </param>
        public void PublishPackage(HttpContextBase context) {
            this.packageService.PublishPackage(context);
        }

        /// <summary>
        ///     Delete a package
        /// </summary>
        /// <param name="context">
        ///     The context.
        /// </param>
        public void DeletePackage(HttpContextBase context) {
            this.packageService.DeletePackage(context);
        }

        /// <summary>
        ///     Download a package.
        /// </summary>
        /// <param name="context">
        ///     The context.
        /// </param>
        public void DownloadPackage(HttpContextBase context) {
            // get the route data
            var routeData = RouteTable.Routes.GetRouteData(context);
            if (routeData == null) {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.StatusDescription = "Bad Request";
                return;
            }

            // get the package data from the route
            var packageId = routeData.GetRequiredString("packageId");
            var version = new SemanticVersion(routeData.GetRequiredString("version"));

            // retrieve the package
            var requestedPackage = this.repository.FindPackage(packageId, version);
            if (requestedPackage == null) {
                // Package not found
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.StatusDescription = string.Format("'Package {0} {1}' Not found.", packageId, version);
                return;
            }

            // send the data
            var blob = this.repository.GetBlobForPackage(requestedPackage);
            context.Response.AddHeader("content-disposition", string.Format("attachment; filename={0}-{1}.nupkg", requestedPackage.Id, requestedPackage.Version));
            context.Response.ContentType = "application/zip";
            blob.DownloadToStream(context.Response.OutputStream);
        }
    }
}