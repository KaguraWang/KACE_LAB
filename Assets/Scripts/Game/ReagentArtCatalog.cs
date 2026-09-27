using System;
using System.Collections.Generic;
using UnityEngine;
namespace ChemLab.Game {
public sealed class ReagentArtCatalog:ScriptableObject {
    [Serializable] public class Entry {public string id;public Sprite image;}
    public List<Entry> entries=new List<Entry>();
    Dictionary<string,Sprite> lookup;
    public Sprite Find(string id){
        if(lookup==null){lookup=new Dictionary<string,Sprite>(StringComparer.Ordinal);foreach(var e in entries)lookup[e.id]=e.image;}
        return lookup.TryGetValue(id,out var sprite)?sprite:null;
    }
}
}
