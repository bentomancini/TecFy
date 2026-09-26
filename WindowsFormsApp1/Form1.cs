using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private PictureBox _btnOlho;
        private Label _lblUltimoLogin;
        private Panel _pnlLogin;
        private Label _lblMarca;
        private Label _lblTituloLogin;
        private Label _lblDescricaoLogin;
        private Label _lblCampoEmail;
        private Label _lblCampoSenha;
        private Label _lblSlogan;

        private static readonly string CaminhoLembrar =
            System.IO.Path.Combine(Application.StartupPath, "lembrar_login.txt");

        private static readonly string CaminhoUltimoLogin =
            System.IO.Path.Combine(Application.StartupPath, "ultimo_login.txt");

        public Form1()
        {
            InitializeComponent();
            ConfigurarVisualLogin();
            btnEntrar.Click += btnEntrar_Click;
            llabelCriar.LinkClicked += llabelCriar_LinkClicked;
            llabelEsqueceu.LinkClicked += llabelEsqueceu_LinkClicked;
            ConfigurarOlhoSenha();
            CriarTextoUltimoLogin();
            AcceptButton = btnEntrar;
            Resize += (s, e) => ReposicionarResponsivo();
        }

        private void ConfigurarVisualLogin()
        {
            Text = "Tecfy - Entrar";
            MinimumSize = new Size(758, 489);
            BackgroundImage = null;
            BackColor = Tema.Fundo;

            // O WebView antigo exibia apenas um texto decorativo; o formulario
            // nativo permanece responsavel pelo login e pela autenticacao.
            webView21.Visible = false;
            webView21.TabStop = false;

            Color fundoCard = Tema.FundoElevado;
            _pnlLogin = new Panel
            {
                BackColor = fundoCard,
                Location = new Point(70, 52),
                Size = new Size(270, 347)
            };
            _pnlLogin.Paint += (s, e) =>
            {
                using (var borda = new Pen(Color.FromArgb(74, 46, 99)))
                    e.Graphics.DrawRectangle(borda, 0, 0, _pnlLogin.Width - 1, _pnlLogin.Height - 1);
            };
            Controls.Add(_pnlLogin);
            _pnlLogin.SendToBack();
            Tema.Arredondar(_pnlLogin, 20);

            _lblMarca = CriarLabelLogin("BEM-VINDO AO TECFY", 99, 70, 210, 19,
                8.5F, FontStyle.Bold, Tema.Destaque, fundoCard);
            _lblTituloLogin = CriarLabelLogin("Entre na sua conta", 99, 98, 218, 32,
                15.5F, FontStyle.Bold, Tema.Texto, fundoCard);
            _lblDescricaoLogin = CriarLabelLogin("Seu próximo som começa aqui.", 99, 134, 216, 25,
                9F, FontStyle.Regular, Tema.TextoSecundario, fundoCard);
            _lblCampoEmail = CriarLabelLogin("E-MAIL", 99, 166, 210, 18,
                8F, FontStyle.Bold, Tema.TextoSecundario, fundoCard);
            _lblCampoSenha = CriarLabelLogin("SENHA", 99, 226, 210, 18,
                8F, FontStyle.Bold, Tema.TextoSecundario, fundoCard);
            _lblSlogan = CriarLabelLogin("Música para cada momento.", 403, 340, 300, 28,
                12F, FontStyle.Bold, Tema.Texto, Tema.Fundo);
            _lblSlogan.TextAlign = ContentAlignment.MiddleCenter;

            txtEmail.BackColor = fundoCard;
            txtSenha.BackColor = fundoCard;
            txtEmail.FillColor = Color.FromArgb(16, 9, 28);
            txtSenha.FillColor = Color.FromArgb(16, 9, 28);
            txtEmail.BorderColor = Color.FromArgb(79, 54, 107);
            txtSenha.BorderColor = Color.FromArgb(79, 54, 107);
            txtEmail.FocusedState.BorderColor = Tema.Destaque;
            txtSenha.FocusedState.BorderColor = Tema.Destaque;
            txtEmail.HoverState.BorderColor = Tema.Destaque;
            txtSenha.HoverState.BorderColor = Tema.Destaque;
            txtEmail.PlaceholderForeColor = Tema.TextoSecundario;
            txtSenha.PlaceholderForeColor = Tema.TextoSecundario;
            txtEmail.PlaceholderText = "seuemail@gmail.com";
            txtSenha.PlaceholderText = "Digite sua senha";
            txtEmail.BorderRadius = 12;
            txtSenha.BorderRadius = 12;
            cboxLembrar.BackColor = fundoCard;
            cboxLembrar.ForeColor = Tema.TextoSecundario;
            llabelEsqueceu.BackColor = fundoCard;
            label1.BackColor = fundoCard;
            label1.ForeColor = Tema.TextoSecundario;
            llabelCriar.BackColor = fundoCard;
            btnEntrar.BackColor = fundoCard;
            btnEntrar.FillColor = Tema.Roxo;
            btnEntrar.HoverState.FillColor = Tema.Destaque;
            btnEntrar.Cursor = Cursors.Hand;
        }

        private Label CriarLabelLogin(string texto, int x, int y, int largura, int altura,
            float tamanho, FontStyle estilo, Color cor, Color fundo)
        {
            var label = new Label
            {
                Text = texto,
                Location = new Point(x, y),
                Size = new Size(largura, altura),
                Font = new Font("Segoe UI", tamanho, estilo),
                ForeColor = cor,
                BackColor = fundo,
                AutoEllipsis = true
            };
            Controls.Add(label);
            label.BringToFront();
            return label;
        }

        private void ConfigurarOlhoSenha()
        {
            txtSenha.UseSystemPasswordChar = true;

            var btnOlho = new PictureBox
            {
                Size = new Size(24, 24),
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(16, 9, 28),
                Image = DesenharOlho(false),
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(
                    txtSenha.Right - txtSenha.Height + 4,
                    txtSenha.Top + (txtSenha.Height - 24) / 2)
            };

            btnOlho.Click += (s, e) =>
            {
                bool mostrar = txtSenha.UseSystemPasswordChar;
                txtSenha.UseSystemPasswordChar = !mostrar;
                btnOlho.Image = DesenharOlho(!mostrar);
                txtSenha.Focus();
                txtSenha.SelectionStart = txtSenha.Text.Length;
            };

            _btnOlho = btnOlho;
            Controls.Add(btnOlho);
            btnOlho.BringToFront();
        }

        private Bitmap DesenharOlho(bool aberto)
        {
            var bmp = new Bitmap(24, 24);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Pen caneta = new Pen(Color.FromArgb(168, 85, 247), 1.6f);
                caneta.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                caneta.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                if (aberto)
                {
                    g.DrawEllipse(caneta, 3, 6, 18, 12);
                    g.FillEllipse(new SolidBrush(Color.FromArgb(168, 85, 247)), 9, 10, 6, 4);
                }
                else
                {
                    g.DrawEllipse(caneta, 4, 6, 16, 12);
                    g.DrawLine(caneta, 3, 3, 21, 21);
                    g.FillEllipse(new SolidBrush(Color.FromArgb(168, 85, 247)), 9, 10, 4, 4);
                }
            }
            return bmp;
        }

        private void CriarTextoUltimoLogin()
        {
            _lblUltimoLogin = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 100, 160),
                Font = new Font("Segoe UI", 8F),
                BackColor = Color.Transparent
            };
            Controls.Add(_lblUltimoLogin);
            _lblUltimoLogin.BringToFront();
            AtualizarTextoUltimoLogin();
        }

        private void AtualizarTextoUltimoLogin()
        {
            try
            {
                if (System.IO.File.Exists(CaminhoUltimoLogin))
                {
                    string[] partes = System.IO.File.ReadAllText(CaminhoUltimoLogin)
                        .Split(new[] { '|' }, 3);
                    if (partes.Length == 3)
                    {
                        _lblUltimoLogin.Text = string.Format(
                            "Ultimo acesso: {0} ({1}) em {2}",
                            partes[0], partes[1], partes[2]);
                        return;
                    }
                }
            }
            catch { }

            _lblUltimoLogin.Text = "";
        }

        private void SalvarUltimoLogin(string nome, string email)
        {
            try
            {
                string data = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                System.IO.File.WriteAllText(CaminhoUltimoLogin,
                    nome + "|" + email + "|" + data);
                AtualizarTextoUltimoLogin();
            }
            catch { }
        }

        private void ReposicionarResponsivo()
        {
            int w = ClientSize.Width, h = ClientSize.Height;
            if (w < 10 || h < 10) return;

            const int baseW = 742, baseH = 450;
            double esc = Math.Min(w / (double)baseW, h / (double)baseH);
            esc = Math.Max(1.0, Math.Min(esc, 1.3));

            int offX = (w - (int)(baseW * esc)) / 2;
            int offY = (h - (int)(baseH * esc)) / 2;

            Mover(_pnlLogin, 70, 52, 270, 347, esc, offX, offY);
            Tema.Arredondar(_pnlLogin, 20);
            Mover(_lblMarca, 99, 70, 210, 19, esc, offX, offY);
            Mover(_lblTituloLogin, 99, 98, 218, 32, esc, offX, offY);
            Mover(_lblDescricaoLogin, 99, 134, 216, 25, esc, offX, offY);
            Mover(_lblCampoEmail, 99, 166, 210, 18, esc, offX, offY);
            Mover(_lblCampoSenha, 99, 226, 210, 18, esc, offX, offY);
            Mover(txtEmail, 99, 186, 212, 38, esc, offX, offY);
            Mover(txtSenha, 99, 246, 212, 38, esc, offX, offY);
            MoverPos(cboxLembrar, 99, 292, esc, offX, offY);
            MoverPos(llabelEsqueceu, 205, 293, esc, offX, offY);
            Mover(btnEntrar, 99, 326, 212, 40, esc, offX, offY);
            MoverPos(label1, 99, 376, esc, offX, offY);
            MoverPos(llabelCriar, 239, 376, esc, offX, offY);
            Mover(guna2CirclePictureBox1, 390, 128, 319, 209, esc, offX, offY);
            Mover(_lblSlogan, 403, 340, 300, 28, esc, offX, offY);

            if (_btnOlho != null)
                _btnOlho.Location = new Point(txtSenha.Right - txtSenha.Height + 4,
                    txtSenha.Top + (txtSenha.Height - _btnOlho.Height) / 2);

            if (_lblUltimoLogin != null)
                _lblUltimoLogin.Location = new Point(
                    (w - _lblUltimoLogin.Width) / 2,
                    h - (int)(32 * esc));
        }

        private static void Mover(Control c, int bx, int by, int bw, int bh,
            double esc, int offX, int offY)
        {
            c.Location = new Point((int)(bx * esc) + offX, (int)(by * esc) + offY);
            c.Size = new Size((int)(bw * esc), (int)(bh * esc));
        }

        private static void MoverPos(Control c, int bx, int by,
            double esc, int offX, int offY)
        {
            c.Location = new Point((int)(bx * esc) + offX, (int)(by * esc) + offY);
        }

        private void btnEntrar_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string senha = txtSenha.Text;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
            {
                MessageBox.Show("Preencha email e senha.", "Atencao",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!email.ToLowerInvariant().EndsWith("@gmail.com"))
            {
                MessageBox.Show("Use um email @gmail.com para entrar.", "Atencao",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UsuarioService.UsuarioLogado usuario = UsuarioService.Autenticar(email, senha);

            if (usuario == null)
            {
                MessageBox.Show("Email ou senha incorretos.", "Atencao",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SalvarLembrar(email);
            SalvarUltimoLogin(usuario.Nome, email);

            MessageBox.Show("Bem-vindo, " + usuario.Nome + "!", "Tecfy",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            using (var form2 = new Form2(usuario.Id, usuario.Nome))
            {
                this.Hide();
                form2.ShowDialog(this);
                this.Show();
                AtualizarTextoUltimoLogin();
            }
        }

        private void SalvarLembrar(string email)
        {
            try
            {
                if (cboxLembrar.Checked)
                    System.IO.File.WriteAllText(CaminhoLembrar, email);
                else if (System.IO.File.Exists(CaminhoLembrar))
                    System.IO.File.Delete(CaminhoLembrar);
            }
            catch { }
        }

        private void CarregarLembrar()
        {
            try
            {
                if (System.IO.File.Exists(CaminhoLembrar))
                {
                    string email = System.IO.File.ReadAllText(CaminhoLembrar).Trim();
                    if (!string.IsNullOrEmpty(email))
                    {
                        txtEmail.Text = email;
                        cboxLembrar.Checked = true;
                    }
                }
            }
            catch { }
        }

        private void llabelCriar_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var form = new CriarContaForm())
                form.ShowDialog(this);
        }

        private void llabelEsqueceu_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var form = new EsqueciSenhaForm())
                form.ShowDialog(this);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CarregarLembrar();
            AtualizarTextoUltimoLogin();
            ReposicionarResponsivo();
        }

        private void webView21_Click_1(object sender, EventArgs e) { }
    }
}
