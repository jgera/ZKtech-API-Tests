using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tools
{
    public partial class APITests : Form
    {
        public zkemkeeper.CZKEM axCZKEM1 = new zkemkeeper.CZKEM();
        SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["gyms"].ConnectionString);

        struct SyncInfo
        {
            public int MachineNo;
            public string EnrollNumber;
            public string Name;
            public string Password;
            public int Privilege;
            public bool IsEnabled;
            public int FingerIndex;
            public string TmpData;
            public int TmpLength;
            public int Flag;
            public bool Sync;
            public bool MachineSynced;
            public bool FingerDataSynced;
            public bool todelete;
        }

        private bool CheckStatus(String IPAddress)
        {
            bool IsConnected = axCZKEM1.Connect_Net(IPAddress, 4370);

            return IsConnected;


        }

        private List<SyncInfo> FetchMachineData2(String IPAddress, bool GetAll)
        {
            int MachineNo = 1;
            bool IsConnected = axCZKEM1.Connect_Net(IPAddress, 4370);
            if (!IsConnected)
                return null;
            List<SyncInfo> l = new List<SyncInfo>();
            SyncInfo s = new SyncInfo();
            axCZKEM1.ReadAllUserID(MachineNo);//read all the user information to the memory
            axCZKEM1.ReadAllTemplate(MachineNo);//read all the users' fingerprint templates to the memory
            while (axCZKEM1.SSR_GetAllUserInfo(MachineNo, out s.EnrollNumber, out s.Name, out s.Password, out s.Privilege, out s.IsEnabled))//get all the users' information from the memory
            {
                if (GetAll)
                    axCZKEM1.GetUserTmpExStr(MachineNo, s.EnrollNumber, 0, out s.Flag, out s.TmpData, out s.TmpLength);//get the corresponding templates string and length from the memory
                l.Add(s);
            }

            s.MachineNo = MachineNo;
            s.FingerIndex = 0; // Default finger index
            return l;
        }


        private void restore(int machineno = 1)
        {

            bool stuts = CheckStatus(txt_ipaddress.Text);
            OpenFileDialog sf = new OpenFileDialog();
            sf.DefaultExt = "*.csv";
            sf.FileName = "backup.csv";
            if (sf.ShowDialog() != DialogResult.OK)
                return;

            string[] Lines = File.ReadAllLines(sf.FileName);


            Cursor = Cursors.WaitCursor;
            axCZKEM1.EnableDevice(machineno, false);

            //if (!axCZKEM1.BeginBatchUpdate(machineno, 1))
            //    return;

            int iPrivilege = 0;  //default previlege
            string password = "";
            bool Enabled = true;  //Enabled by default
            int FingerIndex = 0;
            DateTime start = DateTime.Now;
            foreach (string usr in Lines)
            {
                string[] details = usr.Split(',');
                // SSR_SetUserInfo not working
                if (axCZKEM1.SSR_SetUserInfo(machineno, details[0], details[1], password, iPrivilege, Enabled))//upload user information to the memory
                {
                    axCZKEM1.SetUserTmpExStr(machineno, details[0], FingerIndex, 1, details[2].Trim());
                }
            }
            DateTime end = DateTime.Now;
            TimeSpan span = (end - start);
            string time = String.Format("it took {0} minutes, {1} seconds", span.Minutes, span.Seconds);


            //axCZKEM1.BatchUpdate(machineno);        //upload all the information in the memory
            //axCZKEM1.RefreshData(machineno);        //the data in the device should be refreshed
            //axCZKEM1.EnableDevice(machineno, true);
            Cursor = Cursors.Default;
            MessageBox.Show("Restore done!\n" + time);
        }


        private void backup()
        {
            bool stuts = CheckStatus(txt_ipaddress.Text);

            SaveFileDialog sf = new SaveFileDialog();
            sf.DefaultExt = "*.csv";
            sf.FileName = "backup.csv";
            if (sf.ShowDialog() != DialogResult.OK)
                return;
            DateTime start = DateTime.Now;
            List<SyncInfo> allusers = FetchMachineData2(txt_ipaddress.Text, true);
            DateTime end = DateTime.Now;
            TimeSpan span = (end - start);
            string time = String.Format("{0} minutes, {1} seconds", span.Minutes, span.Seconds);
            string export = "";
            foreach (var i in allusers)
            {
                export += String.Format("{0},{1},{2}\n", i.EnrollNumber, i.Name, i.TmpData);

            }

            File.WriteAllText(sf.FileName, export);
            MessageBox.Show("backup done!\n" + time);

        }


        public APITests()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var Machine = FetchMachineData2(txt_ipaddress.Text, true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bool IsConnected = axCZKEM1.Connect_Net(txt_ipaddress.Text, 4370);
            SyncInfo s = new SyncInfo();
            int varify;
            byte res;

            axCZKEM1.GetUserInfoEx(1, 11, out varify, out res);
        }

        struct log
        {
            public int MachineNo;
            public string EnrollNumber;
            public int VerifyMode;
            public int InOutMode;
            public DateTime DateTime;
            public int WorkCode;
        }


        private List<log> GetLogs(String IpAddress, int MachineNo = 1)
        {
            List<log> Logs = new List<log>();
            log l = new log();
            int year, month, day, hour, min, sec;

            bool IsConnected = axCZKEM1.Connect_Net(IpAddress, 4370);
            //Cursor = Cursors.WaitCursor;

            axCZKEM1.EnableDevice(1, false);//disable the device
            while (axCZKEM1.SSR_GetGeneralLogData(1, out l.EnrollNumber, out l.VerifyMode, out l.InOutMode, out year, out month, out day, out hour, out min, out sec, ref l.WorkCode))
            {
                l.MachineNo = MachineNo;
                l.DateTime = new DateTime(year, month, day, hour, min, sec);
                Logs.Add(l);
            }
            axCZKEM1.EnableDevice(1, true);//disable the device
            return Logs;

        }


        private void ClearLogs(String IpAddress, int MachineNo = 1)
        {

            if (!CheckStatus(IpAddress))
                return;


            axCZKEM1.ClearGLog(MachineNo);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            List<log> Logs = GetLogs(txt_ipaddress.Text);

            UpdateLogs(Logs);
        }
        private void UpdateLogs(List<log> logs)
        {
            string sql = "select max(id) from Logs";
            SqlCommand cmd = new SqlCommand(sql, con);
            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            sda.Fill(dt);
            int count;
            try
            {
                count = (int)dt.Rows[0][0];

            }
            catch (Exception)
            {

                count = 0;
            }


            for (int i = count; i < logs.Count; i++)
            {
                sql = "insert into Logs(MachineNo,EnrollNumber,VerifyMode,InOutMode,DateTime,WorkCode) values (@MachineNo, @EnrollNumber, @VerifyMode, @InOutMode,@DateTime,@WorkCode)";
                cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@MachineNo", logs[i].MachineNo);
                cmd.Parameters.AddWithValue("@EnrollNumber", logs[i].EnrollNumber);
                cmd.Parameters.AddWithValue("@VerifyMode", logs[i].VerifyMode);
                cmd.Parameters.AddWithValue("@InOutMode", logs[i].InOutMode);
                cmd.Parameters.AddWithValue("@DateTime", logs[i].DateTime);
                cmd.Parameters.AddWithValue("@WorkCode", logs[i].WorkCode);
                sda = new SqlDataAdapter(cmd);
                dt = new DataTable();
                sda.Fill(dt);
            }


        }

        private void button4_Click(object sender, EventArgs e)
        {

            int year = 2018;
            int month = 02;

            DateTime date = new DateTime(year, month, 01);
            DateTime FirstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            DateTime LastDayOfMonth = FirstDayOfMonth.AddMonths(1).AddDays(-1);
            GetAttendance(FirstDayOfMonth, LastDayOfMonth);
        }

        struct Attendance
        {

            public bool present;
            public TimeSpan start;
            public TimeSpan end;
            public int count;
        }

        private void GetAttendance(DateTime Start, DateTime End)
        {

            Dictionary<Tuple<DateTime, String>, Attendance> attendence = new Dictionary<Tuple<DateTime, String>, Attendance>();

            string sql = "select DateTime,EnrollNumber from logs where DateTime BETWEEN @Start and @End";
            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Start", Start.ToString("yyyyMMdd"));
            cmd.Parameters.AddWithValue("@End", End.ToString("yyyyMMdd"));

            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            sda.Fill(dt);


            foreach (DataRow row in dt.Rows)
            {
                DateTime d = ((DateTime)row[0]).Date;
                String eno = (string)row[1];

                Tuple<DateTime, String> entry = new Tuple<DateTime, String>(d, eno);

                //Console.WriteLine(d.ToString());
                Attendance A = new Attendance();

                if (!attendence.ContainsKey(entry))
                {
                    A.start = ((DateTime)row[0]).TimeOfDay;
                    A.present = true;
                    A.count = 1;
                    attendence.Add(entry, A);
                }
                else
                {
                    A = attendence[entry];
                    // if start time is greater update end time
                    if (A.start > ((DateTime)row[0]).TimeOfDay)
                    {
                        A.end = A.start;
                        A.start = ((DateTime)row[0]).TimeOfDay;
                        A.count += 1;
                        attendence[entry] = A;
                        continue;
                    }

                    A.count += 1;
                    A.end = ((DateTime)row[0]).TimeOfDay;
                    attendence[entry] = A;

                }

            }

            int a = attendence.Count;
        }
        DataTable Membertimings = new DataTable();
        Dictionary<String, Boolean> status = new Dictionary<string, bool>();

        private void UpdateMemberStatus()
        {

            foreach (DataRow row in Membertimings.Rows)
            {
                TimeSpan start = TimeSpan.Parse((string)row[1]);
                TimeSpan end = TimeSpan.Parse((string)row[2]);
                TimeSpan now = DateTime.Now.TimeOfDay;

                if (now > start && now < end)
                {
                    status.Add((string)row[0], true);
                }
                else
                {
                    status.Add((string)row[0], false);
                }

            }
        }


        // Call infrequently once a day and cache variables in memory
        private void GetMemberTimings()
        {
            // NTP: Add ?? Use windows 7+ 
            // remove not needed rows or old from table MemberTimings

            // Referesh cached when called 
            Membertimings = new DataTable();
            status = new Dictionary<string, bool>();

            DateTime TodayOnly = DateTime.Now.Date;

            // today only Memmbers with restricted timings
            string sql = "select eno,starttime,endtime from MemberTimings where tilldate >= @Start and tilldate <= @End";
            SqlCommand cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@Start", TodayOnly.ToString("yyyyMMdd"));
            cmd.Parameters.AddWithValue("@End", TodayOnly.ToString("yyyyMMdd"));

            string query = cmd.CommandText;

            foreach (SqlParameter p in cmd.Parameters)
            {
                query = query.Replace(p.ParameterName, p.Value.ToString());
            }

            Console.WriteLine(query);

            SqlDataAdapter sda = new SqlDataAdapter(cmd);
            sda.Fill(Membertimings);

            SqlCommand cmd2 = new SqlCommand("select distinct eno from MemberTimings;", con);
            SqlDataAdapter sda2 = new SqlDataAdapter();
            DataTable dt2 = new DataTable();
            sda2.Fill(dt2);

            foreach (DataRow item in dt2.Rows)
            {
                if (!status.ContainsKey((string)item[0]))
                {
                    status.Add((string)item[0], false);
                }
            }


        }

        private void UpdateMemberStatusToMachine(Dictionary<string, bool> status)
        {
            CheckStatus("");
            axCZKEM1.EnableDevice(1, false);
            foreach (var userstatus in status)
            {
                axCZKEM1.SSR_EnableUser(1, userstatus.Key, userstatus.Value);

            }

            axCZKEM1.RefreshData(1);//the data in the device should be refreshed
            axCZKEM1.EnableDevice(1, true);
        }


        private void button5_Click(object sender, EventArgs e)
        {
            GetMemberTimings();
            UpdateMemberStatus();
            UpdateMemberStatusToMachine(status);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            bool IsConnected = axCZKEM1.Connect_Net(this.txt_ipaddress.Text, 4370);
            if (IsConnected)
            {
                label2.Text = "Connected";
                label2.ForeColor = Color.Green;
            }
            else
            {
                label2.Text = "Disconnected";
                label2.ForeColor = Color.Red;
            }

        }

        private void APITests_Load(object sender, EventArgs e)
        {
            //Here you can register the realtime events that you want to be triggered(the parameters 65535 means registering all)
            bool IsConnected = axCZKEM1.Connect_Net(this.txt_ipaddress.Text, 4370);
            if (axCZKEM1.RegEvent(1, 65535))
            {
                axCZKEM1.OnFinger += new zkemkeeper._IZKEMEvents_OnFingerEventHandler(AxCZKEM1_OnFinger);
                this.axCZKEM1.OnVerify += new zkemkeeper._IZKEMEvents_OnVerifyEventHandler(AxCZKEM1_OnVerify);
                this.axCZKEM1.OnAttTransactionEx += new zkemkeeper._IZKEMEvents_OnAttTransactionExEventHandler(AxCZKEM1_OnAttTransactionEx);
                this.axCZKEM1.OnEnrollFingerEx += new zkemkeeper._IZKEMEvents_OnEnrollFingerExEventHandler(AxCZKEM1_OnEnrollFingerEx);
                this.axCZKEM1.OnNewUser += new zkemkeeper._IZKEMEvents_OnNewUserEventHandler(AxCZKEM1_OnNewUser);
            }

        }

        private void AxCZKEM1_OnNewUser(int EnrollNumber)
        {
            MessageBox.Show("Enroll" + EnrollNumber.ToString());
        }

        private void AxCZKEM1_OnEnrollFingerEx(string EnrollNumber, int FingerIndex, int ActionResult, int TemplateLength)
        {
            MessageBox.Show("Enroll");
        }

        private void AxCZKEM1_OnAttTransactionEx(string EnrollNumber, int IsInValid, int AttState, int VerifyMethod, int Year, int Month, int Day, int Hour, int Minute, int Second, int WorkCode)
        {
            MessageBox.Show("Transection");
        }

        private void AxCZKEM1_OnVerify(int UserID)
        {
            MessageBox.Show("Verify");
        }

        private void AxCZKEM1_OnFinger()
        {
            MessageBox.Show("Finger!!");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            ClearLogs(this.txt_ipaddress.Text);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            axCZKEM1.SetSMS(1, 2, 254, 10, "2019-02-12 14:44:00", "Your Membership expires next month.");
            axCZKEM1.SSR_SetUserSMS(1, "6", 2);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            CheckStatus(txt_ipaddress.Text);
            axCZKEM1.ReadAllUserID(1);
            SyncInfo s = new SyncInfo();
            List<SyncInfo> ls = new List<SyncInfo>();

            while (axCZKEM1.SSR_GetAllUserInfo(1, out s.EnrollNumber, out s.Name, out s.Password, out s.Privilege, out s.IsEnabled))//get all the users' information from the memory
            {
                ls.Add(s);

                axCZKEM1.SSR_DeleteEnrollDataExt(1, s.EnrollNumber.ToString(), 12);

                s = new SyncInfo();
            }

            MessageBox.Show("deleted");

        }



        private void button12_Click(object sender, EventArgs e)
        {
            backup();

        }

        private void button11_Click(object sender, EventArgs e)
        {
            restore();
        }
    }
}
