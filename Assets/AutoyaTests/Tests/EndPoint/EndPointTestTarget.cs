using System.Collections.Generic;
using AutoyaFramework.EndPointSelect;

public class main : IEndPoint
{
    public string key0 = "default_val0";
    public string key1 = "default_val1";

    public void UpToDate(Dictionary<string, string> dataSource)
    {
        key0 = dataSource["key0"];
    }
}


public class sub : IEndPoint
{
    public string key0;
    public void UpToDate(Dictionary<string, string> dataSource)
    {
        key0 = "default_val0";
    }
}