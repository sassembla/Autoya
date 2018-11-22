using System;
using System.Collections;
using System.IO;
using AutoyaFramework;
using Miyamasu;
using UnityEngine;

/**
	test for file persist controll.
*/
public class URLCachingImplementationTests : MiyamasuTestRunner
{
    private const string AutoyaURLCachingTestsFileDomain = "AutoyaURLCachingTestsFileDomain";

    [MSetup]
    public IEnumerator Setup()
    {
        var loginDone = false;

        var dataPath = Application.persistentDataPath;
        Autoya.TestEntryPoint(dataPath);
        Autoya.Auth_SetOnAuthenticated(
            () =>
            {
                loginDone = true;
            }
        );

        yield return WaitUntil(
            () =>
            {
                return loginDone;
            },
            () => { throw new TimeoutException("timeout."); }
        );

        // delete all.
        Autoya.Persist_DeleteByDomain(AutoyaURLCachingTestsFileDomain);
    }

    [MTeardown]
    public void Teardown()
    {
        // delete all cache.
        Autoya.Persist_URLCaching_PurgeByDomain(AutoyaURLCachingTestsFileDomain);
    }

    [MTest]
    public IEnumerator LoadExample()
    {
        var loaded = false;
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";
        Autoya.Persist_URLCaching_Load<Sprite>(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                loaded = true;
            },
            (code, reason) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => loaded,
            () => { throw new TimeoutException("timeout."); }
        );
    }

    [MTest]
    public IEnumerator GetSameSprite()
    {
        Sprite sprite = null;


        var loaded = false;
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";
        Autoya.Persist_URLCaching_Load(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                sprite = cached;
                loaded = true;
            },
            (code, reason) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => loaded,
            () => { throw new TimeoutException("timeout."); }
        );

        var loadedAgain = false;
        // call again.
        Autoya.Persist_URLCaching_Load(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                True(sprite == cached, "not match.");
                loadedAgain = true;
            },
            (code, reason) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
           () => loadedAgain,
           () => { throw new TimeoutException("timeout."); }
       );
    }

    [MTest]
    public IEnumerator GetUpdatedSprite()
    {
        Sprite sprite = null;


        var loaded = false;
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";
        Autoya.Persist_URLCaching_Load(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                sprite = cached;
                loaded = true;
            },
            (code, reason) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => loaded,
            () => { throw new TimeoutException("timeout."); }
        );

        var imagePath2 = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=c";

        var loadedAgain = false;
        // call again.
        Autoya.Persist_URLCaching_Load(
            AutoyaURLCachingTestsFileDomain,
            imagePath2,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                True(sprite != cached, "nothing changed.");
                loadedAgain = true;
            },
            (code, reason) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
           () => loadedAgain,
           () => { throw new TimeoutException("timeout."); }
       );
    }

    [MTest]
    public IEnumerator PurgeThenGetNew()
    {
        Sprite sprite = null;


        var loaded = false;
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";
        Autoya.Persist_URLCaching_Load(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                True(cached != null, "cached is null.");
                sprite = cached;
                loaded = true;
            },
            (code, reason) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
            () => loaded,
            () => { throw new TimeoutException("timeout."); }
        );

        // purge cache.
        Autoya.Persist_URLCaching_Purge(AutoyaURLCachingTestsFileDomain, imagePath);

        var loadedAgain = false;

        // get again.
        Autoya.Persist_URLCaching_Load(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                True(cached != null, "cached is null.");
                True(sprite.GetInstanceID() != cached.GetInstanceID(), "nothing changed. sprite:" + sprite + " cached:" + cached);
                loadedAgain = true;
            },
            (code, reason) =>
            {
                Fail();
            }
        );

        yield return WaitUntil(
           () => loadedAgain,
           () => { throw new TimeoutException("timeout."); }
       );
    }

    [MTest]
    public IEnumerator PurgeByDomainThenGetNew()
    {
        Sprite sprite = null;


        var loaded = false;
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";
        Autoya.Persist_URLCaching_Load(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                sprite = cached;
                loaded = true;
            },
            (code, reason) =>
            {
                Fail("reason:" + reason);
            }
        );

        yield return WaitUntil(
            () => loaded,
            () => { throw new TimeoutException("timeout."); },
            10
        );

        Autoya.Persist_URLCaching_PurgeByDomain(AutoyaURLCachingTestsFileDomain);

        var loadedAgain = false;
        // call again.
        Autoya.Persist_URLCaching_Load(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            bytes =>
            {
                // return sprite from bytes.
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                return newSprite;
            },
            cached =>
            {
                True(sprite != cached, "nothing changed.");
                loadedAgain = true;
            },
            (code, reason) =>
            {
                Fail("reason:" + reason);
            }
        );

        yield return WaitUntil(
           () => loadedAgain,
           () => { throw new TimeoutException("timeout."); },
           10
       );
    }


    [MTest]
    public IEnumerator SameURLMultiTimes()
    {
        var loadedCount = 0;
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";

        for (var i = 0; i < 10; i++)
        {
            Autoya.Persist_URLCaching_Load(
                AutoyaURLCachingTestsFileDomain,
                imagePath,
                bytes =>
                {
                    // return sprite from bytes.
                    var tex = new Texture2D(1, 1);
                    tex.LoadImage(bytes);
                    var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                    return newSprite;
                },
                cached =>
                {
                    loadedCount++;
                },
                (code, reason) =>
                {
                    Fail();
                }
            );
        }

        yield return WaitUntil(
            () => loadedCount == 10,
            () => { throw new TimeoutException("timeout."); }
        );
    }

    [MTest]
    public IEnumerator NotExistURLOnce()
    {
        var loadFailed = false;
        var imagePath = "https://upload.wikimedia.org/notExists.jpg?a=b";

        Autoya.Persist_URLCaching_Load(
               AutoyaURLCachingTestsFileDomain,
               imagePath,
               bytes =>
               {
                   // return sprite from bytes.
                   var tex = new Texture2D(1, 1);
                   tex.LoadImage(bytes);
                   var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                   return newSprite;
               },
               cached =>
               {
                   Fail();
               },
               (code, reason) =>
               {
                   loadFailed = true;
                   True(code == 404, "code:" + code + " reason:" + reason);
               }
           );


        yield return WaitUntil(
            () => loadFailed,
            () => { throw new TimeoutException("timeout."); }
        );
    }

    [MTest]
    public IEnumerator NotExistURLMultiTimes()
    {
        var failedCount = 0;
        var imagePath = "https://upload.wikimedia.org/notExists.jpg?a=b";

        for (var i = 0; i < 10; i++)
        {
            Autoya.Persist_URLCaching_Load(
                AutoyaURLCachingTestsFileDomain,
                imagePath,
                bytes =>
                {
                    // return sprite from bytes.
                    var tex = new Texture2D(1, 1);
                    tex.LoadImage(bytes);
                    var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
                    return newSprite;
                },
                cached =>
                {
                    Fail();
                },
                (code, reason) =>
                {
                    failedCount++;
                    True(code == 404, "code:" + code + " reason:" + reason);
                }
            );
        }

        yield return WaitUntil(
            () => failedCount == 10,
            () =>
            {
                throw new TimeoutException("timeout.");
            },
            10
        );
    }
}

