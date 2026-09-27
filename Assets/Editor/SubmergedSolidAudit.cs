using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChemLab.Game;
namespace ChemLab.Editor {
public static class SubmergedSolidAudit {
    public static void Run(){
        int pass=0,fail=0;var lines=new List<string>();
        void Check(string label,bool ok){lines.Add((ok?"PASS ":"FAIL ")+label);if(ok)pass++;else fail++;}
        var cube=new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,1,-1),new Vector3(-1,1,-1),new Vector3(-1,-1,1),new Vector3(1,-1,1),new Vector3(1,1,1),new Vector3(-1,1,1)};
        var faces=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5};
        var unit=new SubmergedSolidShape(cube,faces,Matrix4x4.identity);
        for(int i=0;i<=20;i++){
            float y=-1+i*.1f;var sample=unit.Measure(y);
            Check("cube analytic displaced volume "+i,Math.Abs(sample.Volume-4*(y+1))<1e-6);
            if(i>0)Check("cube analytic buoyancy centroid "+i,Vector3.Distance(sample.Centroid,new Vector3(0,(y-1)*.5f,0))<1e-6);
        }
        Check("dry cube has no displacement",unit.Measure(-2).Volume==0);
        Check("fully immersed cube stops displacing more",Math.Abs(unit.Measure(10).Volume-8)<1e-10);
        var reversed=(int[])faces.Clone();Array.Reverse(reversed);
        Check("reversed global winding retains volume",Math.Abs(new SubmergedSolidShape(cube,reversed,Matrix4x4.identity).Measure(.2f).Volume-unit.Measure(.2f).Volume)<1e-10);
        var tetra=new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.forward};
        var tetraFaces=new[]{0,2,1,0,1,3,0,3,2,1,2,3};
        var pyramid=new SubmergedSolidShape(tetra,tetraFaces,Matrix4x4.identity);
        for(int i=0;i<=10;i++){float y=i*.1f;Check("tetra analytic cut "+i,Math.Abs(pyramid.Measure(y).Volume-(1-Math.Pow(1-y,3))/6)<1e-7);}
        Check("tetra full centroid",Vector3.Distance(pyramid.Measure(1).Centroid,Vector3.one*.25f)<1e-6);
        foreach(bool ice in new[]{false,true})foreach(float angle in new[]{0f,17f,63f}){
            var vertices=ice?IceChunkShape.Vertices:GrainShape.Vertices;var triangles=ice?IceChunkShape.Faces:GrainShape.Faces;
            double sourceVolume=ice?IceChunkShape.Volume:GrainShape.Volume;
            var matrix=Matrix4x4.TRS(new Vector3(.7f,1.3f,-.4f),Quaternion.Euler(angle,31,angle*.4f),new Vector3(.7f,.9f,1.1f));
            var shape=new SubmergedSolidShape(vertices,triangles,matrix);
            // Quaternion -> Matrix4x4 is float precision, not an exact orthogonal
            // rotation. Compare the clipping integral to its actual determinant.
            double determinant=(double)matrix.m00*((double)matrix.m11*matrix.m22-(double)matrix.m12*matrix.m21)
                -(double)matrix.m01*((double)matrix.m10*matrix.m22-(double)matrix.m12*matrix.m20)
                +(double)matrix.m02*((double)matrix.m10*matrix.m21-(double)matrix.m11*matrix.m20);
            double expected=sourceVolume*Math.Abs(determinant);
            lines.Add($"METRIC {ice} {angle} actual={shape.TotalVolume:G12} expected={expected:G12} determinant={determinant:G12} delta={shape.TotalVolume-expected:G12}");
            Check($"{ice} {angle} preserves actual mesh scaled volume",Math.Abs(shape.TotalVolume-expected)<2e-7);
            double prior=0;
            for(int i=0;i<=20;i++){
                float surface=Mathf.Lerp(shape.Bottom,shape.Top,i/20f);var sample=shape.Measure(surface);
                Check($"{ice} {angle} displacement monotonic/bounded {i}",sample.Volume>=prior-1e-8&&sample.Volume<=shape.TotalVolume+1e-8);
                if(sample.Volume>1e-12)Check($"{ice} {angle} buoyancy center inside wetted height {i}",sample.Centroid.y>=shape.Bottom-1e-6&&sample.Centroid.y<=surface+1e-6);
                prior=sample.Volume;
            }
            foreach(double fraction in new[]{.01,.3,.5,.917,.99}){
                float level=shape.PlaneForFraction(fraction);var wet=shape.Measure(level);
                Check($"{ice} {angle} floating density ratio {fraction}",Math.Abs(wet.Volume/shape.TotalVolume-fraction)<2e-6);
            }
        }
        bool invalid=false;try{unit.PlaneForFraction(double.NaN);}catch(ArgumentOutOfRangeException){invalid=true;}
        Check("invalid density ratio rejected",invalid);
        lines.Add($"TOTAL {pass} passed, {fail} failed");Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/submerged-solid-audit.txt",lines);
        if(fail>0)throw new Exception("Submerged solid audit failed: "+fail);
        Debug.Log("Submerged solid audit passed: "+pass);
    }
}
}
