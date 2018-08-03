// /*-------------------------------------------
// ---------------------------------------------
// Creation Date: #DATETIME#
// Author: Ben MacKinnon
// Description: 
// Soluis Technolgies ltd.
// ---------------------------------------------
// -------------------------------------------*/


using UnityEngine;
using UnityEditor;
using System.Collections;

public class KeywordReplace : UnityEditor.AssetModificationProcessor {

    public static void OnWillCreateAsset (string path)
    {
        path = path.Replace(".meta", "");
        int index = path.LastIndexOf(".");
        if (index < 0)
            return;


        string file = path.Substring(index);
        if (file != ".cs" && file != ".js" && file != ".boo")
            return;


        index = Application.dataPath.LastIndexOf("Assets");
        path = Application.dataPath.Substring(0, index) + path;
        if (!System.IO.File.Exists(path))
            return;

        string fileContent = System.IO.File.ReadAllText(path);

        fileContent = fileContent.Replace("#CREATIONDATE#", System.DateTime.Today.ToString("dd/MM/yy") + "");
        fileContent = fileContent.Replace("#PROJECTNAME#", PlayerSettings.productName);
        fileContent = fileContent.Replace("#DEVELOPER#", System.Environment.UserName);
        
        System.IO.File.WriteAllText(path, fileContent);
        AssetDatabase.Refresh();
    }

}
