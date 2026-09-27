/* =====================================================================
   迷你 JSON 解析器
   只为读测试用例用，支持 object / array / string / number / bool / null。
   不依赖 Unity，纯 C#。
   ===================================================================== */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChemLab.Spec {

public class JVal {
    public enum K { Null, Bool, Num, Str, Arr, Obj }
    public K Kind = K.Null;
    public bool B;
    public double N;
    public string S;
    public List<JVal> A;
    public Dictionary<string, JVal> O;

    public bool IsNull => Kind == K.Null;
    public static JVal Null() => new JVal { Kind = K.Null };

    public JVal this[string key] {
        get {
            if (Kind == K.Obj && O != null) { JVal v; if (O.TryGetValue(key, out v)) return v; }
            return Null();
        }
    }
    public JVal this[int i] {
        get {
            if (Kind == K.Arr && A != null && i >= 0 && i < A.Count) return A[i];
            return Null();
        }
    }
    public int Count => Kind == K.Arr && A != null ? A.Count : 0;

    public double Num(double dflt = 0) => Kind == K.Num ? N : (Kind == K.Bool ? (B ? 1 : 0) : dflt);
    public string Str(string dflt = "") => Kind == K.Str ? S : (Kind == K.Num ? N.ToString(CultureInfo.InvariantCulture) : dflt);
    public bool Bool(bool dflt = false) => Kind == K.Bool ? B : (Kind == K.Num ? Math.Abs(N) > 1e-12 : dflt);

    public List<string> Keys() => Kind == K.Obj && O != null ? new List<string>(O.Keys) : new List<string>();
}

public static class MiniJson {
    public static JVal Parse(string text) {
        int i = 0;
        var v = ParseValue(text, ref i);
        SkipWs(text, ref i);
        return v;
    }

    static void SkipWs(string s, ref int i) {
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
    }

    static JVal ParseValue(string s, ref int i) {
        SkipWs(s, ref i);
        if (i >= s.Length) return JVal.Null();
        char c = s[i];
        if (c == '{') return ParseObj(s, ref i);
        if (c == '[') return ParseArr(s, ref i);
        if (c == '"') return new JVal { Kind = JVal.K.Str, S = ParseStr(s, ref i) };
        if (c == 't') { i += 4; return new JVal { Kind = JVal.K.Bool, B = true }; }
        if (c == 'f') { i += 5; return new JVal { Kind = JVal.K.Bool, B = false }; }
        if (c == 'n') { i += 4; return JVal.Null(); }
        return ParseNum(s, ref i);
    }

    static JVal ParseObj(string s, ref int i) {
        var o = new Dictionary<string, JVal>();
        i++;                                   // {
        SkipWs(s, ref i);
        if (i < s.Length && s[i] == '}') { i++; return new JVal { Kind = JVal.K.Obj, O = o }; }
        while (i < s.Length) {
            SkipWs(s, ref i);
            if (s[i] != '"') break;
            string k = ParseStr(s, ref i);
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ':') i++;
            o[k] = ParseValue(s, ref i);
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ',') { i++; continue; }
            if (i < s.Length && s[i] == '}') { i++; break; }
            break;
        }
        return new JVal { Kind = JVal.K.Obj, O = o };
    }

    static JVal ParseArr(string s, ref int i) {
        var a = new List<JVal>();
        i++;                                   // [
        SkipWs(s, ref i);
        if (i < s.Length && s[i] == ']') { i++; return new JVal { Kind = JVal.K.Arr, A = a }; }
        while (i < s.Length) {
            a.Add(ParseValue(s, ref i));
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ',') { i++; continue; }
            if (i < s.Length && s[i] == ']') { i++; break; }
            break;
        }
        return new JVal { Kind = JVal.K.Arr, A = a };
    }

    static string ParseStr(string s, ref int i) {
        var sb = new StringBuilder();
        i++;                                   // "
        while (i < s.Length && s[i] != '"') {
            if (s[i] == '\\' && i + 1 < s.Length) {
                i++;
                char e = s[i];
                switch (e) {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 < s.Length) {
                            sb.Append((char)Convert.ToInt32(s.Substring(i + 1, 4), 16));
                            i += 4;
                        }
                        break;
                    default: sb.Append(e); break;
                }
            } else sb.Append(s[i]);
            i++;
        }
        i++;                                   // 收尾 "
        return sb.ToString();
    }

    static JVal ParseNum(string s, ref int i) {
        int st = i;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' ||
                                s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
        double d;
        double.TryParse(s.Substring(st, i - st), NumberStyles.Float, CultureInfo.InvariantCulture, out d);
        return new JVal { Kind = JVal.K.Num, N = d };
    }

    /* ---------------- 输出：把结果对象写成 JSON ---------------- */
    public static string Write(JVal v) {
        var sb = new StringBuilder();
        WriteTo(sb, v);
        return sb.ToString();
    }
    static void WriteTo(StringBuilder sb, JVal v) {
        if (v == null) { sb.Append("null"); return; }
        switch (v.Kind) {
            case JVal.K.Null: sb.Append("null"); break;
            case JVal.K.Bool: sb.Append(v.B ? "true" : "false"); break;
            case JVal.K.Num:
                if (double.IsNaN(v.N) || double.IsInfinity(v.N)) sb.Append("null");
                else sb.Append(v.N.ToString("R", CultureInfo.InvariantCulture));
                break;
            case JVal.K.Str: WriteStr(sb, v.S); break;
            case JVal.K.Arr:
                sb.Append('[');
                for (int i = 0; i < v.A.Count; i++) { if (i > 0) sb.Append(','); WriteTo(sb, v.A[i]); }
                sb.Append(']');
                break;
            case JVal.K.Obj:
                sb.Append('{');
                bool first = true;
                foreach (var kv in v.O) {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteStr(sb, kv.Key); sb.Append(':'); WriteTo(sb, kv.Value);
                }
                sb.Append('}');
                break;
        }
    }
    static void WriteStr(StringBuilder sb, string s) {
        sb.Append('"');
        foreach (char c in s ?? "") {
            switch (c) {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    /* 便捷构造 */
    public static JVal Obj() => new JVal { Kind = JVal.K.Obj, O = new Dictionary<string, JVal>() };
    public static JVal Arr() => new JVal { Kind = JVal.K.Arr, A = new List<JVal>() };
    public static JVal Num(double d) => new JVal { Kind = JVal.K.Num, N = d };
    public static JVal Str(string s) => new JVal { Kind = JVal.K.Str, S = s };
    public static JVal Bool(bool b) => new JVal { Kind = JVal.K.Bool, B = b };
    public static JVal Put(this JVal o, string k, JVal v) { o.O[k] = v; return o; }
    public static JVal Push(this JVal a, JVal v) { a.A.Add(v); return a; }
}

}
