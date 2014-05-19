using System.Diagnostics;
using System.Net;

namespace NElasticsearch.Models
{
    /// <summary>
    /// Top-level response from ElasticSearch.
    /// </summary>
    [DebuggerDisplay("{hits.hits.Count} hits in {took} ms")]
    public class SearchResponse<T>
    {
        public int took { get; set; }
        public bool timed_out { get; set; }
        public ShardStatistics _shards { get; set; }
        public Hits<T> hits { get; set; }

        //public JValue error;
        public HttpStatusCode status { get; set; }
        //public JObject facets;
    }
}
