namespace BackendPackage.Runtime.Core
{
    public interface IBackendPackageInitializable
    {
        void Initialize(BackendPackageConfig config);
    }
}
