using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LIME
{
    /// <summary>
    /// 単体の親と複数体の子の関係を格納するマップ。
    /// </summary>
    /// <typeparam name="TChildren">多数となる型</typeparam>
    /// <typeparam name="TParent">単数となる型</typeparam>
    public class MatchMatcherMap
    {
        private Dictionary<Matcher, HashSet<Match>> _ownerChildren
            = new Dictionary<Matcher, HashSet<Match>>();
        private Dictionary<Match, Matcher> _childOwner
            = new Dictionary<Match, Matcher>();

        /// <summary>
        /// 任意の子の親を取得・設定する
        /// </summary>
        /// <param name="child">任意の子</param>
        /// <returns>親</returns>
        public Matcher this[Match child]
        {
            get
            {
                return _childOwner[child];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // 親が未設定なら設定して終了
                if (_childOwner.ContainsKey(child) == false)
                {
                    // 親を設定する
                    _childOwner.Add(child, value);

                    // 指定された親に子が居ない時
                    if (_ownerChildren.ContainsKey(value) == false)
                    {
                        // 子のリストを作成する
                        _ownerChildren.Add(value, new HashSet<Match>());
                    }
                    // この子を子のリストに入れる
                    _ownerChildren[value].Add(child);
                    return;
                }

                //
                // 親が設定済みなら一旦親子関係を解消する。
                //

                // 現在の親を取得する
                var currentParent = _childOwner[child];

                // 現在親の兄弟を取得する
                var currentBrothers = _ownerChildren[currentParent];

                // 兄弟からこの子を削除する
                currentBrothers.Remove(child);

                // 兄弟が居なくなった時
                if (currentBrothers.Count == 0)
                {
                    // 兄弟のリストそのものを削除する
                    _ownerChildren.Remove(currentParent);
                }

                // 新しい親が子のリストを所持していない時 
                if (_ownerChildren.ContainsKey(value) == false)
                {
                    // 子のリストを追加する
                    _ownerChildren.Add(value, new HashSet<Match>());
                }
                // 新しい親の子にこの子を追加する
                _ownerChildren[value].Add(child);

                // この子の親を更新する。
                _childOwner[child] = value;
            }
        }

        /// <summary>
        /// 親に含まれる子を列挙するインデクサ
        /// </summary>
        /// <param name="parent">親</param>
        /// <returns></returns>
        public HashSet<Match> this[Matcher parent]
        {
            get
            {
                if (_ownerChildren.ContainsKey(parent) == false)
                {
                    _ownerChildren.Add(parent, new HashSet<Match>());
                }
                return _ownerChildren[parent];
            }
        }

        public HashSet<string> RemovedList = new HashSet<string>();

        /// <summary>
        /// 任意の子の存在を削除する。親子関係も切る。
        /// </summary>
        /// <param name="child"></param>
        public void Remove(Match child)
        {
            if (RemovedList.Contains(child.UniqID))
            {
                throw new ArgumentException();
            }
            RemovedList.Add(child.UniqID);

            //if((child is LeftMatch) || (child is LoopMatch))
            //{
            //    Debug.WriteLine($"Match.Map.Remove({child.SpecialID})");
            //}

            // 現在の親を取得する
            var parentMatcher = _childOwner[child];

            // 現在親の兄弟を取得する
            var currentBrothers = _ownerChildren[parentMatcher];

            // 兄弟からこの子を削除する
            currentBrothers.Remove(child);

            // 兄弟が居なくなった時
            if (currentBrothers.Count == 0)
            {
                // 兄弟のリストそのものを削除する
                _ownerChildren.Remove(parentMatcher);
            }

            // 子→親のリストから子を消す
            _childOwner.Remove(child);
        }

        public bool Contains(Match child)
        {
            return _childOwner.ContainsKey(child);
        }
        public bool Contains(Matcher parent)
        {
            return _ownerChildren.ContainsKey(parent);
        }

        public IEnumerable<Matcher> Parents
        {
            get
            {
                return _ownerChildren.Keys;
            }
        }

        public IEnumerable<Match> Children
        {
            get
            {
                return _childOwner.Keys;
            }
        }

        public void Clear()
        {
            _childOwner.Clear();
            foreach (var key in _ownerChildren.Keys)
            {
                _ownerChildren[key].Clear();
            }
            _ownerChildren.Clear();
        }

        public void Dump()
        {
            var matchers = _ownerChildren.Keys;

            foreach(var keyValue in _ownerChildren)
            {
                var matcher = keyValue.Key;
                Dump(matcher);
            }
        }

        public void Dump(Matcher matcher)
        {
            var hash = _ownerChildren[matcher];
            if(hash.Count > 0)
            {
                Debug.WriteLine($"{matcher.UniqID} {matcher.TypeName}");

                foreach (var match in hash)
                {
                    Debug.WriteLine($"  {match.UniqID} {match.TypeName} [{match.Begin}-{match.End}]");
                }
            }
        }
    }

}
