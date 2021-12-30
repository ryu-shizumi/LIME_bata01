using System;
using System.Collections.Generic;
using System.Text;


namespace LIME
{
    class PythonPerser
    {
        public static void foo()
        {
            ////  識別子およびキーワード
            //var Lu = 'A'.To('Z');
            //var Ll = 'a'.To('z');
            //var alphabet = Lu | Ll;
            //var number = '0'.To('9');

            //var id_start = Lu | Ll | '_';
            //var id_continue = Lu | Ll | number | '_';
            ////var xid_start =  <all characters in id_start whose NFKC normalization is in "id_start xid_continue*">
            ////var xid_continue =  <all characters in id_continue whose NFKC normalization is in "id_continue*">
            ////var identifier =  xid_start xid_continue*
            ////var identifier = new LoopBuilder(

            //// リテラル
            //var cr = '\r'._();
            //var lf = '\n'._();
            //var newline = cr | lf;
            //var backslash = '\\'._();
            //var escaped_char = backslash + newline.Deny();
            //var backslash_not = backslash.Deny();

            //var double_quat = '"'._();

            //// エスケープされてないダブルクオート
            //var plane_double_quat = new BorderBuilder(backslash_not, double_quat) + double_quat;
            //var double_quat_string_char = escaped_char | (newline | '\\' | '\'').Not();
            //// ダブルクオート文字列
            //var double_quat_string = 
            //    new CharLoopBuilder(
            //        plane_double_quat,
            //        double_quat_string_char,
            //        plane_double_quat
            //        );

            //var long_string_char = escaped_char | '\\'._().Not();
            //var triple_double_quat = "\"\"\""._();
            //var double_quat_long_string =
            //    new CharLoopBuilder(
            //        triple_double_quat,
            //        long_string_char,
            //        triple_double_quat
            //        );

            //var single_quat = '\''._();

            //// エスケープされてないシングルクオート
            //var plane_single_quat = new BorderBuilder(backslash_not, single_quat) + single_quat;
            //var single_quat_string_char = escaped_char | (newline | '\\' | '\"').Not();
            //// シングルクオート文字列
            //var single_quat_string =
            //    new CharLoopBuilder(
            //        plane_single_quat,
            //        single_quat_string_char,
            //        plane_single_quat
            //        );

            //var triple_shinle_quat = "'''"._();
            //var single_quat_long_string =
            //    new CharLoopBuilder(
            //        triple_shinle_quat,
            //        long_string_char,
            //        triple_shinle_quat
            //        );


            ////// プレフィックスの無い文字列
            ////var plane_string = double_quat_string | single_quat_string | double_quat_long_string | single_quat_long_string;

            ////var stringprefix =
            ////    "r"._() | "u" | "R" | "U" | "f" | "F" |
            ////    "fr" | "Fr" | "fR" | "FR" | "rf" | "rF" | "Rf" | "RF";
            ////var stringliteral = stringprefix._01() + plane_string;

            ////var bytesprefix = "b"._() | "B" | "br" | "Br" | "bR" | "BR" | "rb" | "rB" | "Rb" | "RB";
            ////var bytesliteral = bytesprefix + plane_string;

            ////var conditional_expression = new RecursionBuilder();
            ////var or_expr = new RecursionBuilder();
            ////var yield_expression = new RecursionBuilder();
            ////var format_spec = new RecursionBuilder();
            ////// フォーマット済み文字列リテラル
            ////var NULL = '\0'._();
            ////var literal_char = ('{'._() | '}' | NULL).Not();
            ////var conversion = "s"._() | "r" | "a";
            ////var f_expression = (conditional_expression | ("*"._() + or_expr)) + 
            ////             ("," + conditional_expression | "," + "*" + or_expr).Above1() + ","._()._01()
            ////           | yield_expression;
            ////var replacement_field = "{" + f_expression + "="._()._01() + ("!" + conversion)._01() + (":" + format_spec)._01() + "}";
            ////format_spec.Inner = (literal_char | NULL).Above1() | replacement_field.Above1();

            ////var f_string = (literal_char | "{{" | "}}" | replacement_field) *


        }
    }
}
