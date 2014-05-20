using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Elasticsearch;
using NElasticsearch;

namespace libgit2sharp.Elasticsearch.Tests
{
    public class ElasticsearchOdbBackendForTests : ElasticsearchOdbBackend
    {
        public ElasticsearchOdbBackendForTests(string elasticsearchUrl, string indexName) : base(elasticsearchUrl, indexName)
        {
        }

        protected override void Dispose()
        {
            client.DeleteIndex(client.DefaultIndexName);
            base.Dispose();
        }

        public override int Write(ObjectId id, Stream dataStream, long length, ObjectType objectType)
        {
            var ret = base.Write(id, dataStream, length, objectType);
            client.Refresh(); // make the write available for all searches immediately
            return ret;
        }

        public ElasticsearchRestClient Client { get { return client; } }
    }
}
