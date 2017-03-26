namespace UmbracoFileSystemProviders.RackspaceCloudFiles.IntegrationTests
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Umbraco.Core.IO;

    [TestClass]
    [DeploymentItem("TestFile\flower.jpg")]
    [DeploymentItem("TestFile\flower2.jpg")]
    public class CloudFilesFileSystemTests
    {
        private readonly string _apiKey = ConfigurationManager.AppSettings["ApiKey"];
        private readonly string _username = ConfigurationManager.AppSettings["Username"];
        private readonly string _container = ConfigurationManager.AppSettings["Container"];
        private readonly string _containerUrl = ConfigurationManager.AppSettings["ContainerUrl"];

        private const string TestFile1 = "TestFile\\flower.jpg";
        private const string TestFile2 = "TestFile\\flower2.jpg";

        [TestMethod]
        public void GetDirectoriesTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var path1 = "1010/flower.jpg";
            var path2 = "1011/flower2.jpg";
            EnsureTestFileAdded(fs, path1);
            EnsureTestFileAdded(fs, path2);

            // Act
            var result = fs.GetDirectories("/").ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("1010", result.First());
            Assert.AreEqual("1011", result.Last());
        }

        [TestMethod]
        public void DeleteDirectoryTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var directory = "1010/";
            var path1 = "1010/flower.jpg";
            var path2 = "1010/flower2.jpg";
            EnsureTestFileAdded(fs, path1);
            EnsureTestFileAdded(fs, path2);

            // Act
            fs.DeleteDirectory(directory);

            // Assert
            Assert.IsFalse(fs.DirectoryExists(directory));
        }

        [TestMethod]
        public void DirectoryExistsTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var directory = "1010/";
            var path1 = "1010/flower.jpg";
            EnsureTestFileAdded(fs, path1);

            // Act
            var result = fs.DirectoryExists(directory);
            var result2 = fs.DirectoryExists("notthere/");

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void AddFileTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var path = "1010/flower.jpg";

            // Act
            EnsureTestFileAdded(fs, path);

            // Assert
            Assert.IsTrue(fs.FileExists(path));
        }

        [TestMethod]
        public void OpenFileTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var path = "1010/flower.jpg";
            EnsureTestFileAdded(fs, path);

            // Act
            var result = fs.OpenFile(path);

            // Assert
            var testFilePath = Path.Combine(Environment.CurrentDirectory, TestFile1);
            long expectedLength;
            using (var stream = File.Open(testFilePath, FileMode.Open))
            {
                expectedLength = stream.Length;
            }

            Assert.AreEqual(expectedLength, result.Length);
        }

        [TestMethod]
        public void DeleteFileTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var path = "1010/flower.jpg";
            EnsureTestFileAdded(fs, path);

            // Act
            fs.DeleteFile(path);

            // Assert
            Assert.IsFalse(fs.FileExists(path));
        }

        [TestMethod]
        public void GetFilesTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var directory = "1010/";
            var path1 = "1010/flower.jpg";
            var path2 = "1010/flower2.jpg";
            var path3 = "1011/flower.jpg";
            EnsureTestFileAdded(fs, path1);
            EnsureTestFileAdded(fs, path2);
            EnsureTestFileAdded(fs, path3);

            // Act
            var result = fs.GetFiles(directory).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("1010/flower.jpg", result.First());
            Assert.AreEqual("1010/flower2.jpg", result.Last());
        }

        [TestMethod]
        public void FileExistsTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var path = "1010/flower.jpg";
            EnsureTestFileAdded(fs, path);

            // Act
            var result = fs.FileExists(path);
            var result2 = fs.FileExists(path.Replace("flower", "notthere"));

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void GetUrlTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var path = "1010/flower.jpg";
            EnsureTestFileAdded(fs, path);

            // Act
            var result = fs.GetUrl(path);

            // Assert
            Assert.AreEqual(_containerUrl + "/1010/flower.jpg", result);
        }

        [TestMethod]
        public void GetLastModifiedTest()
        {
            // Arrange
            var fs = CreateFileSystem();
            var path = "1010/flower.jpg";
            EnsureTestFileAdded(fs, path);

            // Act
            var result = fs.GetLastModified(path);

            // Assert
            var today = DateTime.Now;
            Assert.AreEqual(today.Year, result.Year);
            Assert.AreEqual(today.Month, result.Month);
            Assert.AreEqual(today.Day, result.Day);
        }

        private IFileSystem CreateFileSystem()
        {
            return new CloudFilesFileSystem(_apiKey, _username, _container, "https");
        }

        private static void EnsureTestFileAdded(IFileSystem fs, string path)
        {
            var testFilePath = Path.Combine(Environment.CurrentDirectory, TestFile1);
            using (var stream = File.Open(testFilePath, FileMode.Open))
            {
                fs.AddFile(path, stream, true);
            }
        }
    }
}
