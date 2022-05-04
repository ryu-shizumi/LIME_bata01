using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Drawing;

namespace PipeIO
{
    #region テキスト受信イベント引数
    public class TextRecivedArgs : EventArgs
    {
        public string[] Texts { get; }
        public TextRecivedArgs(string[] optionTexts)
        {
            Texts = optionTexts;
        }
    }
    #endregion

    /// <summary>
    /// パイプで通信するサーバ側
    /// </summary>
    /// <remarks>
    /// インスタンスを作り、TextRecived イベントで受信文字列を取得する。
    /// </remarks>
    public class PipeServer : IDisposable
    {
        /// <summary>
        /// テキスト受信イベント
        /// </summary>
        public event EventHandler<TextRecivedArgs> TextRecived;

        public event Func<bool> OnStop;

        private static int numThreads = 1;

        private string PipeName { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="pipeName"></param>
        public PipeServer(string pipeName = "pipe1")
        {
            PipeName = pipeName;

            Thread thread1 = new Thread(ServerFunc);
            _isAlive = true;
            thread1.Start();
        }

        private bool _isAlive = false;

        ~PipeServer()
        {
            AboatConnection();
        }
        // numThreadsで指定した数まで同じ名前のパイプを作れる
        NamedPipeServerStream _pipeServer = null;

        private void ServerFunc()
        {
            while (_isAlive)
            {
                try
                {
                    _pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, numThreads);

                    // クライアントの接続待ち
                    _pipeServer.WaitForConnection();

                    StreamString ss = new StreamString(_pipeServer);

                    while (_isAlive)
                    {
                        // 受信待ち
                        var count = ss.ReadString();
                        if (count == "") { break; }
                        // 受信したら応答を送信
                        ss.WriteString("");

                        // 最初はオプションデータの数が送られてくる
                        var textsCount = int.Parse(count);

                        string[] texts = null;
                        if (textsCount > 0)
                        {
                            texts = new string[textsCount];

                            for (int i = 0; i < textsCount; i++)
                            {
                                texts[i] = ss.ReadString();
                                // 受信したら応答を送信
                                ss.WriteString("");
                            }
                        }
                        else
                        {
                            texts = new string[0];
                        }

                        // イベントハンドラが有れば実行する
                        TextRecived?.Invoke(this, new TextRecivedArgs(texts));


                        // 末尾の空文字列の受信待ち
                        var tail = ss.ReadString();

                        // 末尾で Wait が帰ってきた時
                        if (tail == "Wait")
                        {
                            // 待機イベントが設定されている時
                            if (OnStop != null)
                            {
                                // 待機状態が解消されるまで待つ。
                                while (OnStop())
                                {
                                    Thread.Sleep(10);
                                }
                            }
                        }
                        // 受信したら応答を送信
                        ss.WriteString("");


                        // if (read == "end") break;
                    }
                }
                catch (OverflowException ofex)
                {
                    // クライアントが切断
                    Console.WriteLine(ofex.Message);
                }
                finally
                {
                    _pipeServer?.Close();
                }
            }
            _pipeServer.Dispose();
            _pipeServer = null;
        }

        public void Dispose()
        {
            AboatConnection();
        }

        private void AboatConnection()
        {
            _isAlive = false;
            // パイプストリームが存在するけど接続を試みている時
            if ((_pipeServer != null) && (_pipeServer.IsConnected == false))
            {
                // 仮のクライアントを作ってすぐ閉じる
                var client = new PipeClient(PipeName);
                client.Dispose();
            }
        }
    }


    public class PipeClient : IDisposable
    {
        private NamedPipeClientStream _pipeClient = null;
        private StreamString ss = null;

        public PipeClient(string pipeName = "pipe1")
        {
            _pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);

            const int timeOutMillisec = 20;
            try
            {
                _pipeClient.Connect(timeOutMillisec);
                ss = new StreamString(_pipeClient);
            }
            catch (Exception e)
            {
                // タイムアウトで帰ってきた時など
                _pipeClient.Close();
                _pipeClient = null;
            }
            finally
            {
            }
        }

        public void SendTexts(params string[] texts)
        {
            var optionCount = texts.Length;
            // データの数を文字列として送信する
            _sendString($"{optionCount}");

            // 文字列を順番に送信する
            foreach (var optionText in texts)
            {
                // 文字列を送信する
                _sendString(optionText);
            }

            // 最後にもう一回空送信をする
            _sendString("");
        }
        private void _sendString(string text)
        {
            if (ss == null)
            {
                return;
            }
            // 文字列を送信する
            ss.WriteString(text);
            // 応答待ち
            string read = ss.ReadString();
        }
        public void SendWait()
        {
            // データの数を文字列として送信する
            _sendString($"{0}");

            // 末尾で "Wait" を送信をする
            _sendString("Wait");
        }
        public void Dispose()
        {
            _pipeClient?.Close();
        }
    }

    public class TextViewClient : IDisposable
    {
        public PipeClient Client;

        public TextViewClient(string pipeName = "pipe1")
        {
            Client = new PipeClient(pipeName);
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public void Clear()
        {
            Client.SendTexts("Clear");
        }

        public void Write(string text, string toolTip = "", Colors foreColor = Colors.Black, Colors backColor = Colors.White)
        {
            Color fc = foreColor.ToColor();
            Color bc = backColor.ToColor();
            Client.SendTexts("Write", text, toolTip, fc.ToArgb().ToString(), bc.ToArgb().ToString());
        }
        public void NewLine()
        {
            Client.SendTexts("NewLine");
        }
        public void WriteLine(string text)
        {
            Write(text, "");
            NewLine();
        }
        public void Wait()
        {
            Client.SendTexts("Paint");
            Client.SendWait();
        }

        public void SuspendLayout()
        {
            Client.SendTexts("SuspendLayout");
        }
        public void ResumeLayout()
        {
            Client.SendTexts("ResumeLayout");
        }
    }


    #region StreamString
    // MSサンプルそのまま(streamに文字列を読み書きしてくれるクラス)
    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            if (len < 0) { return ""; }
            len += ioStream.ReadByte();

            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            try
            {
                ioStream.WriteByte((byte)(len / 256));
                ioStream.WriteByte((byte)(len & 255));
                ioStream.Write(outBuffer, 0, len);
                ioStream.Flush();

                return outBuffer.Length + 2;
            }
            catch (IOException e)
            {
                return 0;
            }
        }
    }
    #endregion

    #region Color
    public enum Colors
    {
        AliceBlue = -984833,
        AntiqueWhite = -332841,
        Aqua = -16711681,
        Aquamarine = -8388652,
        Azure = -983041,
        Beige = -657956,
        Bisque = -6972,
        Black = -16777216,
        BlanchedAlmond = -5171,
        Blue = -16776961,
        BlueViolet = -7722014,
        Brown = -5952982,
        BurlyWood = -2180985,
        CadetBlue = -10510688,
        Chartreuse = -8388864,
        Chocolate = -2987746,
        Coral = -32944,
        CornflowerBlue = -10185235,
        Cornsilk = -1828,
        Crimson = -2354116,
        Cyan = -16711681,
        DarkBlue = -16777077,
        DarkCyan = -16741493,
        DarkGoldenrod = -4684277,
        DarkGray = -5658199,
        DarkGreen = -16751616,
        DarkKhaki = -4343957,
        DarkMagenta = -7667573,
        DarkOliveGreen = -11179217,
        DarkOrange = -29696,
        DarkOrchid = -6737204,
        DarkRed = -7667712,
        DarkSalmon = -1468806,
        DarkSeaGreen = -7357301,
        DarkSlateBlue = -12042869,
        DarkSlateGray = -13676721,
        DarkTurquoise = -16724271,
        DarkViolet = -7077677,
        DeepPink = -60269,
        DeepSkyBlue = -16728065,
        DimGray = -9868951,
        DodgerBlue = -14774017,
        Firebrick = -5103070,
        FloralWhite = -1296,
        ForestGreen = -14513374,
        Fuchsia = -65281,
        Gainsboro = -2302756,
        GhostWhite = -460545,
        Gold = -10496,
        Goldenrod = -2448096,
        Gray = -8355712,
        Green = -16744448,
        GreenYellow = -5374161,
        Honeydew = -983056,
        HotPink = -38476,
        IndianRed = -3318692,
        Indigo = -11861886,
        Ivory = -16,
        Khaki = -989556,
        Lavender = -1644806,
        LavenderBlush = -3851,
        LawnGreen = -8586240,
        LemonChiffon = -1331,
        LightBlue = -5383962,
        LightCoral = -1015680,
        LightCyan = -2031617,
        LightGoldenrodYellow = -329006,
        LightGray = -2894893,
        LightGreen = -7278960,
        LightPink = -18751,
        LightSalmon = -24454,
        LightSeaGreen = -14634326,
        LightSkyBlue = -7876870,
        LightSlateGray = -8943463,
        LightSteelBlue = -5192482,
        LightYellow = -32,
        Lime = -16711936,
        LimeGreen = -13447886,
        Linen = -331546,
        Magenta = -65281,
        Maroon = -8388608,
        MediumAquamarine = -10039894,
        MediumBlue = -16777011,
        MediumOrchid = -4565549,
        MediumPurple = -7114533,
        MediumSeaGreen = -12799119,
        MediumSlateBlue = -8689426,
        MediumSpringGreen = -16713062,
        MediumTurquoise = -12004916,
        MediumVioletRed = -3730043,
        MidnightBlue = -15132304,
        MintCream = -655366,
        MistyRose = -6943,
        Moccasin = -6987,
        NavajoWhite = -8531,
        Navy = -16777088,
        OldLace = -133658,
        Olive = -8355840,
        OliveDrab = -9728477,
        Orange = -23296,
        OrangeRed = -47872,
        Orchid = -2461482,
        PaleGoldenrod = -1120086,
        PaleGreen = -6751336,
        PaleTurquoise = -5247250,
        PaleVioletRed = -2396013,
        PapayaWhip = -4139,
        PeachPuff = -9543,
        Peru = -3308225,
        Pink = -16181,
        Plum = -2252579,
        PowderBlue = -5185306,
        Purple = -8388480,
        RebeccaPurple = -10079335,
        Red = -65536,
        RosyBrown = -4419697,
        RoyalBlue = -12490271,
        SaddleBrown = -7650029,
        Salmon = -360334,
        SandyBrown = -744352,
        SeaGreen = -13726889,
        SeaShell = -2578,
        Sienna = -6270419,
        Silver = -4144960,
        SkyBlue = -7876885,
        SlateBlue = -9807155,
        SlateGray = -9404272,
        Snow = -1286,
        SpringGreen = -16711809,
        SteelBlue = -12156236,
        Tan = -2968436,
        Teal = -16744320,
        Thistle = -2572328,
        Tomato = -40121,
        Turquoise = -12525360,
        Violet = -1146130,
        Wheat = -663885,
        White = -1,
        WhiteSmoke = -657931,
        Yellow = -256,
        YellowGreen = -6632142
    }

    public static class ExColors
    {
        public static Color ToColor(this Colors color)
        {
            return Color.FromArgb((int)color);
        }
    }
    #endregion
}
