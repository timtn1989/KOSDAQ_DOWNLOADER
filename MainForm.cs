using AxKHOpenAPILib;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 코스닥다운로더
{
    public partial class MainForm : Form
    {
        private int 연속요청제한량 = 500;
        private int 요청대기시간 = 900;

        #region 프로퍼티
        private string 틱다운완료일
        {
            get
            {
                return Properties.Settings.Default.틱다운완료일;
            }
            set
            {
                if(!string.IsNullOrEmpty(value))
                    Properties.Settings.Default.틱다운완료일 = value;
            }
        }
        private string 일다운완료일
        {
            get
            {
                return Properties.Settings.Default.일다운완료일;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Properties.Settings.Default.일다운완료일 = value;
            }
        }
        private string 최근다운로드일
        {
            get
            {
                return Properties.Settings.Default.최근다운로드일;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Properties.Settings.Default.최근다운로드일 = value;
            }
        }
        private int 틱다운인덱스
        {
            get
            {
                return Properties.Settings.Default.틱다운인덱스;
            }
            set
            {
                if (0 <= value)
                    Properties.Settings.Default.틱다운인덱스 = value;
            }
        }
        private int 일다운인덱스
        {
            get
            {
                return Properties.Settings.Default.일다운인덱스;
            }
            set
            {
                if (0 <= value)
                    Properties.Settings.Default.일다운인덱스 = value;
            }
        }
        #endregion

        public MainForm()
        {
            InitializeComponent();
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            API.OnEventConnect += onEventConnect;
            API.OnReceiveTrData += onReceiveTrData;
            PathManager.SetUp();

            Run();
        }

        private void onReceiveTrData(object sender, _DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            switch (e.sRQName)
            {
                case "틱요청":
                    틱요청_이벤트(e);
                    break;

                case "일요청":
                    일요청_이벤트(e);
                    break;

                case "코스닥지수_일봉":
                    코스닥지수_이벤트(e);
                    break;
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
        private string 최근거래일 = null;
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
                if(최근거래일 != null)
                {
                    return true;
                }
            }
            return false;
        }
        private void 코스닥지수_이벤트(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            최근거래일 = API.GetCommData(e.sTrCode, e.sRQName, 0, "일자").Trim();
        }
        #endregion

        private string 다운로드날짜 = null;
        private int n다운로드날짜 = int.MaxValue;
        private async void Run(string date = null) //_date => 당일 다운로드 아닐 경우 사용
        {
            if (!Check_다운로드가능시간())
            {
                label_target.Text = "다운로드 불가 시간";
            }

            #region 로그인
            var t로그인 = Task<bool>.Run(() => 로그인());
            await t로그인;
            if (!t로그인.Result)
            {
                MessageBox.Show("로그인>> 실패");
                return;
            }
            #endregion

            #region 최근거래일 구하기
            if (string.IsNullOrEmpty(date))
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
                    다운로드날짜 = 최근거래일;
                    n다운로드날짜 = int.Parse(다운로드날짜);
                }
            }
            else
            {
                다운로드날짜 = date;
                n다운로드날짜 = int.Parse(date);
            }
            #endregion

            //새로운 다운로드 날짜 => 초기화
            if (다운로드날짜 != 최근다운로드일)
            {
                최근다운로드일 = 다운로드날짜;
                틱다운인덱스 = 0;
                일다운인덱스 = 0;
            }

            //다운로드
            if (다운로드날짜 != 틱다운완료일)
            {
                //틱 다운로드 세팅
                string[] codes = Get_전종목(다운로드날짜);
                int codesLength = codes.Length;

                label_target.Text = "틱데이터 다운";
                label_date.Text = 다운로드날짜;
                progressBar_all.Maximum = codesLength;
                progressBar_all.Value = 틱다운인덱스;
                progressBar_part.Maximum = 연속요청제한량;
                progressBar_part.Value = 0;

                var t틱다운 = Task<int>.Run(() => Download_틱(다운로드날짜, codes));
                await t틱다운;

                틱다운인덱스 = t틱다운.Result;
                if (틱다운인덱스 == codesLength)
                {
                    틱다운완료일 = 다운로드날짜;
                }
                Properties.Settings.Default.Save();

                Application.Restart();
            }
            else if (다운로드날짜 != 일다운완료일)
            {
                string[] codes = Get_전종목(다운로드날짜);
                int codesLength = codes.Length;

                label_target.Text = "일데이터 다운";
                label_date.Text = 다운로드날짜;
                progressBar_all.Maximum = codesLength;
                progressBar_all.Value = 일다운인덱스;
                progressBar_part.Maximum = 연속요청제한량;
                progressBar_part.Value = 0;

                var t일다운 = Task<int>.Run(() => Download_일(다운로드날짜, codes));
                await t일다운;

                일다운인덱스 = t일다운.Result;
                bool 다운로드완료 = (일다운인덱스 == codesLength);
                if (다운로드완료)
                {
                    일다운완료일 = 다운로드날짜;
                }
                Properties.Settings.Default.Save();

                if (다운로드완료)
                {
                    label_target.Text = "다운로드 완료";
                }
                else
                {
                    Application.Restart();
                }

            }
            else
            {
                label_target.Text = "다운로드 완료";
            }
        }

        private bool Check_다운로드가능시간()
        {
            var now = DateTime.Now;
            //공휴일
            if(now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
            {
                return true;
            }
            //오후 4시이전
            if(now.TimeOfDay < new TimeSpan(16, 0, 0))
            {
                return false;
            }

            return true;
        }

        private void 거래없음삭제(string[] codes)
        {
            /*
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

                string path = PathManager.GetPath_전종목파일(다운로드날짜);
                File.WriteAllText(path, sb.ToString());
            }
            */
        }

        private StringBuilder stringBuilder = null;

        private ConcurrentQueue<string> completeCode_Queue = new ConcurrentQueue<string>();
        private int Download_일(string date, string[] codes)
        {
            Thread 요청스레드 = new Thread(new ThreadStart(Run_DayRequestThread));
            요청스레드.IsBackground = true;
            요청스레드.Start();

            stringBuilder = new StringBuilder(1024 * 32);
            int length = codes.Length;
            for (int i = 일다운인덱스; i < length; i++)
            {
                var code = codes[i];
                var path = PathManager.GetPath_일파일(code);

                stringBuilder.Clear();

                RequestQueue.Enqueue(new Request(code, 0));
                Thread.Sleep(요청대기시간);
                string completeCode = null;
                while (!completeCode_Queue.TryDequeue(out completeCode))
                {
                    Thread.Sleep(100);
                }
                if (completeCode != code)
                {
                    //MessageBox.Show(string.Format("complete_code({0}) != code({1})", completeCode, code));
                    return i;
                }
                if (0 < stringBuilder.Length)
                {
                    stringBuilder.Length -= 2;
                    string data = stringBuilder.ToString();
                    File.WriteAllText(path, data);
                }

                bool 연속조회제한 = (연속요청제한량 <= requestCnt);
                int nextIdx = i + 1;
                Invoke(new MethodInvoker(delegate ()
                {
                    progressBar_all.Value = nextIdx;
                    progressBar_part.Value = (연속조회제한) ? 연속요청제한량 : requestCnt;
                    label_time.Text = (연속조회제한) ? "00:00:00" :
                                      TimeSpan.FromSeconds(요청대기시간 * 0.001 * (연속요청제한량 - requestCnt)).ToString(@"mm\:ss");
                }));
                if (연속조회제한)
                {
                    return nextIdx;
                }
            }
            return length;
        }
        public void 일요청_이벤트(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            var code = e.sScrNo.Trim();
            var cnt = API.GetRepeatCnt(e.sTrCode, e.sRQName);
            
            for (int i = 0; i < cnt; i++)
            {
                var date = API.GetCommData(e.sTrCode, e.sRQName, i, "일자").Trim();
                var price = API.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim();
                var amount = API.GetCommData(e.sTrCode, e.sRQName, i, "거래대금").Trim();
                var start_price = API.GetCommData(e.sTrCode, e.sRQName, i, "시가").Trim();
                var high_price = API.GetCommData(e.sTrCode, e.sRQName, i, "고가").Trim();
                var low_price = API.GetCommData(e.sTrCode, e.sRQName, i, "저가").Trim();

                string line = string.Join("\t", date, price, amount, start_price, high_price, low_price);
                stringBuilder.AppendLine(line);
            }
            
            completeCode_Queue.Enqueue(code);
        }

        private int Download_틱(string date, string[] codes)
        {
            Thread 요청스레드 = new Thread(new ThreadStart(Run_TicRequestThread));
            요청스레드.IsBackground = true;
            요청스레드.Start();

            stringBuilder = new StringBuilder(1024 * 1024);
            int length = codes.Length;
            for (int i = 틱다운인덱스; i < length; i++)
            {
                var code = codes[i];
                var path = PathManager.GetPath_틱파일(date, code);
                if (!File.Exists(path))
                {
                    //초기화
                    stringBuilder.Clear();

                    //틱요청
                    Debug.WriteLine(code + "요청");
                    RequestQueue.Enqueue(new Request(code, 0));
                    Thread.Sleep(요청대기시간);

                    string completeCode = null;
                    while (!completeCode_Queue.TryDequeue(out completeCode))
                    {
                        Thread.Sleep(100);
                    }
                    if (completeCode != code)
                    {
                        //MessageBox.Show(string.Format("complete_code({0}) != code({1})", completeCode, code));
                        return i;
                    }
                    if(0 < stringBuilder.Length)
                    {
                        stringBuilder.Length -= 2;
                        string data = stringBuilder.ToString();
                        File.WriteAllText(path, data);
                    }

                    bool 연속조회제한 = (연속요청제한량 <= requestCnt);
                    int nextIdx = i + 1;
                    Invoke(new MethodInvoker(delegate ()
                    {
                        progressBar_all.Value = nextIdx;
                        progressBar_part.Value = (연속조회제한) ? 연속요청제한량 : requestCnt;
                        label_time.Text = (연속조회제한) ? "00:00:00" :
                                          TimeSpan.FromSeconds(요청대기시간 * 0.001 * (연속요청제한량 - requestCnt)).ToString(@"mm\:ss");
                    }));
                    if (연속조회제한)
                    {
                        return nextIdx;
                    }
                }
            }
            return length;
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
                if(n다운로드날짜 < lastDate && sPrevNext != 0)
                {
                    RequestQueue.Enqueue(new Request(code, sPrevNext));
                    return;
                }
            }

            for (int i = 0; i < cnt; i++)
            {
                string time = API.GetCommData(e.sTrCode, e.sRQName, i, "체결시간").Trim();
                int date = int.Parse(time.Substring(0, 8));

                if(date < n다운로드날짜)
                {
                    completeCode_Queue.Enqueue(code);
                    return;
                }
                else if(date == n다운로드날짜)
                {
                    string price = API.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim();
                    string vol = API.GetCommData(e.sTrCode, e.sRQName, i, "거래량").Trim();
                    stringBuilder.Append(time.Substring(8));
                    stringBuilder.Append('\t');
                    stringBuilder.Append(price);
                    stringBuilder.Append('\t');
                    stringBuilder.Append(vol);
                    stringBuilder.AppendLine();
                }
            }

            if (sPrevNext == 0)
            {
                completeCode_Queue.Enqueue(code);
                return;
            }
            else
            {
                RequestQueue.Enqueue(new Request(code, sPrevNext));
                return;
            }
        }

        private int requestCnt = 0;
        private struct Request
        {
            public string code;
            public int sPrevNext;
            public Request(string _code, int _sPrevNext)
            {
                code = _code;
                sPrevNext = _sPrevNext;
            }
        }
        private ConcurrentQueue<Request> RequestQueue = new ConcurrentQueue<Request>(); 
        private void Run_TicRequestThread()
        {
            while (requestCnt < 1000)
            {
                Request request;
                if (RequestQueue.TryDequeue(out request))
                {
                    API.SetInputValue("종목코드", request.code);
                    API.SetInputValue("틱범위", "1");
                    API.SetInputValue("수정주가구분", "1");
                    int ret = API.CommRqData("틱요청", "opt10079", request.sPrevNext, request.code);
                    if (ret != 0)
                        MessageBox.Show("Request_틱 : " + ret);
                    else
                        ++requestCnt;

                    Thread.Sleep(요청대기시간);
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
            MessageBox.Show("1000 < requestCnt !!");
        }
        private void Run_DayRequestThread()
        {
            while (requestCnt < 1000)
            {
                Request request;
                if (RequestQueue.TryDequeue(out request))
                {
                    API.SetInputValue("종목코드", request.code);
                    API.SetInputValue("기준일자", 다운로드날짜);
                    API.SetInputValue("수정주가구분", "0");
                    int ret = API.CommRqData("일요청", "opt10081", request.sPrevNext, request.code);
                    if (ret != 0)
                        MessageBox.Show("Request_일 : " + ret);
                    else
                        ++requestCnt;

                    Thread.Sleep(요청대기시간);
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
            MessageBox.Show("1000 < requestCnt !!");
        }

        //여러날짜 다운로드 (최소 거래일 입력)
        private int Download_틱_여러날짜(string date, string[] codes)
        {
            Thread 요청스레드 = new Thread(new ThreadStart(Run_TicRequestThread));
            요청스레드.IsBackground = true;
            요청스레드.Start();

            stringBuilder = new StringBuilder(1024 * 1024);
            int length = codes.Length;
            for (int i = 틱다운인덱스; i < length; i++)
            {
                var code = codes[i];
                var path = PathManager.GetPath_틱파일(date, code);
                if (!File.Exists(path))
                {
                    //초기화
                    stringBuilder.Clear();

                    //틱요청
                    Debug.WriteLine(code + "요청");
                    RequestQueue.Enqueue(new Request(code, 0));
                    Thread.Sleep(요청대기시간);

                    string completeCode = null;
                    while (!completeCode_Queue.TryDequeue(out completeCode))
                    {
                        Thread.Sleep(100);
                    }
                    if (completeCode != code)
                    {
                        //MessageBox.Show(string.Format("complete_code({0}) != code({1})", completeCode, code));
                        return i;
                    }
                    if (0 < stringBuilder.Length)
                    {
                        stringBuilder.Length -= 2;
                        string data = stringBuilder.ToString();
                        File.WriteAllText(path, data);
                    }

                    bool 연속조회제한 = (연속요청제한량 <= requestCnt);
                    int nextIdx = i + 1;
                    Invoke(new MethodInvoker(delegate ()
                    {
                        progressBar_all.Value = nextIdx;
                        progressBar_part.Value = (연속조회제한) ? 연속요청제한량 : requestCnt;
                        label_time.Text = (연속조회제한) ? "00:00:00" :
                                          TimeSpan.FromSeconds(요청대기시간 * 0.001 * (연속요청제한량 - requestCnt)).ToString(@"mm\:ss");
                    }));
                    if (연속조회제한)
                    {
                        return nextIdx;
                    }
                }
            }
            return length;
        }
        private void 틱요청_이벤트_여러날짜(_DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            var code = e.sScrNo.Trim();

            var cnt = API.GetRepeatCnt(e.sTrCode, e.sRQName);
            int sPrevNext = int.Parse(e.sPrevNext);

            if (0 < cnt)
            {
                string lastTime = API.GetCommData(e.sTrCode, e.sRQName, (cnt - 1), "체결시간").Trim();
                int lastDate = int.Parse(lastTime.Substring(0, 8));
                if (n다운로드날짜 < lastDate && sPrevNext != 0)
                {
                    RequestQueue.Enqueue(new Request(code, sPrevNext));
                    return;
                }
            }

            for (int i = 0; i < cnt; i++)
            {
                string time = API.GetCommData(e.sTrCode, e.sRQName, i, "체결시간").Trim();
                int date = int.Parse(time.Substring(0, 8));

                if (date < n다운로드날짜)
                {
                    completeCode_Queue.Enqueue(code);
                    return;
                }
                else if (date == n다운로드날짜)
                {
                    string price = API.GetCommData(e.sTrCode, e.sRQName, i, "현재가").Trim();
                    string vol = API.GetCommData(e.sTrCode, e.sRQName, i, "거래량").Trim();
                    stringBuilder.Append(time.Substring(8));
                    stringBuilder.Append('\t');
                    stringBuilder.Append(price);
                    stringBuilder.Append('\t');
                    stringBuilder.Append(vol);
                    stringBuilder.AppendLine();
                }
            }

            if (sPrevNext == 0)
            {
                completeCode_Queue.Enqueue(code);
                return;
            }
            else
            {
                RequestQueue.Enqueue(new Request(code, sPrevNext));
                return;
            }
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
