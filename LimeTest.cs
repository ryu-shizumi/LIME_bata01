using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace LIME
{
    public static class LimeTest
    {
        public static void test()
        {
            NewMatchesList list = new NewMatchesList();

            //var newRange = new TextRange(0, 1);
            //var newMatch = new CharMatch(newRange);

            //list.Add(new CharMatch(new TextRange(4, 4)));

            //list.Add(new CharMatch(new TextRange(0, 2)));
            //list.Add(new CharMatch(new TextRange(0, 1)));
            //list.Add(new CharMatch(new TextRange(0, 4)));
            //list.Add(new CharMatch(new TextRange(0, 3)));
            //list.Add(new CharMatch(new TextRange(0, 0)));

            //list.Add(new CharMatch(new TextRange(2, 4)));
            //list.Add(new CharMatch(new TextRange(1, 4)));
            //list.Add(new CharMatch(new TextRange(3, 4)));

            //list.Add(new DenyMatch(newMatch));
            //list.Add(new CharMatch(newMatch));
            //list.Add(new EitherMatch(newMatch));
            //list.Add(new DenyMatch(newMatch));
            //list.Add(new CharMatch(newMatch));
            //list.Add(new RightMatch(newMatch));
            //list.Add(new LeftMatch(newMatch));
            //list.Add(new EitherMatch(newMatch));
            //list.Add(new CharsMatch(newMatch));
            //list.Add(new BorderMatch(newMatch.Begin));

            //foreach (var item in list.InnerSet)
            //{
            //    Debug.WriteLine($"{item.TypeName} [{item.Begin}-{item.End}]");
            //}

            var number = '0'.To('9');
            var numbers = number.IsolatedLoop();

            //"7".FindBest(numbers)?.DebugWrite();
            //"7 ".FindBest(numbers)?.DebugWrite();
            //"78".FindBest(numbers)?.DebugWrite();
            //"78  ".FindBest(numbers)?.DebugWrite();
            //" 78  ".FindBest(numbers)?.DebugWrite();


            var Lu = 'A'.To('Z');
            var Ll = 'a'.To('z');
            var alphabet = Lu | Ll;
            var alphabets = alphabet.IsolatedLoop();

            //"abc".FindBest('a'._() + 'b')?.DebugWrite();
            //"1+2".FindBest('1'._() + '+')?.DebugWrite();
            //"1+2".FindBest(number + '+')?.DebugWrite();
            //"1+2".FindBest(number + '+' + number)?.DebugWrite();

            //"123".FindBest(numbers)?.DebugWrite();
            //"1+2".FindBest(numbers)?.DebugWrite();
            //"12".FindBest(number + number)?.DebugWrite();
            //"1a".FindBest(number + alphabet)?.DebugWrite();
            //"1a".FindBest(numbers + alphabet)?.DebugWrite();

            static RecursionMatcher BuildOperation(ref Matcher operand, Matcher operators)
            {
                var exp = new RecursionMatcher();
                var rightOperand = operand;
                operand = rightOperand | exp;
                exp.Inner = operand + operators + rightOperand;
                return exp;
            }
            // 識別子
            Matcher Identifier = Matcher.ForbidLoop(number, alphabet | '_');

            Matcher IntLiteral = numbers;
            Matcher LeftOperand = IntLiteral;
            //Matcher RightOperand;
            //Matcher Operator;
            RecursionMatcher Exp;


            var MulDivExp = BuildOperation(ref LeftOperand, '*'._() | '/');
            var AddSubExp = BuildOperation(ref LeftOperand, '+'._() | '-');

            //" 999 $ ".FindBest(Identifier)?.DebugWrite();
            //" $ _abc123 ".FindBest(Identifier)?.DebugWrite();

            //Exp = MulDivExp;
            //Operator = '*'._() | '/';
            //RightOperand = IntLiteral;
            //LeftOperand = RightOperand | Exp;
            //Exp.Inner = LeftOperand + Operator + RightOperand;

            //Exp = AddSubExp;
            //Operator = '+'._() | '-';
            //RightOperand = LeftOperand;
            //LeftOperand |= RightOperand | Exp;
            //Exp.Inner = LeftOperand + Operator + RightOperand;

            //"12+34".FindBest(numbers)?.DebugWrite();
            //"12+34".FindBest(numbers + '+' + numbers)?.DebugWrite();
            //"12+34".FindBest(AddSubExp)?.DebugWrite();
            //"12+34+56".FindBest(AddSubExp)?.DebugWrite();
            //"12 + 34 *   56 + 78  ".FindBest(AddSubExp)?.DebugWrite();



            // 左結合二項演算の定義法
            // expAAA.Inner = (リテラル | expAAA) + 演算子 + (リテラル)
            // expBBB.Inner = (リテラル | expAAA | expBBB) + 演算子 + (リテラル | expAAA)
            // expCCC.Inner = (リテラル | expAAA | expBBB | expCCC) + 演算子 + (リテラル | expAAA | expBBB)
            // expDDD.Inner = (リテラル | expAAA | expBBB | expCCC | expDDD) + 演算子 + (リテラル | expAAA | expBBB | expCCC)
            // 
            // expNNN.Inner = (自分より優先度の高い式の左辺 | expNNN) + 演算子 + (自分より優先度の高い式の右辺)
            //

            var anyChar = new CodeRangeMatcher(CodeRangeMatcher.CharCodeMin, CodeRangeMatcher.CharCodeMax);

            var stringprefix = "r"._() | "u" | "R" | "U" | "f" | "F"
                                 | "fr" | "Fr" | "fR" | "FR" | "rf" | "rF" | "Rf" | "RF";
            //  <any source character except "\" or newline or the quote>
            var shortstringchar_singleQuote = ('\\'._() | '\r' | '\n' | '\'').Deny();
            var shortstringchar_doubleQuote = ('\\'._() | '\r' | '\n' | '\"').Deny();
            var stringescapeseq = '\\' + anyChar; //<any source character>
            var shortstringitem_singleQuote = shortstringchar_singleQuote | stringescapeseq;
            var shortstringitem_doubleQuote = shortstringchar_doubleQuote | stringescapeseq;
            var shortstring = ('\'' + shortstringitem_singleQuote.Loop() +'\'') | ('"' + shortstringitem_doubleQuote.Loop() +'"');


            var longstringchar = '\\'.Deny();// <any source character except "\">;
            var longstringitem = longstringchar | stringescapeseq;
            var longstring = ("'''" + longstringitem.Loop() +"'''") | ("\"\"\"" + longstringitem.Loop() + "\"\"\"");
            var stringliteral = (stringprefix | "") + (shortstring | longstring);

            //"\"\\\"\"".FindBest(stringliteral)?.DebugWrite();


            var digit = number;
            var digitpart = Matcher.ForbidLoop( '_', digit);
            var exponent = ("e"._() | "E") + ("+"._() | "-" | "") + digitpart;
            var fraction = "." + digitpart;
            var pointfloat = (digitpart | "") + fraction | digitpart + ".";
            var exponentfloat = (digitpart | pointfloat) + exponent;
            var floatnumber = pointfloat | exponentfloat;



            //"3.14".FindBest(floatnumber)?.DebugWrite();
            //GC.Collect();
            //"10.".FindBest(floatnumber)?.DebugWrite();
            //GC.Collect();
            //".001".FindBest(floatnumber)?.DebugWrite();
            //GC.Collect();
            //"1e100".FindBest(floatnumber)?.DebugWrite();
            //GC.Collect();
            //"3.14e-10".FindBest(floatnumber)?.DebugWrite();
            //GC.Collect();
            //"0e0".FindBest(floatnumber)?.DebugWrite();
            //GC.Collect();
            //"3.14_15_93".FindBest(floatnumber)?.DebugWrite();
            //GC.Collect();

            //"𠮷".FindBset('_'.Deny())?.DebugWrite();

            // 数値の列、但し途中にアンダースコアを許す
            var digits = Matcher.ForbidLoop('_', '0'.To('9'));
            // 単純な実数にマッチするパターン。各部にタグ付けしておく。
            var testFloat = (digits["int"] + '.' + digits["real"])["float"];
            // マッチング実行
            var allMatch = "3.14_15_93".FindBest(testFloat);

            // タグで整数部分を取り出す
            var intMatch = allMatch["float"]["int"];
            // タグで実数部分を取り出す
            var realMatch = allMatch["float"]["real"];
            // 整数部、実数部をデバッグ出力する
            Debug.WriteLine($"int={intMatch.Value} real={realMatch.Value}");

            //"abc".FindBest('a'._())?.DebugWrite();
            //"abc".FindBest('a'.Deny())?.DebugWrite();
            //"abc".FindBest('a'._())?.DebugWrite();
            //"abc".FindBest('b'._())?.DebugWrite();
            //"abc".FindBest('c'._())?.DebugWrite();

            //"abc".FindBest('a'.To('z'))?.DebugWrite();

            //"aaa".FindBest('a'._().Repet())?.DebugWrite();
            //"na".FindBest('a'._().Repet())?.DebugWrite();
            //"aan".FindBest('a'._().Repet())?.DebugWrite();

            //"abc".FindBest('a'.To('z').Repet())?.DebugWrite();

            //"abc".FindBest("bc"._())?.DebugWrite();
            //"abc".FindBest("abc"._())?.DebugWrite();
            //"_abc".FindBest("abc"._())?.DebugWrite();
            //"_abc_".FindBest("abc"._())?.DebugWrite();
            //"_a b c_".FindBest("abc"._())?.DebugWrite();
            //"_a b c_".FindBest('a'.To('z').Repet())?.DebugWrite();

            //"_a".FindBest('_'.Deny() | 'b'.Deny() | '&'.Deny())?.DebugWrite();

            //"abc".FindBest(('a'.To('z') | 'A'.To('Z')))?.DebugWrite();
            // "a".FindBest('a'.To('z').Repet())?.DebugWrite();
            //"a".FindBest(('a'.To('z') | '0'.To('9')).Repet())?.DebugWrite();
            //"abc".FindBest(('a'.To('z') | 'A'.To('Z')).Repet())?.DebugWrite();
            //"123456".FindBest(('a'.To('z') | 'A'.To('Z')).Deny().Repet())?.DebugWrite();
            //"abc".FindBest('_'.Deny())?.DebugWrite();
            //"abc".FindBest('_'.Deny().Repet())?.DebugWrite();

            //"_abc".FindBest('_'.Deny().Repet())?.DebugWrite();

        }


        static void foo()
        {
            // https://qiita.com/ryu_shizumi/items/c4aeffe2afc4416fcb69



            //
            // 優先度１
            //

            // 整数値
            var Number = '0'.To('9');
            var Numbers = Number.IsolatedLoop();

            //アルファベット
            var Alphabet = ('A'.To('Z') | 'a'.To('z'));
            // 識別子
            var Identifier = Matcher.ForbidLoop(Alphabet | '_', Number);

            // 「リテラル値式」のマッチャーを作る。(但し中身は空っぽ)
            var LiteralExp = new RecursionMatcher();

            // 「括弧式」のマッチャーを作る。(但し中身は空っぽ)
            var ParenExp = new RecursionMatcher();

            // 「代入可能式」のマッチャーを作る。(但し中身は空っぽ)
            var AssignableExp = new RecursionMatcher();

            // 「関数呼び出し式」のマッチャーを作る。(但し中身は空っぽ)
            var FunctionCallExp = new RecursionMatcher();

            // 「メンバアクセス式」のマッチャーを作る。(但し中身は空っぽ)
            var MemberAccessExp = new RecursionMatcher();

            // 「インデックスアクセス式」のマッチャーを作る。(但し中身は空っぽ)
            var IndexAccessExp = new RecursionMatcher();

            // 「後置デクリメント」のマッチャーを作る。(但し中身は空っぽ)
            var PostDecrementExp = AssignableExp + "--";

            // 「後置インクリメント」のマッチャーを作る。(但し中身は空っぽ)
            var PostIncrementExp = AssignableExp + "++";

            // 優先順位１式
            var Priority1Exp = LiteralExp | Identifier | ParenExp | AssignableExp |
                FunctionCallExp | IndexAccessExp | MemberAccessExp | PostDecrementExp | PostIncrementExp;


            //
            // 優先度２
            //

            // 「前置デクリメント」のマッチャーを作る。(但し中身は空っぽ)
            var PreDecrementExp = "--" + AssignableExp;

            // 「前置インクリメント」のマッチャーを作る。(但し中身は空っぽ)
            var PreIncrementExp = "++" + AssignableExp;

            // 「前置マイナス」のマッチャーを作る。(但し中身は空っぽ)
            var PreMinusExp = new RecursionMatcher();

            // 「前置プラス」のマッチャーを作る。(但し中身は空っぽ)
            var PrePlusExp = new RecursionMatcher();

            // 優先順位２式
            var Priority2Exp = PreDecrementExp | PreIncrementExp | PreMinusExp | PrePlusExp;

            // 優先順位２以上式
            var PriorityAbove2Exp = Priority1Exp | Priority2Exp;


            //
            // 優先度３
            //

            // 「乗除算式」のマッチャーを作る。(但し中身は空っぽ)
            var MulDivExp = new RecursionMatcher();

            // 優先順位３式
            var Priority3Exp = MulDivExp;

            // 優先順位３以上式
            var PriorityAbove3Exp = PriorityAbove2Exp | Priority3Exp;


            //
            // 優先度４
            //

            // 「加減算式」のマッチャーを作る。(但し中身は空っぽ)
            var AddSubExp = new RecursionMatcher();

            // 優先順位４式
            var Priority4Exp = AddSubExp;

            // 優先順位４式
            var PriorityAbove4Exp = PriorityAbove3Exp | Priority4Exp;

            //
            // (優先順位の低い演算子を増やしたい場合はここに挿入する。)
            //

            // 式の全て
            var Exp = PriorityAbove4Exp;

            //
            // 優先度９９９
            //

            // 代入演算文
            var AssignStatement = AssignableExp + '=' + Exp;


            //
            // 以下、中身が未設定なマッチャーの中身を設定
            //

            // 括弧式の中身
            ParenExp.Inner = '(' +
                (
                // 代入可能式を除く優先順位１式
                LiteralExp | Identifier | ParenExp | FunctionCallExp |
                IndexAccessExp | MemberAccessExp | PostDecrementExp | PostIncrementExp |

                // 優先順位２以下の全ての式
                Priority2Exp | Priority3Exp | Priority4Exp
                )
                 + ')';

            // 代入可能式の中身
            AssignableExp.Inner = Identifier | MemberAccessExp | IndexAccessExp | ('(' + AssignableExp + ')');

            // 関数呼び出し式の中身
            FunctionCallExp.Inner = (FunctionCallExp | IndexAccessExp | AssignableExp)
                + Matcher.EnclosedExpressions(Exp);

            // インデックスアクセス式の中身
            IndexAccessExp.Inner = (FunctionCallExp | IndexAccessExp | AssignableExp)
                + Matcher.EnclosedExpressions(Exp, '[', ']');

            // メンバアクセス式の中身
            MemberAccessExp.Inner = Priority1Exp + '.' + Identifier;

            // 前置マイナス式の中身
            PreMinusExp.Inner = '-' + (Priority1Exp | PreDecrementExp | PreIncrementExp | PrePlusExp);

            // 前置プラス式の中身
            PrePlusExp.Inner = '+' + (Priority1Exp | PreDecrementExp | PreIncrementExp | PreMinusExp);

            // 乗除算式の中身
            MulDivExp.Inner = PriorityAbove3Exp + ('*'._() | '/') + PriorityAbove2Exp;

            // 加減算式の中身
            AddSubExp.Inner = PriorityAbove4Exp + ('+'._() | '-') + PriorityAbove3Exp;
        }
    }
}
