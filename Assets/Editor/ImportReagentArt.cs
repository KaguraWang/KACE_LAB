using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
using ChemLab.Game;
namespace ChemLab.Editor {
public static class ImportReagentArt {
    [Serializable] class Manifest {public Item[] items;}
    [Serializable] class Item {public int index,aliasOf;public string id,filename;}
    public static void Run(){
        Lab.EnsureReady();
        string root=Path.GetFullPath("ArtExports/kazewa_reagents_v1");
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(root,"manifest.json")));
        var byIndex=manifest.items.ToDictionary(x=>x.index);
        var byId=manifest.items.ToDictionary(x=>x.id,StringComparer.Ordinal);
        const string destination="Assets/Resources/ReagentArt";
        Directory.CreateDirectory(destination);AssetDatabase.Refresh();
        var catalog=AssetDatabase.LoadAssetAtPath<ReagentArtCatalog>("Assets/Resources/ReagentArtCatalog.asset");
        if(catalog==null){catalog=ScriptableObject.CreateInstance<ReagentArtCatalog>();AssetDatabase.CreateAsset(catalog,"Assets/Resources/ReagentArtCatalog.asset");}
        var entries=new List<ReagentArtCatalog.Entry>();var report=new List<string>{"# 试剂配图匹配报告","","来源：ArtExports/kazewa_reagents_v1。按文件实际存在判断，不依赖过期status。",""};
        int matched=0,missing=0;var imported=new Dictionary<string,Sprite>(StringComparer.Ordinal);
        foreach(var s in ChemData.Sub.Values.OrderBy(x=>x.Id,StringComparer.Ordinal)){
            if(s.IsIon||s.Cls=="internal_phase"||string.IsNullOrEmpty(s.Name)||s.Name==s.Id||(s.State=="aq"&&s.Cls=="ion")||s.Formula.Contains("R")||s.Id=="nascent_o")continue;
            Sprite sprite=null;string filename=null;
            if(byId.TryGetValue(s.Id,out var item)){
                var visited=new HashSet<int>();
                while(item!=null&&visited.Add(item.index)){
                    if(!string.IsNullOrEmpty(item.filename)){
                        if(Path.GetFileName(item.filename)!=item.filename)throw new InvalidDataException("Unsafe image filename");
                        if(File.Exists(Path.Combine(root,"images",item.filename))){filename=item.filename;break;}
                    }
                    if(item.aliasOf<=0||!byIndex.TryGetValue(item.aliasOf,out item))break;
                }
            }
            if(filename!=null){
                if(!imported.TryGetValue(filename,out sprite)){
                    string assetPath=destination+"/"+filename;
                    File.Copy(Path.Combine(root,"images",filename),assetPath,true);
                    AssetDatabase.ImportAsset(assetPath,ImportAssetOptions.ForceSynchronousImport);
                    var importer=(TextureImporter)AssetImporter.GetAtPath(assetPath);
                    importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                    importer.maxTextureSize=256;importer.mipmapEnabled=false;importer.isReadable=false;
                    importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
                    importer.SaveAndReimport();sprite=AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    if(sprite==null)throw new InvalidDataException("Sprite import failed: "+filename);
                    imported.Add(filename,sprite);
                }
                matched++;report.Add($"- 已配图 `{s.Id}` {s.Name} → {filename}");
            }else{missing++;report.Add($"- 暂无图片 `{s.Id}` {s.Name}");}
            entries.Add(new ReagentArtCatalog.Entry{id=s.Id,image=sprite});
        }
        catalog.entries=entries;EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        report.Insert(4,$"匹配 {matched} 个物质，缺图 {missing} 个；实际导入 {imported.Count} 张图片（仅显式aliasOf共享）。\n");
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/reagent-art-mapping.md",report);
        Debug.Log($"[ReagentArt] matched={matched} missing={missing} uniqueImages={imported.Count} total={entries.Count}");
        EditorApplication.Exit(0);
    }
}
}
