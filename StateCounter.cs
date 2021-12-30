using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LIME
{
    /// <summary>
    /// デバッグ用処理状況カウンタ
    /// </summary>
    public abstract class StateCounter
    {
        /// <summary>
        /// 処理状況カウント
        /// </summary>
        public static int Count { get; private set; } = 0;

        /// <summary>
        /// 処理状況カウンタをインクリメントする
        /// </summary>
        public static void Add()
        {
            Count++;
            if (Count == 209)
            {
                var temp = "";
            }
        }

        public static List<string> MemoLog { get; private set; } = new List<string>();
        public static void Memo(string text)
        {
            MemoLog.Add($"{Count} {text}");
        }

        public static void Dumplog()
        {
            Debug.WriteLine("▽▽　Dumplog ▽▽");
            foreach (var log in MemoLog)
            {
                Debug.WriteLine(log);
            }
            Debug.WriteLine("▲▲　Dumplog　▲▲");
        }

        public static void Clear()
        {
            Count = 0;
            MemoLog.Clear();
        }
    }
}
