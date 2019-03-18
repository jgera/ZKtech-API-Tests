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
    public partial class BackupRestore : Form
    {

        public zkemkeeper.CZKEM axCZKEM1 = new zkemkeeper.CZKEM();
        SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["gyms"].ConnectionString);
        
        public BackupRestore()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private bool CheckStatus(String IPAddress)
        {
            bool IsConnected = axCZKEM1.Connect_Net(IPAddress, 4370);
        
            return IsConnected;
        }

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
        }

        private List<SyncInfo> FetchMachineData2(String IPAddress, bool GetAll)
        {
            int MachineNo = 1;
            //            bool IsConnected = axCZKEM1.Connect_Net(IPAddress, 4370);
            //           if (!IsConnected)
            //                return null;
            List<SyncInfo> l = new List<SyncInfo>();
            SyncInfo s = new SyncInfo();

            

            while (axCZKEM1.SSR_GetAllUserInfo(MachineNo, out s.EnrollNumber, out s.Name, out s.Password, out s.Privilege, out s.IsEnabled))//get all the users' information from the memory
            {
                if (GetAll)
                    axCZKEM1.GetUserTmpExStr(MachineNo, s.EnrollNumber, 6, out s.Flag, out s.TmpData, out s.TmpLength);//get the corresponding templates string and length from the memory
                l.Add(s);
            }

            s.FingerIndex = 6; // Default finger index
            s.MachineNo = MachineNo;
            return l;
        }

        private void UpdateOnline(SyncInfo s)
        {
            con.Open();
            //SqlCommand cmd = new SqlCommand("insert into Members values('" + s.MachineNo + "','" + @s.EnrollNumber + "', '" + @s.Name + "','6','" + @s.TmpData + "','" + @s.Privilege + "','" + @s.Password + "','" + @s.IsEnabled + "','" + @s.Flag + "','" + s.Sync + "','" + s.MachineSynced + "','" + s.FingerDataSynced + "','" + s.todelete + "')", con);
            //cmd.ExecuteNonQuery();
            con.Close();
        }


        private void restore(int machineno=1)
        {

            bool stuts = CheckStatus(txt_ipaddress.Text);
            OpenFileDialog sf = new OpenFileDialog();
            sf.DefaultExt = "*.csv";
            sf.FileName = "backup.csv";
            if (sf.ShowDialog() != DialogResult.OK)
                return;

            string[] Lines =  File.ReadAllLines(sf.FileName);


            Cursor = Cursors.WaitCursor;
            axCZKEM1.EnableDevice(machineno, false); 

            if (axCZKEM1.BeginBatchUpdate(machineno, 1))
            {
                int iPrivilege = 0;  //default previlege
                string password = "";
                bool Enabled = true;  //Enabled by default
                int FingerIndex = 6;
                foreach (string usr in Lines)
                {
                    string[] details  = usr.Split(',');

                    if (axCZKEM1.SSR_SetUserInfo(machineno, details[0], details[0], password, iPrivilege , Enabled))//upload user information to the memory
                    {
                        axCZKEM1.SSR_SetUserTmpStr(machineno, details[0], FingerIndex, details[2]);
                    }
                }
               
            }
            
            axCZKEM1.BatchUpdate(machineno);        //upload all the information in the memory
            axCZKEM1.RefreshData(machineno);        //the data in the device should be refreshed
            axCZKEM1.EnableDevice(machineno, true);
            Cursor = Cursors.Default;
            MessageBox.Show("Restore done!");
        }


        private void backup()
        {
            bool stuts = CheckStatus(txt_ipaddress.Text);

            SaveFileDialog sf = new SaveFileDialog();
            sf.DefaultExt = "*.csv";
            sf.FileName = "backup.csv";
            if (sf.ShowDialog() != DialogResult.OK)
                return;

            List<SyncInfo> allusers = FetchMachineData2(txt_ipaddress.Text, true);

            string export = "";
            foreach (var i in allusers)
            {
                export += String.Format("{0},{1},{2}\n", i.EnrollNumber, i.Name, i.TmpData);

            }

            File.WriteAllText(sf.FileName, export);
            MessageBox.Show("backup done!");

        }


        private void upload()
        {
            List<SyncInfo> allusers = FetchMachineData2(txt_ipaddress.Text, true);
            foreach (var item in allusers)
            {
                UpdateOnline(item);
            }

        }


        private void button1_Click(object sender, EventArgs e)
        {
            backup();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            restore();
        }
    }
}
