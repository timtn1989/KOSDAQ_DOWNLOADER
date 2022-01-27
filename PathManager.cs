using System;

namespace 코스닥다운로더
{
    public class PathManager
    {
        public static string 기본경로;

        public static string 틱폴더경로;
        public static string 틱파일경로형식;
        public static string 일파일경로형식;

        public static void SetUp(string _기본경로 = null)
        {
            if (string.IsNullOrEmpty(_기본경로))
                기본경로 = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\전략분석기";
            else
                기본경로 = _기본경로;

            틱폴더경로 = 기본경로 + @"\틱데이터";
            틱파일경로형식 = 틱폴더경로 + @"\{0}\{1}.txt";
            일파일경로형식 = 기본경로 + @"\일데이터\{0}.txt";
        }

        public static string GetPath_전종목파일(string date)
        {
            return string.Format(틱파일경로형식, date, "전종목");
        }
        public static string GetPath_틱파일(string date, string code)
        {
            return string.Format(틱파일경로형식, date, code);
        }
        public static string GetPath_틱폴더(string date)
        {
            return 틱폴더경로 + @"\" + date;
        }
        public static string GetPath_일파일(string code)
        {
            return string.Format(일파일경로형식, code);
        }
    }
}
