using System.Collections;
using System.Collections.Generic;
using AutoyaFramework.EndPointSelect;
using Miyamasu;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;


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
                new main(),
                new sub(),
            }
        );

        var succeeded = false;

        var cor = endPointSelector.UpToDate(
            "https://raw.githubusercontent.com/sassembla/Autoya/master/Assets/AutoyaTests/RuntimeData/EndPoints/mainAndSub.json",
            new Dictionary<string, string>(),
            responseStr =>
            {
                var endPoints = new List<EndPoint>();
                var classNamesAndValuesSource = MiniJson.JsonDecode(responseStr) as Dictionary<string, object>;
                foreach (var classNamesAndValueSrc in classNamesAndValuesSource)
                {
                    var className = classNamesAndValueSrc.Key;
                    var rawParameterList = classNamesAndValueSrc.Value as List<object>;

                    var parameterDict = new Dictionary<string, string>();
                    foreach (var rawParameters in rawParameterList)
                    {
                        var parameters = rawParameters as Dictionary<string, object>;
                        foreach (var parameter in parameters)
                        {
                            var key = parameter.Key;
                            var val = parameter.Value as string;
                            parameterDict[key] = val;
                        }
                    }

                    var endPoint = new EndPoint(className, parameterDict);
                    endPoints.Add(endPoint);
                }

                return new EndPoints(endPoints.ToArray());
            },
            namesAndErrors =>
            {
                if (namesAndErrors.Length == 0)
                {
                    succeeded = true;
                    return;
                }
                Debug.LogError("fauled to parse, errors:" + namesAndErrors.Length);
            },
            failReason =>
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

        var ep = endPointSelector.GetEndPoint<main>();
        Assert.True(ep.key0 == "val0");
        Assert.True(ep.key1 == "default_val1");
    }
}