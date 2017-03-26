namespace UmbracoFileSystemProviders.RackspaceCloudFiles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using net.openstack.Core.Domain;
    using net.openstack.Core.Exceptions.Response;
    using net.openstack.Providers.Rackspace;
    using Umbraco.Core.IO;

    public class CloudFilesFileSystem : IFileSystem
    {
        private readonly string _apiKey;
        private readonly string _username;
        private readonly string _container;

        public CloudFilesFileSystem(string apiKey, string username, string container)
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
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            if (path == null)
            {
                path = string.Empty;
            }

            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }

            var provider = GetProvider();
            return provider.ListObjects(_container, prefix: path)
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
            var directoryContents = provider.ListObjects(_container, prefix: path)
                .Select(x => x.Name)
                .ToList();

            foreach (var directoryPath in directoryContents)
            {
                provider.DeleteObject(_container, directoryPath);
            }

            // Delete "directory" itself (fail silently if already removed)
            if (path.EndsWith("/"))
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
            return provider.ListObjects(_container, prefix: path)
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
                provider.CreateObject(_container, stream, path);
            }
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, string.Empty);
        }

        public IEnumerable<string> GetFiles(string path, string filter)
        {
            var provider = GetProvider();
            return provider.ListObjects(_container, prefix: path)
                .Select(x => x.Name);
        }

        public Stream OpenFile(string path)
        {
            var provider = GetProvider();
            var stream = new MemoryStream();
            provider.GetObject(_container, path, stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public void DeleteFile(string path)
        {
            var provider = GetProvider();
            provider.DeleteObject(_container, path);
        }

        public bool FileExists(string path)
        {
            var provider = GetProvider();
            try
            {
                provider.GetObjectHeaders(_container, path);
                return true;
            }
            catch (ItemNotFoundException)
            {
                return false;
            }
        }

        public string GetRelativePath(string fullPathOrUrl)
        {
            throw new NotImplementedException();
        }

        public string GetFullPath(string path)
        {
            return path;
        }

        public string GetUrl(string path)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset GetLastModified(string path)
        {
            var provider = GetProvider();
            var headers = provider.GetObjectHeaders(_container, path);
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
    }
}