libgit2sharp.Elasticsearch
==========================

Elasticsearch backend for git repositories

Use Elasticsearch to store your git repositories and make them highly-available, or use the power of git to make the most out of your document store

## Usage

To run Elasticsearch locally follow these instructions:

1. Download from http://elasticsearch.org/download
2. Go edit config\elasticsearch.yml and edit:
	* `cluster.name` to something non-default. Your GitHub username will do.
	* replica and shard count, by specifing `index.number_of_shards: 1` and `index.number_of_replicas: 0` (unless you know what you are doing)
3. Make sure you have JAVA_HOME properly set up
4. Run `elasticsearch\bin\elasticsearch.bat`
