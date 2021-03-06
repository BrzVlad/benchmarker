﻿using System;
using Octokit;
using System.Threading.Tasks;

namespace Benchmarker
{
	public static class GitHubInterface
	{
		static GitHubClient gitHubClient;
		public static string githubCredentials;

		public static GitHubClient GitHubClient {
			get {
				if (githubCredentials == null)
					throw new Exception ("client must set github credentials");

				if (gitHubClient != null)
					return gitHubClient;

				gitHubClient = new Octokit.GitHubClient (new Octokit.ProductHeaderValue ("XamarinBenchmark"));
				if (gitHubClient == null)
					throw new Exception ("Could not instantiate GitHub client");

				gitHubClient.Credentials = new Octokit.Credentials (githubCredentials);

				return gitHubClient;
			}
		}

		public async static Task<T> RunWithRetry<T> (Func<Task<T>> run, int numTries = 3) {
			for (var i = 0; i < numTries - 1; ++i) {
				try {
					return await run ();
				} catch (Exception exc) {
					if (exc is NotFoundException)
						throw exc;
					var seconds = (i == 0) ? 10 : 60 * i;
					Logging.GetLogging ().ErrorFormat ("Exception when running task - sleeping {0} seconds and retrying: {1}", seconds, exc);
					await Task.Delay (seconds * 1000);
				}
			}
			return await run ();
		}
	}
}
