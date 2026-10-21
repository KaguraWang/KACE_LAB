using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ChemLab.Editor {
public static class NessieAssetTools {
 public static void Prepare(){
  const string root="Assets/Art/Nessie/";
  AssetDatabase.Refresh();
  foreach(var path in new[]{root+"Textures/Texture_02.png",root+"Textures/Texture_06.png"}){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();
  }
  foreach(var path in Directory.GetFiles("Assets/Resources/Themes/Nessie","*.png")){
   var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.SaveAndReimport();
  }
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(root+"Nessie.fbx");
  if(model==null)throw new Exception("Nessie model failed to import");
  Directory.CreateDirectory(root+"Materials");
  var instance=UnityEngine.Object.Instantiate(model);instance.name="LittleNessie";
  var notes=new System.Collections.Generic.List<string>();
  foreach(var renderer in instance.GetComponentsInChildren<Renderer>()){
   var materials=renderer.sharedMaterials;
   for(int i=0;i<materials.Length;i++){
    var original=materials[i];if(original==null)throw new Exception("Missing material "+renderer.name);
    string name=original.name.Replace(" (Instance)","");
    string path=root+"Materials/"+name+".mat";
    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(mat==null){mat=new Material(original);AssetDatabase.CreateAsset(mat,path);}
    // FBX carries base color; explicitly bind the fabric normal maps too.
    string lower=name.ToLowerInvariant();int index=lower.Contains("ivory")?0:lower.Contains("moss")?4:-1;
    if(index>=0){
     mat.shader=Shader.Find("Standard");mat.color=Color.white;
     mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"Textures/Texture_"+index.ToString("00")+".png");
     mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"Textures/Texture_"+(index+2).ToString("00")+".png"));
     mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Glossiness",.15f);mat.SetFloat("_BumpScale",.4f);
    }
    materials[i]=mat;notes.Add(renderer.name+" / "+name+" / texture="+(mat.mainTexture!=null?mat.mainTexture.name:"solid color"));
   }
   renderer.sharedMaterials=materials;
  }
  if(instance.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.vertexCount)==0)throw new Exception("Empty model");
  PrefabUtility.SaveAsPrefabAsset(instance,root+"LittleNessie.prefab");UnityEngine.Object.DestroyImmediate(instance);
  AssetDatabase.SaveAssets();File.WriteAllLines("Logs/nessie-model-audit.txt",notes);
  Debug.Log("NESSIE_PREFAB_OK");
 }
}
}
