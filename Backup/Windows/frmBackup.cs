using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Backup
{
    public partial class frmBackup : Form
    {
        private string nomeArquivo;        
        List<string> arquivos = new List<string>();

        private void Log(string texto)
        {
            StreamWriter sw = new StreamWriter(Application.StartupPath + "\\Backup.log", true);

            using (sw)
            {
                sw.WriteLine(DateTime.Now.ToString() + texto);
            }
        }

        private void CriarArquivoZip(List<string> arquivos, string ArquivoDestino)
        {
            using (ZipFile zip = new ZipFile())
            {
                if (lstItens.Items.Count > 0)
                {
                    foreach (string item in lstItens.Items)
                    {

                        if (File.Exists(item))
                        {
                            pgbProgresso.Visible = true;
                            lblBackup.Visible = true;

                            try
                            {
                                zip.SaveProgress += zipProgress;
                                zip.AddFile(item, "");                                
                            }
                            catch (Exception erro)
                            {
                                Log(" - Erro: " + erro.Message);
                            }
                        }
                        else
                        {
                            Log(" - Arquivo não encontrado: " + Path.GetFileName(item));
                            return;
                        }

                    }

                    try
                    {
                        zip.Save(ArquivoDestino);
                        zip.Dispose();
                    }
                    catch (Exception erro)
                    {
                        Log(" - Erro: " + erro.Message);
                    }

                } 
                else
                {
                    Log(" - Alerta: Nenhum arquivo selecionado");
                }

            }

        }

        private bool ValidarHora(string hora)
        {
            try
            {
                Convert.ToDateTime(hora);
                Properties.Settings.Default.Horario = mskHorario.Text;
                return true;
            }
            catch 
            {
                MessageBox.Show("Horário inválido.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                mskHorario.Focus();
                return false;                
            }
        }

        public frmBackup()
        {
            InitializeComponent();
        }

        private void frmBackup_Load(object sender, EventArgs e)
        {
            lblData.Text = DateTime.Now.ToLongDateString();
            txtDestino.Text = Properties.Settings.Default.Destino;
            mskHorario.Text = Properties.Settings.Default.Horario;
        }

        private void btnProcurarDestino_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog destino = new FolderBrowserDialog();
            destino.Description = "Selecione o local para gravar o backup:";

            if (destino.ShowDialog() == DialogResult.OK)
            {
                txtDestino.Text = destino.SelectedPath;
                Properties.Settings.Default.Destino = txtDestino.Text;                
            }

        }

        private void mskHorario_Leave(object sender, EventArgs e)
        {
            ValidarHora(mskHorario.Text);
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            if (lstItens.Items.Count > 0)
            {
                Properties.Settings.Default.Save();
                MessageBox.Show("Configurações gravadas com sucesso.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.WindowState = FormWindowState.Minimized;
                RealizarBackup();
            }
            else
            {
                DialogResult result = MessageBox.Show("Não existem arquivos para realizar o backup.\nDeseja continuar?", "Atenção", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Properties.Settings.Default.Save();
                    this.WindowState = FormWindowState.Minimized;
                    RealizarBackup();
                }
            }
        }

        private void btnAdicionarItens_Click(object sender, EventArgs e)
        {
            OpenFileDialog arquivo = new OpenFileDialog();
            arquivo.Multiselect = true;
            arquivo.Title = "Selecione os arquivos para adicionar ao backup:";

            try
            {
                if (arquivo.ShowDialog() == DialogResult.OK)
                {
                    foreach (String file in arquivo.FileNames)
                    {
                        if (!lstItens.Items.Contains(file))
                        {
                            arquivos.Add(file);
                            lstItens.Items.Add(file);
                        }
                        else
                        {
                            MessageBox.Show("Arquivo(s) já selecionado(s):\n\n" + Path.GetFileName(file),
                                "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        }
                    }
                }
            }
            catch (Exception erro)
            {
                Log(" - Erro: " + erro.Message);
            }
        }

        private void frmBackup_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;                
                this.ShowInTaskbar = false;
                notifyIcon1.ShowBalloonTip(500);
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Maximized;
            this.ShowInTaskbar = true;
        }

        private void RealizarBackup()
        {
            try
            {
                nomeArquivo = txtDestino.Text + "\\MeuBackup_" + DateTime.Now.ToShortDateString().Replace("/", "") + ".zip";
                string localNomeDestinoZIP = nomeArquivo;
                CriarArquivoZip(arquivos, localNomeDestinoZIP);
                pgbProgresso.Visible = false;
                lblBackup.Visible = false;
                MessageBox.Show("Backup realizado com sucesso.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Information);                
            }
            catch (Exception erro)
            {
                Log(" - Erro: " + erro.Message);
            }
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string hora = DateTime.Now.ToLongTimeString();
            lblHora.Text = hora;

            if (hora == mskHorario.Text + ":00")
            {
                RealizarBackup();
            }
            
        }

        private void frmBackup_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Se encerrar o programa, o backup não será realizado em segundo plano.\nDeseja sair?", 
                "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Maximized;
            this.ShowInTaskbar = true;
        }

        private void zipProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_AfterWriteEntry)
            {
                this.pgbProgresso.Value = e.EntriesSaved * 100 / e.EntriesTotal;
            }

            else if (e.EventType == ZipProgressEventType.Saving_Completed)
                this.pgbProgresso.Value = 100;
        }

        // Pendente implementação da remoção dos itens
        private void btnRemoverItens_Click(object sender, EventArgs e)
        {
            if(lstItens.SelectedItems.Count > 0)
            {                
            }
            else
            {
                MessageBox.Show("Selecione um item para remover.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
