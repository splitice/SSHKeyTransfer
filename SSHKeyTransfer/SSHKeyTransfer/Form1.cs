using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using Renci.SshNet;
using System.Text.RegularExpressions;
using SSHKeyTransfer.Properties;

namespace SSHKeyTransfer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Execute()
        {
            var keyPath = textBoxFile.Text;
            var host = textBoxHost.Text;
            var user = textBoxUser.Text;
            var pass = textBoxPass.Text;

            Action _execute = () =>
            {
                try
                {
                    //Read Key
                    Status("opening key");
                    FileStream file = File.OpenRead(keyPath);

                    //Connect to SFTP
                    Status("sftp connecting");
                    SftpClient sftp = new SftpClient(host, user, pass);
                    sftp.Connect();

                    //users home directory
                    string homepath = "/home/" + user + "/";
                    if (user == "root")
                    {
                        homepath = "/root/";
                    }

                    //Find authorized keys
                    string authKeys = homepath + ".ssh/authorized_keys";
                    if (!sftp.Exists(authKeys))
                    {
                        Status("creating");
                        if (!sftp.Exists(homepath + ".ssh"))
                            sftp.CreateDirectory(homepath + ".ssh");
                        sftp.Create(authKeys);
                    }

                    //Download
                    Status("downloading");
                    Stream stream = new MemoryStream();
                    sftp.DownloadFile(authKeys, stream);
                    Status("downloaded");

                    //Read
                    byte[] buffer = new byte[10240]; //No key should be this large
                    int length = file.Read(buffer, 0, buffer.Length);

                    //Validate
                    String strKey;
                    if (length < 20)
                    {
                        Status("Invalid Key (Length)");
                        return;
                    }
                    if (buffer[0] == (byte) 's' && buffer[1] == (byte) 's' && buffer[2] == (byte) 'h' &&
                        buffer[3] == (byte) '-' && buffer[4] == (byte) 'r' && buffer[5] == (byte) 's' &&
                        buffer[6] == (byte) 'a')
                    {
                        strKey = Encoding.ASCII.GetString(buffer).TrimEnd();
                    }
                    else
                    {
                        Status("Invalid Key (Format)");
                        return;
                    }

                    stream.Seek(0, SeekOrigin.Begin);
                    StreamReader reader = new StreamReader(stream);

                    //Check for key that might already exist
                    while (reader.EndOfStream)
                    {
                        var line = reader.ReadLine().Trim();
                        if (line == strKey)
                        {
                            Status("key already exists");
                            return;
                        }
                    }

                    //Check new line
                    if (stream.Length != 0)
                    {
                        stream.Seek(0, SeekOrigin.End);
                        stream.WriteByte((byte) '\n');
                    }
                    else
                    {
                        stream.Seek(0, SeekOrigin.End);
                    }

                    //Append
                    Status("appending");
                    stream.Write(buffer, 0, length);

                    //Upload
                    Status("uploading");
                    stream.Seek(0, SeekOrigin.Begin);
                    sftp.UploadFile(stream, authKeys);
                    Status("done");

                    Settings.Default.KeyPath = textBoxFile.Text;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            };

            Thread thread = new Thread(new ThreadStart(_execute));
            thread.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Execute();
        }

        private void Status(string text)
        {
            Invoke(new Action(() =>
            {
                label3.Text = text;
            }));
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
                        Execute();
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
