using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using System.Runtime.InteropServices;

namespace Shadowsocks.View
{
    public partial class ConfigForm : Form
    {
        private ShadowsocksController controller;

        /********************************* <Start> add by Ian.May,Oct.16 *********************************/
        // new add var
        private Point initialRightBottomCorner;
        private int ShadowFogModeFormWidth;

        private bool isHashedPassword;
        private bool isPasswordTextboxClicked;

        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)]string lParam);
        // ends
        /********************************* <End> add by Ian.May,Oct.16 ************************************/

        // this is a copy of configuration that we are working on
        private Configuration _modifiedConfiguration;
        private int _lastSelectedIndex = -1;

        public ConfigForm(ShadowsocksController controller)
        {
            this.Font = System.Drawing.SystemFonts.MessageBoxFont;
            InitializeComponent();

            // a dirty hack
            this.ServersListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PerformLayout();

            /****************************** <Start> add by Ian.May,Oct.16 **************************************/
            //adjust the sequence because the modified UpdatTexts() relies on controller's instance
            this.controller = controller;
            controller.ConfigChanged += controller_ConfigChanged;

            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            ShadowFogModeFormWidth = this.Width;
            //ends
            /*******************************<End> add by Ian.May,Oct.16***************************************/

            LoadCurrentConfiguration();
        }

        private void UpdateTexts()
        {
            AddButton.Text = I18N.GetString("&Add");
            DeleteButton.Text = I18N.GetString("&Delete");
            DuplicateButton.Text = I18N.GetString("Dupli&cate");
            IPLabel.Text = I18N.GetString("Server Addr");
            ServerPortLabel.Text = I18N.GetString("Server Port");
            PasswordLabel.Text = I18N.GetString("Password");
            EncryptionLabel.Text = I18N.GetString("Encryption");
            ProxyPortLabel.Text = I18N.GetString("Proxy Port");
            RemarksLabel.Text = I18N.GetString("Remarks");
            TimeoutLabel.Text = I18N.GetString("Timeout(Sec)");
            OneTimeAuth.Text = I18N.GetString("Onetime Authentication");
            ServerGroupBox.Text = I18N.GetString("Server");
            OKButton.Text = I18N.GetString("OK");
            MyCancelButton.Text = I18N.GetString("Cancel");
            MoveUpButton.Text = I18N.GetString("Move &Up");
            MoveDownButton.Text = I18N.GetString("Move D&own");
            this.Text = I18N.GetString("Edit Servers");
            /**********************************<Start> add by Ian.May,Oct.16**********************************/
            //Text and other controls for shadowfog panel
            //this.Text = I18N.GetString("Edit Servers");
            this.Text = I18N.GetString("Sign In");

            isPasswordTextboxClicked = false;
            // when loading the initial UI, should judge whether to load the user information from local file
            ShadowFogRememberUserCheck.Checked = controller.GetClientUserIsRemember();
            if (ShadowFogRememberUserCheck.Checked)
            {
                ShadowFogUserName.Text = controller.GetClientUserName();
                ShadowFogPassword.Text = controller.GetClientUserPasswordHashed();
            }
            else
            {
                SendMessage(ShadowFogUserName.Handle, EM_SETCUEBANNER, 0, " Username...");
                SendMessage(ShadowFogPassword.Handle, EM_SETCUEBANNER, 0, " Password...");
            }
            // must be put after the textbox value changed, in case the textchanged event triggerd to force this flag being false;
            // problem here: checked is to remember password, only when auto load password case, no modification to textbox, the password is auto hashed;
            isHashedPassword = ShadowFogRememberUserCheck.Checked;
            //Ends
            /***********************************<End> add by Ian.May,Oct.16*************************************/
            /**********************************<Start> add by Ian.May,Dec.30**********************************/
            //tooltip/ for shadowfogToggleCheck
            ToolTip ShadowFogMode = new ToolTip();
            // Set up the delays for the ToolTip.
            ShadowFogMode.AutoPopDelay = 5000;
            ShadowFogMode.InitialDelay = 1000;
            ShadowFogMode.ReshowDelay = 500;
            // Force the ToolTip text to be displayed whether or not the form is active.
            ShadowFogMode.ShowAlways = true;

            // Set up the ToolTip text for the Button and Checkbox.
            ShadowFogMode.SetToolTip(this.ShadoFogToggleCheck, "Switch to Shadowsocks if you uncheck this");
            //ends
            /***********************************<End> add by Ian.May,Dec.30*************************************/
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void ShowWindow()
        {
            this.Opacity = 1;
            this.Show();
            IPTextBox.Focus();
        }

        private bool SaveOldSelectedServer()
        {
            try
            {
                if (_lastSelectedIndex == -1 || _lastSelectedIndex >= _modifiedConfiguration.configs.Count)
                {
                    return true;
                }
                Server server = new Server();

                if (Uri.CheckHostName(server.server = IPTextBox.Text.Trim()) == UriHostNameType.Unknown)
                {
                    MessageBox.Show(I18N.GetString("Invalid server address"));
                    IPTextBox.Focus();
                    return false;
                }
                if (!int.TryParse(ServerPortTextBox.Text, out server.server_port))
                {
                    MessageBox.Show(I18N.GetString("Illegal port number format"));
                    ServerPortTextBox.Focus();
                    return false;
                }
                server.password = PasswordTextBox.Text;
                server.method = EncryptionSelect.Text;
                server.remarks = RemarksTextBox.Text;
                if (!int.TryParse(TimeoutTextBox.Text, out server.timeout))
                {
                    MessageBox.Show(I18N.GetString("Illegal timeout format"));
                    TimeoutTextBox.Focus();
                    return false;
                }
                server.auth = OneTimeAuth.Checked;
                int localPort = int.Parse(ProxyPortTextBox.Text);
                Configuration.CheckServer(server);
                Configuration.CheckLocalPort(localPort);
                _modifiedConfiguration.configs[_lastSelectedIndex] = server;
                _modifiedConfiguration.localPort = localPort;

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return false;
        }

        private void LoadSelectedServer()
        {
            if (ServersListBox.SelectedIndex >= 0 && ServersListBox.SelectedIndex < _modifiedConfiguration.configs.Count)
            {
                Server server = _modifiedConfiguration.configs[ServersListBox.SelectedIndex];

                IPTextBox.Text = server.server;
                ServerPortTextBox.Text = server.server_port.ToString();
                PasswordTextBox.Text = server.password;
                ProxyPortTextBox.Text = _modifiedConfiguration.localPort.ToString();
                EncryptionSelect.Text = server.method ?? "aes-256-cfb";
                RemarksTextBox.Text = server.remarks;
                TimeoutTextBox.Text = server.timeout.ToString();
                OneTimeAuth.Checked = server.auth;
            }
        }

        private void LoadConfiguration(Configuration configuration)
        {
            ServersListBox.Items.Clear();
            foreach (Server server in _modifiedConfiguration.configs)
            {
                ServersListBox.Items.Add(server.FriendlyName());
            }
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = controller.GetConfigurationCopy();
            LoadConfiguration(_modifiedConfiguration);
            _lastSelectedIndex = _modifiedConfiguration.index;
            if (_lastSelectedIndex < 0 || _lastSelectedIndex >= ServersListBox.Items.Count)
            {
                _lastSelectedIndex = 0;
            }
            ServersListBox.SelectedIndex = _lastSelectedIndex;
            UpdateMoveUpAndDownButton();
            LoadSelectedServer();
        }

        /************************************<Start> add by Ian.May,Dec.30*****************************************/
        // this part is specially for shadowfog pannel and shadowsocks pannel transition
        private void LoadBackUpConfiguration()
        {
            _modifiedConfiguration = controller.GetBackUpConfiguration();// load _configBackUp from memory
            LoadConfiguration(_modifiedConfiguration);
            _lastSelectedIndex = _modifiedConfiguration.index;
            if (_lastSelectedIndex < 0)
            {
                _lastSelectedIndex = 0;
            }
            ServersListBox.SelectedIndex = _lastSelectedIndex;
            UpdateMoveUpAndDownButton();
            LoadSelectedServer();
        }
        //Ends
        /*************************************<End> add by Ian.May,Dec.30********************************************/

        private void ConfigForm_Load(object sender, EventArgs e)
        {

        }

        private void ConfigForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Sometimes the users may hit enter key by mistake, and the form will close without saving entries.

            if (e.KeyCode == Keys.Enter)
            {
                Server server = controller.GetCurrentServer();
                if (!SaveOldSelectedServer())
                {
                    return;
                }
                if (_modifiedConfiguration.configs.Count == 0)
                {
                    MessageBox.Show(I18N.GetString("Please add at least one server"));
                    return;
                }
                controller.SaveServers(_modifiedConfiguration.configs, _modifiedConfiguration.localPort);
                controller.SelectServerIndex(_modifiedConfiguration.configs.IndexOf(server));
            }

        }

        private void ServersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ServersListBox.CanSelect)
            {
                return;
            }
            if (_lastSelectedIndex == ServersListBox.SelectedIndex)
            {
                // we are moving back to oldSelectedIndex or doing a force move
                return;
            }
            if (!SaveOldSelectedServer())
            {
                // why this won't cause stack overflow?
                ServersListBox.SelectedIndex = _lastSelectedIndex;
                return;
            }
            if (_lastSelectedIndex >= 0)
            {
                ServersListBox.Items[_lastSelectedIndex] = _modifiedConfiguration.configs[_lastSelectedIndex].FriendlyName();
            }
            UpdateMoveUpAndDownButton();
            LoadSelectedServer();
            _lastSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            Server server = Configuration.GetDefaultServer();
            _modifiedConfiguration.configs.Add(server);
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _modifiedConfiguration.configs.Count - 1;
            _lastSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DuplicateButton_Click( object sender, EventArgs e )
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            Server currServer = _modifiedConfiguration.configs[_lastSelectedIndex];
            var currIndex = _modifiedConfiguration.configs.IndexOf( currServer );
            _modifiedConfiguration.configs.Insert(currIndex + 1, currServer);
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = currIndex + 1;
            _lastSelectedIndex = ServersListBox.SelectedIndex;
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            _lastSelectedIndex = ServersListBox.SelectedIndex;
            if (_lastSelectedIndex >= 0 && _lastSelectedIndex < _modifiedConfiguration.configs.Count)
            {
                _modifiedConfiguration.configs.RemoveAt(_lastSelectedIndex);
            }
            if (_lastSelectedIndex >= _modifiedConfiguration.configs.Count)
            {
                // can be -1
                _lastSelectedIndex = _modifiedConfiguration.configs.Count - 1;
            }
            ServersListBox.SelectedIndex = _lastSelectedIndex;
            LoadConfiguration(_modifiedConfiguration);
            ServersListBox.SelectedIndex = _lastSelectedIndex;
            LoadSelectedServer();
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            if (_modifiedConfiguration.configs.Count == 0)
            {
                MessageBox.Show(I18N.GetString("Please add at least one server"));
                return;
            }
            controller.SaveServers(_modifiedConfiguration.configs, _modifiedConfiguration.localPort);
            // SelectedIndex remains valid
            // We handled this in event handlers, e.g. Add/DeleteButton, SelectedIndexChanged
            // and move operations
            controller.SelectServerIndex(ServersListBox.SelectedIndex);
            this.Close();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ConfigForm_Shown(object sender, EventArgs e)
        {
            IPTextBox.Focus();
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            controller.ConfigChanged -= controller_ConfigChanged;
        }

        private void MoveConfigItem(int step)
        {
            int index = ServersListBox.SelectedIndex;
            Server server = _modifiedConfiguration.configs[index];
            object item = ServersListBox.Items[index];

            _modifiedConfiguration.configs.Remove(server);
            _modifiedConfiguration.configs.Insert(index + step, server);
            _modifiedConfiguration.index += step;

            ServersListBox.BeginUpdate();
            ServersListBox.Enabled = false;
            _lastSelectedIndex = index + step;
            ServersListBox.Items.Remove(item);
            ServersListBox.Items.Insert(index + step, item);
            ServersListBox.Enabled = true;
            ServersListBox.SelectedIndex = index + step;
            ServersListBox.EndUpdate();

            UpdateMoveUpAndDownButton();
        }

        private void UpdateMoveUpAndDownButton()
        {
            if (ServersListBox.SelectedIndex == 0)
            {
                MoveUpButton.Enabled = false;
            }
            else
            {
                MoveUpButton.Enabled = true;
            }
            if (ServersListBox.SelectedIndex == ServersListBox.Items.Count - 1)
            {
                MoveDownButton.Enabled = false;
            }
            else
            {
                MoveDownButton.Enabled = true;
            }
        }

        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            if (ServersListBox.SelectedIndex > 0)
            {
                MoveConfigItem(-1);  // -1 means move backward
            }
        }

        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            if (!SaveOldSelectedServer())
            {
                return;
            }
            if (ServersListBox.SelectedIndex < ServersListBox.Items.Count - 1)
            {
                MoveConfigItem(+1);  // +1 means move forward
            }
        }

        /************************************************************<start>add by Ian.May Oct.16*******************************************************************/
        // main modification for shadowfog mode 
        private void ShadowFogReload_Click(object sender, EventArgs e)
        {
            ShadowFogReload.Text = "Connecting...";
            // RecordClientUser() pass the username&passworf form textbox to ShadofogConfiguration object and save it to file accordingly
            if (isHashedPassword)
            { controller.RecordClientUser(ShadowFogUserName.Text.Trim(), ShadowFogPassword.Text, ShadowFogRememberUserCheck.Checked); }
            else
            { controller.RecordClientUser(ShadowFogUserName.Text.Trim(), ClientUser.SHA256(ShadowFogPassword.Text), ShadowFogRememberUserCheck.Checked); }
            try
            {
                controller.Start();
                controller.isShadowFogStarted = true; // next time will display "restart shadowfog"
                this.Close();
            }
            catch (Exception Error)
            {
                ShadowFogReload.Text = "Start";
                this.Text = I18N.GetString("Sign In");
                controller.RecoverSSConfig();// erase _config obtianed from scheduler
            }
        }

        private void ShadoFogToggleCheck_CheckedChanged(object sender, EventArgs e)
        {
            initialRightBottomCorner = this.Location + this.Size;

            if (ShadoFogToggleCheck.Checked)
            {
                controller.isShadowFogMode = true;
                Text = I18N.GetString("Sign In");
                tableLayoutPanel2.Enabled = false;
                ShadowFogGroupBox.Enabled = true;
                this.Width = ShadowFogModeFormWidth;// (328,530) as defualt when DPI = 144;
                this.Location = initialRightBottomCorner - this.Size;
            }
            else
            {
                controller.isShadowFogMode = false;
                Text = I18N.GetString("Edit Servers");
                tableLayoutPanel2.Enabled = true;
                ShadowFogGroupBox.Enabled = false;
                this.Width = (int)(3.3 * ShadowFogModeFormWidth); // (1020,505) as defualt when DPI = 144;
                this.Location = initialRightBottomCorner - this.Size;
                // this is the only way to enter the shadowsocks panel;
                // Load _configBackup from memory to configForm, avoid user to know the fognode address.
                LoadBackUpConfiguration(); //only server infomation is needed, other settings ignored;
            }
        }

        // only when 1: the password textbox is clicked; 
        // 2: the password textbox value is changed, 
        // we believe this behaviour is to enter new password;
        private void ShadowFogPassword_Click(object sender, EventArgs e)
        {
            isPasswordTextboxClicked = true;
        }

        private void ShadowFogPassword_TextChanged(object sender, EventArgs e)
        {
            if (isPasswordTextboxClicked)
            { isHashedPassword = false; }
        }

        private void ConfigForm_Activated(object sender, EventArgs e)
        {
            controller.isShadowFogMode = ShadoFogToggleCheck.Checked;
            if (controller.isShadowFogStarted)
            {
                ShadowFogReload.Text = "Restart";
                this.Text = I18N.GetString("Running...");
            }
            // since this Form always begins with shadowfog panel it can show the backup config later when unchecking "enable shadowfog“ mode
        }

        private void CreateAccountLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://shadowfog.com/oss/siteIndex");
        }
        //Ends
        /*************************************************<end>add by Ian.May Oct.16**********************************************************/

    }
}
