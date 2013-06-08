using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Renci.SshNet;
using System.Text.RegularExpressions;

namespace SSHKeyTransfer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void execute()
        {
            try
            {
                //Read Key
                Status("opening key");
                FileStream file = File.OpenRead(textBoxFile.Text);

                //Connect to SFTP
                Status("sftp connecting");
                SftpClient sftp = new SftpClient(textBoxHost.Text, textBoxUser.Text, textBoxPass.Text);
                sftp.Connect();

                //Find authorized keys
                string auth_keys = "/" + textBoxUser.Text + "/.ssh/authorized_keys";
                if (!sftp.Exists(auth_keys))
                {
                    Status("creating");
                    if (!sftp.Exists("/" + textBoxUser.Text + "/.ssh"))
                        sftp.CreateDirectory("/" + textBoxUser.Text + "/.ssh");
                    sftp.Create(auth_keys);

                }

                //Download
                Status("downloading");
                Stream stream = new MemoryStream();
                sftp.DownloadFile(auth_keys, stream);
                Status("downloaded");

                //Read
                byte[] buffer = new byte[10240];//No key should be this large
                int length = file.Read(buffer, 0, buffer.Length);

                //Validate
                if (length < 20)
                {
                    Status("Invalid Key (Length)");
                    return;
                }
                if (buffer[0] == (byte)'s' && buffer[1] == (byte)'s' && buffer[2] == (byte)'h' && buffer[3] == (byte)'-' && buffer[4] == (byte)'r' && buffer[5] == (byte)'s' && buffer[6] == (byte)'a')
                {

                }
                else
                {
                    Status("Invalid Key (Format)");
                    return;
                }

                //Check new line
                if (stream.Length != 0)
                {
                    stream.WriteByte((byte)'\n');
                }

                //Append
                Status("appending");
                stream.Write(buffer, 0, length);

                //Upload
                Status("uploading");
                stream.Seek(0, SeekOrigin.Begin);
                sftp.UploadFile(stream, auth_keys);
                Status("done");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            execute();
        }

        private void Status(string text)
        {
            label3.Text = text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            textBoxFile.Text = openFileDialog1.FileName;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            String[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                try
                {
                    IWshRuntimeLibrary.IWshShell shell = new IWshRuntimeLibrary.WshShell();
                    IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(file);

                    String s = shortcut.Arguments;

                    if (Regex.IsMatch(s, "([a-z]+)@([a-z0-9\\.]+)", RegexOptions.IgnoreCase))
                    {
                        Match m = Regex.Match(s, "([a-z]+)@([a-z0-9\\.]+)");
                        textBoxUser.Text = m.Groups[1].Value;
                        textBoxHost.Text = m.Groups[2].Value;
                        execute();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }
    }
}
