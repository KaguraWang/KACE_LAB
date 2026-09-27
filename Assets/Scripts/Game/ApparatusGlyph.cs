using UnityEngine;
using UnityEngine.UI;
using ChemLab.Chem;

namespace ChemLab.Game {
// Resolution-independent outline drawn from the same profile as the apparatus.
// No raster substitute or invented model; never intercepts a pointer event.
public sealed class ApparatusGlyph : MaskableGraphic {
    public VesselKind Kind;
    public static void Create(Transform parent,VesselKind kind){
        var go=new GameObject("ApparatusGlyph",typeof(RectTransform));go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);
        rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-7);rect.sizeDelta=new Vector2(54,34);
        var glyph=go.AddComponent<ApparatusGlyph>();glyph.Kind=kind;glyph.color=LabUiTheme.Ink;glyph.raycastTarget=false;
    }
    protected override void OnPopulateMesh(VertexHelper vh){
        vh.Clear();var geometry=VesselMesh.Get(Kind);var profile=VesselMesh.Profile(geometry.Type,geometry.R,geometry.H);
        Rect rect=GetPixelAdjustedRect();float h=rect.height-3;
        // Slender tools need an illustrative minimum width to remain identifiable.
        float w=Mathf.Clamp(h*geometry.R/geometry.H,3,rect.width*.40f);
        Vector2 P(Vector2 point,int side)=>new Vector2(rect.center.x+side*w*point.x/geometry.R,rect.yMin+1.5f+h*point.y/geometry.H);
        for(int side=-1;side<=1;side+=2)
            for(int i=1;i<profile.Count;i++)Line(vh,P(profile[i-1],side),P(profile[i],side),1.2f);
        Line(vh,P(profile[0],-1),P(profile[0],1),1.2f);
        if(Kind==VesselKind.Cylinder)Line(vh,new Vector2(rect.center.x-9,rect.yMin+1),new Vector2(rect.center.x+9,rect.yMin+1),1.2f);
        if(Kind==VesselKind.Burette){float y=rect.yMin+h*.14f;Line(vh,new Vector2(rect.center.x-6,y),new Vector2(rect.center.x+6,y),1.5f);}
        if(Kind==VesselKind.Beaker||Kind==VesselKind.Cylinder||Kind==VesselKind.Burette)
            for(int i=1;i<4;i++){float y=rect.yMin+h*(.2f+i*.16f);Line(vh,new Vector2(rect.center.x,y),new Vector2(rect.center.x+w*.65f,y),.85f);}
    }
    void Line(VertexHelper vh,Vector2 a,Vector2 b,float width){
        if((b-a).sqrMagnitude<.00001f)return;
        Vector2 n=new Vector2(a.y-b.y,b.x-a.x).normalized*width*.5f;int start=vh.currentVertCount;
        vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);
        vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
        vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
    }
}
}
