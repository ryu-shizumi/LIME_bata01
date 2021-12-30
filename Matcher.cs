using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LIME
{

    #region 文字型・文字列型の拡張関数
    public static class CharEx
    {
        public static CodeRangeMatcher _(this char c)
        {
            return new CodeRangeMatcher(c);
        }

        public static Matcher _(this string s)
        {
            if(s.Length == 0)
            {
                return Matcher.ZeroLength;
            }
            Matcher result = null;

            var stream = new CharCodeStream(s);

            foreach(var code in stream)
            {
                CodeRangeMatcher c;
                c = new CodeRangeMatcher(code);
                if (result == null)
                {
                    result = c;
                }
                else
                {
                    result += c;
                }
            }
            return result;
        }

        public static DenyRangeMatcher Deny(this char c)
        {
            return c._().Deny();
        }
        public static CodeRangeMatcher To(this char min, char max)
        {
            return new CodeRangeMatcher(min, max);
        }

        public static CharRepetMatcher Repet(this char c)
        {
            return c._().IsolatedLoop();
        }

        public static Match FindBest(this string text, Matcher matcher)
        {
            return matcher.FindFirst(text);
        }
    }
    #endregion

    #region マッチャーの基本クラス
    public abstract class Matcher
    {
        public static int _uniqCount = 0;
        /// <summary>
        /// ユニークＩＤ(接頭辞の G は Graph の先頭文字)
        /// </summary>
        public string UniqID { get; private set; }
        public Matcher()
        {
            UniqID = $"G{_uniqCount}";
            _uniqCount++;
        }


        public static void Init(string currentText)
        {
            CurrentText = currentText;
            // _childToOwner = new Dictionary<Matcher, HashSet<OwnerMatcher>>();
        }

        /// <summary>
        /// マッチング処理中の文字列
        /// </summary>
        public static string CurrentText { get; private set; }

        #region マッチング処理
        public IEnumerable<Match> Find(string text)
        {
            Matcher.Init(text);
            Match.Init();

            Debug_OutputTree();

            IEnumerable<Match> results = null;
            using (var root = new RootMatcher(this))
            {
                Executor.Execute(text);
                results = Match.Map[root];
            }

            if(results != null)
            {
                //foreach(var result in results)
                //{
                //    result.DeleteAll();
                //}

                return results;
            }
            return Array.Empty<Match>();
        }
        /// <summary>
        /// マッチングを行い、早く始まり、より長いマッチを返す。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Match FindFirst(string text)
        {
            var matches = Find(text);
            int minBegin = int.MaxValue;
            int maxLength = -999;
            Match result = null;
            foreach(var match in matches)
            {
                if( match.Begin< minBegin)
                {
                    result = match;
                    maxLength = match.Length;
                    minBegin = match.Begin;
                    continue;
                }
                else if(match.Begin == minBegin)
                {
                    if (maxLength < match.Length)
                    {
                        result = match;
                        maxLength = match.Length;
                    }
                }
            }

            return result;
        }
        #endregion

        #region 親子関係処理
        /// <summary>
        /// マッチャー同士の親子関係
        /// </summary>
        private static Dictionary<Matcher, HashSet<OwnerMatcher>> _childToOwner;

        /// <summary>
        /// ルートから走査して見つけ出した親子関係を登録する
        /// </summary>
        /// <param name="child"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        protected static bool AddOwner(Matcher child, OwnerMatcher owner)
        {
            if(_childToOwner == null)
            {
                _childToOwner = new Dictionary<Matcher, HashSet<OwnerMatcher>>();
            }
            if (_childToOwner.ContainsKey(child) == false)
            {
                _childToOwner.Add(child, new HashSet<OwnerMatcher>());
            }
            var hashSet = _childToOwner[child];

            // 未登録の時
            if (hashSet.Contains(owner) == false)
            {
                hashSet.Add(owner);
                return true;
            }

            // この親子関係が登録済みの時はfalseを返す
            return false;
        }

        protected static void RemoveOwner(Matcher child, OwnerMatcher owner)
        {
            _childToOwner[child].Remove(owner);
        }

        ///// <summary>
        ///// マッチャーツリーをルート方向から追いかけて、全ての親子関係を洗い出す
        ///// </summary>
        //public void CreateTree()
        //{
        //    _childToOwner = new Dictionary<Matcher, HashSet<OwnerMatcher>>();

        //    if (this is OwnerMatcher owner)
        //    {
        //        foreach (var child in owner.EnumChildren())
        //        {
        //            bool result = AddOwner(child, owner);

        //            if (result)
        //            {
        //                child.CreateTree();
        //            }
        //        }
        //    }
        //}

        public void Debug_OutputTree()
        {
            var set = new HashSet<RecursionMatcher>();

            Debug_OutputTree_Body("", set);

        }
        private void Debug_OutputTree_Body(string indent, HashSet<RecursionMatcher> set)
        {
            if(this is RecursionMatcher recursion)
            {
                if(set.Contains(recursion))
                {
                    return;
                }
                set.Add(recursion);
            }

            Debug.WriteLine($"{indent}{UniqID} {TypeName} {ToString()}");

            if (this is OwnerMatcher owner)
            {
                foreach (var child in owner.EnumChildren())
                {
                    child.Debug_OutputTree_Body(indent + "  ", set);
                }
            }
        }

        public HashSet<OwnerMatcher> GetParents()
        {
            if(_childToOwner.ContainsKey(this) == false)
            {
                return new HashSet<OwnerMatcher>();
            }
            return _childToOwner[this];
        }

        public bool HasParent
        {
            get
            {
                return _childToOwner.ContainsKey(this);
            }
        }

        #endregion

        #region 単独インスタンスの特殊マッチャー
        /// <summary>文字列の開始</summary>
        public static readonly SpecialMatcher Begin = new SpecialMatcher("Begin");
        /// <summary>文字列の終了</summary>
        public static readonly SpecialMatcher End = new SpecialMatcher("End");
        /// <summary>インデント</summary>
        public static readonly SpecialMatcher Indent = new SpecialMatcher("Indent");
        /// <summary>デデント</summary>
        public static readonly SpecialMatcher Dedent = new SpecialMatcher("Dedent");
        /// <summary>改行</summary>
        public static readonly SpecialMatcher Newline = new SpecialMatcher("NewLine");
        /// <summary>長さゼロ文字列</summary>
        public static readonly SpecialMatcher ZeroLength = new SpecialMatcher("ZeroLength");
        #endregion

        #region 演算子(論理和)
        public static EitherMatcher operator |(Matcher a, Matcher b)
        {
            if ((a == null) || (b == null))
            {
                throw new ArgumentNullException();
            }

            return new EitherMatcher(a, b);
        }
        public static EitherMatcher operator |(Matcher a, char b)
        {
            if (a == null)
            {
                throw new ArgumentNullException();
            }

            return new EitherMatcher(a, b._());
        }
        public static EitherMatcher operator |(char a, Matcher b)
        {
            if (b == null)
            {
                throw new ArgumentNullException();
            }

            return new EitherMatcher(a._(), b);
        }
        public static EitherMatcher operator |(Matcher a, string b)
        {
            if (a == null)
            {
                throw new ArgumentNullException();
            }

            if(b.Length == 1)
            {
                return new EitherMatcher(a, b[0]._());
            }
            else
            {
                return new EitherMatcher(a, b._());
            }
        }
        public static EitherMatcher operator |(string a, Matcher b)
        {
            if (b == null)
            {
                throw new ArgumentNullException();
            }

            if(a.Length == 1)
            {
                return new EitherMatcher(a[0]._(), b);
            }
            else
            {
                return new EitherMatcher(a._(), b);
            }
        }
        #endregion

        #region 演算子(和)
        public static PairMatcher operator +(Matcher a, Matcher b)
        {
            if ((a == null) || (b == null))
            {
                throw new ArgumentNullException();
            }

            return new PairMatcher(a, b);
        }
        public static PairMatcher operator +(Matcher a, char b)
        {
            if (a == null)
            {
                throw new ArgumentNullException();
            }

            return new PairMatcher(a, b._());
        }
        public static PairMatcher operator +(char a, Matcher b)
        {
            if (b == null)
            {
                throw new ArgumentNullException();
            }

            return new PairMatcher(a._(), b);
        }
        public static PairMatcher operator +(Matcher a, string b)
        {
            if (a == null)
            {
                throw new ArgumentNullException();
            }

            return new PairMatcher(a, b._());
        }
        public static PairMatcher operator +(string a, Matcher b)
        {
            if (b == null)
            {
                throw new ArgumentNullException();
            }

            return new PairMatcher(a._(), b);
        }
        #endregion

        public string TypeName
        {
            get
            {
                var result = this.GetType().Name;
                if (result.EndsWith("Matcher"))
                {
                    result = result.Substring(0, result.Length - 7);
                }
                return result;
            }
        }

        public string Tag { get; protected set; }

        public LoopBodyMatcher Loop()
        {
            return new LoopBodyMatcher(this);
        }

        /// <summary>
        /// 関数呼び出しの引数などの、任意の記号に括られた１個以上の式に合致するマッチャーを返す
        /// </summary>
        /// <param name="exp">式</param>
        /// <param name="begin">開始記号</param>
        /// <param name="end">終了記号</param>
        /// <param name="delimiter">式同士を区切りデリミタ</param>
        /// <returns>
        /// 山括弧で囲った中に不等式が入ると、きっと誤動作する。
        /// </returns>
        public static LoopContainMatcher EnclosedExpressions
            (Matcher exp, char begin = '(' , char end = ')' , char? delimiter = ',')
        {
            var head = begin + exp;
            var body = (delimiter == null) ? exp : ',' + exp;
            var tail = end._();

            return new LoopContainMatcher(head, body, tail);
        }


        /// <summary>
        /// 識別子としてよく使われるマッチャーを返す
        /// </summary>
        /// <param name="optionChar">追加で許容する文字</param>
        /// <param name="otherOptions">更に追加で許容する文字</param>
        /// <returns></returns>
        public static LoopContainMatcher Identifier(char optionChar,　params char[] otherOptions)
        {
            CodeRangeMatcher optionals = optionChar._();
            if(otherOptions.Length > 0)
            {
                foreach(char c in otherOptions)
                {
                    optionals |= c;
                }
            }

            var alphabet = 'A'.To('Z') | 'a'.To('z');
            var number = '0'.To('9');

            var headChar = (optionals == null) ? 
                '_' | alphabet : 
                '_' | alphabet | optionals;

            var beginBorder = new BorderMatcher(headChar.Deny(), headChar);

            var head = beginBorder + headChar;


            var bodyChar = (optionals == null) ? 
                '_' | alphabet | number :
                '_' | alphabet | number | optionals;

            var endBorder = new BorderMatcher(bodyChar, bodyChar.Deny());

            return new LoopContainMatcher(head, bodyChar, endBorder);
        }

        /// <summary>
        /// 識別子としてよく使われるマッチャーを返す
        /// </summary>
        /// <returns>
        /// 正規表現の [_A-Za-z][_A-Za-z0-9]* に相当する
        /// </returns>
        public static LoopContainMatcher Identifier()
        {
            var alphabet = 'A'.To('Z') | 'a'.To('z');
            var number = '0'.To('9');

            var headChar = '_' | alphabet;
            var beginBorder = new BorderMatcher(headChar.Deny(), headChar);
            var head = beginBorder + headChar;

            var bodyChar = '_' | alphabet | number;
            var endBorder = new BorderMatcher(bodyChar, bodyChar.Deny());

            return new LoopContainMatcher(head, bodyChar, endBorder);
        }

        /// <summary>
        /// １文字目に使えない文字が設定されている「文字のループ」
        /// </summary>
        /// <param name="forbidChar">１文字目に使えない文字</param>
        /// <param name="loopChar">ループのどこでも使える文字</param>
        /// <returns></returns>
        public static LoopContainMatcher ForbidLoop(char forbidChar, CodeRangeMatcher loopChar)
        {
            return ForbidLoop(forbidChar._(), loopChar);
        }
        /// <summary>
        /// １文字目に使えない文字が設定されている「文字のループ」
        /// </summary>
        /// <param name="forbidChar">１文字目に使えない文字</param>
        /// <param name="loopChar">ループのどこでも使える文字</param>
        /// <returns></returns>
        /// <remarks>
        /// 
        /// 一般的な言語の識別子
        ///     ForbidLoop(
        ///         '0'.To('9'), // 先頭は数字だけ禁止
        ///         'A'.To('Z') | 'a'.To('z') | '_' // アルファベットとアンダースコアが使える
        ///         )
        /// 
        /// 先頭にゼロを許さない整数リテラル
        ///     ForbidLoop(
        ///         '0', // 先頭はゼロだけ禁止
        ///         '1'.To('9') | 'a'.To('z') // 1から9が使える
        ///         )
        ///     
        /// 
        /// </remarks>
        public static LoopContainMatcher ForbidLoop(CodeRangeMatcher forbidChar, CodeRangeMatcher loopChar)
        {
            var beginBorder = new BorderMatcher(loopChar.Deny() | Begin, loopChar);
            var head = beginBorder + loopChar;

            var bodyChar = loopChar | forbidChar;
            var endBorder = new BorderMatcher(bodyChar, bodyChar.Deny() | End);

            return new LoopContainMatcher(head, bodyChar, endBorder);
        }

        public static LoopContainMatcher StringLiteral(Matcher begin, Matcher c, Matcher end)
        {
            var anyChar = new CodeRangeMatcher(0, CodeRangeMatcher.CharCodeMax);

            var stringprefix = "r"._() | "u" | "R" | "U" | "f" | "F"
                                 | "fr" | "Fr" | "fR" | "FR" | "rf" | "rF" | "Rf" | "RF";
            var shortstringchar = ('\\'._() | '\r' | '\n' | '\'').Deny();//  <any source character except "\" or newline or the quote>
            var stringescapeseq = '\\' + anyChar; //<any source character>;
            var shortstringitem = shortstringchar | stringescapeseq;
            var shortstring = ('\'' + shortstringitem.Loop() +'\'') | ('"' + shortstringitem.Loop() +'"');



            return new LoopContainMatcher(begin, shortstringitem, end);
        }

        public abstract Matcher Clone();
    }
    #endregion

    #region CodeRange
    /// <summary>
    /// 文字コード範囲
    /// </summary>
    public struct CodeRange
    {
        public int Min;
        public int Max;
        public CodeRange(int min, int max)
        {
            if(min <= max)
            {
                Min = min;
                Max = max;
            }
            else
            {
                Min = max;
                Max = min;
            }
        }
        /// <summary>
        /// この文字コード範囲が１文字であるかを取得する
        /// </summary>
        public bool IsSingle
        {
            get { return Min == Max; }
        }

        public override string ToString()
        {
            if(Min == Max)
            {
                return char.ConvertFromUtf32(Min);
            }

            return $"{char.ConvertFromUtf32(Min)}-{char.ConvertFromUtf32(Max)}";
        }

        #region マージ

        /// <summary>
        /// ２つのCodeRange列挙子からマージできるならマージ結果、さもなければ小さい方を取得する
        /// </summary>
        /// <param name="enum1">列挙子１</param>
        /// <param name="enum2">列挙子２</param>
        /// <param name="enum1Active">列挙子１の有効性</param>
        /// <param name="enum2Active">列挙子２の有効性</param>
        /// <returns></returns>
        public static CodeRange GetSmaller(
            IEnumerator<CodeRange> enum1, IEnumerator<CodeRange> enum2,
            ref bool enum1Active, ref bool enum2Active)
        {
            CodeRange result;

            if ((enum1Active == false) && (enum2Active == false))
            {
                throw new ArgumentException();
            }
            if (enum1Active)
            {
                result = enum1.Current;
                enum1Active = enum1.MoveNext();
                return result;
            }
            if (enum2Active)
            {
                result = enum2.Current;
                enum2Active = enum2.MoveNext();
                return result;
            }

            enum1Active = true;
            enum2Active = true;
            CodeRange range1 = enum1.Current;
            CodeRange range2 = enum2.Current;


            // range1の方が前にある時
            if (range1.Min < range2.Min)
            {
                int min;
                int max;
                // 接していない時
                // ○○○
                // 　　　　　▲▲▲▲▲
                if (range1.Max + 1 < range2.Min)
                {
                    // 合併させずに前にある方を返信値とする
                    result = range1;
                    enum1Active = enum1.MoveNext();
                }
                else
                {
                    min = range1.Min;

                    // 接している時
                    // ○○○○○
                    // 　　　　　▲▲▲▲▲
                    if (range1.Max + 1 == range2.Min)
                    {
                        max = range2.Max;
                    }

                    // 重なっている時
                    // ○○○○○○○○
                    // 　　　　　▲▲▲▲▲
                    else // (range2.Min <= range1.Max)
                    {
                        max = Math.Max(range2.Max, range1.Max);
                    }
                    enum1Active = enum1.MoveNext();
                    enum2Active = enum2.MoveNext();
                    result = new CodeRange(min, max);
                }
            }

            // range2の方が前にある時
            else if (range2.Min < range1.Min)
            {
                int min;
                int max;
                // 接していない時
                // 　　　　　○○○○○
                // ▲▲▲
                if (range2.Max + 1 < range1.Min)
                {
                    // 合併させずに前にある方を返信値とする
                    result = range2;
                    enum2Active = enum2.MoveNext();
                }
                else
                {
                    min = range2.Min;

                    // 接している時
                    // 　　　　　○○○○○
                    // ▲▲▲▲▲
                    if (range2.Max + 1 == range1.Min)
                    {
                        max = range1.Max;
                    }

                    // 重なっている時
                    // 　　　　　○○○○○
                    // ▲▲▲▲▲▲▲▲
                    else // (range1.Min <= range2.Max)
                    {
                        max = Math.Max(range1.Max, range2.Max);
                    }
                    enum1Active = enum1.MoveNext();
                    enum2Active = enum2.MoveNext();
                    result = new CodeRange(min, max);
                }
            }

            // ２つの範囲が同じ位置から始まっている時
            else
            {
                int min = range1.Min;
                int max = Math.Max(range1.Max, range2.Max);
                enum1Active = enum1.MoveNext();
                enum2Active = enum2.MoveNext();
                result = new CodeRange(min, max);
            }

            return result;
        }

        /// <summary>
        /// ２つのCodeRangeリストをマージする
        /// </summary>
        /// <param name="list1">リスト１</param>
        /// <param name="list2">リスト２</param>
        /// <returns></returns>
        public static IEnumerable<CodeRange> Marge(IEnumerable<CodeRange> list1, IEnumerable<CodeRange> list2)
        {
            var enum1 = list1.GetEnumerator();
            var enum2 = list2.GetEnumerator();

            bool active1 = enum1.MoveNext();
            bool active2 = enum2.MoveNext();

            while (active1 || active2)
            {
                var current = GetSmaller(enum1, enum2, ref active1, ref active2);
                yield return current;
            }
        }
        public static IEnumerable<CodeRange> Marge(IEnumerable<CodeRange> list1, CodeRange item2)
        {
            return Marge(list1, ToEnumerable(item2));
        }
        public static IEnumerable<CodeRange> Marge(CodeRange item1, IEnumerable<CodeRange> list2)
        {
            return Marge(ToEnumerable(item1), list2);
        }
        private static IEnumerable<CodeRange> ToEnumerable(CodeRange item)
        {
            yield return item;
        }
        #endregion

        #region 否定
        public static IEnumerable<CodeRange> Deny(IEnumerable<CodeRange> ranges)
        {
            List<CodeRange> result = new List<CodeRange>();

            int min = 0;
            int max;

            foreach (var range in ranges)
            {
                max = range.Min - 1;

                if(max < 0) { continue; }
                if(range.Max == int.MaxValue) { break; }

                result.Add(new CodeRange(min, max));

                min = range.Max + 1;
            }
            return result;
        }
        #endregion
    }
    #endregion


    

    #region 文字コード範囲マッチャー
    /// <summary>
    /// 文字コード範囲に合致するマッチャー
    /// </summary>
    public class CodeRangeMatcher : Matcher
    {
        public static List<CodeRangeMatcher> InstanceList = new List<CodeRangeMatcher>();
        public const int CharCodeMin = 0x00000000;
        public const int CharCodeMax = 0x7FFFFFFF;

        private IEnumerable<CodeRange> _ranges;


        public CodeRangeMatcher(int code)
        {
            if(code < CharCodeMin)
            {
                code = CharCodeMin;
            }
            if(CharCodeMax < code)
            {
                code = CharCodeMax;
            }
            _ranges = new CodeRange[] { new CodeRange(code, code) };
            InstanceList.Add(this);
        }
        public CodeRangeMatcher(int min, int max)
        {
            if (min < CharCodeMin)
            {
                min = CharCodeMin;
            }
            if (CharCodeMax < max)
            {
                max = CharCodeMax;
            }
            _ranges = new CodeRange[] { new CodeRange(min, max) };
            InstanceList.Add(this);
        }
        public CodeRangeMatcher(IEnumerable<CodeRange> ranges1, IEnumerable<CodeRange> ranges2)
        {
            _ranges = new List<CodeRange>(CodeRange.Marge(ranges1, ranges2));
            InstanceList.Add(this);
        }
        public CodeRangeMatcher(IEnumerable<CodeRange> ranges)
        {
            _ranges = new List<CodeRange>(ranges);
            InstanceList.Add(this);
        }


        public void IsMatch(CharAtom atom)
        {
            foreach(var range in _ranges)
            {
                if ((range.Min <= atom.Code) && (atom.Code <= range.Max))
                {
                    var newMatch = new CharMatch(atom, this);
                    break;
                }
            }
        }

        #region 演算子(論理和)
        public static CodeRangeMatcher operator |(CodeRangeMatcher a, CodeRangeMatcher b)
        {
            return new CodeRangeMatcher(a._ranges, b._ranges);
        }
        public static CodeRangeMatcher operator |(CodeRangeMatcher range, char c)
        {
            return range | new CodeRangeMatcher((int)c);
        }
        public static CodeRangeMatcher operator |(char c, CodeRangeMatcher range)
        {
            return new CodeRangeMatcher((int)c) | range;
        }
        #endregion

        #region インデクサ(タグ付け)
        public CodeRangeMatcher this[string tag]
        {
            get
            {
                var result = new CodeRangeMatcher(_ranges);
                result.Tag = tag;
                return result;
            }
        }
        #endregion

        #region Clone()
        public override Matcher Clone()
        {
            var result = new CodeRangeMatcher(_ranges);
            result.Tag = Tag;
            return result;
        }
        #endregion

        /// <summary>
        /// この文字の否定を表現するマッチャーを取得する
        /// </summary>
        /// <returns></returns>
        public DenyRangeMatcher Deny()
        {
            return new DenyRangeMatcher(_ranges);
        }

        /// <summary>
        /// この文字の１回以上の繰り返しを表現するマッチャーを取得する
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// １文字目に使えない文字が設定されたループの場合は対処できないので、
        /// Matcher.ForbidLoop() を使う。
        /// </remarks>
        public CharRepetMatcher IsolatedLoop()
        {
            return new CharRepetMatcher(this);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[");
            foreach (var range in _ranges)
            {
                sb.Append(range);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
    #endregion

    #region 文字コード範囲否定マッチャー
    /// <summary>
    /// 文字コード範囲否定マッチャー
    /// </summary>
    public class DenyRangeMatcher : Matcher
    {
        public static List<DenyRangeMatcher> InstanceList = new List<DenyRangeMatcher>();

        private IEnumerable<CodeRange> _ranges;


        public DenyRangeMatcher(int code)
        {
            _ranges = new CodeRange[] { new CodeRange(code, code) };
            InstanceList.Add(this);
        }
        public DenyRangeMatcher(int min, int max)
        {
            _ranges = new CodeRange[] { new CodeRange(min, max) };
            InstanceList.Add(this);
        }
        public DenyRangeMatcher(IEnumerable<CodeRange> ranges1, IEnumerable<CodeRange> ranges2)
        {
            _ranges = new List<CodeRange>(CodeRange.Marge(ranges1, ranges2));
            InstanceList.Add(this);
        }
        public DenyRangeMatcher(IEnumerable<CodeRange> ranges)
        {
            _ranges = new List<CodeRange>(ranges);
            InstanceList.Add(this);
        }

        public void IsMatch(CharAtom atom)
        {
            bool onRange = false;

            foreach (var range in _ranges)
            {
                if((range.Min <= atom.Code) && (atom.Code <= range.Max))
                {
                    onRange = true;
                    break;
                }
            }
            if (onRange == false)
            {
                var newMatch = new CharMatch(atom, this);
            }
        }

        #region 演算子(論理和)
        public static DenyRangeMatcher operator |(DenyRangeMatcher a, DenyRangeMatcher b)
        {
            return new DenyRangeMatcher(a._ranges, b._ranges);
        }
        #endregion

        #region インデクサ(タグ付け)
        public DenyRangeMatcher this[string tag]
        {
            get
            {
                var result = new DenyRangeMatcher(_ranges);
                result.Tag = tag;
                return result;
            }
        }
        #endregion
        #region Clone()
        public override Matcher Clone()
        {
            var result = new DenyRangeMatcher(_ranges);
            result.Tag = Tag;
            return result;
        }
        #endregion

        /// <summary>
        /// この否定文字の否定を表現するマッチャーを取得する
        /// </summary>
        /// <returns></returns>
        public CodeRangeMatcher Deny()
        {
            return new CodeRangeMatcher(_ranges);
        }

        /// <summary>
        /// この否定文字の繰り返しを表現するマッチャーを取得する
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// １文字目に使えない文字が設定されたループの場合は対処できないので、
        /// Matcher.ForbidLoop() を使う。
        /// </remarks>
        public CharRepetMatcher IsolatedLoop()
        {
            return new CharRepetMatcher(this);
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[^");
            foreach (var range in _ranges)
            {
                sb.Append(range);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
    #endregion

    
    #region 子持ちマッチャーの基底クラス
    /// <summary>
    /// 子持ちマッチャーの基底クラス
    /// </summary>
    public abstract class OwnerMatcher : Matcher
    {
        /// <summary>
        /// 子要素を列挙する
        /// </summary>
        /// <returns>子要素の列挙子</returns>
        public abstract IEnumerable<Matcher> EnumChildren();

        /// <summary>
        /// 上がってきたマッチを処理し、合致していればマッチを発生させ内部に蓄積する
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="match"></param>
        /// <returns>
        /// matchが親マッチに取り込まれた時はtrueを返す
        /// </returns>
        public abstract void IsMatch(Matcher generator, Match match);
    }
    #endregion

    
    #region 境界マッチャー
    /// <summary>
    /// 文字パターンの始まり又は終わりの境界を検出するマッチャー
    /// </summary>
    public class BorderMatcher : OwnerMatcher
    {
        private Matcher _prev;
        private Matcher _current;

        private bool prevMatch;
        private int prevEnd;

        public BorderMatcher(Matcher prev, Matcher current)
        {
            _prev = prev;
            AddOwner(_prev, this);
            _current = current;
            AddOwner(_current, this);
        }


        public override IEnumerable<Matcher> EnumChildren()
        {
            yield return _prev;
            yield return _current;
        }
        public override void IsMatch(Matcher generator, Match match)
        {
            if(generator == _prev)
            {
                prevMatch = true;
                prevEnd = match.End;
            }
            else
            {
                if(prevMatch && (prevEnd == match.Begin))
                {
                    var newMatch = new BorderMatch(match.Begin, this);
                }
                prevMatch = false;
            }
        }

        #region インデクサ(タグ付け)
        public BorderMatcher this[string tag]
        {
            get
            {
                var result = new BorderMatcher(_prev, _current);
                result.Tag = tag;
                return result;
            }
        }
        #endregion
        #region Clone()
        public override Matcher Clone()
        {
            var result = new BorderMatcher(_prev, _current);
            result.Tag = Tag;
            return result;
        }
        #endregion
    }
    #endregion

    #region 連続文字マッチャー
    /// <summary>
    /// 合致の始まり ～ 合致の終わり を検出するマッチャー
    /// </summary>
    public class CharRepetMatcher : OwnerMatcher
    {
        private BorderMatcher _prev;
        private BorderMatcher _next;

        private int _prevEnd = -1;

        public string Value = "";

        public CharRepetMatcher(CodeRangeMatcher c)
        {
            Value = $"{c}+";

            _prev = new BorderMatcher(c.Deny() | Begin, c);
            AddOwner(_prev, this);
            _next = new BorderMatcher(c, c.Deny() | End);
            AddOwner(_next, this);
        }
        public CharRepetMatcher(DenyRangeMatcher c)
        {
            Value = $"{c}+";

            _prev = new BorderMatcher(c.Deny() | Begin, c);
            AddOwner(_prev, this);
            _next = new BorderMatcher(c, c.Deny() | End);
            AddOwner(_next, this);
        }
        private CharRepetMatcher(BorderMatcher prev, BorderMatcher next)
        {
            _prev = prev;
            AddOwner(_prev, this);
            _next = next;
            AddOwner(_next, this);
        }

        public override IEnumerable<Matcher> EnumChildren()
        {
            yield return _prev;
            yield return _next;
        }

        #region インデクサ(タグ付け)
        public CharRepetMatcher this[string tag]
        {
            get
            {
                var result = new CharRepetMatcher(_prev, _next);
                result.Tag = tag;
                return result;
            }
        }
        #endregion
        #region Clone()
        public override Matcher Clone()
        {
            var result = new CharRepetMatcher(_prev, _next);
            result.Tag = Tag;
            return result;
        }
        #endregion
        public override void IsMatch(Matcher generator, Match match)
        {
            if(generator == _prev)
            {
                _prevEnd = match.End;
            }
            else
            {
                var newMatch = new CharsMatch(new TextRange(_prevEnd, match.Begin), this);
            }
        }

        public override string ToString()
        {
            if(Value != "") { return Value; }
            return base.ToString();
        }
    }
    #endregion

    

    #region 結合マッチャー
    public class PairMatcher : OwnerMatcher
    {
        private Matcher _left;
        private RightMatcher _right;

        /// <summary>
        /// 左要素と右要素を指定するコンストラクタ
        /// </summary>
        /// <param name="left">左要素</param>
        /// <param name="right">右要素</param>
        /// <remarks>
        /// 左・右に同じインスタンスを指定してもIsMatch関数が混乱しないように、
        /// WrapMatcherでそれぞれを包む
        /// 
        /// 右要素 RightMatch を動かす優先順位は最低にして、左より先に右が動かないようにする
        /// </remarks>
        public PairMatcher(Matcher left, Matcher right)
        {
            #region 引数チェック
            if (left == null)
            {
                if (right == null)
                {
                    throw new ArgumentNullException("左辺と右辺が共に null です。");
                }
                else
                {
                    throw new ArgumentNullException("左辺が null です。");
                }
            }
            else if (right == null)
            {
                throw new ArgumentNullException("右辺が null です。");
            }
            #endregion

            _left = left;
            AddOwner(_left, this);

            _right = new RightMatcher(right);
            AddOwner(_right, this);
        }
        private PairMatcher(Matcher left, RightMatcher right)
        {
            _left = left;
            AddOwner(_left, this);

            _right = right;
            AddOwner(_right, this);
        }

        public override IEnumerable<Matcher> EnumChildren()
        {
            yield return _left;
            yield return _right;
        }

        #region インデクサ(タグ付け)
        public PairMatcher this[string tag]
        {
            get
            {
                var result = new PairMatcher(_left, _right);
                result.Tag = tag;
                return result;
            }
        }
        #endregion
        #region Clone()
        public override Matcher Clone()
        {
            var result = new PairMatcher(_left, _right);
            result.Tag = Tag;
            return result;
        }
        #endregion

        public override void IsMatch(Matcher generator, Match match)
        {
            if (generator == _left)
            {
                var newMatch = new LeftMatch(match, this);
                // Leftマッチは動かさないのでNewItemには登録しない
            }
            else if (generator == _right)
            {
                foreach (var wait in Match.Map[this].RemoveEnum())
                {
                    // 結合を検査(空白を挟んだ結合も調べる)して繋がる時
                    if(Executor.CheckConnection(wait,wait.End, match.Begin))
                    {
                        var newMatch = new PairMatch((LeftMatch)wait, (RightMatch)match, this);
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"{_left}{_right}";
        }

        #region 右マッチャー
        /// <summary>
        /// 結合の右側の子マッチャーを包むマッチャー
        /// </summary>
        /// <remarks>
        /// 
        /// var a = 'a'._();
        /// var aa = a + a;
        /// 
        /// この例のように同じインスタンスを結合しようとした際に、
        /// 少なくとも片方をラップしないと、PairMatcher.IsMatch()からは
        /// 左右どちらから上がってきたマッチか判別が付かない。
        /// という訳で右側をラップする事にする。
        /// 右要素から上がったマッチをRightMatch型とし、動かす優先順位を低くする事で、
        /// 左要素より右要素が上に上がってしまう事を避ける。
        /// </remarks>
        private class RightMatcher : OwnerMatcher {
            private Matcher _inner;
            public RightMatcher(Matcher inner)
            {
                _inner = inner;
                AddOwner(inner, this);
            }
            #region Clone()
            public override Matcher Clone()
            {
                return this;
            }
            #endregion
            public override IEnumerable<Matcher> EnumChildren()
            {
                yield return _inner;
            }

            public override void IsMatch(Matcher generator, Match match)
            {
                var newMatch = new RightMatch(match, this);
            }

            public override string ToString()
            {
                return _inner.ToString();
            }
        }
        #endregion
    }
    #endregion

    #region 選択マッチャー
    public class EitherMatcher : OwnerMatcher
    {
        private Matcher _a;
        private Matcher _b;

        public EitherMatcher(Matcher a, Matcher b)
        {
            #region 引数チェック
            if (a == null)
            {
                if(b == null)
                {
                    throw new ArgumentNullException("左辺と右辺が共に null です。");
                }
                else
                {
                    throw new ArgumentNullException("左辺が null です。");
                }
            }
            else if(b == null)
            {
                throw new ArgumentNullException("右辺が null です。");
            }

            if((object)a == (object)b)
            {
                throw new ArgumentException("左辺と右辺が同じインスタンスです。");
            }
            #endregion

            _a = a;
            AddOwner(_a, this);
            _b = b;
            AddOwner(_b, this);
        }

        #region インデクサ(タグ付け)
        public EitherMatcher this[string tag]
        {
            get
            {
                var result = new EitherMatcher(_a, _b);
                result.Tag = tag;
                return result;
            }
        }
        #endregion
        #region Clone()
        public override Matcher Clone()
        {
            var result = new EitherMatcher(_a, _b);
            result.Tag = Tag;
            return result;
        }
        #endregion
        public override IEnumerable<Matcher> EnumChildren()
        {
            yield return _a;
            yield return _b;
        }
        public override void IsMatch(Matcher generator, Match match)
        {
            var newMatch = new EitherMatch(match, this);
        }

        public override string ToString()
        {
            return $"({_a}|{_b})";
        }
    }
    #endregion

    #region ループ本体マッチャー
    public class LoopBodyMatcher : OwnerMatcher
    {
        public Matcher Body { get; private set; }

        public LoopBodyMatcher(Matcher body)
        {
            if(UniqID == "G219")
            {
                var temp = "";
            }

            Body = body;
            AddOwner(Body, this);
        }
        #region インデクサ(タグ付け)
        public LoopBodyMatcher this[string tag]
        {
            get
            {
                var result = new LoopBodyMatcher(Body);
                result.Tag = tag;
                return result;
            }
        }
        #endregion

        #region Clone()
        public override Matcher Clone()
        {
            var result = new LoopBodyMatcher(Body);
            result.Tag = Tag;
            return result;
        }
        #endregion
        
        public override IEnumerable<Matcher> EnumChildren()
        {
            yield return Body;
        }

        public override void IsMatch(Matcher generator, Match match)
        {
            var newMatch = new LoopBodyMatch(match, this);
        }

        #region 演算子(和)
        public static HeadedLoopMatcher operator +(Matcher head, LoopBodyMatcher body)
        {
            return new HeadedLoopMatcher(head, body);
        }
        public static HeadedLoopMatcher operator +(char head, LoopBodyMatcher body)
        {
            return new HeadedLoopMatcher(head._(), body);
        }
        public static HeadedLoopMatcher operator +(string head, LoopBodyMatcher body)
        {
            return new HeadedLoopMatcher(head._(), body);
        }
        #endregion
    }

    public class HeadedLoopMatcher : OwnerMatcher
    {
        public Matcher Head { get; private set; }
        public LoopBodyMatcher Body { get; private set; }


        public HeadedLoopMatcher(Matcher head, LoopBodyMatcher body)
        {
            Head = head;
            Body = body;
        }
        #region Clone()
        public override Matcher Clone()
        {
            throw new NotImplementedException();
        }
        #endregion
        public override IEnumerable<Matcher> EnumChildren()
        {
            yield return Head;
            yield return Body;
        }

        public override void IsMatch(Matcher generator, Match match)
        {
            throw new NotImplementedException();
        }

        #region 演算子(和)
        public static LoopContainMatcher operator +(HeadedLoopMatcher headBody, Matcher tail)
        {
            return new LoopContainMatcher(headBody, tail);
        }
        public static LoopContainMatcher operator +(HeadedLoopMatcher headBody, char tail)
        {
            return new LoopContainMatcher(headBody, tail._());
        }
        public static LoopContainMatcher operator +(HeadedLoopMatcher headBody, string tail)
        {
            return new LoopContainMatcher(headBody, tail._());
        }
        #endregion
    }
    #endregion

    #region ループ包含マッチャー
    /// <summary>
    /// ループする胴体部分を包含するマッチャー
    /// </summary>
    public class LoopContainMatcher : OwnerMatcher
    {
        private Matcher _head;
        private LoopBodyMatcher _body;
        private Matcher _tail;
        public string BodyTag { get; private set; }

        public LoopContainMatcher(Matcher head, Matcher body, Matcher tail)
        {
            if((head == body)||(head == tail))
            {
                _head = new WrapMatcher(head);
            }
            else
            {
                _head = head;
            }
            AddOwner(_head, this);

            if(tail == body)
            {
                _tail = new WrapMatcher(tail);
            }
            else
            {
                _tail = tail;
            }
            AddOwner(_tail, this);

            if(body is LoopBodyMatcher loopBody)
            {
                _body = loopBody;
            }
            else
            {
                _body = new LoopBodyMatcher(body);
            }
            AddOwner(_body, this);

            BodyTag = body.Tag;
        }
        public LoopContainMatcher(HeadedLoopMatcher headBody, Matcher tail)
        {
            _head = headBody.Head;
            AddOwner(_head, this);
            _body = headBody.Body;
            AddOwner(_body, this);
            if (headBody == tail)
            {
                _tail = new WrapMatcher(tail);
            }
            else
            {
                _tail = tail;
            }
            AddOwner(_tail, this);
            BodyTag = headBody.Body.Tag;
        }

        #region インデクサ(タグ付け)
        public LoopContainMatcher this[string tag]
        {
            get
            {
                var result = new LoopContainMatcher(_head,_body,_tail);
                result.Tag = tag;
                result.BodyTag = BodyTag;
                return result;
            }
        }
        #endregion

        #region Clone()
        public override Matcher Clone()
        {
            var result = new LoopContainMatcher(_head, _body, _tail);
            result.Tag = Tag;
            return result;
        }
        #endregion

        public override IEnumerable<Matcher> EnumChildren()
        {
            yield return _head;
            yield return _body;
            yield return _tail;
        }

        public override void IsMatch(Matcher generator, Match match)
        {
            if (generator == _head)
            {
                var newMatch = new LoopMatch(match, this);
                // Loopマッチは動かさないのでNewItemには登録しない
            }
            else if (generator == _body)
            {
                foreach (var wait in Match.Map[this].RemoveEnum())
                {
                    // 結合を検査(空白を挟んだ結合も調べる)して繋がる時
                    if (Executor.CheckConnection(wait, wait.End, match.Begin))
                    {
                        LoopBodyMatch body = (LoopBodyMatch)match;
                        var newMatch = ((LoopMatch)wait).AddBody(body);
                    }
                }
            }
            else
            {
                foreach (var wait in Match.Map[this].RemoveEnum())
                {
                    // 結合を検査(空白を挟んだ結合も調べる)して繋がる時
                    if (Executor.CheckConnection(wait, wait.End, match.Begin))
                    {
                        var newMatch = ((LoopMatch)wait).SetTail(match);
                    }
                }
            }
        }

        #region ラップマッチャー
        /// <summary>
        /// ラップマッチャーは自動生成されるマッチャーであり
        /// </summary>
        private class WrapMatcher : OwnerMatcher {
            private Matcher _inner;

            public WrapMatcher(Matcher inner)
            {
                _inner = inner;
                AddOwner(inner, this);
            }
            #region インデクサ(タグ付け)
            public WrapMatcher this[string tag]
            {
                get
                {
                    var result = new WrapMatcher(_inner);
                    result.Tag = tag;
                    return result;
                }
            }
            #endregion
            #region Clone()
            public override Matcher Clone()
            {
                var result = new WrapMatcher(_inner);
                result.Tag = Tag;
                return result;
            }
            #endregion

            public override IEnumerable<Matcher> EnumChildren()
            {
                yield return _inner;
            }

            public override void IsMatch(Matcher generator, Match match)
            {
                var newMatch = new WrapMatch(match, this);
            }
            public override string ToString()
            {
                return _inner.ToString();
            }
        }
        #endregion
    }
    #endregion

    #region 再帰マッチャー
    public class RecursionMatcher : OwnerMatcher
    {
        private Matcher _inner;
        public Matcher Inner
        {
            get
            {
                return _inner;
            }
            set
            {
                if(value == this)
                {
                    throw new ArgumentException
                        ("再帰マッチャーのInnerプロパティに再帰マッチャー自体を設定できません", nameof(value));
                }

                if(_inner != null)
                {
                    RemoveOwner(_inner, this);
                }

                _inner = value;
                AddOwner(_inner, this);
            }
        }

        #region インデクサ(タグ付け)
        public RecursionMatcher this[string tag]
        {
            get
            {
                var result = (RecursionMatcher)Clone();
                result.Tag = tag;
                return result;
            }
        }
        #endregion
        #region Clone()
        public override Matcher Clone()
        {
            var result = new RecursionMatcher();
            result._inner = _inner.Clone();
            result.Tag = Tag;
            return result;
        }
        #endregion

        public override IEnumerable<Matcher> EnumChildren()
        {
            yield return Inner;
        }

        public override void IsMatch(Matcher generator, Match match)
        {
            var newMatch = new RecursionMatch(match, this);
            Match.Map[newMatch] = this;
        }
    }
    #endregion

    #region ルートマッチャー
    public class RootMatcher : OwnerMatcher , IDisposable
    {
        private Matcher _inner;

        public RootMatcher(Matcher inner)
        {
            _inner = inner;
            AddOwner(_inner, this);
        }

        #region Clone()
        public override Matcher Clone()
        {
            throw new NotImplementedException();
        }
        #endregion

        public void Dispose()
        {
            RemoveOwner(_inner, this);
            _inner = null;
        }

        public override IEnumerable<Matcher> EnumChildren()
        {
            if(_inner == null) { yield break; }
            yield return _inner;
        }

        public override void IsMatch(Matcher generator, Match match)
        {
            var newMatch = new RootMatch(match, this);
        }

        
    }
    #endregion

    #region 単独インスタンスの特殊マッチャー
    /// <summary>
    /// 特殊マッチャー(開始・終了・インデント・デデント・行開始)
    /// </summary>
    public class SpecialMatcher : Matcher
    {
        private string _name;
        public SpecialMatcher(string name)
        {
            _name = name;
        }

        public static void ReizeMatch(TextAtom atom)
        {
            // 開始ダミーが来たら文字列開始トークンを貯めさせる
            if (atom is BeginAtom begin)
            {
                var newMatch = new BeginMatch(begin, Begin);
            }
            // 終了ダミーが来たら文字列終了トークンを貯めさせる
            else if (atom is EndAtom end)
            {
                var newMatch = new EndMatch(end, End);
            }
            else if (atom is IndentAtom indent)
            {
                // インデントトークンを貯めさせる
                var newMatch = new IndentMatch(indent, Indent);
            }
            else if (atom is DedentAtom dedent)
            {
                // デデントトークンを貯めさせる
                var newMatch = new DedentMatch(dedent, Dedent);
            }
            else if (atom is LineheadAtom newline)
            {
                // 行開始トークンを貯めさせる
                var newMatch = new NewlineMatch(newline, Newline);
            }

        }
        #region Clone()
        public override Matcher Clone()
        {
            return this;
        }
        #endregion

        public static void ReizeZerolengthMatch(int index)
        {
            var newMatch = new ZeroLengthMatch(index, ZeroLength);
        }

        public override string ToString()
        {
            return _name;
        }
    }


    


    #endregion

}
