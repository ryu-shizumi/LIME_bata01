using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using PipeIO;

namespace LIME
{

    #region 文字型・文字列型の拡張関数
    public static class CharEx
    {
        /// <summary>
        /// この文字を肯定文字マッチャーに変換する
        /// </summary>
        /// <param name="c">任意の文字</param>
        /// <returns>肯定文字マッチャー</returns>
        public static AffirmCharMatcher _(this char c)
        {
            return new AffirmCharMatcher(c);
        }

        /// <summary>
        /// この文字列をマッチャーに変換する
        /// </summary>
        /// <param name="s">任意の文字列</param>
        /// <returns>文字列に合致するPairマッチャー</returns>
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
                AffirmCharMatcher c;
                c = new AffirmCharMatcher(code);
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

        /// <summary>
        /// この文字を否定文字マッチャーに変換する
        /// </summary>
        /// <param name="c">任意の文字</param>
        /// <returns>否定文字マッチャー</returns>
        public static DenyCharMatcher Deny(this char c)
        {
            return c._().Deny();
        }

        /// <summary>
        /// 文字コード範囲を指定して肯定文字マッチャーを生成する
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static AffirmCharMatcher To(this char min, char max)
        {
            return new AffirmCharMatcher(min, max);
        }

        public static LoopContainMatcher Loop(this char c)
        {
            return c._().Loop();
        }

        public static Match FindBest(this string text, Matcher matcher)
        {
            return matcher.FindFirst(text);
        }
    }
    #endregion

    #region ヘルパー関数群
    public abstract class MatcherHelper
    {
        /// <summary>
        /// 左結合の二項演算(四則演算やシフト演算など)に一致するマッチャーを生成して返す
        /// </summary>
        /// <param name="operators">演算子</param>
        /// <param name="operand">(入力)オペランド (出力)これより優先度の低い二項演算のオペランド</param>
        /// <returns>二項演算に一致するマッチャー</returns>
        public static RecursionMatcher LeftOperation(Matcher operators, ref Matcher operand)
        {
            //
            // 左結合二項演算の定義法
            //
            // expAAA.Inner = (リテラル | expAAA) + 演算子 + (リテラル)
            // expBBB.Inner = (リテラル | expAAA | expBBB) + 演算子 + (リテラル | expAAA)
            // expCCC.Inner = (リテラル | expAAA | expBBB | expCCC) + 演算子 + (リテラル | expAAA | expBBB)
            // expDDD.Inner = (リテラル | expAAA | expBBB | expCCC | expDDD) + 演算子 + (リテラル | expAAA | expBBB | expCCC)
            // 
            // 一般化すると…
            // expNNN.Inner = (自分より優先度の高い式の左辺 | expNNN) + 演算子 + (自分より優先度の高い式の右辺)
            //

            var exp = new RecursionMatcher();
            var newOperand = (exp | operand);
            exp.Inner = newOperand + operators + operand;
            operand = newOperand;
            return exp;
        }

        /// <summary>
        /// 右結合の二項演算(C言語の代入演算など)に一致するマッチャーを生成して返す
        /// </summary>
        /// <param name="operators">演算子</param>
        /// <param name="operand">(入力)オペランド (出力)これより優先度の低い二項演算のオペランド</param>
        /// <returns>二項演算に一致するマッチャー</returns>
        public static RecursionMatcher RightOperation(Matcher operators, ref Matcher operand)
        {
            //
            // 右結合二項演算の定義法
            //
            // expAAA.Inner = (リテラル) + 演算子 + (リテラル | expAAA)
            // expBBB.Inner = (リテラル | expAAA) + 演算子 + (リテラル | expAAA | expBBB)
            // expCCC.Inner = (リテラル | expAAA | expBBB) + 演算子 + (リテラル | expAAA | expBBB | expCCC)
            // expDDD.Inner = (リテラル | expAAA | expBBB | expCCC) + 演算子 + (リテラル | expAAA | expBBB | expCCC | expDDD)
            // 
            // 一般化すると…
            // expNNN.Inner = (自分より優先度の高い式の左辺 | expNNN) + 演算子 + (自分より優先度の高い式の右辺)
            //

            var exp = new RecursionMatcher();
            var newOperand = (exp | operand);
            exp.Inner = operand + operators + newOperand;
            operand = newOperand;
            return exp;
        }
    }
    #endregion

    #region マッチャーの基本クラス
    public abstract class Matcher
    {
        public static bool IsOutputTree = false;


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

            if(IsOutputTree)
            {
                Debug_OutputTree();
            }

            IEnumerable<Match> results = null;
            using (var root = new RootMatcher(this))
            {
                Executor.Execute(text, root);
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
            var matches = new List<Match>(Find(text));
            Match result = null;

            // より早く、より長いマッチを返信する

            foreach(var match in matches)
            {
                if(result == null)
                {
                    result = match;
                    continue;
                }

                if(result.Begin < match.Begin)
                {
                    continue;
                }

                if(result.End < match.End)
                {
                    result = match;
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

        /// <summary>
        /// マッチャーのツリー構造を出力する
        /// </summary>
        /// <returns></returns>
        public string Debug_OutputTree()
        {
            var set = new HashSet<RecursionMatcher>();
            var buffer = new TextBuffer();
            Debug_OutputTree_Body(buffer, "", set);

            return buffer.ToString();
        }

        /// <summary>
        /// マッチャーのツリー構造を出力する本体部分
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="indent"></param>
        /// <param name="set"></param>
        private void Debug_OutputTree_Body(TextBuffer buffer, string indent, HashSet<RecursionMatcher> set)
        {
            if(this is RecursionMatcher recursion)
            {
                if(set.Contains(recursion))
                {
                    return;
                }
                set.Add(recursion);
            }

            buffer.WriteLine($"{indent}{UniqID} {TypeName} {ToString()}");

            if (this is OwnerMatcher owner)
            {
                foreach (var child in owner.EnumChildren())
                {
                    child.Debug_OutputTree_Body(buffer, indent + "  ", set);
                }
            }
        }

        /// <summary>
        /// マッチャーのツリー構造を出力する
        /// </summary>
        /// <returns></returns>
        public void Debug_OutputTreeDetail(TextViewClient client)
        {
            var set = new HashSet<RecursionMatcher>();
            var buffer = new TextBuffer();
            //client.SuspendLayout();
            Debug_OutputTreeDetail_Body(client, "", set, new HashSet<RecursionMatcher>());
            //client.ResumeLayout();
        }
        /// <summary>
        /// マッチャーのツリー構造を出力する本体部分
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="indent"></param>
        /// <param name="set"></param>
        private void Debug_OutputTreeDetail_Body(
            TextViewClient client, string indent, HashSet<RecursionMatcher> set, HashSet<RecursionMatcher> hash)
        {
            if(this is RecursionMatcher rec)
            {
                if(hash.Contains(rec))
                {
                    return;
                }
                hash.Add(rec);
            }


            if (this is RecursionMatcher recursion)
            {
                if (set.Contains(recursion))
                {
                    return;
                }
                set.Add(recursion);
            }

            client.Write($"{indent}{UniqID} {TypeName} {ToString()}","");

            HashSet<Match> matches = Match.Map[this];

            int matchCount = 0;
            // このマッチャー上の全てのマッチに関して処理する
            foreach(var match in matches)
            {
                matchCount++;

                if(matchCount > 1)
                {
                    client.Write(",");
                }

                client.Write(
                    $"{match.UniqID}",
                    BuildMatchTree(match),
                    //$"{match.TypeName} [{match.Begin}-{match.End}] {match.Value}",
                    Colors.Red, Colors.LightGray);
            }

            client.WriteLine("");


            if (this is OwnerMatcher owner)
            {
                var children = new List<Matcher>(owner.EnumChildren());
                // 子マッチャー全てに処理する
                foreach (var child in children)
                {
                    child.Debug_OutputTreeDetail_Body(client, indent + "  ", set, hash);
                }
            }
        }

        private string BuildMatchTree(Match match)
        {
            StringBuilder sb = new StringBuilder();

            BuildBody(match,"");

            void BuildBody(Match m, string indent)
            {
                sb.AppendLine($"{indent}{m.UniqID} {m.TypeName} [{m.Begin}-{m.End}] {m.Value}");
                if(m is OwnerMatch owner)
                {
                    foreach(var inner in owner.Inners())
                    {
                        BuildBody(inner,indent + "  ");
                    }
                }
            }

            return sb.ToString();
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

        /// <summary>
        /// BorderMatcher配下、もしくはBorderMatcher自体であるかを示すフラグ
        /// </summary>
        public bool BordersFollower = false;


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
            AffirmCharMatcher optionals = optionChar._();
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
            var underBar = '_'._();
            var number = '0'.To('9');

            var leadChar = alphabet | underBar;
            var loopChar = leadChar| number;

            return LedLoop(leadChar, loopChar);
        }

        

        /// <summary>
        /// 一文字目だけ制約のあるループ
        /// </summary>
        /// <param name="leadChar">一文字目</param>
        /// <param name="loopChar">二文字目以降</param>
        /// <returns></returns>
        /// <remarks>
        /// 
        /// 一般的な言語の識別子
        ///     LedLoop(
        ///         // 先頭はアルファベット・アンダースコア
        ///         'A'.To('Z') | 'a'.To('z') | '_' 
        ///         
        ///         // 二文字目以降はアルファベット・アンダースコア・数字
        ///         'A'.To('Z') | 'a'.To('z') | '_' | '0'.To('9') 
        ///         )
        /// 
        /// 先頭にゼロを許さない整数リテラル
        ///     LedLoop(
        ///         '1'.To('9')  // 先頭は 1から9
        ///         '0'.To('9')  // 二文字目以降は 0から9
        ///         )
        ///     
        /// 
        /// </remarks>
        public static LoopContainMatcher LedLoop(AffirmCharMatcher leadChar, AffirmCharMatcher loopChar)
        {
            var prevBorder = new BorderMatcher(leadChar.Deny(), leadChar);
            var headPart = prevBorder + leadChar;
            var endBorder = new BorderMatcher(loopChar, loopChar.Deny());

            return new LoopContainMatcher(headPart, loopChar, endBorder);
        }

        public static LoopContainMatcher StringLiteral(Matcher begin, Matcher c, Matcher end)
        {
            var anyChar = new AffirmCharMatcher(0, AffirmCharMatcher.CharCodeMax);

            var stringprefix = "r"._() | "u" | "R" | "U" | "f" | "F"
                                 | "fr" | "Fr" | "fR" | "FR" | "rf" | "rF" | "Rf" | "RF";
            var shortstringchar = ('\\'._() | '\r' | '\n' | '\'').Deny();//  <any source character except "\" or newline or the quote>
            var stringescapeseq = '\\' + anyChar; //<any source character>;
            var shortstringitem = shortstringchar | stringescapeseq;
            var shortstring = ('\'' + shortstringitem.Loop() +'\'') | ('"' + shortstringitem.Loop() +'"');



            return new LoopContainMatcher(begin, shortstringitem, end);
        }

        public abstract Matcher Clone();

        public override string ToString()
        {
            return "";
        }
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

    #region CodeRanges
    //public struct CodeRanges
    //{
    //    private CodeRange[] _inners;
    //    public bool IsDeny { get; }

    //    public CodeRanges(int min, int max)
    //    {
    //        IsDeny = false;
    //        _inners = new CodeRange[1];
    //        _inners[0] = new CodeRange(min, max);
    //    }

    //    public CodeRanges(CodeRange range)
    //    {
    //        if (range == null)
    //        {

    //        }
    //    }
    //    public CodeRanges(CodeRanges sorce, bool isDeny)
    //    {
    //        var srcInner = sorce._inners;
    //        _inners = new CodeRange[srcInner.Length];
    //        srcInner.CopyTo(_inners,0);
    //        IsDeny = isDeny;
    //    }

    //    public CodeRanges Deny()
    //    {
    //        return new CodeRanges(this, !IsDeny);
    //    }
    //}

    #endregion


    #region 文字コード範囲マッチャーの基底クラス
    /// <summary>
    /// 文字コード範囲マッチャーの基底クラス
    /// </summary>
    public abstract class CharMatcher : Matcher
    {
        public static List<CharMatcher> InstanceList = new List<CharMatcher>();
        public const int CharCodeMin = 0x00000000;
        public const int CharCodeMax = 0x10FFFF;

        public IEnumerable<CodeRange> _ranges;

        #region コンストラクタ
        public CharMatcher(int code)
        {
            if (code < CharCodeMin)
            {
                code = CharCodeMin;
            }
            if (CharCodeMax < code)
            {
                code = CharCodeMax;
            }
            _ranges = new CodeRange[] { new CodeRange(code, code) };
            InstanceList.Add(this);
        }
        public CharMatcher(int min, int max)
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
        public CharMatcher(IEnumerable<CodeRange> ranges1, IEnumerable<CodeRange> ranges2)
        {
            _ranges = new List<CodeRange>(CodeRange.Marge(ranges1, ranges2));
            InstanceList.Add(this);
        }
        public CharMatcher(IEnumerable<CodeRange> ranges)
        {
            _ranges = new List<CodeRange>(ranges);
            InstanceList.Add(this);
        }
        #endregion

        protected bool RangesCheck_base(CharAtom atom)
        {
            foreach (var range in _ranges)
            {
                if ((range.Min <= atom.Code) && (atom.Code <= range.Max))
                {
                    return true;
                }
            }
            return false;
        }

        public bool RangesCheck(CharAtom atom)
        {
            var result = RangesCheck_base(atom);
            if(IsDeny)
            {
                result = !result;
            }
            return result;
        }

        public abstract bool IsDeny { get; }

        public void IsMatch(CharAtom atom)
        {
            if (RangesCheck(atom))
            {
                var newMatch = new CharMatch(atom, this);
            }
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

    #region 文字コード範囲マッチャー
    /// <summary>
    /// 肯定文字マッチャー。char型から暗黙の型変換で生成可能。
    /// </summary>
    public class AffirmCharMatcher : CharMatcher
    {
        public AffirmCharMatcher(int code) : base(code) { }

        public AffirmCharMatcher(int min, int max) : base(min,max)
        {
        }
        public AffirmCharMatcher(IEnumerable<CodeRange> ranges1, IEnumerable<CodeRange> ranges2)
            :base(ranges1, ranges2) { }
        public AffirmCharMatcher(IEnumerable<CodeRange> ranges) : base(ranges) { }

        public override bool IsDeny { get { return false; } }

        #region 暗黙の型変換
        public static implicit operator AffirmCharMatcher(char c)
        {
            return c._();
        }
        #endregion

        #region 演算子(論理和)
        public static AffirmCharMatcher operator |(AffirmCharMatcher a, AffirmCharMatcher b)
        {
            return new AffirmCharMatcher(a._ranges, b._ranges);
        }
        public static AffirmCharMatcher operator |(AffirmCharMatcher range, char c)
        {
            return range | new AffirmCharMatcher((int)c);
        }
        public static AffirmCharMatcher operator |(char c, AffirmCharMatcher range)
        {
            return new AffirmCharMatcher((int)c) | range;
        }
        #endregion

        #region インデクサ(タグ付け)
        public AffirmCharMatcher this[string tag]
        {
            get
            {
                var result = new AffirmCharMatcher(_ranges);
                result.Tag = tag;
                return result;
            }
        }
        #endregion

        #region Clone()
        public override Matcher Clone()
        {
            var result = new AffirmCharMatcher(_ranges);
            result.Tag = Tag;
            return result;
        }
        #endregion

        /// <summary>
        /// この文字の否定を表現するマッチャーを取得する
        /// </summary>
        /// <returns></returns>
        public DenyCharMatcher Deny()
        {
            return new DenyCharMatcher(_ranges);
        }

        /// <summary>
        /// この文字の１回以上の繰り返しを表現するマッチャーを取得する
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// １文字目に使えない文字が設定されたループの場合は対処できないので、
        /// Matcher.LedLoop() を使う。
        /// </remarks>
        public LoopContainMatcher Loop()
        {
            var deny = Deny();

            var prevBorder = new BorderMatcher(deny, this);
            var endBorder = new BorderMatcher(this, deny);

            return new LoopContainMatcher(prevBorder, this, endBorder);
        }

        public override string ToString()
        {
            var rangeList = new List<CodeRange>(_ranges);
            
            if((rangeList.Count == 1) && (rangeList[0].Min == rangeList[0].Max))
            {
                return rangeList[0].ToString();
            }

            var sb = new StringBuilder();
            sb.Append("[");
            foreach (var range in rangeList)
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
    /// 文字マッチャー(否定)
    /// </summary>
    public class DenyCharMatcher : CharMatcher
    {
        public DenyCharMatcher(int code) : base(code) { }
        public DenyCharMatcher(int min, int max) : base(min, max) { }
        public DenyCharMatcher(IEnumerable<CodeRange> ranges1, IEnumerable<CodeRange> ranges2)
            : base(ranges1, ranges2) { }
        public DenyCharMatcher(IEnumerable<CodeRange> ranges) : base(ranges) { }

        public override bool IsDeny { get { return true; } }



        #region 演算子(論理和)
        public static DenyCharMatcher operator |(DenyCharMatcher a, DenyCharMatcher b)
        {
            return new DenyCharMatcher(a._ranges, b._ranges);
        }
        #endregion

        #region インデクサ(タグ付け)
        public DenyCharMatcher this[string tag]
        {
            get
            {
                var result = new DenyCharMatcher(_ranges);
                result.Tag = tag;
                return result;
            }
        }
        #endregion

        #region Clone()
        public override Matcher Clone()
        {
            var result = new DenyCharMatcher(_ranges);
            result.Tag = Tag;
            return result;
        }
        #endregion

        /// <summary>
        /// この否定文字の否定を表現するマッチャーを取得する
        /// </summary>
        /// <returns></returns>
        public AffirmCharMatcher Deny()
        {
            return new AffirmCharMatcher(_ranges);
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

    #region 子持ちマッチャーインターフェイス
    public interface IHasChildrenMatcher
    {
        /// <summary>
        /// 子要素を列挙する
        /// </summary>
        /// <returns>子要素の列挙子</returns>
        public IEnumerable<Matcher> EnumChildren();
    }
    #endregion

    #region 子持ちマッチャーの基底クラス
    /// <summary>
    /// 子持ちマッチャーの基底クラス
    /// </summary>
    public abstract class OwnerMatcher : Matcher, IHasChildrenMatcher
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
    public class BorderMatcher : Matcher, IHasChildrenMatcher
    {
        public static List<BorderMatcher> InstanceList = new List<BorderMatcher>();
        private Matcher _prev;
        private Matcher _next;

        //private bool prevMatch;
        //private int prevEnd;

        public BorderMatcher(Matcher prev, Matcher next)
        {
            //BordersFollower = true;

            _prev = prev;
            //AddOwner(_prev, this);
            _next = next;
            //AddOwner(_next, this);

            //// 配下全てに「Border配下」とマーキングする
            //MarkBordersFollower();

            InstanceList.Add(this);
        }


        public IEnumerable<Matcher> EnumChildren()
        {
            yield return _prev;
            yield return _next;
        }

        public void IsMatch(BorderAtom atom)
        {
            bool prevOk = false;
            if(atom.Begin == 2)
            {
                var temp = "";
            }


            //if ((atom.Prev is BeginAtom) && (_prev == Begin))
            if ((atom.Begin == 0)) 
            {
                prevOk = true;
            }
            else if(atom.Prev is CharAtom prevChar)
            {
                if(_prev is CharMatcher rangeBase)
                {
                    prevOk = rangeBase.RangesCheck(prevChar);
                }
            }

            if(prevOk == false)
            {
                return;
            }

            bool nextOK = false;

            if((atom.Next is EndAtom))
            {
                nextOK = true;
            }
            else if(atom.Next is CharAtom nextChar)
            {
                if (_next is CharMatcher rangeBase)
                {
                    nextOK = rangeBase.RangesCheck(nextChar);
                }
            }

            if(nextOK == false)
            {
                return;
            }

            var newMatch = new BorderMatch(atom.Begin, this);
        }

        #region インデクサ(タグ付け)
        public BorderMatcher this[string tag]
        {
            get
            {
                var result = new BorderMatcher(_prev, _next);
                result.Tag = tag;
                return result;
            }
        }
        #endregion

        #region Clone()
        public override Matcher Clone()
        {
            var result = new BorderMatcher(_prev, _next);
            result.Tag = Tag;
            return result;
        }
        #endregion

        #region Border配下のマッチを優先的に動かす処理
        ///// <summary>
        ///// Borderマッチャーから再帰的に配下に「Border配下」という印を付ける。
        ///// </summary>
        //public void MarkBordersFollower()
        //{
        //    var hash = new HashSet<Matcher>();
        //    MarkBordersFollower_body(this, hash);
        //}

        //private void MarkBordersFollower_body(Matcher target, HashSet<Matcher> hash)
        //{
        //    target.BordersFollower = true;

        //    if (target is OwnerMatcher owner)
        //    {
        //        foreach(var child in owner.EnumChildren())
        //        {
        //            if(hash.Contains(child))
        //            {
        //                continue;
        //            }
        //            hash.Add(child);
        //            MarkBordersFollower_body(child, hash);
        //        }
        //    }
        //}
        #endregion

        public override string ToString()
        {
            return $"({_prev}:{_next})";
        }
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

        public CharRepetMatcher(AffirmCharMatcher c)
        {
            Value = $"{c}+";

            _prev = new BorderMatcher(c.Deny(), c);
            AddOwner(_prev, this);
            _next = new BorderMatcher(c, c.Deny());
            AddOwner(_next, this);
        }
        public CharRepetMatcher(DenyCharMatcher c)
        {
            Value = $"{c}+";

            _prev = new BorderMatcher(c.Deny(), c);
            AddOwner(_prev, this);
            _next = new BorderMatcher(c, c.Deny());
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

                //Debug.WriteLine($"G12上のLeftマッチのUniqID = {newMatch.UniqID}");

                var list = Match.Map[this];
                var count = list.Count;
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

        public override string ToString()
        {
            return Body.ToString();
        }
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
    /// <remarks>
    /// 先頭部・胴体部・末尾部の３つから構成され、先頭と末尾の両方が揃うまでマッチを発生させる事は無い。
    /// 先頭と末尾の両方が揃えば、胴体部の一致数がゼロ個でもマッチは発生する。
    /// </remarks>
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
                    if (wait is LoopBodysMatch) { continue; }
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
                    if(wait is LoopBodysMatch) { continue; }
                    // 結合を検査(空白を挟んだ結合も調べる)して繋がる時
                    if (Executor.CheckConnection(wait, wait.End, match.Begin))
                    {
                        var newMatch = ((LoopMatch)wait).SetTail(match);
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"({_head}{_body}{_tail})";
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

        public override string ToString()
        {
            return TypeName;
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
