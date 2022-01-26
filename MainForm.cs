using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AxKHOpenAPILib;

namespace 코스닥다운로더
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            API.OnEventConnect += onEventConnect;
            API.OnReceiveTrData += onReceiveTrData;
            PathManager.SetUp();
            Run();
        }

        private void onReceiveTrData(object sender, _DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            if (e.sRQName == "코스닥지수_일봉")
            {
                코스닥지수_이벤트(e);
            }
            else if(e.sRQName == "틱요청")
            {
                틱요청_이벤트(e);
            }
        }

        #region 로그인
        로그인콜백 로그인상태 = 로그인콜백.대기;
        enum 로그인콜백
        {
            대기 = -1,
            완료 = 0,
            사용자장보교환실패 = -100,
            서버접속실패 = -101,
            버전관리실패 = -102,
        }
        private bool 로그인()
        {
            int ret = API.CommConnect();
            if (ret != 0)
                return false;

            for (int i = 0; i < 60; i++)
            {
                Thread.Sleep(1000);
                switch (로그인상태)
                {
                    case 로그인콜백.대기:
                        break;
                    case 로그인콜백.완료:
                        return true;
                    case 로그인콜백.사용자장보교환실패:
                        return false;
                    case 로그인콜백.서버접속실패:
                        return false;
                    case 로그인콜백.버전관리실패:
                        return false;
                }
            }
            return false;
        }
        private void onEventConnect(object sender, _DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            switch (e.nErrCode)
            {
                case 0:
                    로그인상태 = 로그인콜백.완료;
                    break;
                default:
                    로그인상태 = (로그인콜백)e.nErrCode;
                    break;
            }
        }
        #endregion

        #region 코스닥 지수 관련
        private int 최근거래일 = 0;
        private bool Request_코스닥지수_일봉()
        {
            API.SetInputValue("업종코드", "101");
            API.SetInputValue("기준일자", "");
            int ret = API.CommRqData("코스닥지수_일봉", "opt20006", 0, "화면번호");
            if (ret != 0)
                return false;

            for (int i = 0; i < 60; i++)
            {
                Thread.Sleep(1000);
                if(최근거래일 != 0)
                {
                    return true;
                }
            }
            return false;
        }
        private void 코스닥지수_이벤트(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            최근거래일 = int.Parse(API.GetCommData(e.sTrCode, e.sRQName, 0, "일자").Trim());
        }
        #endregion

        private async void Run(string _date = null)
        {
            #region 로그인
            var t로그인 = Task<bool>.Run(() => 로그인());
            await t로그인;
            if (!t로그인.Result)
            {
                MessageBox.Show("로그인>> 실패");
                return;
            }
            else
            {
                Debug.WriteLine("로그인>> 성공");
            }
            #endregion


            #region Request_코스닥지수_일봉
            string date = _date;
            if (string.IsNullOrEmpty(_date))
            {
                var t코스닥지수 = Task<bool>.Run(() => Request_코스닥지수_일봉());
                await t코스닥지수;
                if (!t코스닥지수.Result)
                {
                    MessageBox.Show("Request_코스닥지수_일봉>> 실패");
                    return;
                }
                else
                {
                    date = 최근거래일.ToString();
                    Debug.WriteLine("Request_코스닥지수_일봉>> 성공");
                }
            }
            #endregion

            string[] codes = Get_전종목(date);

            label_date.Text = date;
            progressBar_all.Maximum = codes.Length;
            progressBar_all.Value = 0;
            progressBar_part.Maximum = 800;
            progressBar_part.Value = 0;

            var t다운로드 = Task<bool>.Run(() => Download_틱(date, codes));
            await t다운로드;

            bool 다운로드완료 = t다운로드.Result;
            if (다운로드완료)
            {
                progressBar_all.Value = progressBar_all.Maximum;
                MessageBox.Show("다운로드 완료");
            }
            else
            {
                MessageBox.Show("연속요청제한 종료");
                //Application.Restart();
            }

            //거래없음 삭제
            int emptyCnt = empty_codes.Count;
            if (0 < emptyCnt)
            {
                int sb_size = codes.Length * 8 + 2;
                StringBuilder sb = new StringBuilder(sb_size);
                foreach (var code in codes)
                {
                    if (empty_codes.Contains(code))
                        continue;
                    else
                        sb.AppendLine(code);
                }
                sb.Length -= 2;

                string path = PathManager.GetPath_전종목파일(date);
                File.WriteAllText(path, sb.ToString());
                Debug.WriteLine("거래없는 종목 제거 : " + emptyCnt);
            }
        }

        private int 지정일 = int.MaxValue;
        StringBuilder sb_틱 = null;
        private bool complete = false;
        private string complete_code = null;
        private List<string> empty_codes = new List<string>(64);
        private bool Download_틱(string date, string[] codes)
        {
            sb_틱 = new StringBuilder(1024 * 1024 * 16);
            지정일 = int.Parse(date);
            int maxCnt = 800;
            int length = codes.Length;
            for (int i = 0; i < length; i++)
            {
                var code = codes[i];
                //Debug.WriteLine("1) code = " + code);
                var path = PathManager.GetPath_틱파일(date, code);
                if (!File.Exists(path))
                {
                    //초기화
                    sb_틱.Clear();
                    complete = false;
                    complete_code = null;
                    //Debug.WriteLine("2) complete_code = null") ;

                    //틱요청
                    Request_틱(code);
                    while (!complete)
                    {
                        Thread.Sleep(100);
                    }
                    if (complete_code != code)
                    {
                        MessageBox.Show("complete_code != code : " + complete_code + " != " + code);
                        return false;
                    }
                    if(0 < sb_틱.Length)
                    {
                        sb_틱.Length -= 2;
                        string data = sb_틱.ToString();
                        File.WriteAllText(path, data);
                    }
                    else
                    {
                        empty_codes.Add(code);
                    }

                    bool 연속조회제한 = (maxCnt <= requestCnt_틱);
                    Invoke(new MethodInvoker(delegate ()
                    {
                        progressBar_all.Value = i + 1;
                        progressBar_part.Value = (연속조회제한) ? 800 : requestCnt_틱;
                        label_time.Text = (연속조회제한) ? "00:00:00" :
                                          TimeSpan.FromSeconds(1.3 * (800 - requestCnt_틱)).ToString(@"mm\:ss");
                    }));
                    if (연속조회제한)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void 틱요청_이벤트(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            var code = e.sScrNo.Trim();

            var cnt = API.GetRepeatCnt(e.sTrCode, e.sRQName);
            int sPrevNext = int.Parse(e.sPrevNext);

            if (0 < cnt)
            {
                string lastTime = API.GetCommData(e.sTrCode, e.sRQName, (cnt - 1), "체결시간").Trim();
                int lastDate = int.Parse(lastTime.Substring(0, 8));
                if(지정일 < lastDate && sPrevNext != 0)
                {
                    Request_틱(code, sPrevNext);
                    return;
                }
            }

            for (int i = 0; i < cnt; i++)
            {
                string time = API.GetCommData(e.sTrCode, e.sRQName, i, "체결시간").Trim();
                int date = int.Parse(time.Substring(0, 8));

                if(date < 지정일)
                {
                    complete_code = code;
                    //Debug.WriteLine("3) complete_code = " + complete_code);
                    complete = true;
                    return;
                }
                else if(date == 지정일)
                {
                    string price = API.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim();
                    string vol = API.GetCommData(e.sTrCode, e.sRQName, i, "거래량").Trim();
                    sb_틱.Append(time.Substring(8));
                    sb_틱.Append('\t');
                    sb_틱.Append(price);
                    sb_틱.Append('\t');
                    sb_틱.Append(vol);
                    sb_틱.AppendLine();
                }
            }

            if (sPrevNext == 0)
            {
                complete_code = code;
                //Debug.WriteLine("3) complete_code = " + complete_code);
                complete = true;
                return;
            }
            else
            {
                Request_틱(code, sPrevNext);
            }
        }

        private int requestCnt_틱 = 0;
        private async void Request_틱(string code, int sPrevNext = 0)
        {
            if (sPrevNext == 0)
            {
                Thread.Sleep(1200);
            }
            else
            {
                await Task.Delay(1200);
            }

            ++requestCnt_틱;
         
            API.SetInputValue("종목코드", code);
            API.SetInputValue("틱범위", "1");
            API.SetInputValue("수정주가구분", "1");
            int ret = API.CommRqData("틱요청", "opt10079", sPrevNext, code);
            if (ret != 0)
                Debug.WriteLine("Request_틱 : " + ret);
        }

        #region 전종목
        private string[] Get_전종목(string date)
        {
            string dir = PathManager.GetPath_틱폴더(date);
            string path = PathManager.GetPath_전종목파일(date);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                return Save_전종목(date);
            }
            else
            {
                return File.ReadAllLines(path);
            }
        }
        public string[] Save_전종목(string date)
        {
            #region 종목 제거 문자열
            string KODEX = "KODEX";
            string KOSEF = "KOSEF";
            string TIGER = "TIGER";
            string KINDEX = "KINDEX";
            string KBSTAR = "KBSTAR";
            string TREX = "TREX";
            string ARIRANG = "ARIRANG";
            string KTOP = "KTOP";
            string SOL = "SOL";
            string HANARO = "HANARO";
            string TIMEFOLIO = "TIMEFOLIO";
            string 마이다스 = "마이다스";
            string 신한 = "신한";
            string 대신 = "대신";
            string 미래에셋 = "미래에셋";
            string 삼성 = "삼성";
            string QV = "QV";
            string TRUE = "TRUE";
            string KB = "KB";
            string 메리츠 = "메리츠";
            string 하나 = "하나";
            string HK = "HK";
            string 네비게이터 = "네비게이터";
            string FOCUS = "FOCUS";
            string 스팩 = "스팩";
            //string 증거금100 = "증거금100";
            //string 거래정지 = "거래정지";
            #endregion

            var kosdaq = API.GetCodeListByMarket("10").Trim();
            var kosdaqs = kosdaq.Split(';');
            int kosdaqNum = kosdaqs.Length;

            List<string> codes = new List<string>(1024 * 2);

            //ETF, ETN... 종목 제거
            for (int i = 0; i < kosdaqNum; i++)
            {
                var name = API.GetMasterCodeName(kosdaqs[i]);
                if (string.IsNullOrEmpty(name))
                    continue;
                if (name.Contains(스팩))
                    continue;
                string[] split = name.Split(' ');
                if (split.Length == 1)
                {
                    codes.Add(kosdaqs[i]);
                    continue;
                }
                string target = split[0];
                if (target.Equals(KODEX))
                {
                    continue;
                }
                else if (target.Equals(KOSEF))
                {
                    continue;
                }
                else if (target.Equals(TIGER))
                {
                    continue;
                }
                else if (target.Equals(KINDEX))
                {
                    continue;
                }
                else if (target.Equals(KBSTAR))
                {
                    continue;
                }
                else if (target.Equals(TREX))
                {
                    continue;
                }
                else if (target.Equals(ARIRANG))
                {
                    continue;
                }
                else if (target.Equals(KTOP))
                {
                    continue;
                }
                else if (target.Equals(SOL))
                {
                    continue;
                }
                else if (target.Equals(HANARO))
                {
                    continue;
                }
                else if (target.Equals(TIMEFOLIO))
                {
                    continue;
                }
                else if (target.Equals(마이다스))
                {
                    continue;
                }
                else if (target.Equals(신한))
                {
                    continue;
                }
                else if (target.Equals(대신))
                {
                    continue;
                }
                else if (target.Equals(미래에셋))
                {
                    continue;
                }
                else if (target.Equals(삼성))
                {
                    continue;
                }
                else if (target.Equals(신한))
                {
                    continue;
                }
                else if (target.Equals(QV))
                {
                    continue;
                }
                else if (target.Equals(TRUE))
                {
                    continue;
                }
                else if (target.Equals(KB))
                {
                    continue;
                }
                else if (target.Equals(메리츠))
                {
                    continue;
                }
                else if (target.Equals(하나))
                {
                    continue;
                }
                else if (target.Equals(HK))
                {
                    continue;
                }
                else if (target.Equals(네비게이터))
                {
                    continue;
                }
                else if (target.Equals(FOCUS))
                {
                    continue;
                }

                codes.Add(kosdaqs[i]);
            }

            int sb_size = codes.Count * 8 + 2;
            StringBuilder sb = new StringBuilder(sb_size);
            for (int i = 0; i < codes.Count; i++)
            {
                sb.AppendLine(codes[i]);
            }

            sb.Length -= 2;

            string path = PathManager.GetPath_전종목파일(date);
            File.WriteAllText(path, sb.ToString());
            return codes.ToArray();
        }
        #endregion

    }
}
