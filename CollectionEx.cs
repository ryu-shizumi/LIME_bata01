using System;
using System.Collections.Generic;
using System.Text;

namespace LIME
{
    public static class CollectionEx
    {
        /// <summary>
        /// ICollection<T>の要素を削除するの為の列挙子を取得する
        /// </summary>
        /// <typeparam name="T">コレクションが格納するデータ型</typeparam>
        /// <param name="collection">コレクション</param>
        /// <returns></returns>
        public static IEnumerable<T> RemoveEnum<T>(this ICollection<T> collection)
        {
            var list = new List<T>(collection);

            foreach (var item in list)
            {
                // 既に削除済みの要素は無視する
                if (collection.Contains(item) == false)
                {
                    continue;
                }
                yield return item;
            }
        }
    }
}
