using System;
using System.Diagnostics;
using LIME;

namespace LIME_bata01
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Debug.WriteLine($"現在時刻は {DateTime.Now} です。");

            LimeTest.test();

        }
    }
}
