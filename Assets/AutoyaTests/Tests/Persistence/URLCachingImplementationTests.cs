using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AutoyaFramework;
using Miyamasu;
using NUnit.Framework;
using UnityEngine;

/**
	test for file persist controll.
*/
public class URLCachingImplementationTests : MiyamasuTestRunner
{
    private const string AutoyaURLCachingTestsFileDomain = "AutoyaURLCachingTestsFileDomain";
    private const string AutoyaURLCachingTestsFileDomain2 = "AutoyaURLCachingTestsFileDomain2";

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
        Autoya.Persist_DeleteByDomain(AutoyaURLCachingTestsFileDomain2);
    }

    [MTeardown]
    public void Teardown()
    {
        // delete all cache.
        Autoya.Persist_URLCaching_PurgeByDomain(AutoyaURLCachingTestsFileDomain);
        Autoya.Persist_URLCaching_PurgeByDomain(AutoyaURLCachingTestsFileDomain2);
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

    [MTest]
    public IEnumerator CachingWithKey()
    {
        var key = "myKey?a=b";
        var loaded = false;
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";

        Autoya.Persist_URLCaching_Load(
               AutoyaURLCachingTestsFileDomain,
               key,
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
    public IEnumerator CachingWithKeyOtherURL()
    {
        var key = "myKey?a=b";
        {
            var loaded = false;
            var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";

            Autoya.Persist_URLCaching_Load(
                AutoyaURLCachingTestsFileDomain,
                key,
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
        {
            var loaded = false;
            var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=c";

            Autoya.Persist_URLCaching_Load(
                AutoyaURLCachingTestsFileDomain,
                key,
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

                }
            );

            yield return WaitUntil(
                () => loaded,
                () => { throw new TimeoutException("timeout."); }
            );
        }
    }

    [MTest]
    public IEnumerator Timeout()
    {
        // large picture.
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/2/2e/Zhao_Chang_-_Picture_of_the_New_Year_-_Google_Art_Project.jpg";
        var loadedFailed = false;

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
                Fail();
            },
            (code, reason) =>
            {
                loadedFailed = true;
            },
            null,
            1
        );

        yield return WaitUntil(
            () => loadedFailed,
            () => { throw new TimeoutException("timeout."); },
            10
        );
    }

    [MTest]
    public IEnumerator TimeoutWithKey()
    {
        // large picture.
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/2/2e/Zhao_Chang_-_Picture_of_the_New_Year_-_Google_Art_Project.jpg";
        var key = "myKey?a=b";
        var loadedFailed = false;
        Autoya.Persist_URLCaching_Load<Sprite>(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            key,
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
                loadedFailed = true;
            },
            null,
            1
        );

        yield return WaitUntil(
            () => loadedFailed,
            () => { throw new TimeoutException("timeout."); },
            1000
        );
    }

    [MTest]
    public IEnumerator ExecuteExpiration()
    {
        var done = false;
        var day = -1;// マイナスを入れると絶対にexpire対象になる

        // store one image.
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
                loaded = true;
            },
            (code, reason) =>
            {
                loaded = true;
                Fail();
            }
        );

        yield return WaitUntil(
            () => loaded,
            () => { throw new TimeoutException("timeout for making cache."); }
        );

        var pathOfCachedImage = Autoya.Persist_URLCaching_PathOf(AutoyaURLCachingTestsFileDomain, imagePath);
        Assert.True(File.Exists(pathOfCachedImage), "not exist.");

        // delete expired file.
        Autoya.Persist_URLCaching_ExecuteExpiration(
            AutoyaURLCachingTestsFileDomain,
            day,
            30,
            () =>
            {
                done = true;
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("timeout."); },
            50
        );

        // check deletion.
        Assert.False(File.Exists(pathOfCachedImage), "should not exist.");
    }

    [MTest]
    public IEnumerator ExecuteExpirationForManyFiles()
    {
        var done = false;
        var day_minus_one = -1;// マイナスを入れると絶対にexpire対象になる

        // store many images.
        var loaded = 0;
        var imagePathsList = new List<string>();
        for (var i = 0; i < 40; i++)
        {
            var path = "https://dummyimage.com/" + (i + 1) + ".png/09f/fff";
            imagePathsList.Add(path);
        };
        var imagePaths = imagePathsList.ToArray();

        var pathOfCachedImages = new List<string>();
        foreach (var imagePath in imagePaths)
        {
            Autoya.Persist_URLCaching_Load<Texture2D>(
                AutoyaURLCachingTestsFileDomain,
                imagePath,
                bytes =>
                {
                    return null;
                },
                cached =>
                {
                    loaded++;
                    var pathOf = Autoya.Persist_URLCaching_PathOf(AutoyaURLCachingTestsFileDomain, imagePath);
                    pathOfCachedImages.Add(pathOf);
                },
                (code, reason) =>
                {
                    Debug.LogError("code:" + code + " reason:" + reason + " imagePath:" + imagePath);
                    loaded++;
                    Fail();
                },
                null,
                100
            );
        }

        yield return WaitUntil(
            () => loaded == imagePaths.Length,
            () => { throw new TimeoutException("timeout for making cache."); },
            30
        );

        // delete expired file.
        Autoya.Persist_URLCaching_ExecuteExpiration(
            AutoyaURLCachingTestsFileDomain,
            day_minus_one,
            30,
            () =>
            {
                done = true;
            }
        );

        yield return WaitUntil(
            () => done,
            () => { throw new TimeoutException("timeout."); },
            50
        );

        // check deletion.
        foreach (var path in pathOfCachedImages)
        {
            Assert.False(File.Exists(path), "should not exist.");
        }
    }

    [MTest]
    public IEnumerator Unload()
    {
        // store one image.
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
                loaded = true;
            },
            (code, reason) =>
            {
                loaded = true;
                Fail();
            }
        );

        yield return WaitUntil(
            () => loaded,
            () => { throw new TimeoutException("timeout for making cache."); }
        );

        var pathOfCachedImage = Autoya.Persist_URLCaching_PathOf(AutoyaURLCachingTestsFileDomain, imagePath);
        Assert.True(File.Exists(pathOfCachedImage), "not exist.");

        // キャッシュ done.

        Autoya.Persist_URLCaching_Unload(
            AutoyaURLCachingTestsFileDomain,
            imagePath,
            true
        );

        var isLoaded = Autoya.Persist_URLCaching_IsLoaded(
            AutoyaURLCachingTestsFileDomain,
            imagePath
        );

        Assert.True(!isLoaded, "failed to unload.");
    }

    [MTest]
    public IEnumerator UnloadByDomain()
    {
        var imagePath = "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9e/2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg/400px-2016-06-14_Orange_and_white_tabby_cat_born_in_2016_茶トラ白ねこ_DSCF6526☆彡.jpg?a=b";

        // store one image in test domain.
        {
            var loaded = false;
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
                    loaded = true;
                },
                (code, reason) =>
                {
                    loaded = true;
                    Fail();
                }
            );

            yield return WaitUntil(
                () => loaded,
                () => { throw new TimeoutException("timeout for making cache."); }
            );
        }

        // store one image in test domain2.
        {
            var loaded = false;
            Autoya.Persist_URLCaching_Load(
                AutoyaURLCachingTestsFileDomain2,
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
                    loaded = true;
                    Fail();
                }
            );

            yield return WaitUntil(
                () => loaded,
                () => { throw new TimeoutException("timeout for making cache."); }
            );
        }

        // キャッシュ done.

        // AutoyaURLCachingTestsFileDomainの画像をunloadする
        Autoya.Persist_URLCaching_UnloadByDomain(
            AutoyaURLCachingTestsFileDomain,
            true
        );

        var isLoaded = Autoya.Persist_URLCaching_IsLoaded(
            AutoyaURLCachingTestsFileDomain,
            imagePath
        );

        Assert.True(!isLoaded, "failed to unload.");


        var isLoaded2 = Autoya.Persist_URLCaching_IsLoaded(
            AutoyaURLCachingTestsFileDomain2,
            imagePath
        );

        Assert.True(isLoaded2, "failed to unloadByDomain.");
    }
}

