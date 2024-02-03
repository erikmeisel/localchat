using Microsoft.Win32;
using System;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace chatapp
{
    public partial class MainForm : Form
    {
        private UdpClient udpClient;
        private Thread receivingThread;
        NotifyIcon notifyIcon;
        public MainForm()
        {
            InitializeComponent();
            name.Text = System.Environment.MachineName;
            createNotificationIcon();
            InitializeChat();
            AddToStartup();
        }

        void createNotificationIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Icon;// Ruta al archivo del icono
            notifyIcon.Text = "Local Chat"; // Texto que se muestra al pasar el cursor sobre el icono
            // Mostrar el icono en la bandeja del sistema
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += notifyIcon_MouseClick;
            // Agregar un menú contextual al icono
            ContextMenu contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add("Ver", (sender, e) => {
                if (IsDisposed) Show();
            }); // Opción para salir de la aplicación
            contextMenu.MenuItems.Add("Salir", (sender, e) => {
                udpClient.Close();
                notifyIcon.Dispose();
                Application.Exit();
            }); // Opción para salir de la aplicación
            notifyIcon.ContextMenu = contextMenu;

        }
        private void InitializeChat()
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 12345));
            receivingThread = new Thread(ReceiveMessages);
            receivingThread.Start();
        }

        private void ReceiveMessages()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (true)
                {
                    try
                    {
                        byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                        string message = Encoding.ASCII.GetString(receivedBytes);
                        Thread.Sleep(100);
                        // Update the UI with the received message
                        Invoke(new Action(() => UpdateChat(message)));
                    } catch (Exception e)
                    {
                        break;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // The UdpClient is closed, thread can exit
                Application.Exit();
            }
        }

        private void UpdateChat(string message)
        {
            string[] msgs = message.Split(':');
            if (msgs[0]!=name.Text)
            {
                ShowToastNotification(message);
            }
            chatTextBox.AppendText($"{message}\r\n");
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                Hide();
                e.Cancel = true;
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            string message = $"{name.Text}: {messageTextBox.Text}";
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);

            // Broadcast the message to the local network
            udpClient.Send(messageBytes, messageBytes.Length, new IPEndPoint(IPAddress.Broadcast, 12345));
            // Clear the message input
            messageTextBox.Clear();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
 
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void MainForm_Leave(object sender, EventArgs e)
        {
            
        }

        private void messageTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar==13)
            {
                sendButton_Click(sender, new EventArgs());
            }
        }
        private void ShowToastNotification(string message)
        {
            if (!Visible || WindowState == FormWindowState.Minimized)
            {
                notifyIcon.ShowBalloonTip(10000, "Mensaje", message, ToolTipIcon.Info);
            } else
            {
                PlayNotificationSound();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void AddToStartup()
        {
            // Agregar al registro de Windows para que se inicie con el sistema
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            // El nombre que quieras darle a tu aplicación en el registro
            string appName = "LocalChatApp";

            // Ruta completa al ejecutable de tu aplicación
            string appPath = Application.ExecutablePath;

            // Asegúrate de que no esté ya agregado
            if (key.GetValue(appName) == null)
            {
                key.SetValue(appName, appPath);
            }
        }
        public void PlayNotificationSound()
        {
            bool found = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue(null); // pass null to get (Default)
                        if (o != null)
                        {
                            SoundPlayer theSound = new SoundPlayer((String)o);
                            theSound.Play();
                            found = true;
                        }
                    }
                }
            }
            catch
            { }
            if (!found)
                SystemSounds.Beep.Play(); // consolation prize
        }
    }
}
