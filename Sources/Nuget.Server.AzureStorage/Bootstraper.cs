namespace Nuget.Server.AzureStorage {
    using AutoMapper;

    using NuGet;
    using NuGet.Server;

    using Nuget.Server.AzureStorage.Domain.Services;
    using Nuget.Server.AzureStorage.Doman.Entities;

    using NuGet.Server.Infrastructure;

    public static class Bootstraper {
        public static void SetUp() {
            NinjectBootstrapper.Kernel.Rebind<IServerPackageRepository>()
                               .To<AzureServerPackageRepository>();
            NinjectBootstrapper.Kernel.Bind<AzureServerPackageRepository>()
                               .To<AzureServerPackageRepository>();
            NinjectBootstrapper.Kernel.Rebind<IPackageService>()
                               .To<AzurePackageService>();
            NinjectBootstrapper.Kernel.Bind<IPackageLocator>()
                               .To<AzurePackageLocator>();
            NinjectBootstrapper.Kernel.Bind<IAzurePackageSerializer>()
                               .To<AzurePackageSerializer>();

            Mapper.CreateMap<IPackage, AzurePackage>();
            Mapper.CreateMap<PackageDependencySet, AzurePackageDependencySet>();
        }
    }
}