using System;
using AutoyaFramework.Connections.HTTP;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using AutoyaFramework.Settings.Auth;

namespace AutoyaFramework {
	/*
		http request methods.
	*/
	public enum HttpMethod {
		Get,
		Post,
		Put,
		Delete
	}

	/**
		authenticated http feature.
	*/
	public partial class Autoya {
		private HTTPConnection _autoyaHttp;
		
		/*
			public HTTP APIs.
		*/

		// respose by string
		public static string Http_Get (
			string url, 
			Action<string, string> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Get, url, additionalHeader, string.Empty);
			
			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Get(
					connectionId,
					headers,
					url,
					(string conId, int code, Dictionary<string, string> responseHeader, string resultData) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty, 
							(_conId, _data) => {
								var stringData = _data as string;
								var reason = string.Empty;
								var validated = autoya.OnValidateHttpResponse(HttpMethod.Get, url, responseHeader, stringData, out reason);
								if (!validated) {
									failed(connectionId, code, reason, new AutoyaStatus());
									return;
								}
								
								succeeded(_conId, stringData);
							},
							failed
						);
					},
					(conId, code, reason, responseHeader) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => {}, failed);
					},
					timeoutSec
				)
			);
			
			return connectionId;
		}

		public static string Http_Post (
			string url, 
			string data,
			Action<string, string> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Post, url, additionalHeader, data);
			
			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Post(
					connectionId,
					headers,
					url,
					data,
					(string conId, int code, Dictionary<string, string> responseHeader, string resultData) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
							(_conId, _data) => {
								var stringData = _data as string;
								var message = string.Empty;
								var validated = autoya.OnValidateHttpResponse(HttpMethod.Post, url, responseHeader, stringData, out message);
								if (!validated) {
									failed(connectionId, code, message, new AutoyaStatus(false, false, true));
									return;
								}
								
								succeeded(_conId, stringData);
							},
							failed
						);
					},
					(conId, code, reason, responseHeader) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => {}, failed);
					},
					timeoutSec
				)
			);

			return connectionId;
		}

		public static string Http_Put (
			string url, 
			string data,
			Action<string, string> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Put, url, additionalHeader, data);
			
			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Put(
					connectionId,
					headers,
					url,
					data,
					(string conId, int code, Dictionary<string, string> responseHeader, string resultData) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
							(_conId, _data) => {
								var stringData = _data as string;
								var reason = string.Empty;
								var validated = autoya.OnValidateHttpResponse(HttpMethod.Put, url, responseHeader, stringData, out reason);
								if (!validated) {
									failed(connectionId, code, reason, new AutoyaStatus());
									return;
								}
								
								succeeded(_conId, stringData);
							},
							failed
						);
					},
					(conId, code, reason, responseHeader) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => {}, failed);
					},
					timeoutSec
				)
			);

			return connectionId;
		}

		public static string Http_Delete (
			string url, 
			string data,
			Action<string, string> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}

			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Delete, url, additionalHeader, data);
			
			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Delete(
					connectionId,
					headers,
					url,
					(string conId, int code, Dictionary<string, string> responseHeader, string resultData) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
							(_conId, _data) => {
								var stringData = _data as string;
								var reason = string.Empty;
								var validated = autoya.OnValidateHttpResponse(HttpMethod.Delete, url, responseHeader, stringData, out reason);
								if (!validated) {
									failed(connectionId, code, reason, new AutoyaStatus());
									return;
								}
								
								succeeded(_conId, stringData);
							}, 
							failed
						);
					},
					(conId, code, reason, responseHeader) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => {}, failed);
					},
					timeoutSec
				)
			);

			return connectionId;
		}


		// response by byte[]
		public static string Http_Get (
			string url, 
			Action<string, byte[]> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Get, url, additionalHeader, string.Empty);
			
			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Get(
					connectionId,
					headers,
					url,
					(string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty, 
							(_conId, _data) => {
								var byteData = _data as byte[];
								var reason = string.Empty;
								var validated = autoya.OnValidateHttpResponse(HttpMethod.Get, url, responseHeader, byteData, out reason);
								if (!validated) {
									failed(connectionId, code, reason, new AutoyaStatus());
									return;
								}
								
								succeeded(_conId, byteData);
							},
							failed
						);
					},
					(conId, code, reason, responseHeader) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => {}, failed);
					},
					timeoutSec
				)
			);
			
			return connectionId;
		}

		public static string Http_Post (
			string url, 
			string data,
			Action<string, byte[]> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Post, url, additionalHeader, data);
			
			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Post(
					connectionId,
					headers,
					url,
					data,
					(string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
							(_conId, _data) => {
								var byteData = _data as byte[];
								var message = string.Empty;
								var validated = autoya.OnValidateHttpResponse(HttpMethod.Post, url, responseHeader, byteData, out message);
								if (!validated) {
									failed(connectionId, code, message, new AutoyaStatus(false, false, true));
									return;
								}
								
								succeeded(_conId, byteData);
							},
							failed
						);
					},
					(conId, code, reason, responseHeader) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => {}, failed);
					},
					timeoutSec
				)
			);

			return connectionId;
		}

		public static string Http_Put (
			string url, 
			string data,
			Action<string, byte[]> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}
			
			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Put, url, additionalHeader, data);
			
			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Put(
					connectionId,
					headers,
					url,
					data,
					(string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
							(_conId, _data) => {
								var byteData = _data as byte[];
								var reason = string.Empty;
								var validated = autoya.OnValidateHttpResponse(HttpMethod.Put, url, responseHeader, byteData, out reason);
								if (!validated) {
									failed(connectionId, code, reason, new AutoyaStatus());
									return;
								}
								
								succeeded(_conId, byteData);
							},
							failed
						);
					},
					(conId, code, reason, responseHeader) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => {}, failed);
					},
					timeoutSec
				)
			);

			return connectionId;
		}

		public static string Http_Delete (
			string url, 
			string data,
			Action<string, byte[]> succeeded, 
			Action<string, int, string, AutoyaStatus> failed, 
			Dictionary<string, string> additionalHeader=null, 
			double timeoutSec=BackyardSettings.HTTP_TIMEOUT_SEC
		) {
			var connectionId = Guid.NewGuid().ToString();

			if (autoya == null) {
				var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				return connectionId;
			} 
			if (!Autoya.Auth_IsAuthenticated()) {
				var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", failed).Coroutine();
				autoya.mainthreadDispatcher.Commit(cor);
				
				return connectionId;
			}

			if (additionalHeader == null) {
				additionalHeader = new Dictionary<string, string>();
			}
			var headers = autoya.httpRequestHeaderDelegate(HttpMethod.Delete, url, additionalHeader, data);
			
			autoya.mainthreadDispatcher.Commit(
				autoya._autoyaHttp.Delete(
					connectionId,
					headers,
					url,
					(string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
							(_conId, _data) => {
								var byteData = _data as byte[];
								var reason = string.Empty;
								var validated = autoya.OnValidateHttpResponse(HttpMethod.Delete, url, responseHeader, byteData, out reason);
								if (!validated) {
									failed(connectionId, code, reason, new AutoyaStatus());
									return;
								}
								
								succeeded(_conId, byteData);
							}, 
							failed
						);
					},
					(conId, code, reason, responseHeader) => {
						autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => {}, failed);
					},
					timeoutSec
				)
			);

			return connectionId;
		}



		private class ConnectionErrorInstance {
			private readonly string connectionId;
			private const int code = AuthSettings.AUTOYA_HTTP_CODE_INTERNAL_UNAUTHORIZED;
			private readonly string reason;
			private readonly Action<string, int, string, AutoyaStatus> failed;
			private static AutoyaStatus status = new AutoyaStatus();

			public ConnectionErrorInstance (string connectionId, string reason, Action<string, int, string, AutoyaStatus> failed) {
				this.connectionId = connectionId;
				this.reason = reason;
				this.failed = failed;
			}

			public IEnumerator Coroutine () {
				yield return null;
				failed(connectionId, code, reason, status);
			}
		}
	}
}