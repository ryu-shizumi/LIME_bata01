using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIME
{
    #region ブランクリスト
    /// <summary>
    /// ブランクリスト
    /// </summary>
    public class BlankList
    {
        private List<BlankAtom> _list = new();

        public void Add(BlankAtom token)
        {
            _list.Add(token);
        }
        /// <summary>
        /// 任意の開始位置のブランクが存在するかを取得する
        /// </summary>
        /// <param name="begin"></param>
        /// <returns></returns>
        public bool ContainsBegin(int begin)
        {
            var index = IndexOfBegin(begin);
            return 0 <= index;
        }
        /// <summary>
        /// 任意の開始位置のブランクが存在するかを取得する
        /// </summary>
        /// <param name="begin"></param>
        /// <returns></returns>
        public bool ContainsEnd(int end)
        {
            var index = IndexOfEnd(end);
            return 0 <= index;
        }
        /// <summary>
        /// 任意の開始位置を持つブランクのリスト内インデックスを取得する
        /// </summary>
        /// <param name="begin"></param>
        /// <returns></returns>
        public int IndexOfBegin(int begin)
        {
            return _list.FindValue(begin, (t) => t.Begin);
        }
        /// <summary>
        /// 任意の終了位置を持つブランクのリスト内インデックスを取得する
        /// </summary>
        /// <param name="begin"></param>
        /// <returns></returns>
        public int IndexOfEnd(int end)
        {
            return _list.FindValue(end, (t) => t.End);
        }
        /// <summary>
        /// 任意の開始位置を持つブランクを取得する
        /// </summary>
        /// <param name="begin"></param>
        /// <returns></returns>
        public BlankAtom BeginToBlank(int begin)
        {
            var index = IndexOfBegin(begin);
            if (index == -1) { throw new ArgumentOutOfRangeException(); }
            return _list[index];
        }
        /// <summary>
        /// 任意の終了位置を持つブランクを取得する
        /// </summary>
        /// <param name="end"></param>
        /// <returns></returns>
        public BlankAtom EndToBlank(int end)
        {
            var index = IndexOfEnd(end);
            if (index == -1) { throw new ArgumentOutOfRangeException(); }
            return _list[index];
        }
    }

    #region 二分検索の為の IList<T> 拡張メソッド
    public static class ListEx
    {
        /// <summary>
        /// リストから任意の値を持つアイテム検索しインデックスを返す
        /// </summary>
        /// <typeparam name="TItem">リストが格納するアイテムの型</typeparam>
        /// <typeparam name="TValue">検索する値の型。比較可能である事</typeparam>
        /// <param name="list">リスト</param>
        /// <param name="value">検索する値</param>
        /// <param name="itemValuator">アイテムから値を抽出する関数</param>
        /// <returns>発見できればそのインデックス。存在しなければ -1</returns>
        public static int FindValue<TItem, TValue>
            (this IList<TItem> list, TValue value, Func<TItem, TValue> itemValuator)
            where TValue : IComparable<TValue>
        {
            return FindValue_body(list, value, 0, list.Count - 1, itemValuator);
        }
        private static int FindValue_body<TItem, TValue>
            (IList<TItem> list, TValue value,
            int left, int right, Func<TItem, TValue> itemValuator)
             where TValue : IComparable<TValue>
        {
            if (list.Count == 0)
            { return -1; }
            var center = (left + right) / 2;
            var itemValue = itemValuator(list[center]);

            var comp = itemValue.CompareTo(value);

            if (comp == 0)
            { return center; }

            if (left == right)
            { return -1; }

            int nextLeft;
            int nextRight;
            if (comp < 0)
            {
                nextLeft = center + 1;
                nextRight = right;
            }
            else
            {
                nextLeft = left;
                nextRight = center - 1;
            }
            if (nextRight < nextLeft)
            { return -1; }

            return FindValue_body(list, value, nextLeft, nextRight, itemValuator);
        }

        public static int FindTest(this IList<int> list, int value)
        {
            return FindValue(list, value, (a) => a);
        }
    }
    #endregion

    #endregion
}
