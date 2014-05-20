using System.Net;
using System.Text;
using NElasticsearch.Models;
using RestSharp;

namespace NElasticsearch.Commands
{
    public static class SearchCommand
    {
        public static SearchResponse<T> Search<T>(this ElasticsearchRestClient client,
            object query, string indexName = null, string typeName = null) where T : new()
        {
            var response = client.Execute<SearchResponse<T>>(GetSearchRequest(query, indexName, typeName));
            // TODO post-processing
            if (response.StatusCode != HttpStatusCode.OK)
                return null;
            return response.Data;
        }

        public static string Search(this ElasticsearchRestClient client,
            object query, string indexName = null, string typeName = null)
        {
            var response = client.Execute(GetSearchRequest(query, indexName, typeName));
            // TODO post-processing
            if (response.StatusCode != HttpStatusCode.OK)
                return null;
            return response.Content;
        }

        private static RestRequest GetSearchRequest(object query, string indexName = null, string typeName = null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(indexName))
            {
                sb.Append(indexName);
                sb.Append('/');
            }
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                sb.Append(typeName);
                sb.Append('/');
            }
            sb.Append("_search");

            if (query == null)
                return new RestRequest(sb.ToString(), Method.GET);

            var request = new RestRequest(sb.ToString(), Method.POST)
            {
                RequestFormat = DataFormat.Json,
            };
            request.AddBody(query);
            return request;
        }
    }
}
