using System;
using System.Collections;
using AutoyaFramework;
using AutoyaFramework.Settings.EndPoint;
using Miyamasu;
using NUnit.Framework;

/**
    EndPointImplementationのTestを行う
 */
public class EndPointImplementationTests : MiyamasuTestRunner
{
    private string baseUrl;

    [MSetup]
    public IEnumerator Setup()
    {
        baseUrl = EndPointSelectorSettings.ENDPOINT_INFO_URL;
        while (!Autoya.Auth_IsAuthenticated())
        {
            yield return null;
        }
    }

    [MTeardown]
    public void Teardown()
    {
        EndPointSelectorSettings.ENDPOINT_INFO_URL = baseUrl;
    }

    [MTest]
    public IEnumerator EndPointUpdateWithEmptyInfo()
    {
        var cor = Autoya.Debug_EndPointUpdate(new AutoyaFramework.EndPointSelect.IEndPoint[] { new main(), new sub() });
        while (cor.MoveNext())
        {
            yield return null;
        }

        EndPointSelectorSettings.ENDPOINT_INFO_URL = "https://raw.githubusercontent.com/sassembla/Autoya/master/Assets/AutoyaTests/RuntimeData/EndPoints/empty.json";

        // empty json effects nothing. eps are not changed from default.
        var mainEp = Autoya.EndPoint_GetEndPoint<main>();
        Assert.True(mainEp.key0 == "default_val0", "not match. mainEp.key0:" + mainEp.key0);
        Assert.True(mainEp.key1 == "default_val1", "not match. mainEp.key1:" + mainEp.key1);

        var subEp = Autoya.EndPoint_GetEndPoint<sub>();
        Assert.True(subEp.key0 == null, "not match. subEp.key0:" + subEp.key0);
    }

    [MTest]
    public IEnumerator EndPointUpdate()
    {
        EndPointSelectorSettings.ENDPOINT_INFO_URL = "https://raw.githubusercontent.com/sassembla/Autoya/master/Assets/AutoyaTests/RuntimeData/EndPoints/mainAndSub.json";

        var cor = Autoya.Debug_EndPointUpdate(new AutoyaFramework.EndPointSelect.IEndPoint[] { new main(), new sub() });
        while (cor.MoveNext())
        {
            yield return null;
        }

        // epUpdate succeeded and updated.
        var mainEp = Autoya.EndPoint_GetEndPoint<main>();
        Assert.True(mainEp.key0 == "val0", "not match. mainEp.key0:" + mainEp.key0);
        Assert.True(mainEp.key1 == "default_val1", "not match. mainEp.key1:" + mainEp.key1);

        var subEp = Autoya.EndPoint_GetEndPoint<sub>();
        Assert.True(subEp.key0 == "default_val0", "not match. subEp.key0:" + subEp.key0);
    }


    [MTest]
    public IEnumerator EndPointUpdateSub()
    {
        EndPointSelectorSettings.ENDPOINT_INFO_URL = "https://raw.githubusercontent.com/sassembla/Autoya/master/Assets/AutoyaTests/RuntimeData/EndPoints/sub.json";

        var cor = Autoya.Debug_EndPointUpdate(new AutoyaFramework.EndPointSelect.IEndPoint[] { new main(), new sub() });
        while (cor.MoveNext())
        {
            yield return null;
        }

        // epUpdate succeeded, main not updated and sub is updated.
        var mainEp = Autoya.EndPoint_GetEndPoint<main>();
        Assert.True(mainEp.key0 == "default_val0", "not match. mainEp.key0:" + mainEp.key0);
        Assert.True(mainEp.key1 == "default_val1", "not match. mainEp.key1:" + mainEp.key1);

        var subEp = Autoya.EndPoint_GetEndPoint<sub>();
        Assert.True(subEp.key0 == "default_val0", "not match. subEp.key0:" + subEp.key0);
    }
}