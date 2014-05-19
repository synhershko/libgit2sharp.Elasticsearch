using System;
using System.IO;
using System.Linq;
using System.Text;
using libgit2sharp.Elasticsearch.Tests;
using Xunit;
using Xunit.Extensions;

namespace LibGit2Sharp.Elasticsearch.Tests
{
    public class OdbBackendFixture : IDisposable
    {
        private static readonly string TempPath = Path.Combine(Path.GetTempPath(), "Elasticsearch.OdbBackend");

        public OdbBackendFixture()
        {
            Directory.CreateDirectory(TempPath);
        }

        public void Dispose()
        {
            Helpers.DeleteDirectory(TempPath);
        }

        private static Repository InitNewRepository(string baseDir)
        {
            string tempPath = Path.Combine(baseDir, Guid.NewGuid().ToString());
            string path = Repository.Init(tempPath);

            var repository = new Repository(path);

            return repository;
        }

        private Repository Build(bool useElasticsearch)
        {
            var repository = InitNewRepository(TempPath);

            if (!useElasticsearch)
            {
                return repository;
            }

            SetElasticsearchOdbBackend(repository, IndexNameFrom(repository));

            return repository;
        }

        private static void SetElasticsearchOdbBackend(Repository repository, string repoName)
        {
            repository.ObjectDatabase.AddBackend(new ElasticsearchOdbBackendWithCleanup(@"http://localhost:9200", repoName), priority: 5);
        }

        private static string IndexNameFrom(Repository repository)
        {
            var dir = new DirectoryInfo(repository.Info.WorkingDirectory);
            return dir.Name.Substring(0, 7);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanStageAFileAndLookupTheGeneratedBlob(bool useElasticsearch)
        {
            using (var repo = Build(useElasticsearch))
            {
                const string path = "test.txt";
                const string content = "test\n";

                var objectId = new ObjectId("9daeafb9864cf43055ae93beb0afd6c7d144bfa4");

                Assert.False(repo.ObjectDatabase.Contains(objectId));
                Assert.Null(repo.Lookup<Blob>(objectId));

                Helpers.Touch(repo.Info.WorkingDirectory, path, content);
                repo.Index.Stage(path);

                var ie = repo.Index[path];
                Assert.NotNull(ie);

                Assert.True(repo.ObjectDatabase.Contains(ie.Id));

                //TODO: Maybe lookup of a blob should only trigger read_header()
                var blob = repo.Lookup<Blob>(objectId);
                Assert.Equal(content.Length, blob.Size);
                Assert.Equal(content, blob.GetContentText());
            }   
        }

        private static void AddCommitToRepo(Repository repo)
        {
            const string path = "test.txt";
            const string content = "test\n";

            Helpers.Touch(repo.Info.WorkingDirectory, path, content);
            repo.Index.Stage(path);

            var author = new Signature("nulltoken", "emeric.fermas@gmail.com", DateTimeOffset.Parse("Wed, Dec 14 2011 08:29:03 +0100"));
            repo.Commit("Initial commit", author, author);
        }

        private static void AssertGeneratedShas(Repository repo)
        {
            Commit commit = repo.Commits.Single();
            Assert.Equal("1fe3126578fc4eca68c193e4a3a0a14a0704624d", commit.Sha);
            Tree tree = commit.Tree;
            Assert.Equal("2b297e643c551e76cfa1f93810c50811382f9117", tree.Sha);

            GitObject blob = tree.Single().Target;
            Assert.IsAssignableFrom<Blob>(blob);
            Assert.Equal("9daeafb9864cf43055ae93beb0afd6c7d144bfa4", blob.Sha);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanGeneratePredictableObjectShas(bool useElasticsearch)
        {
            using (var repo = Build(useElasticsearch))
            {
                AddCommitToRepo(repo);

                AssertGeneratedShas(repo);
            }
        }

        private static Blob CreateBlob(Repository repo, string content)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return repo.ObjectDatabase.CreateBlob(stream);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanCreateLargeBlobs(bool useElasticsearch)
        {
            using (var repo = Build(useElasticsearch))
            {
                var zeros = new string('0', 128 * 1024 + 3);

                var objectId = new ObjectId("3e7b4813e7b08195c7f59ca8efb6069fc9cf21a7");

                Blob blob = CreateBlob(repo, zeros);
                Assert.Equal(objectId, blob.Id);

                Assert.True(repo.ObjectDatabase.Contains(objectId));

                blob = repo.Lookup<Blob>(objectId);
                Assert.Equal(zeros, blob.GetContentText());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanEnumerateGitObjects(bool useElasticsearch)
        {
            using (var repo = Build(useElasticsearch))
            {
                AddCommitToRepo(repo);

                Assert.Equal(3, repo.ObjectDatabase.Count());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanLookupByShortObjectId(bool useElasticsearch)
        {
            /*
             * $ echo "aabqhq" | git hash-object -t blob --stdin
             * dea509d0b3cb8ee0650f6ca210bc83f4678851ba
             * 
             * $ echo "aaazvc" | git hash-object -t blob --stdin
             * dea509d097ce692e167dfc6a48a7a280cc5e877e
             */

            using (var repo = Build(useElasticsearch))
            {
                Blob blob1 = CreateBlob(repo, "aabqhq\n");
                Assert.Equal("dea509d0b3cb8ee0650f6ca210bc83f4678851ba", blob1.Sha);
                Blob blob2 = CreateBlob(repo, "aaazvc\n");
                Assert.Equal("dea509d097ce692e167dfc6a48a7a280cc5e877e", blob2.Sha);

                Assert.Equal(2, repo.ObjectDatabase.Count());

                Assert.Throws<AmbiguousSpecificationException>(() => repo.Lookup("dea509d0"));
                Assert.Throws<AmbiguousSpecificationException>(() => repo.Lookup("dea509d"));

                Assert.Equal(blob1, repo.Lookup("dea509d0b"));
                Assert.Equal(blob2, repo.Lookup("dea509d09"));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanFetch(bool useElasticsearch)
        {
            using (var repo = Build(useElasticsearch))
            {
                Assert.Equal(0, repo.ObjectDatabase.Count());

                repo.Network.Remotes.Add("origin", "https://github.com/libgit2/TestGitRepository");
                repo.Fetch("origin");

                Assert.Equal(69, repo.ObjectDatabase.Count());
            }
        }

        [Fact]
        public void CanReopenElasticsearchBackedRepository()
        {
            string indexName;
            string gitDirPath;

            const string blobSha = "dea509d0b3cb8ee0650f6ca210bc83f4678851ba";

            using (var repo = Build(useElasticsearch: true))
            {
                Blob blob = CreateBlob(repo, "aabqhq\n");
                Assert.Equal(blobSha, blob.Sha);

                indexName = IndexNameFrom(repo);
                gitDirPath = repo.Info.Path;
            }

            using (var repo = new Repository(gitDirPath))
            {
                Assert.Null(repo.Lookup<Blob>(blobSha));

                SetElasticsearchOdbBackend(repo, indexName);

                var blob = repo.Lookup<Blob>(blobSha);
                Assert.Equal(blobSha, blob.Sha);
            }
        }
    }
}