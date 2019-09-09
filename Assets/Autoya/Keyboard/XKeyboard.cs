using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class XKeyboard : IDisposable
{
    public XKeyboard(string text)
    {
        XKeyboard.XKeyboard_Show();
        XKeyboard.XKeyboard_SetText(text);
    }

    public void Hide()
    {
        XKeyboard.XKeyboard_Hide();
    }



    public string GetText()
    {
        var ret = XKeyboard.XKeyboard_GetText();
        return ret;
    }

    public bool IsDone()
    {
        var ret = XKeyboard.XKeyboard_IsDone();
        // Debug.Log("done ret:" + ret);
        return ret == 1;
    }

    public bool IsActive()
    {
        var ret = XKeyboard.XKeyboard_IsActive();
        // Debug.Log("active ret:" + ret);
        return ret == 1;
    }

    public float GetKeyboardHeight()
    {
        float x = 0f;
        float y = 0f;
        float w = 0f;
        float h = 0;
        XKeyboard.XKeyboard_GetRect(ref x, ref y, ref w, ref h);
        return y;
    }


#if UNITY_EDITOR || UNITY_ANDROID
    private static void XKeyboard_Show()
    {

    }

    private static void XKeyboard_Hide()
    {

    }


    private static void XKeyboard_SetText(string text)
    {

    }

    private static string XKeyboard_GetText()
    {
        return string.Empty;
    }

    private static int XKeyboard_IsActive()
    {
        return 0;
    }


    private static int XKeyboard_IsDone()
    {
        return 0;
    }

    private static void XKeyboard_GetRect(ref float x, ref float y, ref float w, ref float h)
    {

    }

#elif UNITY_IOS
    [DllImport("__Internal")]
    public static extern void XKeyboard_Show();

    [DllImport("__Internal")]
    public static extern void XKeyboard_Hide();


    [DllImport("__Internal")]
    public static extern void XKeyboard_SetText(string text);

    [DllImport("__Internal")]
    public static extern string XKeyboard_GetText();

    [DllImport("__Internal")]
    public static extern int XKeyboard_IsActive();


    [DllImport("__Internal")]
    public static extern int XKeyboard_IsDone();

    [DllImport("__Internal")]
    public static extern void XKeyboard_GetRect(ref float x, ref float y, ref float w, ref float h);
#endif


    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Hide();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~LiveKeyboard()
    // {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }
    #endregion
}