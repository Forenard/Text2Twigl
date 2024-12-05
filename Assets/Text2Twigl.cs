using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class Text2Twigl : EditorWindow
{
    [MenuItem("Text2Twigl/Create")]
    private static void CreateWindow()
    {
        var window = GetWindowWithRect<Text2Twigl>(new Rect(0, 0, 650, 650));
        window.Show();
    }

    public static Texture2D _fontTexture;
    private string _jisx0208name = "JISX0208ToUnicode";
    private string _jisx0201name = "JISX0201ToUnicode";
    private string _fullwidthFontName = "misaki_gothic_2nd";
    private string _halfwidthFontName = "misaki_gothic_2nd_4x8";
    private string _text = "";
    private string _log = "";
    private void OnGUI()
    {
        GUILayout.Space(10);
        _jisx0208name = EditorGUILayout.TextField("JISX0208ToUnicode (Path)", _jisx0208name);
        _jisx0201name = EditorGUILayout.TextField("JISX0201ToUnicode (Path)", _jisx0201name);
        _fullwidthFontName = EditorGUILayout.TextField("Fullwidth Font (Path)", _fullwidthFontName);
        _halfwidthFontName = EditorGUILayout.TextField("Halfwidth Font (Path)", _halfwidthFontName);

        GUILayout.Space(10);
        // _text = EditorGUILayout.TextField("文章", _text, GUILayout.Height(100));
        // multiline
        _text = EditorGUILayout.TextArea(_text, GUILayout.Height(100));
        GUILayout.Space(10);
        if (GUILayout.Button("変換"))
        {
            _log = "";
            TextAsset _jis2uniFull = Resources.Load<TextAsset>(_jisx0208name);
            TextAsset _jis2uniHalf = Resources.Load<TextAsset>(_jisx0201name);
            Texture2D _fontTextureFull = Resources.Load<Texture2D>(_fullwidthFontName);
            Texture2D _fontTextureHalf = Resources.Load<Texture2D>(_halfwidthFontName);
            var lineFull = _jis2uniFull.text.Split('\n');
            var uni2fontTexelFull = new Dictionary<int, Vector2Int>();
            for (int i = 0; i < lineFull.Length; i++)
            {
                /*
                0  1  2    3    4    5     6
                区 点 JIS  SJIS EUC  UTF-8  UTF-16 実体(SJIS)
                03 50 2352 8271 A3D2 EFBCB2 FF32 Ｒ
                */
                var l = lineFull[i].Split(' ');
                if (l.Length < 7) continue;
                if (!int.TryParse(l[0], out int cu))
                {
                    continue;
                }
                if (!int.TryParse(l[1], out int ten))
                {
                    continue;
                }
                if (!int.TryParse(l[6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int uni))
                {
                    continue;
                }
                uni2fontTexelFull.Add(uni, new Vector2Int(ten - 1, cu - 1));
            }
            var lineHalf = _jis2uniHalf.text.Split('\n');
            var uni2fontTexelHalf = new Dictionary<int, Vector2Int>();
            for (int i = 0; i < lineHalf.Length; i++)
            {
                /*
                0   1    2    3      4      5
                JIS SJIS EUC  UTF-8  UTF-16 実体(SJIS)
                21  21   21   21     0021   !
                A1  A1   8EA1 EFBDA1 FF61   ｡
                */
                var l = lineHalf[i].Split(' ');
                if (l.Length < 5) continue;
                if (!int.TryParse(l[4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int uni))
                {
                    continue;
                }
                Vector2Int texel = new Vector2Int();
                if (uni < 0xff00)
                {
                    // ASCII
                    texel.x = uni % 16;
                    texel.y = uni / 16;
                }
                else
                {
                    // 半角カナ
                    texel.x = (uni - 0xff00 + 0x0040) % 16;
                    texel.y = (uni - 0xff00 + 0x0040) / 16;
                }
                uni2fontTexelHalf.Add(uni, texel);
            }
            var _line = _text.Split('\n').ToList();
            Dictionary<uint, HexInfo> _hexdict = new Dictionary<uint, HexInfo>();
            _hexdict.Add(0u, new HexInfo { hex = 0u, id = 0, comment = $"Null", c = ' ' });// error
            List<List<int>> _idlineList = new List<List<int>>();
            for (int i = 0; i < _line.Count; i++)
            {
                string line = _line[i];
                List<int> _idline = new List<int>();
                for (int j = 0; j < line.Length; j++)
                {
                    char c = line[j];
                    int uni = char.ConvertToUtf32($"{c}", 0);
                    if (uni2fontTexelFull.TryGetValue(uni, out var texelFull))
                    {
                        GetFullHexCode(texelFull, _fontTextureFull, out uint bitl, out uint bitr);
                        if (!_hexdict.ContainsKey(bitl))
                            _hexdict.Add(bitl, new HexInfo { hex = bitl, id = _hexdict.Count, comment = $"{c}(l)", c = c });
                        _idline.Add(_hexdict[bitl].id);
                        if (!_hexdict.ContainsKey(bitr))
                            _hexdict.Add(bitr, new HexInfo { hex = bitr, id = _hexdict.Count, comment = $"{c}(r)", c = c });
                        _idline.Add(_hexdict[bitr].id);
                    }
                    else if (uni2fontTexelHalf.TryGetValue(uni, out var texelHalf))
                    {
                        GetHalfHexCode(texelHalf, _fontTextureHalf, out uint bit);
                        if (!_hexdict.ContainsKey(bit))
                            _hexdict.Add(bit, new HexInfo { hex = bit, id = _hexdict.Count, comment = $"{c}", c = c });
                        _idline.Add(_hexdict[bit].id);
                    }
                    else
                    {
                        _idline.Add(0);
                    }
                }
                _idlineList.Add(_idline);
            }
            List<HexInfo> _sortedHexList = _hexdict.Values.ToList();
            _sortedHexList.Sort((a, b) => a.id - b.id);
            StringBuilder _sb = new StringBuilder();
            int idlineMax = _idlineList.Max(x => x.Count);
            _sb.AppendLine($"const int HEXCOUNT={_sortedHexList.Count};");
            _sb.AppendLine($"const int TEXTCOUNT={_idlineList.Count};");
            _sb.AppendLine($"const int MAXTEXTLEN={idlineMax};");
            _sb.AppendLine($"const uint[] HEX=uint[HEXCOUNT](");
            for (int i = 0; i < _sortedHexList.Count; i++)
            {
                _sb.Append($"0x{_sortedHexList[i].hex:X8}u");
                if (i < _sortedHexList.Count - 1)
                {
                    _sb.Append(",");
                }
                _sb.AppendLine($"//{_sortedHexList[i].comment}");
            }
            _sb.AppendLine(");");

            for (int i = 0; i < _idlineList.Count; i++)
            {
                var idline = _idlineList[i];
                _sb.Append($"const int[] TEXT{i}=int[MAXTEXTLEN](");
                for (int j = 0; j < idlineMax; j++)
                {
                    if (j < idline.Count)
                        _sb.Append($"{idline[j]}");
                    else
                        _sb.Append($"0");
                    if (j < idlineMax - 1)
                    {
                        _sb.Append(",");
                    }
                }
                _sb.AppendLine(");");
            }
            _sb.Append("int[] TEXTLEN = int[TEXTCOUNT](");
            for (int i = 0; i < _idlineList.Count; i++)
            {
                _sb.Append($"{_idlineList[i].Count}");
                if (i < _idlineList.Count - 1)
                {
                    _sb.Append(",");
                }
            }
            _sb.AppendLine(");");
            _sb.Append("int[MAXTEXTLEN] getText(int i){int[MAXTEXTLEN] t;");
            for (int i = 0; i < _idlineList.Count; i++)
            {
                if (i == 0)
                    _sb.Append($"if(i=={i})t=TEXT{i};");
                else
                    _sb.Append($"else if(i=={i})t=TEXT{i};");
            }
            _sb.AppendLine("return t;}");
            _log = _sb.ToString();
        }
        GUILayout.Space(10);
        GUILayout.Label("ログ");
        GUILayout.TextArea(_log);
    }
    private void GetFullHexCode(Vector2Int texel, Texture2D fontTexture, out uint bitl, out uint bitr)
    {
        bitl = 0; bitr = 0;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // y-invert
                int tx = texel.x * 8 + x, ty = texel.y * 8 + 7 - y;
                // y-flip
                ty = fontTexture.height - ty - 1;
                var col = fontTexture.GetPixel(tx, ty);
                if (x < 4)
                {
                    bitl |= (uint)((col.r < 0.5f ? 1 : 0) << (x + y * 4));
                }
                else
                {
                    bitr |= (uint)((col.r < 0.5f ? 1 : 0) << (x - 4 + y * 4));
                }
            }
        }
    }
    private void GetHalfHexCode(Vector2Int texel, Texture2D fontTexture, out uint bit)
    {
        bit = 0;
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // y-invert
                int tx = texel.x * 4 + x, ty = texel.y * 8 + 7 - y;
                // y-flip
                ty = fontTexture.height - ty - 1;
                var col = fontTexture.GetPixel(tx, ty);
                bit |= (uint)((col.r < 0.5f ? 1 : 0) << (x + y * 4));
            }
        }
    }
    private struct HexInfo
    {
        public uint hex;
        public int id;
        public string comment;
        public char c;
    }
}