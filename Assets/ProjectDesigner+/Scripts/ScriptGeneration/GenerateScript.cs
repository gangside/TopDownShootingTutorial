using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static  class GenerateScript 
{
    // Start is called before the first frame update
    /// <summary>
    /// Use this to create classes based on nodes
    /// </summary>
    /// <param name="curClass"></param>
    private static void CreateClass(Node node, string path, bool makeSerializable)
    {
        string savePath = path+ "/"+ node.name + ".cs";
        string deriveFrom = "";

        Node parent = node.parentNode;//TODO: assign parent nodes based on connections -- or derivation..
        if (parent != null)
            deriveFrom = string.Format(" : {0} ", parent.name);
      

        string className = "public class " + node.name;
        className += deriveFrom;

        using (StreamWriter outfile = new StreamWriter(savePath))
        {
            outfile.WriteLine("using UnityEngine;");
            outfile.WriteLine("using System;");
            outfile.WriteLine("using System.Collections;");
            outfile.WriteLine("using System.Collections.Generic;");

            outfile.WriteLine("using Object = UnityEngine.Object;");
            outfile.WriteLine("using GameObject = UnityEngine.GameObject;\n");

            if (makeSerializable)
                outfile.WriteLine("[Serializable]");
            outfile.WriteLine(""+className + "{");
            outfile.WriteLine(" ");

            string fields = GenerateFields(node,makeSerializable);
            outfile.WriteLine("\n");
            /*string methods = */GenerateFunctions(node);
            
            /*
            string methods = create_GetterSetter(outfile);*/
            outfile.WriteLine(fields);
            //outfile.WriteLine(methods);
            outfile.WriteLine("}");//Close bracket of the class
        }//Write file(override if exist)


    }
    private static string GenerateFields(Node curClass,bool serializable)
    {
        string fields = "";

        for (int i = 0; i < curClass.nodeFields.Count; i++)
        {
            string curLine = "";
            NodeElement curField = curClass.nodeFields[i];
            
            if(serializable)
                curLine+= "\t[SerializeField] ";
            curLine += curField.protectionLevel.ToString().TrimStart('@');
            curLine += " ";
            if (curField.isCustom)
            {
                curLine += curField.customName;
                curLine += " ";
            }
            else
            {
                curLine += curField.fieldType.ToString().TrimStart('@');
                curLine += " ";
            }
            curLine += curField.name;
            curLine += ";\n";
            fields += curLine;
    }
        return fields;
    }

    private static string GenerateFunctions(Node node)
    {
        string methods = "";

        for (int i = 0; i < node.nodeMethods.Count; i++)
        {
            string curLine = "";
            NodeMethod method = node.nodeMethods[i];
            string protection= method.protectionLevel.ToString().TrimStart('@');
            string returnType = method.fieldType.ToString().TrimStart('@');

            curLine += "\t"+protection + " " + returnType + " " + method.name + "()\n\t{\n\t\treturn ";
            if (method.fieldType == FieldType.@int)
                curLine += "0";
            else if (method.fieldType== FieldType.@bool)
                curLine += "false";
            else if (method.fieldType == FieldType.Quaternion)
                curLine += "Quaternion.identity";
            else if (method.fieldType == FieldType.Vector2|| method.fieldType == FieldType.Vector3)
                curLine += "Vector3.zero";
            else if(method.fieldType != FieldType.@void)
                curLine += "null";
            curLine += ";\n\t}\n";
            methods += curLine;
       
        }
        return methods;
    }
    public static void Generate(List<Group> groups, List<Node> nodes, string root,bool makeSerializable)
    {
        Dictionary<Group, string> keyValuePairs = null;
        if (groups!=null)//TODO:Better condition check
            if (groups.Count > 0)
                keyValuePairs=GenerateGroups(groups, root);

        string savePath = CreateValidDirectory(root);

        foreach (var node in nodes)
        {
            string nodePath = savePath;
            if (node.parentGroup != null)
            {
                string directoryInfo;
                bool exists = keyValuePairs.TryGetValue(node.parentGroup, out directoryInfo);
                if (exists )
                    CreateClass(node, directoryInfo, makeSerializable);
                else
                    CreateClass(node, nodePath, makeSerializable);


            }
            else
                CreateClass(node, savePath, true);
        }

    }

    private static Dictionary<Group, string> GenerateGroups(List<Group> groups, string root)
    {
        Dictionary<Group, string> keyValuePairs = new Dictionary<Group, string>();
        string trimmedPath = root.Trim('\\','/');
        string finalPath = CreateValidDirectory(trimmedPath);
        for (int i = 0; i < groups.Count; i++)
        {
            if (!Directory.Exists(finalPath + "/" + groups[i].header))
            {

                /*DirectoryInfo info=*/Directory.CreateDirectory(finalPath + "/" + groups[i].header);
                keyValuePairs.Add(groups[i], finalPath + "/" + groups[i].header);
            }
            else
                keyValuePairs.Add(groups[i], finalPath + "/" + groups[i].header);
        }
       

        return keyValuePairs;



    }
    private static bool CheckIfDirectoryIsValid(string path)
    {
        try
        {
            Directory.GetFiles(path);
        }
        catch (Exception ex)
        {
            if (ex is DirectoryNotFoundException || ex is UnauthorizedAccessException
                                                 || ex is ArgumentException || ex is IOException)
            {
                return false;
            }

            throw;
        }
        return true; 
    }
    private static string CreateValidDirectory(string path)
    {
        if (CheckIfDirectoryIsValid(path))
            return path;
        return "Assets";
     
    }

}
