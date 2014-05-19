using System.Diagnostics;

namespace NElasticsearch.Models
{
    /// <summary>
    /// Shard statistics response from ElasticSearch.
    /// </summary>
    [DebuggerDisplay("{failed} failed, {successful} success")]
    public class ShardStatistics
    {
        public int total { get; set; }
        public int successful { get; set; }
        public int failed { get; set; }
    }
}
