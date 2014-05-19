using LibGit2Sharp.Elasticsearch;
using NElasticsearch;

namespace libgit2sharp.Elasticsearch.Tests
{
    public class ElasticsearchOdbBackendWithCleanup : ElasticsearchOdbBackend
    {
        public ElasticsearchOdbBackendWithCleanup(string elasticsearchUrl, string indexName) : base(elasticsearchUrl, indexName)
        {
        }

        protected override void Dispose()
        {
            client.DeleteIndex(client.DefaultIndexName);
            base.Dispose();
        }

        public ElasticsearchRestClient Client { get { return client; } }
    }
}
