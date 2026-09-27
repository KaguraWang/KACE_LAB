using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChemLab.Chem;
using ChemLab.Game;

namespace ChemLab.Editor {
public static class GlasswareBrandingAudit {
    public static void Run(){
        var lines=new List<string>();int pass=0,fail=0;
        void Check(string name,bool result){lines.Add((result?"PASS ":"FAIL ")+name);if(result)pass++;else fail++;}
        var texture=Resources.Load<Texture2D>("Branding/kazewa-industries-transparent");
        Check("transparent brand texture imported",texture!=null);
        Check("brand shader imported",Shader.Find("ChemLab/GlassMark")!=null);
        foreach(VesselKind kind in Enum.GetValues(typeof(VesselKind))){
            var root=new GameObject("Audit "+kind);
            GlasswareBranding.CreateVesselMark(root.transform,kind);
            var mark=root.GetComponentInChildren<GlasswareBranding>();
            bool ceramic=kind==VesselKind.Crucible||kind==VesselKind.Dish;
            Check(kind+" brand eligibility",ceramic?mark==null:mark!=null);
            if(mark!=null){
                var mesh=mark.GetComponent<MeshFilter>().sharedMesh;
                Check(kind+" has a visible mesh and material",mesh!=null&&mesh.vertexCount>0&&mark.GetComponent<MeshRenderer>().sharedMaterial!=null);
                Check(kind+" print has no picking collider",mark.GetComponent<Collider>()==null);
                float minAngle=float.PositiveInfinity,maxAngle=float.NegativeInfinity,minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
                foreach(var vertex in mesh.vertices){
                    float angle=Mathf.Atan2(vertex.z,vertex.x);
                    minAngle=Mathf.Min(minAngle,angle);maxAngle=Mathf.Max(maxAngle,angle);
                    minY=Mathf.Min(minY,vertex.y);maxY=Mathf.Max(maxY,vertex.y);
                }
                // All scale lines occupy [-PI/2-.24,-PI/2+.24]. Their
                // number labels start near -PI/2+.36. The logo begins
                // beyond that entire printed sector, with a safety gap.
                Check(kind+" avoids graduations and capacity numbers",minAngle>0f);
                Check(kind+" faces the default camera",minAngle<1.0f&&maxAngle>1.0f);
                Check(kind+" occupies only part of the wall",maxY-minY<VesselMesh.Get(kind).H*.5f);
                int centerVertex=mesh.vertexCount/2;
                var point=mesh.vertices[centerVertex];
                Check(kind+" exterior normals",Vector3.Dot(mesh.normals[centerVertex],new Vector3(point.x,0,point.z))>0);
                if(kind==VesselKind.Pipette)
                    Check("pipette capacity ring and blue band remain clear",maxY<4.91f);
                lines.Add($"METRIC {kind} angle=[{minAngle:F3},{maxAngle:F3}] y=[{minY:F3},{maxY:F3}]");
            }
            UnityEngine.Object.DestroyImmediate(root);
        }
        var tube=new GameObject("Audit condenser");
        GlasswareBranding.CreateTubeMark(tube.transform,.055f,3f);
        var tubeMark=tube.GetComponentInChildren<GlasswareBranding>();
        Check("long glass accessory bears a mark",tubeMark!=null&&tubeMark.GetComponent<MeshFilter>().sharedMesh!=null);
        Check("accessory print has no picking collider",tubeMark!=null&&tubeMark.GetComponent<Collider>()==null);
        UnityEngine.Object.DestroyImmediate(tube);
        lines.Add($"TOTAL {pass} passed, {fail} failed");
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/glassware-branding-audit.txt",lines);
        if(fail>0)throw new Exception("Glassware branding audit failed: "+fail+" checks; see Logs/glassware-branding-audit.txt");
        Debug.Log("Glassware branding audit passed: "+pass+" checks");
    }
}
}
