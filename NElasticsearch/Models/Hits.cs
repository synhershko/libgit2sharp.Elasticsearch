using System.Collections.Generic;
using System.Diagnostics;

namespace NElasticsearch.Models
{
    /// <summary>
    /// Container of hit responses from ElasticSearch.
    /// </summary>
    [DebuggerDisplay("{hits.Count} hits of {total}")]
    public class Hits<T>
    {
        public long total { get; set; }
        public double? max_score { get; set; }
        public List<Hit<T>> hits { get; set; }
    }
}
