using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using NElasticsearch.Models;
using RestSharp;
using RestSharp.Deserializers;

namespace NElasticsearch
{
    public class ElasticsearchRestClient
    {
        private readonly RestClient _internalClient;

        public ElasticsearchRestClient(string elasticsearchUrl)
        {
            _internalClient = new RestClient(elasticsearchUrl);
            _internalClient.ClearHandlers();
            _internalClient.AddHandler("application/json", new JsonDeserializer());
            _internalClient.AddHandler("text/json", new JsonDeserializer());
            _internalClient.AddHandler("text/x-json", new JsonDeserializer());
            _internalClient.AddHandler("*", new JsonDeserializer());
        }

        public RestRequestAsyncHandle ExecuteAsync(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback)
        {
            throw new NotImplementedException();
        }

        public RestRequestAsyncHandle ExecuteAsync<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback)
        {
            throw new NotImplementedException();
        }

        public IRestResponse Execute(IRestRequest request)
        {
            return _internalClient.Execute(request);
        }

        public IRestResponse<T> Execute<T>(IRestRequest request) where T : new()
        {
            return _internalClient.Execute<T>(request);
        }

        public Uri BuildUri(IRestRequest request)
        {
            return _internalClient.BuildUri(request);
        }

        public RestRequestAsyncHandle ExecuteAsyncGet(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback, string httpMethod)
        {
            throw new NotImplementedException();
        }

        public RestRequestAsyncHandle ExecuteAsyncPost(IRestRequest request, Action<IRestResponse, RestRequestAsyncHandle> callback, string httpMethod)
        {
            throw new NotImplementedException();
        }

        public RestRequestAsyncHandle ExecuteAsyncGet<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback, string httpMethod)
        {
            throw new NotImplementedException();
        }

        public RestRequestAsyncHandle ExecuteAsyncPost<T>(IRestRequest request, Action<IRestResponse<T>, RestRequestAsyncHandle> callback, string httpMethod)
        {
            throw new NotImplementedException();
        }

        public IRestResponse ExecuteAsGet(IRestRequest request, string httpMethod)
        {
            return _internalClient.ExecuteAsGet(request, httpMethod);
        }

        public IRestResponse ExecuteAsPost(IRestRequest request, string httpMethod)
        {
            return _internalClient.ExecuteAsPost(request, httpMethod);
        }

        public IRestResponse<T> ExecuteAsGet<T>(IRestRequest request, string httpMethod) where T : new()
        {
            return _internalClient.ExecuteAsGet<T>(request, httpMethod);
        }

        public IRestResponse<T> ExecuteAsPost<T>(IRestRequest request, string httpMethod) where T : new()
        {
            return _internalClient.ExecuteAsPost<T>(request, httpMethod);
        }

        public Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse<T>> ExecuteGetTaskAsync<T>(IRestRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse<T>> ExecuteGetTaskAsync<T>(IRestRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse<T>> ExecutePostTaskAsync<T>(IRestRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse<T>> ExecutePostTaskAsync<T>(IRestRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse> ExecuteTaskAsync(IRestRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse> ExecuteTaskAsync(IRestRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse> ExecuteGetTaskAsync(IRestRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse> ExecuteGetTaskAsync(IRestRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse> ExecutePostTaskAsync(IRestRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<IRestResponse> ExecutePostTaskAsync(IRestRequest request, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public CookieContainer CookieContainer { get; set; }
        public string UserAgent { get; set; }
        public int Timeout { get; set; }
        public bool UseSynchronizationContext { get; set; }
        public IAuthenticator Authenticator { get; set; }
        public string BaseUrl { get; set; }
        public IList<Parameter> DefaultParameters { get; private set; }
        public X509CertificateCollection ClientCertificates { get; set; }
        public IWebProxy Proxy { get; set; }

        public string DefaultIndexName { get; set; }
        
        public GetResponse<T> Get<T>(string id, string typeName, string indexName = null) where T : new()
        {
            var request = new RestRequest(indexName ?? DefaultIndexName + "/" + typeName + "/{id}", Method.GET);
            request.AddUrlSegment("id", id);
            request.RequestFormat = DataFormat.Json;
            var response = Execute<GetResponse<T>>(request);
            // TODO post-processing
            if (response.StatusCode != HttpStatusCode.OK)
                return null;
            return response.Data;
        }

        public void Index<T>(T obj, string id, string typeName, string indexName = null)
        {
            var request =
                new RestRequest((indexName ?? DefaultIndexName) + "/" + typeName +
                                (!string.IsNullOrWhiteSpace(id) ? "/" + id : string.Empty), Method.POST);

            
            request.RequestFormat = DataFormat.Json;
            request.AddBody(obj);
            var response = Execute(request);
            // TODO check status
        }

        public void DeleteIndex(string indexName)
        {
            var request = new RestRequest(indexName, Method.DELETE);
            var response = Execute(request);
        }

        public void Refresh(string indexName = null)
        {
            var request = new RestRequest((indexName ?? DefaultIndexName) + "/_refresh", Method.POST);
            var response = Execute(request);
        }
    }
}
