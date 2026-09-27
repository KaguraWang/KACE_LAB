using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;

namespace ChemLab.Editor {
public static class SubstanceInventoryExport {
    static string Cell(string value)=>(value??"").Replace("|","\\|").Replace("\r"," ").Replace("\n"," ");
    static string State(string value){
        switch(value){case "s":return "固体 (s)";case "l":return "液体 (l)";case "g":return "气体 (g)";case "aq":return "溶液/水合态 (aq)";default:return Cell(value);}
    }
    [MenuItem("ChemLab/导出物质总清单")]
    public static void Run(){
        Lab.EnsureReady();
        var all=ChemData.Sub.Values.OrderBy(s=>s.Id,StringComparer.Ordinal).ToArray();
        var shelf=all.Where(s=>!s.IsIon&&s.Cls!="internal_phase").ToArray();
        var ions=all.Where(s=>s.IsIon).ToArray();
        var internalPhases=all.Where(s=>!s.IsIon&&s.Cls=="internal_phase").ToArray();
        if(shelf.Length+ions.Length+internalPhases.Length!=all.Length)throw new Exception("Inventory partition mismatch");
        var text=new StringBuilder();
        text.AppendLine("# ChemLab 当前物质总清单\n");
        text.AppendLine("导出时间："+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"（本机时间）\n");
        text.AppendLine("来源：教师工程运行时初始化完成后的 `ChemData.Sub`，包含基础表、有机扩展、离子与内部相态；不是从文档手工摘录。\n");
        text.AppendLine($"总计 **{all.Length}** 个内部条目：试剂架可添加物质 **{shelf.Length}** 种，离子/带电物种 **{ions.Length}** 种，内部相态 **{internalPhases.Length}** 种。同一化学式的不同内部 ID 分别保留，例如水与冰。\n");
        text.AppendLine("物态列是当前程序的试剂架默认值（内部物种按登记状态列出），不是本次重新核实的实验物性；温度、压力及溶剂变化后物态可能不同。原始名称和状态单列，便于识别旧表与现行显示之间的差别。收录不表示全部反应、物性或外观均已实现/验收。\n");
        void Section(string heading,Substance[] rows,bool onShelf){
            text.AppendLine("## "+heading+"\n");
            text.AppendLine("| 序号 | 当前名称 | 化学式 | 内部 ID | 默认物态 | 分类 | 原始登记名称 | 原始登记状态 |");
            text.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
            int index=0;
            foreach(var s in rows){
                var name=onShelf?PhaseProperties.ShelfName(s.Id):s.Name;
                var phase=onShelf?PhaseProperties.Phase(s.Id):s.State;
                text.AppendLine($"| {++index} | {Cell(name)} | {Cell(s.Formula)} | `{Cell(s.Id)}` | {State(phase)} | {Cell(s.Cls)} | {Cell(s.Name)} | {State(s.State)} |");
            }
            text.AppendLine();
        }
        Section("一、试剂架可添加物质",shelf,true);
        Section("二、反应引擎离子/带电物种（不直接显示在试剂架）",ions,false);
        Section("三、内部相态（不直接显示在试剂架）",internalPhases,false);
        var path=Path.GetFullPath("物质总清单.md");
        File.WriteAllText(path,text.ToString(),new UTF8Encoding(false));
        Debug.Log($"[SubstanceInventory] total={all.Length} shelf={shelf.Length} ions={ions.Length} internal={internalPhases.Length} path={path}");
        if(Application.isBatchMode)EditorApplication.Exit(0);
    }
}
}
