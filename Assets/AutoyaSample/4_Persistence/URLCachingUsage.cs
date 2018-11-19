using UnityEngine;
using AutoyaFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class URLCachingUsage : MonoBehaviour
{

    public Image image;

    void Start()
    {
        // sprite file cache domain. can be path.
        var spriteDomain = "persistence/sprite_cache";

        // purge　all files in domain.
        Autoya.Persist_URLCaching_PurgeByDomain(spriteDomain);

        // download image from url, then get cached & sprite assets asynchronously.
        var queryString = "?q=0";
        Autoya.Persist_URLCaching_Load<Sprite>(
            spriteDomain,
            "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2a/Jewelkatz_Romeo_Of_Stalker-Bars.jpg/500px-Jewelkatz_Romeo_Of_Stalker-Bars.jpg" + queryString,
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
                // attach cached sprite to current screen image.
                image.sprite = cached;

                // set aspect.
                image.preserveAspect = true;
            },
            (code, reason) =>
            {
                Debug.LogError("code:" + code + " reason:" + reason);
            }
        );
    }
}
