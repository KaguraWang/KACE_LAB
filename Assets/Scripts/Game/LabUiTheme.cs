using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Game {
// User-selected C direction: translucent white surfaces, black ink and Miku teal.
// UI geometry only; no textures, scene materials or input hit boxes are changed.
public static class LabUiTheme {
    public static readonly Color Panel=new Color(1,1,1,.88f);
    public static readonly Color Card=new Color(1,1,1,.82f);
    public static readonly Color Ink=Color.black;
    public static readonly Color Muted=new Color(.15f,.17f,.18f,1);
    public static readonly Color Accent=new Color32(0x39,0xC5,0xBB,0xFF);
    public static readonly Color Border=new Color(.25f,.27f,.28f,.85f);
    public static void Button(Button button){
        var c=button.colors;c.normalColor=Color.white;
        c.highlightedColor=new Color(.90f,.98f,.97f,1);
        c.pressedColor=new Color(.76f,.91f,.89f,1);
        c.selectedColor=Color.white;c.disabledColor=new Color(.8f,.8f,.8f,.6f);
        c.fadeDuration=.12f;button.colors=c;
    }
    public static void Input(InputField input){
        if(input.targetGraphic!=null)input.targetGraphic.color=Accent;
        var background=input.GetComponent<Image>();if(background!=null)background.color=Accent;
        if(input.textComponent!=null)input.textComponent.color=Ink;
        if(input.placeholder is Text placeholder)placeholder.color=Muted;
        input.customCaretColor=true;input.caretColor=Ink;
        input.selectionColor=new Color(0,.2f,.18f,.25f);
        var c=input.colors;c.normalColor=c.highlightedColor=c.selectedColor=Color.white;
        c.pressedColor=Color.white;input.colors=c;
    }
}

// Remains an Image so existing masks, color transitions and hit testing work.
public sealed class LabUiSurface : Image {
    public float Radius=9;
    public bool StrongBorder;
    public bool WarningBorder;
    protected override void OnPopulateMesh(VertexHelper mesh){
        if(sprite!=null){base.OnPopulateMesh(mesh);return;}
        mesh.Clear();Rect rect=GetPixelAdjustedRect();
        if(rect.width<=0||rect.height<=0)return;
        float radius=Mathf.Min(Radius,Mathf.Min(rect.width,rect.height)*.5f);
        const int steps=6,count=4*(steps+1);
        mesh.AddVert(rect.center,color,Vector2.zero);
        for(int i=0;i<count;i++)mesh.AddVert(Point(rect,radius,i,steps),color,Vector2.zero);
        for(int i=0;i<count;i++)mesh.AddTriangle(0,1+i,1+(i+1)%count);
        if(rect.width<8||rect.height<8)return;
        Color edge=WarningBorder?new Color32(0xA6,0x19,0x19,0xFF):StrongBorder?LabUiTheme.Border:new Color(.4f,.46f,.48f,.23f);
        edge.a*=color.a;
        var inner=Rect.MinMaxRect(rect.xMin+1,rect.yMin+1,rect.xMax-1,rect.yMax-1);
        int start=mesh.currentVertCount;
        for(int i=0;i<count;i++){
            mesh.AddVert(Point(rect,radius,i,steps),edge,Vector2.zero);
            mesh.AddVert(Point(inner,Mathf.Max(0,radius-1),i,steps),edge,Vector2.zero);
        }
        for(int i=0;i<count;i++){
            int a=start+2*i,b=start+2*((i+1)%count);
            mesh.AddTriangle(a,b,a+1);mesh.AddTriangle(a+1,b,b+1);
        }
    }
    static Vector2 Point(Rect rect,float radius,int index,int steps){
        int corner=index/(steps+1);float angle=(180+corner*90+(index%(steps+1))*90f/steps)*Mathf.Deg2Rad;
        var center=new Vector2(corner==0||corner==3?rect.xMin+radius:rect.xMax-radius,
            corner<2?rect.yMin+radius:rect.yMax-radius);
        return center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
    }
}
}
