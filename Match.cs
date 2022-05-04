using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LIME
{

    #region 開始・終端インデックス

    public class TextRange
    {
        public int Begin { get; protected set; }
        public int End { get; protected set; }

        public virtual int Length
        {
            get { return End - Begin; }
        }
        public TextRange(int begin, int end)
        {
            Begin = begin;
            End = end;
        }
        public TextRange(TextRange beginEnd)
        {
            Begin = beginEnd.Begin;
            End = beginEnd.End;
        }

        public string Value
        {
            get
            {
                return Matcher.CurrentText.Substring(Begin, Length);
            }
        }
    }
    #endregion

    #region 終了インデックスによるインスタンス管理リスト
    public class EndInstanceList<TKey, TValue> where TValue : Match
    {
        private Dictionary<TKey, HashSet<TValue>> _dict
            = new Dictionary<TKey, HashSet<TValue>>();

        public void Add(TKey key, TValue value)
        {
            if(_dict.ContainsKey(key) == false)
            {
                _dict.Add(key, new HashSet<TValue>());
            }
            _dict[key].Add(value);
        }

        public void Remove(TKey key, TValue value)
        {
            if( (_dict.ContainsKey(key)) && (_dict[key].Contains(value)))
            {
                _dict[key].Remove(value);   
                if(_dict[key].Count == 0)
                {
                    _dict.Remove(key);
                }
            }
        }

        public void RemoveSameKeyAll(TKey key)
        {
            if(_dict.ContainsKey(key))
            {
                //Match.Map.Dump();

                var hashSet = _dict[key];
                var list = new List<TValue>(hashSet);
                //Debug.WriteLine($"{list[0].TypeName} [{key}-]");

                var removeEnum = hashSet.RemoveEnum();
                hashSet.Clear();
                _dict.Remove(key);

                for (int i = 0; i < list.Count; i++)
                {
                    Match item = list[i];

                    if(item.UniqID == "T9")
                    {
                        var temp = "";
                    }

                    //Debug.WriteLine($"{item.UniqID} {item.TypeName} Map.Remove");
                    // このマッチは不要なので Map から参照を消す
                    Match.Map.Remove(item);
                }
            }
        }

        public void Clear()
        {
            foreach(var key in _dict.Keys)
            {
                _dict[key].Clear();
            }
        }

        public void Dump()
        {
            var keys = _dict.Keys;

            if(keys.Count == 0)
            {
                Debug.WriteLine("(要素なし)");
                return;
            }

            foreach(var key in keys)
            {
                var sb = new StringBuilder();

                foreach(var match in _dict[key])
                {
                    if(sb.Length != 0)
                    {
                        sb.Append(",");
                    }
                    sb.Append($"{match.UniqID}[{match.Begin}-{match.End}]");
                }

                string inners = sb.ToString();

                Debug.WriteLine($"_dict[{key}]=( {inners} )");
            }
        }
    }
    #endregion

    #region 開始インデックスによるインスタンスの参照カウンタ
    public class BeginReferenceCounter
    {
        private Dictionary<int, int> _dict = new();

        public void Add(Match match)
        {
            var begin = match.Begin;
            if(_dict.ContainsKey(begin) == false)
            {
                _dict.Add(begin, 0);
            }
            //Debug.WriteLine($"Add\t{match.UniqID}\t{match.TypeName}\t{begin}\t{_dict[begin]}\t++");
            _dict[begin]++;
        }

        /// <summary>
        /// マッチ１個分だけ、開始位置の参照カウントを減らす
        /// </summary>
        /// <param name="match">削除対象マッチ</param>
        /// <returns>削除対象マッチと開始位置が同じマッチの残数/returns>
        public int Remove(Match match)
        {
            var begin = match.Begin;
            //Debug.WriteLine($"Remove\t{match.UniqID}\t{match.TypeName}\t{begin}\t{_dict[begin]}\t--");

            if (_dict.ContainsKey(begin) == false)
            {
                throw new IndexOutOfRangeException();
            }
            if(_dict[begin] == 0)
            {
                throw new IndexOutOfRangeException();
            }

            _dict[begin]--;

            var result = _dict[begin];

            // このインデックスから始まるマッチが無くなった時
            if(_dict[begin] == 0)
            {
                _dict.Remove(begin);
            }

            return result;
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public void Dump()
        {
            foreach(var key in _dict.Keys)
            {
                Debug.WriteLine($"_dict[{key}]={_dict[key]}");
            }

        }
    }
    #endregion

    #region マッチの基底クラス
    public abstract class Match : TextRange
    {
        public string Tag { get; private set; }

        /// <summary>
        /// 開始インデックスで管理されたインスタンスリスト
        /// </summary>
        private static BeginReferenceCounter _beginInstanceCounter = new();

        public static int _uniqCount = 0;
        /// <summary>
        /// ユニークＩＤ(接頭辞の T は Token の先頭文字)
        /// </summary>
        public string UniqID { get; private set; }

        public Matcher Generator { get; private set; }

        public string SpecialID
        {
            get
            {
                var genID = Generator == null ? "GFFFF" : Generator.UniqID;

                return $"{genID}[{Begin}-{End}] {TypeName}";

            }
        }

        #region コンストラクタ
        public Match(TextRange beginEnd, Matcher generator) : base(beginEnd)
        {
            InitThis(generator);
        }

        public Match(int begin, int end, Matcher generator) : base(begin, end)
        {
            InitThis(generator);
        }

        /// <summary>
        /// コンストラクタから呼ばれる初期化処理
        /// </summary>
        /// <param name="generator"></param>
        private void InitThis(Matcher generator)
        {
            UniqID = $"T{_uniqCount}";
            _uniqCount++;
            Generator = generator;

            // Root
            //     インスタンスカウンターでカウントしない
            //     新規インスタンスに追加しない
            // Left と Loop
            //     インスタンスカウンターでカウントする
            //     新規インスタンスに追加しない
            // その他
            //     インスタンスカウンターでカウントする
            //     新規インスタンスに追加する

            if((this is RootMatch) ||(this is LoopBodysMatch))
            {
                // インスタンスカウンターでカウントしない
                // 新規インスタンスに追加しない
            }
            else if((this is LeftMatch) || (this is LoopMatch))
            {
                // 開始インデックス毎の「インスタンス数カウンター」をインクリメントする
                _beginInstanceCounter.Add(this);

                // 新規インスタンスに追加しない
            }
            else
            {
                // 開始インデックス毎の「インスタンス数カウンター」をインクリメントする
                _beginInstanceCounter.Add(this);

                // 新規インスタンス(未移動インスタンス)に追加する
                NewItems.Add(this);
            }

            // 自分の位置を「自分を生成したマッチャー」に設定する
            Map[this] = generator;

            Tag = generator.Tag;

            //Debug.WriteLine($"☆ [{Begin}-{End}] \"{Value}\" {UniqID} {Generator.UniqID}");
        }
        #endregion

        ~Match()
        {
            //if ((this is LeftMatch) || (this is LoopMatch))
            //{
            //    Debug.WriteLine($"{UniqID} {TypeName} Remove");
            //}
        }

        /// <summary>
        /// このインスタンスの「開始インデックスリスト」の参照カウンタをデクリメントする
        /// </summary>
        public void InstanceCounterRemove(TextAtom atom)
        {
            var begin = Begin;

            //if(Length == 0)
            //{
            //    Debug.WriteLine($"LengthZero Remove {UniqID} [{Begin}-{End}]");
            //}

            if (!(this is LeftMatch) && !(this is LoopMatch))
            {
                var count = _beginInstanceCounter.Remove(this);
                //Debug.WriteLine($"✕ [{Begin}-{End}] \"{Value}\" {UniqID} {Generator.UniqID}");
                //Debug.WriteLine($"BeginCounter[{Begin}] Remove {count + 1}→{count} {UniqID}");

                if(atom is CharAtom)
                {
                    // もし参照カウントを減らした結果、
                    // 同じインデックスで始まるマッチがゼロ個になった時
                    if (count == 0)
                    {
                        ////Debug.WriteLine($"LoopMatch.RemoveSameKeyAll({begin})");
                        LoopMatch.EndInstances.RemoveSameKeyAll(begin);
                        ////Debug.WriteLine($"LeftMatch.RemoveSameKeyAll({begin})");
                        LeftMatch.EndInstances.RemoveSameKeyAll(begin);
                    }

                }
            }
        }

        #region 初期化処理
        public static void Init()
        {
            if(NewItems != null)
            {
                NewItems.Clear();
            }
            NewItems = new NewMatchesList();
            if(Map != null)
            {
                Map.Clear();
            }
            Map = new MatchMatcherMap();

            if(_beginInstanceCounter != null)
            {
                _beginInstanceCounter.Clear();
            }

            LoopMatch.EndInstances.Clear();
            LeftMatch.EndInstances.Clear();
        }
        #endregion

        /// <summary>
        /// 新規に生成され、まだ動いていないマッチ
        /// </summary>
        public static NewMatchesList NewItems { get; private set; }

        /// <summary>
        /// マッチャーとそこに居るマッチの対応
        /// </summary>
        public static MatchMatcherMap Map { get; private set; }
            = new MatchMatcherMap();

        #region インデクサ(タグ)
        /// <summary>
        /// 任意のタグを持つ子要素を返信する。幅優先検索で最初に見つかった子要素を返信する
        /// </summary>
        /// <param name="tag">任意のタグ</param>
        /// <returns></returns>
        public Match this[string tag]
        {
            get
            {
                return FindTaggedMatch(tag);
            }
        }

        private Match FindTaggedMatch(string tag)
        {
            if(this is OwnerMatch owner)
            {
                foreach (var inner in owner.Inners())
                {
                    if (inner.Tag == tag)
                    {
                        return inner;
                    }
                }
                foreach (var inner in owner.Inners())
                {
                    var result = inner.FindTaggedMatch(tag);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        //private void FindTag(string tag, ref MatchList matches)
        //{
        //    if (Tag == tag)
        //    {
        //        matches.Add(this);
        //        return;
        //    }

        //    if ((Tag == null) && (this is OwnerMatch owner))
        //    {
        //        foreach(var inner in owner.Inners())
        //        {
        //            inner.FindTag(tag, ref matches);
        //        }
        //    }
        //}

        //public class MatchList : List<Match>
        //{
        //    public Match this[string tag]
        //    {
        //        get
        //        {
        //            return this[0][tag];
        //        }
        //    }
        //}

        //public class MatchChane
        //{
        //    public bool HasNext { get; private set; }
        //    public MatchChane(List<Match> list)
        //    {

        //    }
        //}
        #endregion

        #region デバッグ用処理
        //public void DebugWrite(string label = "")
        //{
        //    if(label != "")
        //    {
        //        Debug.WriteLine(label);
        //    }
            
        //    if(this is DummyMatch)
        //    {
        //        Debug.WriteLine("(該当無し)");
        //    }
        //    else
        //    {
        //        DebugWrite_Body("");
        //    }

        //    Debug.WriteLine(" _ _ _ _ _ _ _ _ _ _ _ _ _ _ _");
        //}
        //private void DebugWrite_Body(string indent)
        //{
        //    Debug.WriteLine($"{indent}{Value} {UniqID} {SpecialID}");

        //    if(this is OwnerMatch owner)
        //    {
        //        foreach(var inner in owner.Inners())
        //        {
        //            inner.DebugWrite_Body($"{indent}  ");
        //        }
        //    }
        //}
        public void Dump()
        {
            DumpBody(this, "");
            void DumpBody(Match target, string indent)
            {
                var c = "";
                if(target is CharMatch cm)
                {
                    c = cm.Value;
                }
                Debug.WriteLine($"{indent}{target.UniqID} {target.TypeName}{c}");
                if(target is OwnerMatch owner)
                {
                    foreach(var inner in owner.Inners())
                    {
                        DumpBody(inner, indent + "  ");
                    }
                }
            }
        }
        #endregion

        public string TypeName
        {
            get
            {
                var result = this.GetType().Name;
                if(result.EndsWith("Match"))
                {
                    result = result.Substring(0, result.Length - 5);
                }
                return result;
            }
        }
    }
    #endregion

    public static class MatchEx
    {
        public static void DebugWrite(this Match match, string label = "")
        {
            if (label != "")
            {
                Debug.WriteLine(label);
            }

            if (match == null)
            {
                Debug.WriteLine("(該当無し)");
            }
            else
            {
                DebugWrite_Body(match, "");
            }

            Debug.WriteLine(" _ _ _ _ _ _ _ _ _ _ _ _ _ _ _");
        }

        #region デバッグ用処理
        
        private static void DebugWrite_Body(Match match, string indent)
        {
            Debug.WriteLine($"{indent}{match.Value} {match.UniqID} {match.SpecialID}");

            if (match is OwnerMatch owner)
            {
                foreach (var inner in owner.Inners())
                {
                    DebugWrite_Body(inner, $"{indent}  ");
                }
            }
        }
        #endregion
    }

    #region DummyMatch
    public class DummyMatch : Match
    {
        public DummyMatch()
            : base(0,0,null)
        {

        }
    }
    #endregion

    #region 文字マッチ
    public class CharMatch : Match
    {
        public CharMatch(TextRange beginEnd, Matcher generator) : base(beginEnd, generator) { }
    }

    public class CharBeginMatch : Match
    {
        public CharBeginMatch(TextRange beginEnd, Matcher generator) : base(beginEnd.Begin, beginEnd.Begin, generator) { }
    }
    public class CharEndMatch : Match
    {
        public CharEndMatch(TextRange beginEnd, Matcher generator) : base(beginEnd.End, beginEnd.End, generator) { }
    }
    #endregion

    #region 否定文字マッチ

    //public class DenyMatch : Match
    //{
    //    public DenyMatch(TextRange beginEnd, Matcher generator) : base(beginEnd,generator) { }
    //}
    #endregion

    #region 境界マッチ
    public class BorderMatch : Match
    {
        public BorderMatch(int begin, Matcher generator) : base(begin, begin, generator) { }
    }
    #endregion

    #region 連続文字マッチ
    public class CharsMatch : Match
    {
        public CharsMatch(TextRange beginEnd, Matcher generator) : base(beginEnd, generator) { }
    }
    #endregion

    #region 子持ちマッチの基底クラス
    public abstract class OwnerMatch : Match
    {
        public OwnerMatch(TextRange beginEnd, Matcher generator) : base(beginEnd, generator) { }

        public abstract IEnumerable<Match> Inners();

        public Dictionary<string, List<Match>> TaggedSubmatches;

    }
    #endregion


    #region ラップマッチ
    public class WrapMatch : OwnerMatch
    {
        public Match Inner { get; private set; }

        public WrapMatch(Match inner, Matcher generator) : base(inner, generator)
        {
            Inner = inner;
        }

        public override IEnumerable<Match> Inners()
        {
            yield return Inner;
        }
    }
    #endregion

    #region 右マッチ
    public class RightMatch : OwnerMatch
    {
        public Match Inner { get; private set; }

        public RightMatch(Match inner, Matcher generator) : base(inner, generator)
        {
            Inner = inner;
        }

        public override IEnumerable<Match> Inners()
        {
            yield return Inner;
        }
    }
    #endregion

    #region 左マッチ(結合マッチの未完成品)

    public class LeftMatch : OwnerMatch
    {
        /// <summary>
        /// 終了インデックスで管理された消去候補インスタンスリスト
        /// </summary>
        public static EndInstanceList<int, LeftMatch> EndInstances { get; private set; }
            = new EndInstanceList<int, LeftMatch>();



        public Match Inner { get; private set; }
        public LeftMatch(Match left, Matcher generator) : base(left, generator)
        {
            Inner = left;
            EndInstances.Add(End, this);
        }
        public override IEnumerable<Match> Inners()
        {
            yield return Inner;
        }
        /// <summary>
        /// 消去候補インスタンスリストからこのインスタンスを削除する
        /// </summary>
        public void RemoveEndInstance()
        {
            EndInstances.Remove(End, this);
        }
    }
    #endregion

    #region 結合マッチ
    public class PairMatch : OwnerMatch
    {
        public LeftMatch Left { get; private set; }
        public RightMatch Right { get; private set; }

        public PairMatch(LeftMatch left, RightMatch right, Matcher generator)
            : base(new TextRange(left.Begin, right.End), generator)
        {
            Left = left;
            // 消去候補インスタンスリストからこのleftを消す
            left.RemoveEndInstance();

            Right = right;
        }

        public override IEnumerable<Match> Inners()
        {
            yield return Left;
            yield return Right;
        }
    }
    #endregion

    #region ループ本体部(個別)マッチ
    public class LoopBodyMatch : OwnerMatch
    {
        public Match Inner { get; private set; }

        public LoopBodyMatch(Match body, Matcher generator) : base(body, generator)
        {
            if(body is LoopBodyMatch)
            {
                throw new ArgumentException();
            }

            Inner = body;
        }

        public override IEnumerable<Match> Inners()
        {
            yield return Inner;
        }
    }
    #endregion

    #region ループ本体部(全体)マッチ
    public class LoopBodysMatch : OwnerMatch
    {
        public LoopBodyMatch[] Bodys { get; private set; }


        public LoopBodysMatch(LoopBodyMatch body, Matcher generator)
            : base(new TextRange(body.Begin, body.End), generator)
        {
            Bodys = new LoopBodyMatch[1];
            Bodys[0] = body;
        }
        private LoopBodysMatch(LoopBodyMatch[] bodys, Matcher generator)
            : base(new TextRange(bodys[0].Begin, bodys[bodys.Length-1].End), generator)
        {
            Bodys = bodys;
        }
        public LoopBodysMatch AppendNew(LoopBodyMatch newBody)
        {
            // Bodys に新要素を追加した複製を作成する
            var bodyLen = Bodys == null ? 0 : Bodys.Length;
            var newBodys = new LoopBodyMatch[bodyLen + 1];
            if (bodyLen > 0)
            {
                Array.Copy(Bodys, newBodys, bodyLen);
            }
            newBodys[bodyLen] = newBody;

            var result = new LoopBodysMatch(newBodys, Generator);
            return result;
        }

        public override IEnumerable<Match> Inners()
        {
            foreach(var body in Bodys)
            {
                yield return body;
            }
        }
    }
    #endregion

    #region ループ包含マッチ
    public class LoopMatch : OwnerMatch
    {
        /// <summary>
        /// 終了インデックスで管理された消去候補インスタンスリスト
        /// </summary>
        public static EndInstanceList<int, LoopMatch> EndInstances { get; private set; }
            = new EndInstanceList<int, LoopMatch>();
        
        public Match Head { get; private set; }
        public LoopBodysMatch Bodys { get; private set; }

        public bool Finished { get; private set; } = false;

        public LoopMatch(Match head, Matcher generator)
            : base(new TextRange(head.Begin, head.End), generator)
        {
            Head = head;
            Bodys = null;
            EndInstances.Add(End, this);
        }

        //public LoopMatch(LoopMatch org, Match newBody, Matcher generator)
        //    : base(new TextRange(org.Begin, newBody.End),generator)
        //{
        //    Head = org.Head;

        //    var orgBodyLen = org.Bodys.Length;

        //    Bodys = new Match[orgBodyLen + 1];
        //    if(orgBodyLen > 0)
        //    {
        //        Array.Copy(org.Bodys, Bodys, orgBodyLen);
        //    }
        //    Bodys[orgBodyLen] = newBody;
        //}

        public LoopMatch(Match head, LoopBodysMatch bodys, Matcher generator)
            : base(new TextRange(head.Begin, bodys.End), generator)
        {
            Head = head;
            Bodys = bodys;
            EndInstances.Add(End, this);
        }

        /// <summary>
        /// 本体部を追加した新しいインスタンスを返す
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public LoopMatch AddBody(LoopBodyMatch body)
        {
            LoopBodysMatch bodys;
            if(Bodys == null)
            {
                bodys = new LoopBodysMatch(body, Generator);

                if(bodys.UniqID == "T29")
                {
                    var temp = "";
                }

            }
            else
            {
                bodys = Bodys.AppendNew(body);
            }
            var result = new LoopMatch(Head, bodys, Generator);
            return result;
        }

        /// <summary>
        /// 末尾部を追加した新しいインスタンスを返す
        /// </summary>
        /// <param name="tail"></param>
        /// <returns></returns>
        public LoopFinishedMatch SetTail(Match tail)
        {
            var result = new LoopFinishedMatch(Head, Bodys, tail, Generator);
            Finished = true;
            return result;
        }

        public override IEnumerable<Match> Inners()
        {
            yield return Head;
            if(Bodys != null)
            {
                foreach (var body in Bodys.Inners())
                {
                    yield return body;
                }
            }
        }
    }
    #endregion

    #region ループ完成マッチ
    public class LoopFinishedMatch : OwnerMatch
    {
        public Match Head { get; private set; }
        public LoopBodysMatch Bodys { get; private set; }
        public Match Tail { get; private set; }

        public LoopFinishedMatch(Match head, LoopBodysMatch bodys, Match tail, Matcher generator)
            : base(new TextRange(head.Begin, tail.End), generator)
        {
            Head = head;
            Bodys = bodys;
            Tail = tail;
        }


        public override IEnumerable<Match> Inners()
        {
            yield return Head;
            if (Bodys != null)
            {
                yield return Bodys;
            }
            if (Tail != null)
            {
                yield return Tail;
            }
        }
    }
    #endregion

    #region 選択マッチ
    public class EitherMatch : OwnerMatch
    {
        public Match Inner { get; private set; }
        public EitherMatch(Match match, Matcher generator) : base(match, generator)
        {
            Inner = match;
        }

        public override IEnumerable<Match> Inners()
        {
            yield return Inner;
        }
    }
    #endregion

    #region 合併マッチ(複数の EitherMatch から作る)
    public class CombinedMatch : OwnerMatch
    {
        private List<EitherMatch> _inners;
        private CombinedMatch(EitherMatch firstItem, IEnumerable<EitherMatch> otherItems, Matcher generator)
            : base(firstItem, generator)
        {
            _inners = new List<EitherMatch>();
            _inners.Add(firstItem);

            foreach(var item in otherItems)
            {
                _inners.Add(item);
            }
        }

        public static CombinedMatch CreateInstance(EitherMatch firstItem, IEnumerable<EitherMatch> otherItems)
        {
            int begin = firstItem.Begin;
            int end = firstItem.End;
            foreach(var match in otherItems)
            {
                if (begin != match.Begin) { throw new ArgumentOutOfRangeException(); }
                if (end != match.End) { throw new ArgumentOutOfRangeException(); }
            }

            return new CombinedMatch(firstItem, otherItems, firstItem.Generator);
        }


        public override IEnumerable<Match> Inners()
        {
            foreach(var item in _inners)
            {
                yield return item;
            }
        }
    }
    #endregion
    
    #region 再帰マッチ
    public class RecursionMatch : OwnerMatch
    {
        private Match _inner;
        public RecursionMatch(Match inner, Matcher generator) : base(inner, generator)
        {
            _inner = inner;
        }
        public override IEnumerable<Match> Inners()
        {
            yield return _inner;
        }
    }
    #endregion

    #region ルートマッチ
    public class RootMatch : OwnerMatch
    {
        public Match Inner { get; private set; }

        public RootMatch(Match match, Matcher generator) : base(match, generator)
        {
            Inner = match;
        }

        public override IEnumerable<Match> Inners()
        {
            yield return Inner;
        }
    }
    #endregion

    #region 特殊マッチ
    public class SpecialMatch : Match
    {
        public SpecialMatch(TextRange beginEnd, Matcher generator) : base(beginEnd, generator) { }

        public SpecialMatch(int begin, int end, Matcher generator) : base(begin, end, generator) { }
    }

    /// <summary>
    /// 文字列開始マッチ
    /// </summary>
    public class BeginMatch : SpecialMatch { public BeginMatch(BeginAtom atom, Matcher generator) : base(atom, generator) { } }

    /// <summary>
    /// 文字列終了マッチ
    /// </summary>
    public class EndMatch : SpecialMatch { public EndMatch(EndAtom atom, Matcher generator) : base(atom, generator) { } }

    /// <summary>
    /// インデントマッチ
    /// </summary>
    public class IndentMatch : SpecialMatch { public IndentMatch(IndentAtom atom, Matcher generator) : base(atom, generator) { } }

    /// <summary>
    /// デデントマッチ
    /// </summary>
    public class DedentMatch : SpecialMatch { public DedentMatch(DedentAtom atom, Matcher generator) : base(atom, generator) { } }

    /// <summary>
    /// 改行マッチ
    /// </summary>
    public class NewlineMatch : SpecialMatch { public NewlineMatch(LineheadAtom atom, Matcher generator) : base(atom, generator) { } }

    /// <summary>
    /// 長さゼロ文字列マッチ
    /// </summary>
    public class ZeroLengthMatch : SpecialMatch { public ZeroLengthMatch(int index, Matcher generator) : base(index, index, generator) { } }
    #endregion
}
