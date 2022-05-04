using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LIME
{
    /// <summary>
    /// 新規生成されたMatchインスタンスを格納し、特定の順番で列挙するコンテナ型
    /// </summary>
    public class NewMatchesList : IEnumerable<Match>
    {
        // 独自に定義した並べ替え順序を実装するIComparerを指定してSortedListを作成
        SortedSet<Match> _set = new SortedSet<Match>(new MatchComparer());

        public SortedSet<Match> InnerSet
        {
            get { return _set; }
        }

        public void Add(Match match)
        {
            _set.Add(match);
        }

        public void Clear()
        {
            _set.Clear();
        }

        public int Count
        {
            get { return _set.Count; }
        }

        /// <summary>
        /// 引数と同じBegin・End・Matcher(位置)のEitherMatchを探す
        /// </summary>
        /// <param name="either"></param>
        /// <returns>引数と同じBegin・End・Matcher(位置)のEitherMatchが格納されたリスト</returns>
        public List<EitherMatch> EnumSameEithers(EitherMatch either)
        {
            // 引数と同じBegin・End・MatcherのEitherMatchを探す

            List<EitherMatch> eithers = new List<EitherMatch>();

            foreach (var item in _set)
            {
                if (item.Begin < either.Begin) { continue; }
                if (either.Begin < item.Begin) { break; }
                if (item.End < either.End) { continue; }
                if (either.End < item.End) { break; }

                if(Match.Map[item] != Match.Map[either]) { continue; }


                if (item is EitherMatch eitherItem)
                {
                    eithers.Add(eitherItem);
                }
            }

            return eithers;
        }

        public void RemoveEithers(IEnumerable<EitherMatch> items)
        {
            foreach(var item in items)
            {
                _set.Remove(item);
            }
        }

        /// <summary>
        /// 列挙の途中で要素を挿入されても大丈夫な列挙処理
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Match> GetEnumerator()
        {
            // 列挙で最初の値を取得したら列挙を打ち切る
            // 列挙処理を維持したままだとコレクションを変更できないので例外で落ちる
            while(_set.Count > 0)
            {
                Match firstItem = null;
                foreach(var item in _set)
                {
                    firstItem = item;
                    _set.Remove(item);
                    break;
                }
                // ] を排出しようとした時、既に,1が存在するかを確認する

                if(firstItem.Value == "]")
                {
                    var temp = "";
                }

                // Debug.WriteLine($"[{firstItem.Begin}-{firstItem.End}] \"{firstItem.Value}\" {firstItem.UniqID} {firstItem.Generator.UniqID}");
                yield return firstItem;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region 比較用クラス
        private class MatchComparer : IComparer<Match>
        {
            /// <summary>
            /// ソート用比較関数
            /// </summary>
            /// <param name="match1">マッチ１</param>
            /// <param name="match2">マッチ２</param>
            /// <returns></returns>
            public int Compare(Match match1, Match match2)
            {
                // 開始位置が最優先
                var beginDiff = match1.Begin - match2.Begin;
                if (beginDiff != 0)
                {
                    return beginDiff;
                }

                // 終了位置がその次
                // (開始位置が同じなので「短さ」と同義)
                var endDiff = match1.End - match2.End;
                if (endDiff != 0)
                {
                    return endDiff;
                }

                // 最後が型の優先度
                var typeDiff = TypePriority(match1) - TypePriority(match2);
                if (typeDiff != 0)
                {
                    return typeDiff;
                }

                // それでも差が出ない時は、マッチのインスタンス変数を比較する
                return match2.GetHashCode() - match1.GetHashCode();
            }

            private static Dictionary<Type, int> _typePriorityDict;
            private static int TypePriority(Match match)
            {
                int numBase = 0;
                int AutoNumber()
                {
                    return numBase++;
                }

                // "]" でパターン終了が確定するが、"]"自体が先走って行ってしまう。

                // 辞書が未設定の時は設定する
                if (_typePriorityDict == null)
                {
                    _typePriorityDict = new Dictionary<Type, int>();

                    _typePriorityDict.Add(typeof(BorderMatch), AutoNumber());
                    _typePriorityDict.Add(typeof(LeftMatch), AutoNumber());
                    //_typePriorityDict.Add(typeof(DenyMatch), AutoNumber());
                    //_typePriorityDict.Add(typeof(LoopBodyMatch), AutoNumber());
                    //_typePriorityDict.Add(typeof(LoopFinishedMatch), AutoNumber());
                    _typePriorityDict.Add(typeof(CharMatch), AutoNumber());
                    _typePriorityDict.Add(typeof(CharsMatch), AutoNumber());
                    _typePriorityDict.Add(typeof(EitherMatch), AutoNumber());
                    _typePriorityDict.Add(typeof(RightMatch), int.MaxValue);
                }

                // 型ごとの優先度を返信する
                var matchType = match.GetType();
                if (_typePriorityDict.ContainsKey(matchType))
                {
                    return _typePriorityDict[matchType];
                }
                else
                {
                    // 合致する優先度が無い時は大きい数を返しておく
                    return int.MaxValue - 1;
                }

            }
        }
        #endregion
    }


}
