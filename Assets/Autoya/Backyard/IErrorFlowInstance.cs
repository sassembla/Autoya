using System;
using System.Collections.Generic;

public class IHTTPErrorFlow {
    public virtual void HandleErrorFlow (string connectionId, Dictionary<string, string> responseHeaders, int httpCode, object data, string errorReason, Action<string, object> succeeded, Action<string, int, string> failed){
        if (200 <= httpCode && httpCode < 299) {
            succeeded(connectionId, data);
            return;
        }
        failed(connectionId, httpCode, errorReason);
    }
}