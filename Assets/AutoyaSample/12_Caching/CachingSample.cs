using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using AutoyaFramework;
using UnityEngine;
using UnityEngine.UI;

public class CachingSample : MonoBehaviour
{
    public Button button;

    // Use this for initialization
    IEnumerator Start()
    {
        var s = new System.Diagnostics.Stopwatch();
        s.Start();

        var imageNameBase = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/200px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg";

        var loaded = false;

        s.Start();
        var cor1 = Cache.LoadFrom(
            "a",
            imageNameBase,// url
            bytes =>
            {
                s.Stop();
                Debug.Log("byteをキャッシュからゲット1 " + s.ElapsedTicks);
                loaded = true;
            },
            error =>
            {
                Debug.LogError("キャッシュ取得失敗1 error:" + error);
            }
        );
        while (cor1.MoveNext())
        {
            yield return null;
        }

        while (!loaded)
        {
            Debug.Log("waiting.");
            yield return null;
        }

        s.Reset();
        s.Start();
        var cor2 = Cache.LoadFrom(
            "a",
            imageNameBase,// url
            bytes =>
            {
                s.Stop();
                Debug.Log("byteをキャッシュからゲット2 " + s.ElapsedTicks);
            },
            error =>
            {
                Debug.LogError("キャッシュ取得失敗2 error:" + error);
            }
        );
        while (cor2.MoveNext())
        {
            yield return null;
        }



        // この上にさらにオンメモリのImageCache層を追加する。

        ImageCache.LoadImage(
            "a",
            imageNameBase,
            image =>
            {
                Debug.Log("image化成功");
                button.image.sprite = image;
            },
            error =>
            {

            }
        );
    }

    // Update is called once per frame
    void Update()
    {

    }
}

public class ImageCache
{
    private static Dictionary<string, Sprite> imageCache = new Dictionary<string, Sprite>();
    public static void LoadImage(string cacheDomain, string url, Action<Sprite> onLoaded, Action<string> onLoadFailed)
    {
        if (imageCache.ContainsKey(url))
        {
            onLoaded(imageCache[url]);
            return;
        }

        var cor = Cache.LoadFrom(
            cacheDomain,
            url,
            bytes =>
            {
                try
                {
                    var tex = new Texture2D(1, 1);
                    tex.LoadImage(bytes);
                    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                    imageCache[url] = sprite;
                    onLoaded(sprite);
                }
                catch (Exception e)
                {
                    onLoadFailed(e.Message);
                }

            },
            error =>
            {
                onLoadFailed(error);
            }
        );

        Autoya.Mainthread_Commit(cor);
    }
}

public class Cache
{
    // 古いのを消すみたいなのを作りたいところだが。fileのdate見ればいいのか。もしくはcyclicにN個のファイルの記録を持つか。そのファイル以外を定期的に消す。

    const string CONST_VALUE = "_";
    private static object writeLock = new object();

    private static Hashtable caching = new Hashtable();
    public static IEnumerator LoadFrom(string storePath, string url, Action<byte[]> onLoaded, Action<string> onLoadFailed)
    {
        var targetFileNameBytes = AutoyaFramework.Encrypt.SHA_2.SHA_2.Sha256Bytes(url, storePath);
        var targetFileName = Convert.ToBase64String(targetFileNameBytes).Replace("/", "_").Replace("+", "_").Replace("=", "_");

        // ファイルがキャッシュされているかどうかチェックする
        var isExistInStorage = Autoya.Persist_IsExist(storePath, targetFileName);

        if (isExistInStorage)
        {
            Autoya.Persist_Load(
                storePath,
                targetFileName,
                bytes => onLoaded(bytes),
                error => onLoadFailed(error)
            );
            yield break;
        }


        // もしすでにロード中だったら待機する
        if (caching.ContainsKey(targetFileName))
        {
            while (caching.ContainsKey(targetFileName))
            {
                yield return null;
            }

            // キャッシュされてるはずなのでチェックなしで取得
            Autoya.Persist_Load(
                storePath,
                targetFileName,
                bytes => onLoaded(bytes),
                error => onLoadFailed(error)
            );

            yield break;
        }

        lock (writeLock)
        {
            caching.Add(targetFileName, CONST_VALUE);
        }

        var request = new AutoyaFramework.Connections.HTTP.HTTPConnection();
        var cor = request.GetByBytes(
            "cache_" + storePath,
            new Dictionary<string, string>(),
            url,
            (conId, code, resp, bytes) =>
            {
                // 取得したbyte列を非同期で保存する
                Autoya.Persist_Append(storePath, targetFileName, bytes, () =>
                {
                    lock (writeLock)
                    {
                        caching.Remove(targetFileName);
                    }
                    // キャッシュ成功
                }, error =>
                {
                    lock (writeLock)
                    {
                        caching.Remove(targetFileName);
                    }
                });

                // 取得したbyte列を返す
                onLoaded(bytes);
            },
            (conId, code, reason, resp) =>
            {
                onLoadFailed(reason);
            }
        );
        while (cor.MoveNext())
        {
            yield return null;
        }
    }

}
