using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/**
	implementation of HTTP connection with timeout.
*/
namespace AutoyaFramework.Connections.HTTP
{

    public class HTTPConnection
    {

        // response by string
        public IEnumerator Get(string connectionId, Dictionary<string, string> requestHeader, string url, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Get(url))
            {
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        var result = Encoding.UTF8.GetString(bytes);
                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, result);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + result, responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        public IEnumerator Post(string connectionId, Dictionary<string, string> requestHeader, string url, string data, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Post(url, data))// UnityWebRequest post should contains body. this cannot be avoid.
            {
                var utf8EncodedData = Encoding.UTF8.GetBytes(data);
                if (0 < utf8EncodedData.Length)
                {
                    // TODO: 2021.3.x workaround. 解消したら消す
                    request.uploadHandler.Dispose();
                    request.uploadHandler = (UploadHandler)new UploadHandlerRaw(utf8EncodedData);
                }

                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;
                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        var result = Encoding.UTF8.GetString(bytes);
                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, result);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + result, responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        public IEnumerator Put(string connectionId, Dictionary<string, string> requestHeader, string url, string data, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Put(url, data))
            {
                // TODO: 2021.3.x workaround. 解消したら消す
                request.uploadHandler.Dispose();
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        var result = Encoding.UTF8.GetString(bytes);
                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, result);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + result, responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        public IEnumerator Delete(string connectionId, Dictionary<string, string> requestHeader, string url, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Delete(url))
            {
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        var result = Encoding.UTF8.GetString(bytes);
                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, result);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + result, responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        // response by byte[]
        public IEnumerator GetByBytes(string connectionId, Dictionary<string, string> requestHeader, string url, Action<string, int, Dictionary<string, string>, byte[]> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Get(url))
            {
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, bytes);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + Encoding.UTF8.GetString(bytes), responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        public IEnumerator PostByBytes(string connectionId, Dictionary<string, string> requestHeader, string url, string data, Action<string, int, Dictionary<string, string>, byte[]> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Post(url, data))
            {
                // TODO: 2021.3.x workaround. 解消したら消す
                request.uploadHandler.Dispose();
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;
                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, bytes);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + Encoding.UTF8.GetString(bytes), responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        public IEnumerator PutByBytes(string connectionId, Dictionary<string, string> requestHeader, string url, string data, Action<string, int, Dictionary<string, string>, byte[]> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Put(url, data))
            {
                // TODO: 2021.3.x workaround. 解消したら消す
                request.uploadHandler.Dispose();
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(Encoding.UTF8.GetBytes(data));
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, bytes);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + Encoding.UTF8.GetString(bytes), responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        public IEnumerator DeleteByBytes(string connectionId, Dictionary<string, string> requestHeader, string url, Action<string, int, Dictionary<string, string>, byte[]> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Delete(url))
            {
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, bytes);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + Encoding.UTF8.GetString(bytes), responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        // request & response by byte[]
        public IEnumerator Post(string connectionId, Dictionary<string, string> requestHeader, string url, byte[] data, Action<string, int, Dictionary<string, string>, byte[]> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Post(url, string.Empty))
            {
                // set data if not 0.
                if (0 < data.Length)
                {
                    // TODO: ここには UnityWebRequest.Postでstring.Emptyを渡しているため、request.uploadHandlerにはnullが入っている。 そのため、request.uploadHandler.Dispose(); は不要。
                    // というか UnityWebRequest.Post(url, byte[])) やUnityWebRequest.Post(url, UploadHandler))がほしい。
                    // 今後のUnityのバグ解消があれば対応する。
                    request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
                }

                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;
                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, bytes);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + Encoding.UTF8.GetString(bytes), responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }

        public IEnumerator Put(string connectionId, Dictionary<string, string> requestHeader, string url, byte[] data, Action<string, int, Dictionary<string, string>, string> succeeded, Action<string, int, string, Dictionary<string, string>> failed, double timeoutSec = 0)
        {
            var currentDate = DateTime.UtcNow;
            var limitTick = (TimeSpan.FromTicks(currentDate.Ticks) + TimeSpan.FromSeconds(timeoutSec)).Ticks;

            using (var request = UnityWebRequest.Put(url, data))
            {
                // TODO: 2021.3.x workaround. 解消したら消す
                request.uploadHandler.Dispose();
                request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
                if (requestHeader != null)
                {
                    foreach (var kv in requestHeader)
                    {
                        request.SetRequestHeader(kv.Key, kv.Value);
                    }
                }

                var p = request.SendWebRequest();

                while (!p.isDone)
                {
                    yield return null;

                    // check timeout.
                    if (0 < timeoutSec && limitTick < DateTime.UtcNow.Ticks)
                    {
                        request.Abort();
                        failed(connectionId, BackyardSettings.HTTP_TIMEOUT_CODE, BackyardSettings.HTTP_TIMEOUT_MESSAGE, null);
                        yield break;
                    }
                }

                var responseCode = (int)request.responseCode;
                var responseHeaders = request.GetResponseHeaders();

                switch (request.result)
                {
                    case UnityWebRequest.Result.Success:
                    case UnityWebRequest.Result.ProtocolError:
                        var bytes = request.downloadHandler.data;
                        if (bytes == null)
                        {
                            bytes = new byte[0];
                        }

                        var result = Encoding.UTF8.GetString(bytes);
                        if (200 <= responseCode && responseCode <= 299)
                        {
                            succeeded(connectionId, responseCode, responseHeaders, result);
                        }
                        else
                        {
                            failed(connectionId, responseCode, BackyardSettings.HTTP_CODE_ERROR_SUFFIX + result, responseHeaders);
                        }
                        break;
                    case UnityWebRequest.Result.ConnectionError:
                        if (responseCode == 200)
                        {
                            // 後続のHttpResponseHandlingの処理でfailedが呼ばれるようにするため、isNetworkErrorかつ200のときにレスポンスコードを200以外にする
                            responseCode = BackyardSettings.HTTP_NETWORK_ERROR_AND_STATUS_OK_CODE;
                        }

                        failed(connectionId, responseCode, request.error, responseHeaders);
                        break;
                }
            }
        }
    }
}
