using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.Settings.EndPoint;
using Miyamasu;
using NUnit.Framework;
using UnityEngine;


/**
	test for endpoint selector.
*/

public class EndPointTests : MiyamasuTestRunner
{
    [MTest]
    public IEnumerator ChangeEndPoint()
    {
        /*
            この機構は、起動時に通信を行い、特定のファイルの内容を更新することを前提としている。
            失敗した場合は起動しない、という選択肢も取る必要がある。
        */
        var retryCount = 3;

        var endPointSelector = new EndPointSelector(
            new IEndPoint[]{
                new slideshow(),
                new UnusedEndPoint(),
            }
        );

        var succeeded = false;

        var cor = endPointSelector.UpToDate(
            "https://httpbin.org/json",
            new Dictionary<string, string>(),
            namesAndErrors =>
            {
                if (namesAndErrors.Length == 0)
                {
                    succeeded = true;
                    return;
                }
                Debug.LogError("fauled to parse, errors:" + namesAndErrors.Length);
            },
            () =>
            {
                Debug.LogError("failed to get endPoints.");
            },
            10.0,
            retryCount
        );

        while (cor.MoveNext())
        {
            yield return null;
        }

        Assert.True(succeeded);

        var ep = endPointSelector.GetEndPoint<slideshow>();
        Assert.True(ep.author == "Yours Truly", "not match.");
        Assert.True(ep.date == "default_date");
    }
}