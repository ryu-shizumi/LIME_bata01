using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LIME
{
    

    public abstract class TextAtom : TextRange
    {
        public TextAtom(TextRange beginEnd)
            : base(beginEnd)
        { }
        public TextAtom(int begin, int end)
            : base(begin, end)
        { }

        public string TypeName
        {
            get
            {
                var result = this.GetType().Name;
                if (result.EndsWith("Atom"))
                {
                    result = result.Substring(0, result.Length - 4);
                }
                return result;
            }
        }
    }

    #region 素の要素
    public class PlaneAtom : TextAtom
    {
        public PlaneAtom(int begin)
            : base(begin, begin + 1)
        { }
    }
    #endregion

    #region 物理的(長さを持つ)要素
    public abstract class PhysicalAtom : TextAtom
    {
        public PhysicalAtom(TextRange beginEnd) : base(beginEnd) { }
        public PhysicalAtom(int begin, int end) : base(begin, end) { }

        public abstract IEnumerable<CharAtom> EnumChars();
    }
    #endregion

    #region 文字アトム
    public class CharAtom : PhysicalAtom
    {
        public int Code { get; private set; }

        public CharAtom(char highSurrogate, char lowSurrogate, int begin, int end)
            : base(begin, end)
        {
            Code = char.ConvertToUtf32(highSurrogate, lowSurrogate);
        }

        public CharAtom(char c, int begin) : base(begin, begin + 1)
        {
            Code = c;
        }

        public CharAtom(int code, int begin, int end)
            : base(begin, end)
        {
            Code = code;
        }
        public CharAtom(int code, TextRange beginEnd)
            : base(beginEnd)
        {
            Code = code;
        }

        public override IEnumerable<CharAtom> EnumChars()
        {
            var chars = char.ConvertFromUtf32(Code);
            var length = chars.Length;

            for(int i = 0; i < length; i++)
            {
                yield return new CharAtom(chars[i], i);
            }
        }
    }
    #endregion

    #region ブランクアトム
    /// <summary>
    /// 空白要素。１以上の長さを持つ。
    /// </summary>
    /// <remarks>
    /// 
    /// BlankAtom
    ///   NewLineCharAtom      改行文字
    ///     CrAtom             Cr
    ///     LfAtom             Lf
    ///     CrLfAtom           CrLf
    ///   SpacesAtom           空白
    ///     LineHeadSpacesAtom 行頭空白
    /// 
    /// </remarks>
    public abstract class BlankAtom : PhysicalAtom
    {
        public BlankAtom(int begin, int end) : base(begin, end) { }
    }

    public abstract class NewLineCharAtom : BlankAtom
    {
        public NewLineCharAtom(int begin, int end) : base(begin, end) { }

    }

    /// <summary>Cr文字アトム(長さ１)</summary>

    public class CrAtom : NewLineCharAtom
    {
        public CrAtom(int begin) : base(begin, begin + 1) { }

        public override IEnumerable<CharAtom> EnumChars()
        {
            yield return new CharAtom('\r', Begin);
        }
    }

    /// <summary>Lf文字アトム(長さ１)</summary>
    public class LfAtom : NewLineCharAtom
    {
        public LfAtom(int begin) : base(begin, begin + 1) { }
        public override IEnumerable<CharAtom> EnumChars()
        {
            yield return new CharAtom('\n', Begin);
        }
    }

    /// <summary>CrLf文字アトム(長さ２)</summary>

    public class CrLfAtom : NewLineCharAtom
    {
        public CrLfAtom(int begin) : base(begin, begin + 2) { }
        public override IEnumerable<CharAtom> EnumChars()
        {
            yield return new CharAtom('\r', Begin);
            yield return new CharAtom('\n', Begin);
        }
    }

    /// <summary>空白文字アトム(長さ１以上)</summary>
    public class SpacesAtom : BlankAtom
    {
        public SpacesAtom(int begin, int end) : base(begin, end) { }

        public override IEnumerable<CharAtom> EnumChars()
        {
            for (int begin = Begin; begin < End; begin++)
            {
                yield return new CharAtom(Matcher.CurrentText[begin], begin);
            }
        }
    }

    /// <summary>行頭空白文字アトム(長さ１以上)</summary>
    public class LineHeadSpacesAtom : SpacesAtom
    {
        public LineHeadSpacesAtom(int begin, int end) : base(begin, end) { }
    }
    #endregion

    #region デリミタアトム
    /// <summary>
    /// 区切り要素。長さを持たない。
    /// </summary>
    /// <remarks>
    /// DelimiterAtom
    ///   NewLineAtom        改行
    ///     FirstNewLineAtom ゼロ文字目の改行
    ///   IndentAtom         インデント
    ///   DedentAtom         デデント
    ///   TextBeginAtom      文字列開始
    ///   TextEndAtom        文字列終了
    /// 
    /// </remarks>
    public abstract class DelimiterAtom : TextAtom
    {
        public DelimiterAtom(int begin) : base(begin, begin) { }
    }

    /// <summary>行頭アトム(長さゼロ)</summary>
    public class LineheadAtom : DelimiterAtom
    {
        public LineheadAtom(int begin) : base(begin) { }
    }

    /// <summary>最初の行頭アトム(長さゼロ)</summary>
    public class FirstLineheadAtom : LineheadAtom
    {
        public FirstLineheadAtom() : base(0) { }
    }

    /// <summary>インデントアトム(長さゼロ)</summary>
    public class IndentAtom : DelimiterAtom
    {
        public IndentAtom(int begin) : base(begin) { }
    }

    /// <summary>デデントアトム(長さゼロ)</summary>
    public class DedentAtom : DelimiterAtom
    {
        public DedentAtom(int begin) : base(begin) { }
    }

    /// <summary>一貫性の無いデデントアトム(長さゼロ)</summary>
    public class InconsistentDedentAtom : DelimiterAtom
    {
        public InconsistentDedentAtom(int begin) : base(begin) { }
    }

    /// <summary>文字列開始アトム(長さゼロ)</summary>
    public class BeginAtom : DelimiterAtom
    {
        public BeginAtom() : base(0) { }
    }

    /// <summary>文字列終了アトム(長さゼロ)</summary>
    public class EndAtom : DelimiterAtom
    {
        public EndAtom(int begin) : base(begin) { }
    }
    #endregion


}
