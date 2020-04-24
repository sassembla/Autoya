using System.Collections.Generic;
using AutoyaFramework.Settings.EndPoint;

public class slideshow : IEndPoint
{
    public string author = "default_author";
    public string date = "default_date";

    public void UpToDate(Dictionary<string, string> dataSource)
    {
        author = dataSource["author"];
    }
}


public class UnusedEndPoint : IEndPoint
{
    public string A;
    public void UpToDate(Dictionary<string, string> dataSource)
    {
        A = "here_comes";
    }
}