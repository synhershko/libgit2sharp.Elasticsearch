using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NElasticsearch;
using NElasticsearch.Commands;
using RestSharp.Extensions;
using GO = libgit2sharp.Elasticsearch.Models.GitObject;

namespace LibGit2Sharp.Elasticsearch
{
    public class ElasticsearchOdbBackend : OdbBackend
    {
        private readonly ConcurrentDictionary<string, GO> _cache = new ConcurrentDictionary<string, GO>(); 

        private readonly string _indexName;
        protected ElasticsearchRestClient client;
        private const string GitObjectsType = "gitobject";

        public ElasticsearchOdbBackend(string elasticsearchUrl, string indexName)
        {
            _indexName = indexName;

            client = new ElasticsearchRestClient(elasticsearchUrl)
            {
                DefaultIndexName = indexName
            };

            // TODO mappings
        }

        protected override void Dispose()
        {
            base.Dispose();
        }

        public override int Read(ObjectId id, out Stream data, out ObjectType objectType)
        {
            // TODO cache
            var response = client.Get<GO>(id.Sha, GitObjectsType);
            if (response == null || !response.found)
            {
                objectType = ObjectType.Blob;
                data = null;
                return (int) ReturnCode.GIT_ENOTFOUND;
            }

            var obj = response._source;
            objectType = obj.Type;

            data = Allocate(obj.Size);
            var bytes = obj.GetDataAsByteArray();
            data.Write(bytes, 0, bytes.Length);

            return (int) ReturnCode.GIT_OK;
        }

        private const int DefaultPageSize = 10;
        public override int ReadPrefix(string shortSha, out ObjectId id, out Stream data, out ObjectType objectType)
        {
            id = null;
            data = null;
            objectType = default(ObjectType);

            ObjectId matchingKey = null;
            bool moreThanOneMatchingKeyHasBeenFound = false;

            var query = new {constant_score = new {filter = new {prefix = new {Sha = shortSha}}}};
            var ret = ForEachInternal(query, objectId =>
            {
                if (matchingKey != null)
                {
                    moreThanOneMatchingKeyHasBeenFound = true;
                    return (int)ReturnCode.GIT_EAMBIGUOUS;
                }

                matchingKey = objectId;

                return (int)ReturnCode.GIT_OK;
            });

            if (ret != (int)ReturnCode.GIT_OK
                && ret != (int)ReturnCode.GIT_EUSER)
            {
                return ret;
            }

            if (moreThanOneMatchingKeyHasBeenFound)
            {
                return (int)ReturnCode.GIT_EAMBIGUOUS;
            }

            ret = Read(matchingKey, out data, out objectType);

            if (ret != (int)ReturnCode.GIT_OK)
            {
                return ret;
            }

            id = matchingKey;

            return (int)ReturnCode.GIT_OK;
        }

        public override int ReadHeader(ObjectId id, out int length, out ObjectType objectType)
        {
            // TODO check cache
            var response = client.Get<GO>(id.Sha, GitObjectsType);
            if (response == null || !response.found)
            {
                objectType = ObjectType.Blob;
                length = 0;
                return (int)ReturnCode.GIT_ENOTFOUND;
            }

            var obj = response._source;
            objectType = obj.Type;
            length = (int) obj.Size;

            return (int)ReturnCode.GIT_OK; 
        }

        public override int Write(ObjectId id, Stream dataStream, long length, ObjectType objectType)
        {
            client.Index(new GO
            {
                Size = length,
                Type = objectType,
                Sha = id.Sha,
                Data = Convert.ToBase64String(dataStream.ReadAsBytes()),
            }, id.Sha, GitObjectsType);

            return (int)ReturnCode.GIT_OK;
        }

        public override int ReadStream(ObjectId id, out OdbBackendStream stream)
        {
            throw new NotImplementedException("ReadStream");
        }

        public override int WriteStream(long length, ObjectType objectType, out OdbBackendStream stream)
        {
            stream = new ElasticsearchOdbBackendWriteOnlyStream(this, objectType, length);

            return (int)ReturnCode.GIT_OK;
        }

        public override bool Exists(ObjectId id)
        {
            // TODO cache
            var response = client.Get<GO>(id.Sha, GitObjectsType);
            return response != null && response.found;
        }

        private int ForEachInternal(object query, ForEachCallback callback)
        {
            var curPage = 0;
            var pageSize = 10;
            var collectedResults = 0;

            // TODO better and more stable paging strategy
            while (true)
            {
                var q = new
                {
                    query = query,
                    from = curPage * pageSize,
                    size = pageSize,
                    fields = new string[] { }, // force returning only the ID
                };

                var results = client.Search<GO>(q, client.DefaultIndexName, GitObjectsType);
                if (results == null) // TODO smarter error handling
                {
                    return (int)ReturnCode.GIT_OK;
                }

                if (results.hits.hits.Select(hit => callback(new ObjectId(hit._id))).Any(ret => ret != (int)ReturnCode.GIT_OK))
                {
                    return (int)ReturnCode.GIT_EUSER;
                }

                collectedResults += results.hits.hits.Count;
                if (results.hits.hits.Count < pageSize || collectedResults == results.hits.total)
                    break;

                curPage++;
            }

            return (int)ReturnCode.GIT_OK;
        }

        private const int DefaultFetchAllPageSize = 30;
        public override int ForEach(ForEachCallback callback)
        {
            return ForEachInternal(new {match_all = new {}}, callback);
        }

        protected override OdbBackendOperations SupportedOperations
        {
            get
            {
                return OdbBackendOperations.Read |
                       OdbBackendOperations.Write |
                       OdbBackendOperations.ReadPrefix |
                       OdbBackendOperations.Exists |
                       OdbBackendOperations.ForEach;
            }
        }

        [Serializable]
        private class ObjectDescriptor
        {
            public ObjectType ObjectType { get; set; }
            public long Length { get; set; }
        }

        private class ElasticsearchOdbBackendWriteOnlyStream : OdbBackendStream
        {
            private readonly List<byte[]> _chunks = new List<byte[]>();

            private readonly ObjectType _type;
            private readonly long _length;

            public ElasticsearchOdbBackendWriteOnlyStream(ElasticsearchOdbBackend backend, ObjectType objectType, long length)
                : base(backend)
            {
                _type = objectType;
                _length = length;
            }

            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return true;
                }
            }

            public override int Write(Stream dataStream, long length)
            {
                var buffer = new byte[length];

                int offset = 0, bytesRead;
                int toRead = Convert.ToInt32(length);

                do
                {
                    toRead -= offset;
                    bytesRead = dataStream.Read(buffer, offset, toRead);
                    offset += bytesRead;
                } while (bytesRead != 0);

                if (offset != (int)length)
                {
                    throw new InvalidOperationException(
                        string.Format("Too short buffer. {0} bytes were expected. {1} have been successfully read.",
                            length, bytesRead));
                }

                _chunks.Add(buffer);

                return (int)ReturnCode.GIT_OK;
            }

            public override int FinalizeWrite(ObjectId oid)
            {
                //TODO: Drop the check of the size when libgit2 #1837 is merged
                long totalLength = _chunks.Sum(chunk => chunk.Length);

                if (totalLength != _length)
                {
                    throw new InvalidOperationException(
                        string.Format("Invalid object length. {0} was expected. The "
                                      + "total size of the received chunks amounts to {1}.",
                                      _length, totalLength));
                }

                using (Stream stream = new FakeStream(_chunks, _length))
                {
                    Backend.Write(oid, stream, _length, _type);
                }

                return (int)ReturnCode.GIT_OK;
            }

            public override int Read(Stream dataStream, long length)
            {
                throw new NotImplementedException();
            }

            private class FakeStream : Stream
            {
                private readonly IList<byte[]> _chunks;
                private readonly long _length;
                private int currentChunk = 0;
                private int currentPos = 0;

                public FakeStream(IList<byte[]> chunks, long length)
                {
                    _chunks = chunks;
                    _length = length;
                }

                public override void Flush()
                {
                    throw new NotImplementedException();
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    throw new NotImplementedException();
                }

                public override void SetLength(long value)
                {
                    throw new NotImplementedException();
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    var totalCopied = 0;

                    while (totalCopied < count)
                    {
                        if (currentChunk > _chunks.Count - 1)
                        {
                            return totalCopied;
                        }

                        var toBeCopied = Math.Min(_chunks[currentChunk].Length - currentPos, count - totalCopied);

                        Buffer.BlockCopy(_chunks[currentChunk], currentPos, buffer, offset + totalCopied, toBeCopied);
                        currentPos += toBeCopied;
                        totalCopied += toBeCopied;

                        Debug.Assert(currentPos <= _chunks[currentChunk].Length);

                        if (currentPos == _chunks[currentChunk].Length)
                        {
                            currentPos = 0;
                            currentChunk++;
                        }
                    }

                    return totalCopied;
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    throw new NotImplementedException();
                }

                public override bool CanRead
                {
                    get { return true; }
                }

                public override bool CanSeek
                {
                    get { throw new NotImplementedException(); }
                }

                public override bool CanWrite
                {
                    get { throw new NotImplementedException(); }
                }

                public override long Length
                {
                    get { return _length; }
                }

                public override long Position
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }
            }
        }
    }
}