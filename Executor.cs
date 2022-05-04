using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;

using System.Text.RegularExpressions;
using PipeIO;

namespace LIME
{
    #region マッチング実行器
    /// <summary>
    /// 文字列を受け取り、アトムに変換し、末端マッチャーに与え、マッチングを実行する
    /// </summary>
    public static class Executor
    {
        public static BlankList Blanks = new BlankList();

        public static int CallCount = 0;

        /// <summary>
        /// 文字列を受け取り、文字トークンを放出する。
        /// </summary>
        /// <param name="text"></param>
        public static void Execute(string text, Matcher root)
        {
            TextViewClient client = new TextViewClient();
            
            CallCount++;

            // 肯定文字・否定文字から親のある者のみ抽出
            var chars = CharMatcher.InstanceList.Where(m => m.HasParent);
            // 境界マッチャーから親のある者のみ抽出
            var borders = BorderMatcher.InstanceList.Where(m => m.HasParent);

            // 文字列を要素列に分解する
            IEnumerable <TextAtom> atoms = new List<TextAtom>(Atomize(text));

            var atomIndex = -1;
            foreach (var atom in atoms)
            {
                atomIndex++;
                var atomBegin = atom.Begin;

                if (atom is CharAtom c)
                {
                    // 長さゼロマッチを貯めさせる
                    SpecialMatcher.ReizeZerolengthMatch(atom.Begin);

                    // 肯定文字・否定文字ビルダーに文字マッチを貯めさせる
                    chars.ForEach(m => m.IsMatch(c));
                }

                // 境界アトムの時
                else if(atom is BorderAtom borderAtom)
                {
                    borders.ForEach(m => m.IsMatch(borderAtom));
                }

                // 開始・終了・インデント・デデント・改行の時
                else if (atom is DelimiterAtom)
                {
                    SpecialMatcher.ReizeMatch(atom);
                }

                // 生成直後でまだ上位に上げてないマッチ全てを上に上げる
                while (Match.NewItems.Count > 0)
                {

                    // 動かす優先順位の高いマッチを取得する
                    foreach (var newMatch in Match.NewItems)
                    {

                        //if(newMatch.UniqID == "T29")
                        //{
                        //    var temp = "";
                        //}

                        //// ツリー構造を取得する
                        //client.Clear();
                        //client.WriteLine($"{atom.Begin}");
                        //root.Debug_OutputTreeDetail(client);
                        ////client.WriteLine(tree);
                        //client.Wait();


                        Match targetMatch = newMatch;
                        // マッチが居るマッチャーを取得する
                        var matcher = Match.Map[targetMatch];

                        // EitherMatchの時は合併を試みる
                        if (targetMatch is EitherMatch either)
                        {
                            // このマッチャーに居る別のマッチを取得する
                            var same = Match.NewItems.EnumSameEithers(either);

                            if (same.Count > 0)
                            {
                                targetMatch = CombinedMatch.CreateInstance(either, same);
                                Match.NewItems.RemoveEithers(same);

                                // CombinedMatch に取り込まれたので参照カウントを減らす
                                foreach (var sameItem in same)
                                {
                                    sameItem.InstanceCounterRemove(atom);

                                    // このマッチは不要なので Map から参照を消す
                                    Match.Map.Remove(sameItem);
                                }
                            }
                        }

                        // マッチャーの親マッチャー達を取得する
                        var matchersParents = matcher.GetParents();

                        // すべての親マッチャーにマッチを送る
                        foreach (var matchParent in matchersParents)
                        {
                            if ((matchParent.HasParent) || (matchParent is RootMatcher))
                            {
                                matchParent.IsMatch(matcher, targetMatch);
                            }
                        }

                        //if(targetMatch.UniqID == "T15")
                        //{
                        //    var temp = "";
                        //}

                        if(targetMatch.Length == 0)
                        {
                            var temp = "";
                        }

                        // このマッチは不要なので Map から参照を消す
                        Match.Map.Remove(targetMatch);

                        // このマッチが取り込まれたか否かに関わらず、参照カウントを減らす。
                        targetMatch.InstanceCounterRemove(atom);
                    }
                }
            }
        }

        #region アトム化関数群
        /// <summary>
        /// テキストからアトムの列挙子を得ると同時に、ブランクを出力する。
        /// </summary>
        /// <param name="text"></param>
        /// <param name="blanks"></param>
        /// <returns></returns>
        public static IEnumerable<TextAtom> Atomize(string text)
        {
            IEnumerable<TextAtom> atoms;

            // 文字列からアトム列に変換する
            atoms = TextToCharAtoms(text).ToArray();
            // アトム列の前後に「開始」と「最初の行頭」と「終端」を挿入する
            atoms = InsertBeginEnd(atoms).ToArray();

            // 空白を纏める
            atoms = ConvertSpaces(atoms).ToArray();
            // CrとLfを纏め、行頭アトムを挿入する
            atoms = InsertLinehead(atoms).ToArray();
            // 通常空白の抽出とインデント・デデントの挿入
            atoms = InsertIndent(atoms).ToArray();

            // ブランク系トークンをバラして行開始トークンを差し込む
            atoms = DivideBlank(atoms).ToArray();

            // 文字同士の間に境界アトムを挿入する
            atoms = InsertBorder(atoms).ToArray();

            //// ダミー文字を消去する
            //atoms = RemoveDummy(atoms);



            return atoms;
        }

        #region 文字列を文字アトム列に変換する
        /// <summary>
        /// 文字列を文字アトム列に変換する
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static IEnumerable<TextAtom> TextToCharAtoms(string text)
        {
            var codes = new CharCodeStream(text);
            int index = 0;
            foreach (var code in codes)
            {
                int length = (code <= 0x010000) ? 1 : 2;
                yield return new CharAtom(code, index, index + length);
                index += length;
            }

        }
        #endregion

        #region 空白を纏める
        /// <summary>
        /// １文字以上連続した空白を示す文字アトムを空白アトムに纏める
        /// </summary>
        /// <param name="atoms"></param>
        /// <param name="addFunc">リストに空白アトムを格納する為のコールバック関数</param>
        /// <returns></returns>
        private static IEnumerable<TextAtom> ConvertSpaces(IEnumerable<TextAtom> atoms)
        {
            static bool IsBlankChar(TextAtom atom)
            {
                if (atom is CharAtom c)
                {
                    switch (c.Code)
                    {
                    case ' ':
                    case '\t':
                        return true;
                    }
                }
                return false;
            }

            int spaceStart = -1;
            int spaceEnd = -1;
            SpacesAtom newBlank;

            foreach (var atom in atoms)
            {
                if(IsBlankChar(atom))
                {
                    if (spaceStart < 0)
                    {
                        spaceStart = atom.Begin;
                    }
                    spaceEnd = atom.End;
                    continue;
                }

                if (0 <= spaceStart)
                {
                    newBlank = new SpacesAtom(spaceStart, spaceEnd);
                    spaceStart = -1;
                    spaceEnd = -1;
                    Blanks.Add(newBlank);
                    yield return newBlank;
                }
                yield return atom;
            }

            if (0 <= spaceStart)
            {
                newBlank = new SpacesAtom(spaceStart, spaceEnd);
                spaceStart = -1;
                spaceEnd = -1;
                Blanks.Add(newBlank);
                yield return newBlank;
            }
        }
        #endregion

        #region CrとLfを纏め、行頭アトムを挿入する
        /// <summary>
        /// CrとLfを纏め、Cr、Lf、CrLfの直後に行頭アトムを差し込む
        /// </summary>
        /// <param name="atoms"></param>
        /// <returns></returns>
        private static IEnumerable<TextAtom> InsertLinehead(IEnumerable<TextAtom> atoms)
        {
            var queue = new BufferQueue<TextAtom>(atoms, 2);

            while(queue.Count > 0)
            {
                if((queue.Count == 2) &&
                    (queue[0] is CharAtom c1) &&(c1.Code == '\r') &&
                    (queue[1] is CharAtom c2) && (c2.Code == '\n'))
                {
                    queue.Dequeue();
                    queue.Dequeue();
                    yield return new CrLfAtom(c1.Begin);
                    yield return new LineheadAtom(c2.End);
                }
                else 
                {
                    if(queue[0] is CharAtom c)
                    {
                        queue.Dequeue();
                        switch (c.Code)
                        {
                        case '\r':
                            yield return new CrAtom(c.Begin);
                            yield return new LineheadAtom(c.End);
                            break;
                        case '\n':
                            yield return new LfAtom(c.Begin);
                            yield return new LineheadAtom(c.End);
                            break;
                        default:
                            yield return c;
                            break;
                        }
                    }
                    else
                    {
                        yield return queue.Dequeue();
                    }
                }
            }
        }
        #endregion

        #region 行頭空白の抽出とインデント・デデントの挿入
        private static IEnumerable<TextAtom> InsertIndent(IEnumerable<TextAtom> atoms)
        {
            var queue = new BufferQueue<TextAtom>(atoms,3);
            var nestStack = new Stack<int>();
            nestStack.Push(0);

            while (queue.Count> 0)
            {
                // 行頭・空白・改行文字以外、の時
                if ((queue.Count == 3) &&
                    (queue[0] is LineheadAtom newLine) &&
                    (queue[1] is SpacesAtom space) &&
                    !(queue[2] is NewLineCharAtom))
                {
                    queue.Dequeue();
                    queue.Dequeue();
                    yield return newLine;
                    // スペースは「行頭スペース」として扱う
                    yield return new LineHeadSpacesAtom(space.Begin, space.End);

                    //
                    // インデント・デデントの計算
                    //


                    // ネストが変わらない時は何もしない
                    if (nestStack.Peek() == space.Length)
                    {
                        continue;
                    }
                    // ネストが深くなっていればスタックを積む
                    else if (nestStack.Peek() < space.Length)
                    {
                        nestStack.Push(space.Length);
                        yield return new IndentAtom(space.End);
                        continue;
                    }

                    // このスペースのネストよりスタックのネストが深い時は
                    // 同等の深さのネストが見つかるまでスタックから値を抜いていく
                    while(space.Length < nestStack.Peek())
                    {
                        nestStack.Pop();

                        if (nestStack.Peek() < space.Length)
                        {
                            nestStack.Push(space.Length);
                            yield return new InconsistentDedentAtom(space.End);
                            break;
                        }

                        yield return new DedentAtom(space.End);
                    }
                }
                else
                {
                    yield return queue.Dequeue();
                }
            }
        }
        #endregion

        #region ブランク系トークンを文字トークンにバラす
        /// <summary>
        /// ブランク系トークンを文字トークンにバラす
        /// </summary>
        /// <param name="atoms"></param>
        /// <returns></returns>
        private static IEnumerable<TextAtom> DivideBlank(IEnumerable<TextAtom> atoms)
        {
            foreach (var atom in atoms)
            {
                if (atom is BlankAtom blank)
                {
                    foreach (var c in blank.EnumChars())
                    {
                        yield return c;
                    }
                }
                else
                {
                    yield return atom;
                }
            }
        }
        #endregion

        #region ダミー文字を消去する
        private static IEnumerable<TextAtom> RemoveDummy(IEnumerable<TextAtom> atoms)
        {
            foreach (var atom in atoms)
            {
                if (atom is BeginAtom) { continue; }
                if (atom is EndAtom) { continue; }
                yield return atom;
            }
        }
        #endregion

        #region アトム列の前後に開始と終端を挿入する
        /// <summary>
        /// アトム列に開始アトムと終了アトムを追加する
        /// </summary>
        /// <param name="atoms"></param>
        /// <returns></returns>
        private static IEnumerable<TextAtom> InsertBeginEnd(IEnumerable<TextAtom> atoms)
        {
            // 文字列開始アトムを挿入する
            yield return new BeginAtom();

            // 最初の行の開始アトムを挿入する
            yield return new FirstLineheadAtom();

            int end = 0;

            foreach (var atom in atoms)
            {
                end = atom.End;
                yield return atom;
            }

            yield return new EndAtom(end);
        }
        #endregion

        #region 文字と文字の間に境界アトムを挿入する
        private static IEnumerable<TextAtom> InsertBorder(IEnumerable<TextAtom> atoms)
        {
            TextAtom prev = null;

            foreach (var atom in atoms)
            {
                if ((atom.Begin == 0) && (atom is CharAtom))
                {
                    yield return new BorderAtom(prev, atom);
                }
                else if(atom is EndAtom)
                {
                    yield return new BorderAtom(prev, atom);
                }
                else if((prev is CharAtom cPrev) && (atom is CharAtom cNext))
                {
                    yield return new BorderAtom(prev, atom);
                }
                yield return atom;
                prev = atom;
            }
        }
        #endregion

        #endregion アトム化関数群


        public static bool CheckConnection(Match prev, int prevEnd, int nextBegin)
        {
            // 直接左右が隣接している時はＯＫ
            if (prevEnd == nextBegin) { return true; }

            // NullStringMatch は空白を挟んで隣接する事はできない
            if (prev is ZeroLengthMatch)
            {
                return false;
            }
            // BorderMatch は空白を挟んで隣接する事はできない
            if (prev is BorderMatch)
            {
                return false;
            }

            // 左終端と連結するブランクが無い時はＮＧ
            if (Blanks.ContainsBegin(prevEnd) == false) { return false; }

            var currentBlank = Blanks.BeginToBlank(prevEnd);

            var currentEnd = prevEnd;
            // var nextEnd = Blanks.BeginToBlank(currentEnd).End;

            do
            {
                if (currentBlank.End == nextBegin)
                {
                    return true;
                }
                if (currentBlank.End > nextBegin)
                {
                    return false;
                }
                // 次のブランクが存在しない時
                if (Blanks.ContainsBegin(currentBlank.End) == false)
                {
                    return false;
                }
                // 次のブランクを引っ張り出す
                currentBlank = Blanks.BeginToBlank(currentBlank.End);
                continue;

            } while (true);

        }
    }
    #endregion

}
