namespace UmbracoFileSystemProviders.RackspaceCloudFiles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using net.openstack.Core.Domain;
    using net.openstack.Core.Exceptions.Response;
    using net.openstack.Providers.Rackspace;
    using Umbraco.Core;
    using Umbraco.Core.Cache;
    using Umbraco.Core.IO;
    using Umbraco.Core.Logging;

    public class CloudFilesFileSystem : IFileSystem
    {
        protected const string Delimiter = "/";

        private readonly string _apiKey;
        private readonly string _username;
        private readonly string _container;
        private readonly string _urlProtocol;

        public CloudFilesFileSystem(string apiKey, string username, string container, string urlProtocol)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "apiKey cannot be null");
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username), "username cannot be null");
            }

            if (string.IsNullOrEmpty(container))
            {
                throw new ArgumentNullException(nameof(container), "containerName cannot be null");
            }

            _apiKey = apiKey;
            _username = username;
            _container = container;
            _urlProtocol = urlProtocol;
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            var provider = GetProvider();
            return provider.ListObjects(_container, prefix: ResolvePath(provider, path))
                .Select(x => x.Name.Split('/').First())
                .Distinct();
        }

        public void DeleteDirectory(string path)
        {
            DeleteDirectory(path, false);

        }

        public void DeleteDirectory(string path, bool recursive)
        {
            var provider = GetProvider();
            var directoryContents = provider.ListObjects(_container, prefix: ResolvePath(provider, path))
                .Select(x => x.Name)
                .ToList();

            foreach (var directoryPath in directoryContents)
            {
                LogHelper.Info<CloudFilesFileSystem>($"Deleting directory in Rackspace cloud files, container: {_container}, path: {directoryPath}");
                provider.DeleteObject(_container, directoryPath);
                LogHelper.Info<CloudFilesFileSystem>($"Deleted directory in Rackspace cloud files, container: {_container}, path: {directoryPath}");
            }

            // Delete "directory" itself (fail silently if already removed)
            if (path.EndsWith(Delimiter))
            {
                path = path.Substring(0, path.Length - 1);
            }

            try
            {
                provider.DeleteObject(_container, path);
            }
            catch (ItemNotFoundException)
            {
            }
        }

        public bool DirectoryExists(string path)
        {
            var provider = GetProvider();
            return provider.ListObjects(_container, prefix: ResolvePath(provider, path))
                .Any();
        }

        public void AddFile(string path, Stream stream)
        {
            AddFile(path, stream, true);
        }

        public void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            var provider = GetProvider();
            if (overrideIfExists || FileExists(path) == false)
            {
                LogHelper.Info<CloudFilesFileSystem>($"Adding file in Rackspace cloud files, container: {_container}, path: {path}");
                provider.CreateObject(_container, stream, ResolvePath(provider, path));
                LogHelper.Info<CloudFilesFileSystem>($"Added file in Rackspace cloud files, container: {_container}, path: {path}");
            }
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, string.Empty);
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            var provider = GetProvider();
            return provider.ListObjects(_container, prefix: ResolvePath(provider, path))
                .Select(x => x.Name);
        }

        public Stream OpenFile(string path)
        {
            var provider = GetProvider();
            var stream = new MemoryStream();
            provider.GetObject(_container, ResolvePath(provider, path), stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public void DeleteFile(string path)
        {
            var provider = GetProvider();
            LogHelper.Info<CloudFilesFileSystem>($"Deleting file in Rackspace cloud files, container: {_container}, path: {path}");
            provider.DeleteObject(_container, ResolvePath(provider, path));
            LogHelper.Info<CloudFilesFileSystem>($"Deleted file in Rackspace cloud files, container: {_container}, path: {path}");
        }

        public bool FileExists(string path)
        {
            var provider = GetProvider();
            try
            {
                provider.GetObjectHeaders(_container, ResolvePath(provider, path));
                return true;
            }
            catch (ItemNotFoundException)
            {
                return false;
            }
        }

        public string GetRelativePath(string fullPathOrUrl)
        {
            if (string.IsNullOrEmpty(fullPathOrUrl))
            {
                return string.Empty;
            }

            if (fullPathOrUrl.StartsWith(Delimiter))
            {
                fullPathOrUrl = fullPathOrUrl.Substring(1);
            }

            var provider = GetProvider();
            var containerUrl = GetContainerUrl(provider);
            if (fullPathOrUrl.StartsWith(containerUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                fullPathOrUrl = fullPathOrUrl.Substring(containerUrl.Length);
            }

            return fullPathOrUrl;
        }

        public string GetFullPath(string path)
        {
            return path;
        }

        public string GetUrl(string path)
        {
            var provider = GetProvider();
            var containerUrl = GetContainerUrl(provider);
            return string.Concat(containerUrl, ResolvePath(provider, path));
        }

        public DateTimeOffset GetLastModified(string path)
        {
            var provider = GetProvider();
            var headers = provider.GetObjectHeaders(_container, ResolvePath(provider, path));
            var lastModifiedHeader = headers["Last-Modified"];
            return DateTimeOffset.Parse(lastModifiedHeader);
        }

        public DateTimeOffset GetCreated(string path)
        {
            // No header for created, so return last modifiled
            return GetLastModified(path);
        }

        private CloudFilesProvider GetProvider()
        {
            var identity = new CloudIdentity
            {
                APIKey = _apiKey,
                Username = _username
            };

            return new CloudFilesProvider(identity);
        }

        private string GetContainerUrl(CloudFilesProvider provider)
        {
            return ApplicationContext.Current.ApplicationCache.StaticCache
                .GetCacheItem<string>("UmbracoFileSystemProviders.RackspaceCloudFiles.ContainerUrl",
                    () =>
                    {
                        var container = provider.GetContainerCDNHeader(_container);
                        var containerUrl = _urlProtocol.ToLowerInvariant() == "https" ? container.CDNSslUri : container.CDNUri;
                        if (containerUrl.EndsWith(Delimiter) == false)
                        {
                            containerUrl = containerUrl + Delimiter;
                        }

                        return containerUrl;
                    });
        }

        private string ResolvePath(CloudFilesProvider provider, string path)
        {
            if (path == null)
            {
                path = string.Empty;
            }

            if (path.Contains("//"))
            {
                var containerUrl = GetContainerUrl(provider);
                path = path.Replace(containerUrl, string.Empty);
            }

            if (path.StartsWith(Delimiter))
            {
                path = path.Substring(1);
            }

            path = path.Replace("\\", Delimiter);

            return path;
        }
    }
}