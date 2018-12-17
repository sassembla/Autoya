using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

public class LinkXMLGenerator
{
    /*
        IDからClassNameを得る
     */
    private static string GetClassNameById(int id)
    {
        var editorAssemblyPath = string.Empty;
        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                editorAssemblyPath = Path.Combine(EditorApplication.applicationPath, "Contents/Managed/UnityEditor.dll");
                break;

            case RuntimePlatform.WindowsEditor:
                var windowsDirBasePath = Directory.GetParent(Directory.GetParent(EditorApplication.applicationPath).ToString()).ToString();
                editorAssemblyPath = Path.Combine(windowsDirBasePath, "Editor/Data/Managed/UnityEditor.dll");
                break;

        }

        var type = Assembly.LoadFile(editorAssemblyPath).GetType("UnityEditor.UnityType");
        if (type != null)
        {
            var methodInfo = type.GetMethod("FindTypeByPersistentTypeID");
            if (methodInfo != null)
            {
                var parametersArray = new object[] { id };
                var resultObj = methodInfo.Invoke(methodInfo, parametersArray);
                var props = resultObj.GetType().GetProperties();
                foreach (var prop in props)
                {
                    if (prop.Name == "name")
                    {
                        var result = prop.GetValue(resultObj);
                        return result as string;
                    }

                    // Debug.Log("key:" + prop.Name + " result:" + result);
                }
            }
        }
        return string.Empty;
    }

    /*
        Link.xmlを生成する。
        すでに存在する場合マージを行う。
     */
    public static void ExportLinkXMLWithUsingClassIds(string generateTargetFolderPath, params int[] classIds)
    {
        var targetPath = Path.Combine(generateTargetFolderPath, "link.xml");

        var lastClassNames = new List<string>();
        foreach (var classId in classIds)
        {
            lastClassNames.Add(GetClassNameById(classId));
        }

        var baseRuntimeDllPath = string.Empty;
        var wholeRuntimeDllPath = string.Empty;

        switch (Application.platform)
        {
            case RuntimePlatform.OSXEditor:
                baseRuntimeDllPath = Path.Combine(EditorApplication.applicationPath, "Contents/Managed/UnityEngine.dll");
                wholeRuntimeDllPath = Path.Combine(EditorApplication.applicationPath, "Contents/Managed/UnityEngine");
                break;
            case RuntimePlatform.WindowsEditor:
                baseRuntimeDllPath = Path.Combine(EditorApplication.applicationPath, "not yet tested");
                wholeRuntimeDllPath = Path.Combine(EditorApplication.applicationPath, "not yet tested");
                break;
        }

        var dllPaths = Directory.GetFiles(wholeRuntimeDllPath).Where(path => path.EndsWith(".dll")).ToList();
        dllPaths.Add(baseRuntimeDllPath);

        var className_FullNameDict = CollectClassNames(dllPaths.ToArray());
        var asmName_classNamesDict = new Dictionary<string, HashSet<string>>();
        foreach (var lastClassName in lastClassNames)
        {
            if (!className_FullNameDict.ContainsKey(lastClassName))
            {
                continue;
            }

            var asmName_fullClassNamePair = className_FullNameDict[lastClassName];
            var asmName = asmName_fullClassNamePair.Key;
            var fullClassName = asmName_fullClassNamePair.Value;

            if (!asmName_classNamesDict.ContainsKey(asmName))
            {
                asmName_classNamesDict[asmName] = new HashSet<string>();
            }

            asmName_classNamesDict[asmName].Add(fullClassName);
        }

        // ファイルが存在しなければ生成する。
        if (!File.Exists(targetPath))
        {
            var emptyLinkXml = "<linker></linker>";
            using (var sw = new StreamWriter(targetPath))
            {
                sw.WriteLine(emptyLinkXml);
            }
            AssetDatabase.Refresh();
        }

        // 既存のファイルを読み込む
        var document = new XmlDocument();
        document.Load(targetPath);


        // 既存のファイルの要素を読み込む
        foreach (var child in document.ChildNodes)
        {
            foreach (var child2 in ((XmlElement)child).ChildNodes)
            {
                var asmName = ((XmlElement)child2).GetAttribute("fullname");

                if (!asmName_classNamesDict.ContainsKey(asmName))
                {
                    asmName_classNamesDict[asmName] = new HashSet<string>();
                }

                foreach (var child3 in ((XmlElement)child2).ChildNodes)
                {
                    var className = ((XmlElement)child3).GetAttribute("fullname");
                    asmName_classNamesDict[asmName].Add(className);
                }
            }
        }

        var stringBuilder = new StringBuilder();

        var header =
"<linker>"
        ;
        stringBuilder.AppendLine(header);

        foreach (var asmName in asmName_classNamesDict.Keys)
        {
            var asmHeaderDescription =
"   <assembly fullname=\"" + asmName + "\">"
            ;
            stringBuilder.AppendLine(asmHeaderDescription);



            var fullClassNames = asmName_classNamesDict[asmName];
            foreach (var fullClassName in fullClassNames)
            {
                var classDescription =
"		<type fullname=\"" + fullClassName + "\" preserve=\"all\"/>"
                ;
                stringBuilder.AppendLine(classDescription);
            }


            var asmFooterDescription =
"   </assembly>"
            ;
            stringBuilder.AppendLine(asmFooterDescription);
        }



        var footer =
"</linker>"
        ;


        stringBuilder.AppendLine(footer);

        using (var sw = new StreamWriter(targetPath, false))
        {
            sw.WriteLine(stringBuilder.ToString());
        }

        AssetDatabase.Refresh();
    }

    private static Dictionary<string, KeyValuePair<string, string>> CollectClassNames(string[] dllPaths)
    {
        var resultDict = new Dictionary<string, KeyValuePair<string, string>>();
        foreach (var path in dllPaths)
        {
            try
            {
                var assembly = Assembly.LoadFile(path);
                var asmName = assembly.GetName().Name;
                if (asmName == "UnityEngine.CoreModule")
                {
                    asmName = "UnityEngine";
                }

                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var typeLastName = type.ToString().Split('.').Last();
                    // Debug.Log("type:" + typeLastName + " full:" + type.FullName);

                    // ignore nested class(a+b)
                    if (typeLastName.Contains("+"))
                    {
                        continue;
                    }

                    var asmNameFullNamePair = new KeyValuePair<string, string>(asmName, type.FullName);
                    resultDict[typeLastName] = asmNameFullNamePair;
                }
            }
            catch (Exception e)
            {
                Debug.Log("e:" + e);
            }
        }

        return resultDict;
    }
}