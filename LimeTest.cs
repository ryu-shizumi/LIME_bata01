using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using static LIME.MatcherHelper;
using PipeIO;

namespace LIME
{
    public static class LimeTest
    {
        public static void test2()
        {
            var number = '0'.To('9');
            var numbers = number.Loop();

            // 左結合二項演算の定義法
            //
            // expAAA.Inner = (リテラル | expAAA) + 演算子 + (リテラル)

            //var MulExp_tiny = new RecursionMatcher();
            //MulExp_tiny.Inner = (number | MulExp_tiny) + '*' + number;

            var MulDivExp = new RecursionMatcher();
            MulDivExp.Inner = (numbers | MulDivExp) + ('*'._() | '/') + numbers;

            var AddSubExp = new RecursionMatcher();
            AddSubExp.Inner = (numbers | MulDivExp|  AddSubExp) + ('+'._() | '-') + (numbers | MulDivExp);


            //"1".FindBest(number).DebugWrite("Test000_001");
            //"12".FindBest(number).DebugWrite("Test000_002");
            //"123".FindBest(number).DebugWrite("Test000_003");

            //"1*2".FindBest(MulExp_tiny).DebugWrite("Test001_001");
            //"1*2*3".FindBest(MulExp_tiny).DebugWrite("Test001_002");

            "1*2".FindBest(MulDivExp).DebugWrite("Test002_001");
            "1*2*3".FindBest(MulDivExp).DebugWrite("Test002_002");
            "12*34".FindBest(MulDivExp).DebugWrite("Test002_003");
            "12*34*56".FindBest(MulDivExp).DebugWrite("Test002_004");

            "12+34*56+78".FindBest(AddSubExp).DebugWrite("Test003_001");


            //var client = new TextViewClient();
            //client.Clear();
            //client.WriteLine("aaabb");
            //client.WriteLine("ccccc");

            Matcher.IsOutputTree = true;


            var c = 'a'._() | 'b';


            // 数値の列、但し途中にアンダースコアを許す
            //var digits = Matcher.ForbidLoop('_', '0'.To('9'));

            var digits = Matcher.LedLoop('0'.To('9'), '_' | '0'.To('9'));

            //"[01234567]".FindBest(digits).DebugWrite();
            //"[0_1234567]".FindBest(digits).DebugWrite();
            //"[_0]".FindBest(digits).DebugWrite();
            //"[*0]".FindBest(digits).DebugWrite();

            //var TestExp = (('[' + digits) + (',' + digits).Loop()) + ']';
            //var TestExp = ('[' + digits) + (',' + digits).Loop() + ']';
            //"[0,1]".FindBest(TestExp).DebugWrite();

            //var TestExp00 = digits;
            //"[01]".FindBest(TestExp00).DebugWrite("00");
            //var TestExp01 = digits + ']';
            //"[0]".FindBest(TestExp01).DebugWrite("01");
            //var TestExp02 = '[' + digits;
            //"[0]".FindBest(TestExp02).DebugWrite("02");
            //var TestExp03 = '[' + digits;
            //"[0".FindBest(TestExp03).DebugWrite("03");
            //var TestExp04 = '[' + digits;
            //"[01".FindBest(TestExp04).DebugWrite("04");
            //var TestExp05 = digits + ']';
            //"[01".FindBest(TestExp05).DebugWrite("05");

            //var forbidChar = '_'._();
            //var number = '0'.To('9');
            //var loopChar = number;

            //var headChar = loopChar;
            //var bodyChar = (loopChar | forbidChar);
            //var prevHead = bodyChar.Deny();

            //var begin = Matcher.Begin;

            //var prevBorder = new BorderMatcher(prevHead , loopChar);

            //var headPart = prevBorder + headChar;

            //var endBorder = new BorderMatcher(bodyChar, bodyChar.Deny());

            var testDigits = digits;// new LoopContainMatcher(headPart, bodyChar, endBorder);
            var testDigits01 = testDigits + ']';
            var testDigits02 = '[' + testDigits;
            var testDigits03 = '[' + testDigits + ']';

            //var testPattern = prevBorder | loopChar;

            //"[0".FindBest(prevHead).DebugWrite("00n");
            //"[0".FindBest(prevBorder).DebugWrite("00a");


            //"[0".FindBest('[' + loopChar).DebugWrite("006");
            //"[0]".FindBest('[' + loopChar).DebugWrite("005");

            //"[0]".FindBest(testDigits).DebugWrite("001");
            //"[0]".FindBest(testDigits01).DebugWrite("002");
            //"[0]".FindBest(testDigits02).DebugWrite("003");
            //"[0]".FindBest(testDigits03).DebugWrite("004");
            //"[01]".FindBest(testDigits).DebugWrite("001+");
            //"[01]".FindBest(testDigits01).DebugWrite("002+");
            //"[01]".FindBest(testDigits02).DebugWrite("003+");
            //"[01]".FindBest(testDigits03).DebugWrite("004+");
        }

        public static void test()
        {
            var number = '0'.To('9');
            var numbers = number.Loop();


            var Lu = 'A'.To('Z');
            var Ll = 'a'.To('z');
            var alphabet = Lu | Ll;
            var alphabets = alphabet.Loop();

            var anyChar = new AffirmCharMatcher(AffirmCharMatcher.CharCodeMin, AffirmCharMatcher.CharCodeMax);

            var stringprefix = "r"._() | "u" | "R" | "U" | "f" | "F"
                                 | "fr" | "Fr" | "fR" | "FR" | "rf" | "rF" | "Rf" | "RF";
            //  <any source character except "\" or newline or the quote>
            var shortstringchar_singleQuote = ('\\'._() | '\r' | '\n' | '\'').Deny();
            var shortstringchar_doubleQuote = ('\\'._() | '\r' | '\n' | '\"').Deny();
            var stringescapeseq = '\\' + anyChar; //<any source character>
            var shortstringitem_singleQuote = shortstringchar_singleQuote | stringescapeseq;
            var shortstringitem_doubleQuote = shortstringchar_doubleQuote | stringescapeseq;
            var shortstring = ('\'' + shortstringitem_singleQuote.Loop() + '\'') | ('"' + shortstringitem_doubleQuote.Loop() + '"');


            var longstringchar = '\\'.Deny();// <any source character except "\">;
            var longstringitem = longstringchar | stringescapeseq;
            var longstring = ("'''" + longstringitem.Loop() + "'''") | ("\"\"\"" + longstringitem.Loop() + "\"\"\"");
            var stringliteral = (stringprefix | "") + (shortstring | longstring);



            var digit = number;
            var digitpart = Matcher.LedLoop(digit, digit | '_');
            var exponent = ("e"._() | "E") + ("+"._() | "-" | "") + digitpart;
            var fraction = "." + digitpart;
            var pointfloat = (digitpart | "") + fraction | digitpart + ".";
            var exponentfloat = (digitpart | pointfloat) + exponent;
            var floatnumber = pointfloat | exponentfloat;



            //"3.14".FindBest(floatnumber).DebugWrite("浮動小数点数_001");
            ////GC.Collect();
            //"10.".FindBest(floatnumber).DebugWrite("浮動小数点数_002");
            ////GC.Collect();
            //".001".FindBest(floatnumber).DebugWrite("浮動小数点数_003");
            ////GC.Collect();
            //"1e100".FindBest(floatnumber).DebugWrite("浮動小数点数_004");
            ////GC.Collect();
            //"3.14e-10".FindBest(floatnumber).DebugWrite("浮動小数点数_005");
            ////GC.Collect();
            //"0e0".FindBest(floatnumber).DebugWrite("浮動小数点数_006");
            ////GC.Collect();
            //"3.14_15_93".FindBest(floatnumber).DebugWrite("浮動小数点数_007");
            ////GC.Collect();

            //"𠮷".FindBset('_'.Deny())?.DebugWrite();

            // 数値の列、但し途中にアンダースコアを許す
            var digits = Matcher.LedLoop(digit, digit | '_');
            //// 単純な実数にマッチするパターン。各部にタグ付けしておく。
            //var testFloat = (digits["int"] + '.' + digits["real"])["float"];
            //// マッチング実行
            //var allMatch = "3.14_15_93".FindBest(testFloat);

            //// タグで整数部分を取り出す
            //var intMatch = allMatch["float"]["int"];
            //// タグで実数部分を取り出す
            //var realMatch = allMatch["float"]["real"];
            //// 整数部、実数部をデバッグ出力する
            //Debug.WriteLine($"int={intMatch.Value} real={realMatch.Value}");



            // 識別子
            Matcher Identifier = Matcher.LedLoop(alphabet | '_', alphabet | '_' | number);

            Matcher IntLiteral = numbers;
            Matcher literalExp = stringliteral | IntLiteral | floatnumber;
            //operand = Identifier | literalExp;

            var Exp = new RecursionMatcher();


            var ParenExp = new RecursionMatcher();       // 括弧式
            var AssignableExp = new RecursionMatcher();  // 代入可能式
            var FunctionCallExp = new RecursionMatcher();// 関数呼び出し式
            var MemberAccessExp = new RecursionMatcher();// メンバアクセス式
            var IndexAccessExp = new RecursionMatcher(); // インデックスアクセス式
            var PostDecrementExp = AssignableExp + "--"; // 後置デクリメント
            var PostIncrementExp = AssignableExp + "++"; // 後置インクリメント

            // 優先順位１式
            var Priority1Exp =
                literalExp | Identifier | ParenExp | AssignableExp |
                FunctionCallExp | MemberAccessExp | IndexAccessExp |
                PostDecrementExp | PostIncrementExp;


            var PreDecrementExp = "--" + AssignableExp;  // 前置デクリメント
            var PreIncrementExp = "++" + AssignableExp;  // 前置インクリメント
            var PreMinusExp = new RecursionMatcher();    // 前置マイナス
            var PrePlusExp = new RecursionMatcher();     // 前置プラス

            // 優先順位２式
            var Priority2Exp =
                PreDecrementExp | PreIncrementExp | PreMinusExp | PrePlusExp;

            // 優先順位２以上式
            var PriorityAbove2Exp = Priority1Exp | Priority2Exp;

            Matcher Operand = PriorityAbove2Exp;

            var MulDivExp = LeftOperation('*'._() | '/', ref Operand);
            var AddSubExp = LeftOperation('+'._() | '-', ref Operand);
            var ShiftExp = LeftOperation("<<"._() | ">>", ref Operand);

            // 括弧式の中身
            ParenExp.Inner = '(' +
                (
                // 代入可能式を除く優先順位１式
                literalExp | Identifier | ParenExp | FunctionCallExp |
                MemberAccessExp | IndexAccessExp | PostDecrementExp | PostIncrementExp |

                // 優先順位２以下の全ての式
                Priority2Exp | MulDivExp | AddSubExp | ShiftExp
                )
                 + ')';

            // 代入可能式の中身
            AssignableExp.Inner = Identifier | MemberAccessExp | IndexAccessExp | ('(' + AssignableExp + ')');

            var ParenArgs = (('(' + Exp) + (',' + Exp).Loop() + ')') | "()";

            // 関数呼び出し式の中身
            FunctionCallExp.Inner =
                (Identifier | FunctionCallExp | IndexAccessExp | AssignableExp) + ParenArgs;

            var BracketArgs = (('[' + Exp) + (',' + Exp).Loop() + ']') | "[]";

            // インデックスアクセス式の中身
            IndexAccessExp.Inner =
                (Identifier | FunctionCallExp | IndexAccessExp | AssignableExp) + BracketArgs;

            // メンバアクセス式の中身
            MemberAccessExp.Inner = Priority1Exp + '.' + Identifier;

            // 前置マイナス式の中身
            PreMinusExp.Inner = '-' + (Priority1Exp | PreDecrementExp | PreIncrementExp | PrePlusExp);

            // 前置プラス式の中身
            PrePlusExp.Inner = '+' + (Priority1Exp | PreDecrementExp | PreIncrementExp | PreMinusExp);

            // 代入文
            Matcher AssignStatement = AssignableExp + '=' + Exp;



            var ExExp = (('[') + (',' + Exp).Loop() + ']');

            var TestExp = (('[' + digits) + (',' + digits).Loop()) + ']';
            //var TestExp = ('[' + digits) + (',' + digits).Loop() + ']';

            Exp.Inner = Priority1Exp | Priority2Exp | MulDivExp | AddSubExp | ShiftExp;


            //"\"\\\"\"".FindBest(stringliteral).DebugWrite();

            //"12*34".FindBest(MulDivExp).DebugWrite("Test001");
            //"12*34*56".FindBest(MulDivExp).DebugWrite("Test002");

            //"12+34".FindBest(numbers).DebugWrite("Test010");
            //"12+34".FindBest(numbers + '+' + numbers).DebugWrite("Test011");
            //"12+34".FindBest(AddSubExp).DebugWrite("Test012");
            //"12+34+56".FindBest(AddSubExp).DebugWrite("Test013");
            //"12+34+56+78".FindBest(AddSubExp).DebugWrite("Test013_2");
            //"12 + 34 *   (3.14_15_93 + 78)  >> \"nnn\" ".FindBest(Exp).DebugWrite("Test014");
            //"[,123,456]".FindBest(ExExp).DebugWrite("Test015");
            //Matcher.IsOutputTree = true;
            //"[0,1,2,3,4,5]".FindBest(TestExp).DebugWrite("Test016");
            //"nnnn()".FindBest(Exp).DebugWrite("Test017");
            //"12 * 34".FindBest(Exp).DebugWrite("Test018");
            //"12 + 34".FindBest(Exp).DebugWrite("Test019");
            "12 << 34".FindBest(Exp).DebugWrite("Test020");
            //"12 << 34 + 56".FindBest(Exp).DebugWrite("Test021");
        }


        static void foo()
        {
            // https://qiita.com/ryu_shizumi/items/c4aeffe2afc4416fcb69

            var number = '0'.To('9');
            var numbers = number.Loop();

            var Lu = 'A'.To('Z');
            var Ll = 'a'.To('z');
            var alphabet = Lu | Ll;

            //
            // 優先度１
            //

            // 整数値
            var Number = '0'.To('9');
            var Numbers = Number.Loop();

            //アルファベット
            var Alphabet = ('A'.To('Z') | 'a'.To('z'));
            // 識別子
            var Identifier = Matcher.LedLoop(alphabet | '_', alphabet | '_' | number);

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
