using System;
using AutoyaFramework.Connections.HTTP;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using AutoyaFramework.Settings.Auth;

namespace AutoyaFramework
{
    /**
		authenticated http feature.
	*/
    public partial class Autoya
    {
        private HTTPConnection _autoyaHttp;

        private void AddFrameworkHeaderParam(Dictionary<string, string> additionalRequestHeaders)
        {
            additionalRequestHeaders[AuthSettings.AUTH_REQUESTHEADER_APPVERSION] = OnAppVersionRequired();

            if (autoya.assetBundleFeatState == AssetBundlesFeatureState.Ready)
            {
                additionalRequestHeaders[AuthSettings.AUTH_REQUESTHEADER_RESVERSION] = OnResourceVersionRequired();
            }
        }

        /*
			public HTTP APIs.
		*/

        // respose by string
        public static string Http_Get(
            string url,
            Action<string, string> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }

            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("GET", url, additionalHeader, string.Empty);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.Get(
                    connectionId,
                    headers,
                    url,
                    (string conId, int code, Dictionary<string, string> responseHeader, string resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var stringData = _data as string;
                                var reason = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("GET", url, responseHeader, stringData, out reason);
                                if (!validated)
                                {
                                    failed(connectionId, code, reason, new AutoyaStatus());
                                    return;
                                }

                                succeeded(_conId, stringData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("GET", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }

        public static string Http_Post(
            string url,
            string data,
            Action<string, string> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("POST", url, additionalHeader, data);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.Post(
                    connectionId,
                    headers,
                    url,
                    data,
                    (string conId, int code, Dictionary<string, string> responseHeader, string resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var stringData = _data as string;
                                var message = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("POST", url, responseHeader, stringData, out message);
                                if (!validated)
                                {
                                    failed(connectionId, code, message, new AutoyaStatus(false, false, true));
                                    return;
                                }

                                succeeded(_conId, stringData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("POST", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }

        public static string Http_Put(
            string url,
            string data,
            Action<string, string> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("PUT", url, additionalHeader, data);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.Put(
                    connectionId,
                    headers,
                    url,
                    data,
                    (string conId, int code, Dictionary<string, string> responseHeader, string resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var stringData = _data as string;
                                var reason = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("PUT", url, responseHeader, stringData, out reason);
                                if (!validated)
                                {
                                    failed(connectionId, code, reason, new AutoyaStatus());
                                    return;
                                }

                                succeeded(_conId, stringData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("PUT", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }

        public static string Http_Delete(
            string url,
            Action<string, string> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("DELETE", url, additionalHeader, string.Empty);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.Delete(
                    connectionId,
                    headers,
                    url,
                    (string conId, int code, Dictionary<string, string> responseHeader, string resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var stringData = _data as string;
                                var reason = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("DELETE", url, responseHeader, stringData, out reason);
                                if (!validated)
                                {
                                    failed(connectionId, code, reason, new AutoyaStatus());
                                    return;
                                }

                                succeeded(_conId, stringData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("DELETE", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }


        // response by byte[]
        public static string Http_GetByBytes(
            string url,
            Action<string, byte[]> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("GET", url, additionalHeader, string.Empty);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.GetByBytes(
                    connectionId,
                    headers,
                    url,
                    (string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var byteData = _data as byte[];
                                var reason = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("GET", url, responseHeader, byteData, out reason);
                                if (!validated)
                                {
                                    failed(connectionId, code, reason, new AutoyaStatus());
                                    return;
                                }

                                succeeded(_conId, byteData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("GET", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }

        public static string Http_PostByBytes(
            string url,
            string data,
            Action<string, byte[]> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("POST", url, additionalHeader, data);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.PostByBytes(
                    connectionId,
                    headers,
                    url,
                    data,
                    (string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var byteData = _data as byte[];
                                var message = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("POST", url, responseHeader, byteData, out message);
                                if (!validated)
                                {
                                    failed(connectionId, code, message, new AutoyaStatus(false, false, true));
                                    return;
                                }

                                succeeded(_conId, byteData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("POST", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }

        public static string Http_PutByBytes(
            string url,
            string data,
            Action<string, byte[]> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("PUT", url, additionalHeader, data);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.PutByBytes(
                    connectionId,
                    headers,
                    url,
                    data,
                    (string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var byteData = _data as byte[];
                                var reason = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("PUT", url, responseHeader, byteData, out reason);
                                if (!validated)
                                {
                                    failed(connectionId, code, reason, new AutoyaStatus());
                                    return;
                                }

                                succeeded(_conId, byteData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("PUT", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }

        public static string Http_DeleteByBytes(
            string url,
            string data,
            Action<string, byte[]> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("DELETE", url, additionalHeader, data);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.DeleteByBytes(
                    connectionId,
                    headers,
                    url,
                    (string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var byteData = _data as byte[];
                                var reason = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("DELETE", url, responseHeader, byteData, out reason);
                                if (!validated)
                                {
                                    failed(connectionId, code, reason, new AutoyaStatus());
                                    return;
                                }

                                succeeded(_conId, byteData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("DELETE", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }


        // request & response by bytes[]
        public static string Http_Post(
            string url,
            byte[] data,
            Action<string, byte[]> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("POST", url, additionalHeader, data);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.Post(
                    connectionId,
                    headers,
                    url,
                    data,
                    (string conId, int code, Dictionary<string, string> responseHeader, byte[] resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var byteData = _data as byte[];
                                var message = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("POST", url, responseHeader, byteData, out message);
                                if (!validated)
                                {
                                    failed(connectionId, code, message, new AutoyaStatus(false, false, true));
                                    return;
                                }

                                succeeded(_conId, byteData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("POST", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }

        public static string Http_Put(
            string url,
            byte[] data,
            Action<string, byte[]> succeeded,
            Action<string, int, string, AutoyaStatus> failed,
            Dictionary<string, string> additionalHeader = null,
            double timeoutSec = BackyardSettings.HTTP_TIMEOUT_SEC,
            string userConnectionId = null
        )
        {
            var connectionId = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(userConnectionId))
            {
                connectionId = userConnectionId;
            }


            if (autoya == null)
            {
                var cor = new ConnectionErrorInstance(connectionId, "Autoya is null.", new AutoyaStatus(), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);
                return connectionId;
            }
            if (!Autoya.Auth_IsAuthenticated())
            {
                var cor = new ConnectionErrorInstance(connectionId, "not authenticated.", new AutoyaStatus(false, true), failed).Coroutine();
                autoya.mainthreadDispatcher.Commit(cor);

                return connectionId;
            }

            if (additionalHeader == null)
            {
                additionalHeader = new Dictionary<string, string>();
            }

            autoya.AddFrameworkHeaderParam(additionalHeader);

            var headers = autoya.httpRequestHeaderDelegate("PUT", url, additionalHeader, data);

            autoya.mainthreadDispatcher.Commit(
                autoya._autoyaHttp.Put(
                    connectionId,
                    headers,
                    url,
                    data,
                    (string conId, int code, Dictionary<string, string> responseHeader, string resultData) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, resultData, string.Empty,
                            (_conId, _data) =>
                            {
                                var byteData = _data as byte[];
                                var reason = string.Empty;
                                var validated = autoya.OnValidateHttpResponse("PUT", url, responseHeader, byteData, out reason);
                                if (!validated)
                                {
                                    failed(connectionId, code, reason, new AutoyaStatus());
                                    return;
                                }

                                succeeded(_conId, byteData);
                            },
                            failed
                        );
                    },
                    (conId, code, reason, responseHeader) =>
                    {
                        autoya.HttpResponseHandling(conId, responseHeader, code, string.Empty, reason, (_conId, _data) => { },
                            (_conId, _code, _reason, _status) =>
                            {
                                // use validated reason for some security appearance.
                                var validatedReason = autoya.OnValidateFailedHttpResponse("PUT", url, _code, responseHeader, _reason);
                                failed(_conId, _code, validatedReason, _status);
                            }
                        );
                    },
                    timeoutSec
                )
            );

            return connectionId;
        }

        private class ConnectionErrorInstance
        {
            private readonly string connectionId;
            private const int code = AuthSettings.AUTOYA_HTTP_CODE_INTERNAL_UNAUTHORIZED;
            private readonly string reason;
            private readonly Action<string, int, string, AutoyaStatus> failed;
            private readonly AutoyaStatus status;

            public ConnectionErrorInstance(string connectionId, string reason, AutoyaStatus status, Action<string, int, string, AutoyaStatus> failed)
            {
                this.connectionId = connectionId;
                this.reason = reason;
                this.failed = failed;
                this.status = status;
            }

            public IEnumerator Coroutine()
            {
                yield return null;
                failed(connectionId, code, reason, status);
            }
        }
    }
}