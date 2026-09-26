using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form2 : Form
    {
        private string _nomeUsuario;
        private int _avatarId;
        private readonly int _usuarioId;
        private System.Windows.Forms.FlowLayoutPanel pnlResultados;
        private System.Windows.Forms.Timer _timerBusca;
        private string _ultimaBusca = "";
        private string _textoBuscaMusicas = "";
        private string _textoBuscaArtistas = "";
        private bool _campoBuscaArtistas;
        private bool _trocandoContextoBusca;
        private List<SpotifyService.Faixa> _faixasAtuais = new List<SpotifyService.Faixa>();
        private HashSet<int> _idsFavoritos = new HashSet<int>();
        private List<System.Windows.Forms.Panel> _cards = new List<System.Windows.Forms.Panel>();
        private bool _ajustandoCards;
        private ControleInicio _ctlInicio;
        private ControlePlaylists _ctlPlaylists;
        private ControleArtistas _ctlArtistas;
        private System.Windows.Forms.Button btnVoltar;
        private System.Windows.Forms.Button btnDesfazer;
        private System.Collections.Generic.Stack<string> _pilhaVoltar = new System.Collections.Generic.Stack<string>();
        private System.Collections.Generic.Stack<string> _pilhaDesfazer = new System.Collections.Generic.Stack<string>();

        private NAudio.Wave.WaveOutEvent _output;
        private NAudio.Wave.MediaFoundationReader _reader;
        private SpotifyService.Faixa _faixaTocando;
        private System.Windows.Forms.Button btnPlayPause;
        private System.Windows.Forms.Button btnParar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TrackBar trkProgresso;
        private System.Windows.Forms.Label lblTempo;
        private System.Windows.Forms.PictureBox picCapa;
        private System.Windows.Forms.Label lblNomeMusica;
        private System.Windows.Forms.Panel _barraPlayer;
        private System.Windows.Forms.FlowLayoutPanel flpPlaylistsLateral;
        private int _duracaoAtual;             // duracao (segundos) da faixa em reproducao
        private System.Windows.Forms.Timer _timerPosicao;
        private bool _suppressStop;            // evita reset de UI durante troca de faixa
        private string _arquivoTemp;            // caminho do arquivo temporario (preview baixado)
        private bool _carregandoMusica;         // impede cliques duplicados de iniciarem 2 reprodutores
        private int _tocandoToken;              // geracao atual de reproducao (descarta eventos/resultados antigos)
        private System.Windows.Forms.Timer _timerResize; // debounce do redimensionamento
        private Guna.UI2.WinForms.Guna2CirclePictureBox _picAvatar; // avatar com iniciais
        private System.Windows.Forms.ContextMenuStrip _menuPerfil;  // dropdown do avatar
        private ControleEqualizer _eq;     // animacao de equalizador no player
        private float _volume = 0.5f;      // multiplicador de volume (0..1) dos atalhos
        private List<Image> _fotosPerfil;          // imagens da pasta fotoperfil
        private List<string> _fotosPerfilNomes;    // nomes dos arquivos de foto
        private static readonly string CaminhoPerfil = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".",
            "perfil_usuario.json");

        public Form2()
            : this(0, "Usuario")
        {
        }

        public Form2(int usuarioId, string nomeUsuario)
        {
            InitializeComponent();
            _usuarioId = usuarioId;
            _nomeUsuario = nomeUsuario;

            ConfigurarVisualPrincipal();

            CriarPlayer();
            CriarPainelResultados();
            CriarBarraPlayer();
            CriarTimerBusca();
            CriarBotoesNavegacao();
            CriarAbasInicioPlaylists();

            btnInicio.Click += btnInicio_Click;
            btnArtistas.Click += btnArtistas_Click;
            btnPlaylists.Click += btnPlaylists_Click;

            CriarPlaylistsLateral();
            ConfigurarLayoutResponsivo();
            AtualizarEstiloAbas();

            // Lupa da busca vira botao funcional e o Enter executa a pesquisa.
            guna2PictureBox21.BringToFront();
            guna2PictureBox21.Cursor = Cursors.Hand;
            guna2PictureBox21.Click += guna2PictureBox21_Click;
            txtBusca.KeyDown += txtBusca_KeyDown;

            // O icone do coracao tambem abre as favoritas (o botao ja tem seu handler).
            if (guna2PictureBox19 != null)
            {
                guna2PictureBox19.Cursor = Cursors.Hand;
                guna2PictureBox19.Click += btnFavoritas_Click;
                guna2PictureBox19.BringToFront();
            }
        }

        private void ConfigurarVisualPrincipal()
        {
            Text = "Tecfy - Sua música";
            BackgroundImage = null;
            BackColor = Tema.Fundo;

            guna2Panel5.FillColor = Tema.FundoPainel;
            guna2Panel5.BorderRadius = 0;
            guna2Panel5.BorderColor = Color.FromArgb(55, 36, 73);
            guna2Panel5.BorderThickness = 1;
            label5.Text = "SUA BIBLIOTECA";
            label5.ForeColor = Tema.TextoSecundario;
            label5.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

            txtBusca.Location = new Point(190, 83);
            txtBusca.Size = new Size(ClientSize.Width - 220, 40);
            txtBusca.BorderRadius = 18;
            txtBusca.BorderColor = Color.FromArgb(77, 55, 104);
            txtBusca.FocusedState.BorderColor = Tema.Destaque;
            txtBusca.HoverState.BorderColor = Tema.Destaque;
            txtBusca.FillColor = Tema.FundoElevado;
            txtBusca.ForeColor = Tema.Texto;
            txtBusca.PlaceholderForeColor = Tema.TextoSecundario;
            txtBusca.PlaceholderText = "Buscar músicas...";
            txtBusca.Font = new Font("Segoe UI", 10F);
            guna2PictureBox21.Image = DesenharLupa();
            guna2PictureBox21.BackColor = Tema.FundoElevado;

            // A saudacao completa ja esta na home; o topo mostra so o perfil.
            label3.Visible = false;
            label4.Visible = false;
            guna2PictureBox7.Visible = false;
            lblBomdia.AutoSize = false;
            lblBomdia.Size = new Size(260, 30);
            lblBomdia.Location = new Point(ClientSize.Width - 340, 26);
            lblBomdia.TextAlign = ContentAlignment.MiddleRight;
            lblBomdia.AutoEllipsis = true;
            lblBomdia.ForeColor = Tema.Texto;
            lblBomdia.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        }

        private static Bitmap DesenharLupa()
        {
            var imagem = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(imagem))
            using (var caneta = new Pen(Tema.Destaque, 2.4F))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawEllipse(caneta, 3, 3, 13, 13);
                g.DrawLine(caneta, 15, 15, 21, 21);
            }
            return imagem;
        }

        // Organiza o layout para que a janela redimensione sem sobrepor:
        // o painel lateral cresce em altura, a area de conteudo cresce nos dois
        // eixos (mantendo busca/labels no topo) e o player fica embaixo.
        private void ConfigurarLayoutResponsivo()
        {
            // Tamanho minimo para nunca esconder os controles do topo/player.
            MinimumSize = new Size(920, 620);

            if (guna2Panel5 != null)
                guna2Panel5.Dock = DockStyle.Left;

            if (_barraPlayer != null)
                _barraPlayer.Dock = DockStyle.Bottom;

            // Deixa uma margem embaixo da barra do player para nao encostar
            // diretamente na borda da janela.
            if (_barraPlayer != null)
                _barraPlayer.Margin = new Padding(0, 0, 0, 8);

            // A busca estica horizontalmente junto com a janela (mais espaco
            // para digitar termos longos), ancorada a esquerda e direita.
            if (txtBusca != null)
                txtBusca.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            if (guna2PictureBox21 != null)
                guna2PictureBox21.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            if (txtBusca != null)
                txtBusca.Resize += (s, e) => ReposicionarLupaBusca();
            ReposicionarLupaBusca();

            // Bloco de saudacao e sino acompanham a borda direita ao redimensionar.
            if (guna2PictureBox7 != null)
                guna2PictureBox7.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (lblBomdia != null)
                lblBomdia.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (label3 != null)
                label3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            if (label4 != null)
                label4.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Na barra do player, o trackbar tem largura fixa (nao estica) e o tempo
            // fica colado na direita, com limite definido.
            if (trkProgresso != null)
                trkProgresso.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            if (lblTempo != null)
                lblTempo.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            // Evita o "flicker" (tremido) ao redimensionar a janela.
            ReduzirFlicker();

            // Debounce: enquanto o usuario arrasta a borda, o Resize dispara dezenas
            // de vezes por segundo. Aplicar layout a cada evento so faz piscar/travar.
            // So recalculamos depois que a janela para por ~120ms.
            _timerResize = new System.Windows.Forms.Timer { Interval = 120 };
            _timerResize.Tick += (s, e) =>
            {
                _timerResize.Stop();
                RedimensionarAreaConteudo();
            };

            Resize += (s, e) =>
            {
                _timerResize.Stop();
                _timerResize.Start();
            };

            // Aplica o layout inicial de uma vez (sem debounce) ao abrir.
            RedimensionarAreaConteudo();
        }

        // Habilita double buffering nos controles principais para que o desenho
        // do resize nao produza "tremor"/flicker.
        private void ReduzirFlicker()
        {
            var alvos = new System.Windows.Forms.Control[]
            {
                this, guna2Panel5, _barraPlayer, pnlResultados,
                _ctlInicio, _ctlPlaylists, _ctlArtistas,
                flpPlaylistsLateral, txtBusca, guna2PictureBox7
            };

            foreach (var alvo in alvos)
            {
                if (alvo == null)
                    continue;

                try
                {
                    var prop = typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    prop?.SetValue(alvo, true, null);
                }
                catch
                {
                }
            }
        }

        // Mantem a lupa sempre colada no lado direito da barra de busca,
        // que agora estica junto com a janela.
        private void ReposicionarLupaBusca()
        {
            if (txtBusca == null || guna2PictureBox21 == null)
                return;

            guna2PictureBox21.Location = new System.Drawing.Point(
                txtBusca.Right - guna2PictureBox21.Width - 6,
                txtBusca.Top + (txtBusca.Height - guna2PictureBox21.Height) / 2);
        }

        // Cria o painel do painel lateral que lista as playlists do usuario,
        // abaixo de "Suas Favoritas". Cada playlist vira um botao que, ao ser
        // clicado, abre a pagina de playlists e seleciona a lista escolhida.
        private void CriarPlaylistsLateral()
        {
            if (guna2Panel5 == null || _ctlPlaylists == null)
                return;

            flpPlaylistsLateral = new System.Windows.Forms.FlowLayoutPanel
            {
                Location = new System.Drawing.Point(10, 392),
                Size = new System.Drawing.Size(131, 200),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left
                    | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = System.Windows.Forms.FlowDirection.TopDown,
                Padding = new System.Windows.Forms.Padding(0, 2, 0, 2),
                BackColor = System.Drawing.Color.Transparent
            };
            guna2Panel5.Controls.Add(flpPlaylistsLateral);
            flpPlaylistsLateral.Visible = true;

            // Rotulo acima dos botoes de playlist do painel lateral.
            var lblMinhasPlaylists = new System.Windows.Forms.Label
            {
                Text = "Minhas Playlists",
                Location = new System.Drawing.Point(10, 372),
                Size = new System.Drawing.Size(131, 16),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                ForeColor = System.Drawing.Color.FromArgb(168, 85, 247),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.Transparent
            };
            guna2Panel5.Controls.Add(lblMinhasPlaylists);
        }

        // Recarrega os botoes de playlist do painel lateral (apos criar, renomear ou excluir).
        public void AtualizarPlaylistsLateral()
        {
            if (flpPlaylistsLateral == null)
                return;

            flpPlaylistsLateral.Controls.Clear();

            if (_usuarioId <= 0)
                return;

            System.Data.DataTable tabela = null;
            try
            {
                tabela = PlaylistDAO.ListarComResumo(_usuarioId);
            }
            catch
            {
            }

            if (tabela == null || tabela.Rows.Count == 0)
            {
                var vazio = new System.Windows.Forms.Label
                {
                    Text = "Sem playlists",
                    ForeColor = System.Drawing.Color.Gray,
                    Font = new System.Drawing.Font("Segoe UI", 8F),
                    AutoSize = true,
                    Padding = new System.Windows.Forms.Padding(2, 6, 0, 6)
                };
                flpPlaylistsLateral.Controls.Add(vazio);
                return;
            }

            foreach (System.Data.DataRow linha in tabela.Rows)
            {
                int id = System.Convert.ToInt32(linha["Id"]);
                string nome = System.Convert.ToString(linha["Nome"]);
                int quantidade = linha["QuantidadeMusicas"] == System.DBNull.Value
                    ? 0
                    : System.Convert.ToInt32(linha["QuantidadeMusicas"]);
                string capaUrl = linha["CapaUrl"] == System.DBNull.Value
                    ? null
                    : System.Convert.ToString(linha["CapaUrl"]);

                flpPlaylistsLateral.Controls.Add(CriarCardPlaylistLateral(id, nome, quantidade, capaUrl));
            }
        }

        // Cria um card clicavel no painel lateral com nome, capa (ou icone) e
        // quantidade de musicas da playlist. Ao clicar, abre a pagina de playlists
        // ja com a lista selecionada.
        private System.Windows.Forms.Control CriarCardPlaylistLateral(int id, string nome, int quantidade, string capaUrl)
        {
            var card = new System.Windows.Forms.Panel
            {
                Width = 131,
                Height = 68,
                Margin = new System.Windows.Forms.Padding(0, 3, 0, 3),
                Padding = new System.Windows.Forms.Padding(4),
                BackColor = System.Drawing.Color.FromArgb(28, 16, 42),
                Tag = id,
                Cursor = System.Windows.Forms.Cursors.Hand
            };

            var picCapa = new System.Windows.Forms.PictureBox
            {
                Location = new System.Drawing.Point(4, 4),
                Size = new System.Drawing.Size(60, 60),
                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                BackColor = System.Drawing.Color.FromArgb(45, 20, 65),
                Image = DesenharIconePlaylist(),
                Tag = id,
                Cursor = System.Windows.Forms.Cursors.Hand
            };
            Tema.Arredondar(picCapa, 10);

            if (!string.IsNullOrWhiteSpace(capaUrl))
            {
                try { picCapa.LoadAsync(capaUrl); } catch { }
            }

            var lblNome = new System.Windows.Forms.Label
            {
                Text = nome,
                Location = new System.Drawing.Point(68, 10),
                Size = new System.Drawing.Size(59, 22),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.Transparent,
                Tag = id,
                Cursor = System.Windows.Forms.Cursors.Hand
            };

            var lblQtd = new System.Windows.Forms.Label
            {
                Text = quantidade + " musica" + (quantidade == 1 ? "" : "s"),
                Location = new System.Drawing.Point(68, 34),
                Size = new System.Drawing.Size(59, 16),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                ForeColor = System.Drawing.Color.Silver,
                Font = new System.Drawing.Font("Segoe UI", 8.5F),
                BackColor = System.Drawing.Color.Transparent,
                Tag = id,
                Cursor = System.Windows.Forms.Cursors.Hand
            };

            // Clicar em qualquer parte do card abre a playlist.
            card.Click += BtnPlaylistLateral_Click;
            picCapa.Click += BtnPlaylistLateral_Click;
            lblNome.Click += BtnPlaylistLateral_Click;
            lblQtd.Click += BtnPlaylistLateral_Click;

            card.Controls.Add(picCapa);
            card.Controls.Add(lblNome);
            card.Controls.Add(lblQtd);
            return card;
        }

        private System.Drawing.Bitmap DesenharIconePlaylist()
        {
            var bmp = new System.Drawing.Bitmap(50, 50);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.FromArgb(45, 20, 65));

                using (var pincel = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(168, 85, 247)))
                {
                    g.FillRectangle(pincel, 14, 14, 22, 22);
                }

                using (var pincel = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(45, 20, 65)))
                {
                    g.FillRectangle(pincel, 24, 22, 4, 4);
                    g.FillRectangle(pincel, 20, 26, 4, 4);
                }
            }
            return bmp;
        }

        private void BtnPlaylistLateral_Click(object sender, EventArgs e)
        {
            int idPlaylist;
            if (sender is System.Windows.Forms.Control ctl && ctl.Tag is int)
            {
                idPlaylist = (int)ctl.Tag;
            }
            else
            {
                return;
            }

            MostrarAbaPlaylists();
            RedimensionarAreaConteudo();
            _ctlPlaylists.AbrirPlaylist(idPlaylist);
        }

        // Reposiciona/redimensiona a area de conteudo (aba ativa) conforme a janela.
        private void RedimensionarAreaConteudo()
        {
            const int x = 190;      // apos a sidebar (151) + margem
            const int y = 150;      // abaixo da busca
            const int margem = 10;  // distancia ate a borda direita/inferior

            int largura = Math.Max(320, ClientSize.Width - x - margem);
            int alturaJogo = ClientSize.Height - 150 - 90 - margem; // 150 (topo) + 90 (player de 80 + margem)
            int altura = Math.Max(180, alturaJogo);

            var tamanho = new Size(largura, altura);

            SuspendLayout();
            try
            {
                if (pnlResultados != null) { pnlResultados.Location = new Point(x, y); pnlResultados.Size = tamanho; }
                if (_ctlInicio != null) { _ctlInicio.Location = new Point(x, y); _ctlInicio.Size = tamanho; }
                if (_ctlPlaylists != null) { _ctlPlaylists.Location = new Point(x, y); _ctlPlaylists.Size = tamanho; }
                if (_ctlArtistas != null) { _ctlArtistas.Location = new Point(x, y); _ctlArtistas.Size = tamanho; }
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private void btnInicio_Click(object sender, EventArgs e)
        {
            _timerBusca.Stop();
            RedimensionarAreaConteudo();
            MostrarAbaInicio();
        }

        private void btnPlaylists_Click(object sender, EventArgs e)
        {
            _timerBusca.Stop();
            RedimensionarAreaConteudo();
            MostrarAbaPlaylists();
        }

        private void btnArtistas_Click(object sender, EventArgs e)
        {
            _timerBusca.Stop();
            RedimensionarAreaConteudo();
            MostrarAbaArtistas();
            _ctlArtistas.ExibirLista();
        }

        // Marca visualmente a aba ativa e "apaga" as demais, com uma transicao
        // suave de cor (preenchimento roxo na ativa, transparente nas inativas).
        private void AtualizarEstiloAbas()
        {
            // Guarda os botoes a cada chamada (criados no Designer).
            MarcarAbaAtiva(btnInicio, AbaInicioAtiva);
            MarcarAbaAtiva(btnArtistas, AbaArtistasAtiva);
            MarcarAbaAtiva(btnPlaylists, AbaPlaylistsAtiva);
            AtualizarContextoBusca();
        }

        private void AtualizarContextoBusca()
        {
            if (_campoBuscaArtistas != AbaArtistasAtiva)
            {
                _timerBusca.Stop();
                if (_campoBuscaArtistas)
                    _textoBuscaArtistas = txtBusca.Text;
                else
                    _textoBuscaMusicas = txtBusca.Text;

                _campoBuscaArtistas = AbaArtistasAtiva;
                _trocandoContextoBusca = true;
                try
                {
                    txtBusca.Text = _campoBuscaArtistas ? _textoBuscaArtistas : _textoBuscaMusicas;
                }
                finally
                {
                    _trocandoContextoBusca = false;
                }
            }

            txtBusca.PlaceholderText = _campoBuscaArtistas
                ? "Buscar artistas..." : "Buscar músicas...";
        }

        private bool AbaInicioAtiva = true;
        private bool AbaArtistasAtiva = false;
        private bool AbaPlaylistsAtiva = false;

        private void MarcarAbaAtiva(Guna.UI2.WinForms.Guna2Button botao, bool ativa)
        {
            if (botao == null)
                return;

            Color corAlvo = ativa
                ? Color.FromArgb(124, 58, 237)
                : Color.FromArgb(28, 16, 42);

            if (ativa)
            {
                botao.FillColor = corAlvo;
                botao.ForeColor = Color.White;
                botao.BorderColor = Color.FromArgb(168, 85, 247);
                botao.BorderThickness = 1;
            }
            else
            {
                botao.FillColor = corAlvo;
                botao.ForeColor = Color.FromArgb(200, 190, 230);
                botao.BorderColor = Color.Transparent;
                botao.BorderThickness = 0;
            }
        }

        // Faz uma aba "entrar" com leve crescimento vertical + opacidade,
// SEM mover o Top (que ficaria acumulando e empurrando a tela para baixo).
private void AnimarEntradaAba(Control aba)
        {
            if (aba == null || !aba.Visible)
                return;

            int alturaAlvo = aba.Height;
            int topoAlvo = aba.Top;
            aba.Height = Math.Max(10, (int)(alturaAlvo * 0.92));
            aba.Top = topoAlvo;

            var timer = new System.Windows.Forms.Timer { Interval = 12, Tag = aba };
            timer.Tick += (s, e2) =>
            {
                aba.Height = Math.Min(alturaAlvo, aba.Height + 8);
                aba.Top = topoAlvo;

                if (aba.Height >= alturaAlvo)
                {
                    aba.Height = alturaAlvo;
                    aba.Top = topoAlvo;
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private void MostrarAbaInicio()
        {
            RegistrarNavegacao("inicio");
            pnlResultados.Visible = false;
            _ctlPlaylists.Visible = false;
            _ctlArtistas.Visible = false;
            AbaInicioAtiva = true;
            AbaArtistasAtiva = false;
            AbaPlaylistsAtiva = false;
            AtualizarEstiloAbas();
            if (_ctlInicio.Visible == false)
            {
                _ctlInicio.Atualizar();
                _ctlInicio.Visible = true;
            }
            _ctlInicio.BringToFront();
            AnimarEntradaAba(_ctlInicio);
        }

        private void MostrarAbaPlaylists()
        {
            RegistrarNavegacao("playlists");
            pnlResultados.Visible = false;
            _ctlInicio.Visible = false;
            _ctlArtistas.Visible = false;
            AbaInicioAtiva = false;
            AbaArtistasAtiva = false;
            AbaPlaylistsAtiva = true;
            AtualizarEstiloAbas();
            if (_ctlPlaylists.Visible == false)
            {
                _ctlPlaylists.Atualizar();
                _ctlPlaylists.Visible = true;
            }
            _ctlPlaylists.BringToFront();
            AnimarEntradaAba(_ctlPlaylists);
        }

        private void MostrarAbaArtistas()
        {
            RegistrarNavegacao("artistas");
            pnlResultados.Visible = false;
            _ctlInicio.Visible = false;
            _ctlPlaylists.Visible = false;
            AbaInicioAtiva = false;
            AbaArtistasAtiva = true;
            AbaPlaylistsAtiva = false;
            AtualizarEstiloAbas();
            _ctlArtistas.Visible = true;
            _ctlArtistas.BringToFront();
            AnimarEntradaAba(_ctlArtistas);
        }

        private void MostrarResultadosBusca()
        {
            RegistrarNavegacao("busca");
            // Ao pesquisar, volta a exibir a area de resultados e esconde as abas.
            _ctlInicio.Visible = false;
            _ctlPlaylists.Visible = false;
            _ctlArtistas.Visible = false;
            AbaInicioAtiva = false;
            AbaArtistasAtiva = false;
            AbaPlaylistsAtiva = false;
            AtualizarEstiloAbas();
            pnlResultados.Visible = true;
            pnlResultados.BringToFront();
        }

        private void CriarAbasInicioPlaylists()
        {
            // Hospeda as abas na mesmas regiao do painel de resultados,
            // respeitando o tamanho/posicao do conteudo (243,150 + 530x230).
            var local = pnlResultados.Location;
            var tamanho = pnlResultados.Size;

            _ctlInicio = new ControleInicio(_usuarioId, _nomeUsuario)
            {
                Location = local,
                Size = tamanho,
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left
                    | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom
            };

            _ctlPlaylists = new ControlePlaylists(_usuarioId)
            {
                Location = local,
                Size = tamanho,
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left
                    | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom
            };
            _ctlPlaylists.FaixaSolicitada += OnPlaylistFaixaSolicitada;
            _ctlPlaylists.PlaylistsAlteradas += () => AtualizarPlaylistsLateral();

            _ctlArtistas = new ControleArtistas(_usuarioId)
            {
                Location = local,
                Size = tamanho,
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left
                    | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom
            };
            _ctlArtistas.MusicaSolicitada += OnPlaylistFaixaSolicitada;

            // Cards de destaque da home: abrir artista ou tocar um album.
            _ctlInicio.ArtistaSolicitado += art =>
            {
                MostrarAbaArtistas();
                _ctlArtistas.ExibirArtista(art);
            };
            _ctlInicio.ExplorarArtistasSolicitado += () =>
            {
                MostrarAbaArtistas();
                _ctlArtistas.ExibirLista();
            };
            _ctlInicio.AlbumSolicitado += OnAlbumDestaqueSolicitado;

            Controls.Add(_ctlInicio);
            Controls.Add(_ctlPlaylists);
            Controls.Add(_ctlArtistas);

            // Esconde as abas; a area de resultados continua sendo a padrao.
            _ctlInicio.Visible = false;
            _ctlPlaylists.Visible = false;
            _ctlArtistas.Visible = false;
        }

        private void OnPlaylistFaixaSolicitada(SpotifyService.Faixa faixa)
        {
            _faixaTocando = faixa;
            BeginInvoke((Action)delegate { Tocar(_faixaTocando); });
        }

        // Clicou num album em destaque na home: busca as faixas desse album na
        // iTunes e toca a primeira encontrada.
        private async void OnAlbumDestaqueSolicitado(string termoAlbum)
        {
            lblStatus.Text = "Buscando album: " + termoAlbum + "...";

            List<SpotifyService.Faixa> faixas = null;
            try { faixas = await SpotifyService.BuscarFaixasItunesAsync(termoAlbum, 6); }
            catch { }

            if (faixas == null || faixas.Count == 0)
            {
                lblStatus.Text = "Nenhuma musica encontrada para o album selecionado.";
                return;
            }

            OnPlaylistFaixaSolicitada(faixas[0]);
        }

        private void CriarPlayer()
        {
            _output = new NAudio.Wave.WaveOutEvent();
            _output.Volume = 0.5f;
            _output.PlaybackStopped += Output_PlaybackStopped;
        }

        private void CriarPainelResultados()
        {
            pnlResultados = new System.Windows.Forms.FlowLayoutPanel
            {
                Location = new System.Drawing.Point(243, 150),
                Size = new System.Drawing.Size(660, 330),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left
                    | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom,
                BackColor = System.Drawing.Color.FromArgb(13, 7, 20),
                AutoScroll = true,
                FlowDirection = System.Windows.Forms.FlowDirection.TopDown,
                WrapContents = false,
                Padding = new System.Windows.Forms.Padding(6)
            };
            Controls.Add(pnlResultados);
            pnlResultados.BringToFront();
            pnlResultados.Resize += (s, e) => RedimensionarCards();
        }

        private void RedimensionarCards()
        {
            if (pnlResultados == null || _ajustandoCards)
                return;

            _ajustandoCards = true;
            try
            {
                int largura = LarguraCardResultados();
                pnlResultados.SuspendLayout();
                foreach (var card in _cards)
                {
                    if (card.Width != largura)
                        card.Width = largura;
                }
                pnlResultados.ResumeLayout(true);
                pnlResultados.PerformLayout();
            }
            finally
            {
                _ajustandoCards = false;
            }
        }

        private int LarguraCardResultados()
        {
            // Deixa espaco para a barra vertical e para a margem do card.
            return Math.Max(260, pnlResultados.ClientSize.Width
                - pnlResultados.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 8);
        }

        private void CriarBotoesNavegacao()
        {
            // Botoes circulares de voltar/avancar no topo da area de conteudo,
            // como no Spotify. Usam pilhas para andar para tras e para frente
            // entre as abas visitadas (inicio, playlists, artistas e busca).
            btnVoltar = CriarBotaoNavegacao("‹", 190, BtnVoltar_Click);
            btnDesfazer = CriarBotaoNavegacao("›", 230, BtnDesfazer_Click);

            Controls.Add(btnVoltar);
            Controls.Add(btnDesfazer);
            btnVoltar.BringToFront();
            btnDesfazer.BringToFront();
            AtualizarEstadoNavegacao();
        }

        private System.Windows.Forms.Button CriarBotaoNavegacao(string texto, int x, EventHandler clique)
        {
            var botao = new System.Windows.Forms.Button
            {
                Text = texto,
                Location = new System.Drawing.Point(x, 25),
                Size = new System.Drawing.Size(34, 34),
                BackColor = System.Drawing.Color.FromArgb(45, 20, 65),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Cursor = System.Windows.Forms.Cursors.Hand
            };
            botao.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(botao, 17);
            botao.Click += clique;
            return botao;
        }

        // Registra a aba atual na pilha de navegacao (sem duplicar a mesma seguida).
        private void RegistrarNavegacao(string visao)
        {
            if (_pilhaVoltar.Count > 0)
            {
                string topo = _pilhaVoltar.Peek();
                if (topo == visao)
                    return;
            }

            _pilhaVoltar.Push(visao);
            _pilhaDesfazer.Clear();
            AtualizarEstadoNavegacao();
        }

        private void BtnVoltar_Click(object sender, EventArgs e)
        {
            if (_pilhaVoltar.Count == 0)
                return;

            string atual = _pilhaVoltar.Pop();
            _pilhaDesfazer.Push(atual);
            AplicarVisao(_pilhaVoltar.Peek());
            AtualizarEstadoNavegacao();
        }

        private void BtnDesfazer_Click(object sender, EventArgs e)
        {
            if (_pilhaDesfazer.Count == 0)
                return;

            string proxima = _pilhaDesfazer.Pop();
            _pilhaVoltar.Push(proxima);
            AplicarVisao(proxima);
            AtualizarEstadoNavegacao();
        }

        private void AtualizarEstadoNavegacao()
        {
            if (btnVoltar == null || btnDesfazer == null)
                return;

            btnVoltar.Enabled = _pilhaVoltar.Count > 1;
            btnDesfazer.Enabled = _pilhaDesfazer.Count > 0;
        }

        private void AplicarVisao(string visao)
        {
            RedimensionarAreaConteudo();
            switch (visao)
            {
                case "inicio":
                    MostrarAbaInicio();
                    break;
                case "playlists":
                    MostrarAbaPlaylists();
                    break;
                case "artistas":
                    MostrarAbaArtistas();
                    break;
                case "busca":
                    MostrarResultadosBusca();
                    break;
            }
        }

        private void CriarTimerBusca()
        {
            _timerBusca = new System.Windows.Forms.Timer { Interval = 500 };
            _timerBusca.Tick += TimerBusca_Tick;
        }

        private void CriarBarraPlayer()
        {
            // Barra fixada na parte de baixo do formulario (Dock)
            // para nao ser reposicionada pelo AutoScale do form.
            var barra = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Bottom,
                Height = 80,
                BackColor = Tema.FundoPainel
            };
            _barraPlayer = barra;
            barra.Paint += (s, e) =>
            {
                using (var borda = new Pen(Color.FromArgb(65, 44, 84)))
                    e.Graphics.DrawLine(borda, 0, 0, barra.Width, 0);
            };

            trkProgresso = new System.Windows.Forms.TrackBar
            {
                Location = new System.Drawing.Point(480, 14),
                Size = new System.Drawing.Size(300, 20),
                Maximum = 100,
                TickStyle = System.Windows.Forms.TickStyle.None,
                BackColor = System.Drawing.Color.FromArgb(28, 16, 42)
            };
            trkProgresso.Enabled = false;

            lblTempo = new System.Windows.Forms.Label
            {
                Text = "0:00 / 0:00",
                ForeColor = Tema.TextoSecundario,
                Font = new System.Drawing.Font("Segoe UI", 8.25F),
                Location = new System.Drawing.Point(790, 14),
                Size = new Size(82, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Capa da faixa em reproducao.
            picCapa = new System.Windows.Forms.PictureBox
            {
                Location = new System.Drawing.Point(170, 4),
                Size = new System.Drawing.Size(72, 72),
                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                BackColor = System.Drawing.Color.FromArgb(45, 20, 65),
                Image = DesenharCapaVazia()
            };
            Tema.Arredondar(picCapa, 11);

            // Animacao de equalizador ao lado do nome da musica (visivel
            // somente enquanto uma faixa esta sendo reproduzida).
            _eq = new ControleEqualizer
            {
                Location = new System.Drawing.Point(232, 18),
                Size = new System.Drawing.Size(28, 24),
                BackColor = System.Drawing.Color.Transparent,
                Visible = false
            };

            // Nome e artista da faixa.
            lblNomeMusica = new System.Windows.Forms.Label
            {
                Text = "Nenhuma musica",
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(268, 12),
                AutoSize = true,
                MaximumSize = new System.Drawing.Size(200, 20),
                AutoEllipsis = true
            };

            lblStatus = new System.Windows.Forms.Label
            {
                Text = "Nenhuma musica tocando",
                ForeColor = System.Drawing.Color.FromArgb(168, 85, 247),
                Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(268, 36),
                AutoSize = true,
                MaximumSize = new System.Drawing.Size(200, 18)
            };

            btnPlayPause = new System.Windows.Forms.Button
            {
                Text = "▶",
                Location = new System.Drawing.Point(8, 20),
                Size = new System.Drawing.Size(90, 28),
                BackColor = System.Drawing.Color.FromArgb(124, 58, 237),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnPlayPause.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnPlayPause, 14);
            btnPlayPause.Click += BtnPlayPause_Click;

            btnParar = new System.Windows.Forms.Button
            {
                Text = "■ Parar",
                Location = new System.Drawing.Point(103, 20),
                Size = new System.Drawing.Size(50, 28),
                BackColor = System.Drawing.Color.FromArgb(45, 20, 65),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnParar.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnParar, 14);
            btnParar.Click += BtnParar_Click;

            _timerPosicao = new System.Windows.Forms.Timer { Interval = 500 };
            _timerPosicao.Tick += TimerPosicao_Tick;

            barra.Controls.Add(trkProgresso);
            barra.Controls.Add(lblTempo);
            barra.Controls.Add(picCapa);
            barra.Controls.Add(lblNomeMusica);
            barra.Controls.Add(btnPlayPause);
            barra.Controls.Add(btnParar);
            barra.Controls.Add(lblStatus);
            barra.Controls.Add(_eq);
            Controls.Add(barra);
            barra.BringToFront();
            barra.Resize += (s, e) => AjustarBarraPlayer();
            AjustarBarraPlayer();
        }

        private void AjustarBarraPlayer()
        {
            if (_barraPlayer == null || trkProgresso == null || lblTempo == null)
                return;

            int inicio = trkProgresso.Left;
            trkProgresso.Width = Math.Max(100, _barraPlayer.ClientSize.Width - inicio - 100);
            lblTempo.Left = trkProgresso.Right + 8;
        }

        // Mostra/esconde e anima o equalizador conforme o estado do player.
        private void AtualizarEqualizer()
        {
            if (_eq == null)
                return;

            bool tocando = _faixaTocando != null
                && _output != null
                && _output.PlaybackState == NAudio.Wave.PlaybackState.Playing;

            if (tocando)
            {
                _eq.Visible = true;
                _eq.Iniciar();
            }
            else
            {
                _eq.Parar();
                _eq.Visible = false;
            }
        }

        private void AdicionarAviso(string mensagem)
        {
            pnlResultados.Controls.Clear();
            _cards.Clear();

            var lbl = new System.Windows.Forms.Label
            {
                Text = mensagem,
                ForeColor = System.Drawing.Color.White,
                AutoSize = true,
                Padding = new System.Windows.Forms.Padding(8),
                Font = new System.Drawing.Font("Segoe UI", 9.75F)
            };
            pnlResultados.Controls.Add(lbl);
        }

        private string FormatarDuracao(int segundos)
        {
            if (segundos <= 0)
                return "0:00";

            int min = segundos / 60;
            int sec = segundos % 60;
            return min + ":" + sec.ToString("00");
        }

        private void CriarCard(SpotifyService.Faixa faixa)
        {
            var card = new System.Windows.Forms.Panel
            {
                Width = LarguraCardResultados(),
                Height = 94,
                Margin = new System.Windows.Forms.Padding(3),
                BackColor = System.Drawing.Color.FromArgb(28, 16, 42),
                Padding = new System.Windows.Forms.Padding(6)
            };
            Tema.Arredondar(card, 14);

            var picCapa = new System.Windows.Forms.PictureBox
            {
                Location = new System.Drawing.Point(6, 5),
                Size = new System.Drawing.Size(84, 84),
                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                BackColor = System.Drawing.Color.FromArgb(13, 7, 20)
            };
            Tema.Arredondar(picCapa, 12);

            if (!string.IsNullOrWhiteSpace(faixa.ImagemUrl))
            {
                try { picCapa.LoadAsync(faixa.ImagemUrl); }
                catch { }
            }

            var lblNome = new System.Windows.Forms.Label
            {
                Text = faixa.Nome,
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold),
                Location = new System.Drawing.Point(100, 16),
                AutoSize = true,
                MaximumSize = new System.Drawing.Size(220, 60),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left
                    | System.Windows.Forms.AnchorStyles.Right
            };

            var lblArtista = new System.Windows.Forms.Label
            {
                Text = faixa.Artistas,
                ForeColor = System.Drawing.Color.Silver,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                Location = new System.Drawing.Point(100, 47),
                AutoSize = true,
                MaximumSize = new System.Drawing.Size(220, 30),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left
                    | System.Windows.Forms.AnchorStyles.Right
            };

            var lblDuracao = new System.Windows.Forms.Label
            {
                Text = FormatarDuracao(faixa.DuracaoSegundos),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Location = new System.Drawing.Point(330, 24),
                Size = new System.Drawing.Size(42, 22),
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right
            };

            // Distancia fixa do conjunto de botoes ate a borda direita do card.
            const int margemDireita = 3;

            var btnPlay = new System.Windows.Forms.Button
            {
                Text = "▶",
                Size = new System.Drawing.Size(34, 34),
                BackColor = System.Drawing.Color.FromArgb(124, 58, 237),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Tag = faixa,
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right
            };
            btnPlay.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnPlay, 17);
            btnPlay.Click += BtnPlay_Click;

            bool jaFavorita = faixa.MusicaId.HasValue && _idsFavoritos.Contains(faixa.MusicaId.Value);

            var btnFav = new System.Windows.Forms.Button
            {
                Text = jaFavorita ? "♥" : "♡",
                Size = new System.Drawing.Size(40, 34),
                BackColor = System.Drawing.Color.FromArgb(45, 20, 65),
                ForeColor = jaFavorita ? System.Drawing.Color.HotPink : System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 13F),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Tag = faixa,
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right
            };
            btnFav.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnFav, 14);
            btnFav.Click += BtnFav_Click;

            var btnAddPlaylist = new System.Windows.Forms.Button
            {
                Text = "＋",
                Size = new System.Drawing.Size(43, 34),
                BackColor = System.Drawing.Color.FromArgb(45, 20, 65),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
                FlatStyle = System.Windows.Forms.FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Tag = faixa,
                Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right
            };
            btnAddPlaylist.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnAddPlaylist, 17);
            btnAddPlaylist.Click += BtnAddPlaylist_Click;

            // Posiciona os botoes relativos ao card (na borda direita) e
            // a duracao a esquerda do botao play.
            btnAddPlaylist.Location = new System.Drawing.Point(card.Width - margemDireita - 43, 30);
            btnFav.Location = new System.Drawing.Point(btnAddPlaylist.Left - 40 - 2, 30);
            btnPlay.Location = new System.Drawing.Point(btnFav.Left - 34 - 2, 30);
            lblDuracao.Location = new System.Drawing.Point(btnPlay.Left - 42 - 6, 36);

            card.Controls.Add(picCapa);
            card.Controls.Add(lblNome);
            card.Controls.Add(lblArtista);
            card.Controls.Add(lblDuracao);
            card.Controls.Add(btnPlay);
            card.Controls.Add(btnFav);
            card.Controls.Add(btnAddPlaylist);

            pnlResultados.Controls.Add(card);
            _cards.Add(card);
            card.Resize += (s, e) => AtualizarPosicoesCard(card, lblDuracao, btnPlay, btnFav, btnAddPlaylist);
        }

        private void AtualizarPosicoesCard(System.Windows.Forms.Control card,
            System.Windows.Forms.Label lblDuracao,
            System.Windows.Forms.Control btnPlay,
            System.Windows.Forms.Control btnFav,
            System.Windows.Forms.Control btnAddPlaylist)
        {
            const int margemDireita = 3;
            btnAddPlaylist.Location = new System.Drawing.Point(card.Width - margemDireita - btnAddPlaylist.Width, 30);
            btnFav.Location = new System.Drawing.Point(btnAddPlaylist.Left - btnFav.Width - 2, 30);
            btnPlay.Location = new System.Drawing.Point(btnFav.Left - btnPlay.Width - 2, 30);
            lblDuracao.Location = new System.Drawing.Point(btnPlay.Left - lblDuracao.Width - 6, 36);
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            var btn = sender as System.Windows.Forms.Button;
            var faixa = btn?.Tag as SpotifyService.Faixa;

            if (faixa == null)
                return;

            Tocar(faixa);
        }

        private async void BtnFav_Click(object sender, EventArgs e)
        {
            var btn = sender as System.Windows.Forms.Button;
            var faixa = btn?.Tag as SpotifyService.Faixa;

            if (faixa == null || _usuarioId <= 0)
            {
                lblStatus.Text = "Faca login para favoritar musicas.";
                return;
            }

            bool jaFavorita = faixa.MusicaId.HasValue && _idsFavoritos.Contains(faixa.MusicaId.Value);

            if (jaFavorita)
            {
                var remover = FavoritoDAO.Remover(_usuarioId, faixa.MusicaId.Value);
                if (remover.Ok)
                {
                    _idsFavoritos.Remove(faixa.MusicaId.Value);
                    btn.Text = "♡";
                    btn.ForeColor = System.Drawing.Color.White;
                    lblStatus.Text = "Removida das favoritas: " + faixa.Nome;
                    AtualizarContadorFavoritas();
                }
                else
                {
                    lblStatus.Text = remover.Erro;
                }
                return;
            }

            lblStatus.Text = "Favoritando: " + faixa.Nome + "...";

            var idMusica = await SalvarMusicaNoBanco(faixa);
            if (idMusica <= 0)
            {
                lblStatus.Text = "Erro ao salvar musica.";
                return;
            }

            var favoritar = FavoritoDAO.Adicionar(_usuarioId, idMusica);
            if (favoritar.Ok)
            {
                faixa.MusicaId = idMusica;
                _idsFavoritos.Add(idMusica);
                btn.Text = "♥";
                btn.ForeColor = System.Drawing.Color.HotPink;
                lblStatus.Text = "Adicionada as favoritas: " + faixa.Nome;
                AtualizarContadorFavoritas();
            }
            else
            {
                lblStatus.Text = favoritar.Erro;
            }
        }

        private async void BtnAddPlaylist_Click(object sender, EventArgs e)
        {
            var btn = sender as System.Windows.Forms.Button;
            var faixa = btn?.Tag as SpotifyService.Faixa;

            if (faixa == null || _usuarioId <= 0)
            {
                lblStatus.Text = "Faca login para adicionar a playlist.";
                return;
            }

            using (var escolher = new FormEscolherPlaylist(_usuarioId, faixa.Nome))
            {
                if (escolher.ShowDialog(this) != DialogResult.OK)
                {
                    // O usuario pode ter criado uma playlist dentro do dialogo
                    // e depois cancelado: reflete a mudanca nas listas do app.
                    _ctlPlaylists.Atualizar();
                    AtualizarPlaylistsLateral();
                    return;
                }

                lblStatus.Text = "Adicionando: " + faixa.Nome + "...";

                var idMusica = await SalvarMusicaNoBanco(faixa);
                if (idMusica <= 0)
                {
                    lblStatus.Text = "Erro ao salvar musica.";
                    return;
                }

                var resultado = PlaylistDAO.AdicionarMusica(escolher.PlaylistEscolhida, idMusica);
                if (resultado.Ok)
                {
                    lblStatus.Text = "Adicionada a playlist \"" + escolher.NomePlaylistEscolhida
                        + "\": " + faixa.Nome;
                    _ctlPlaylists.Atualizar();
                    AtualizarPlaylistsLateral();
                }
                else
                {
                    lblStatus.Text = resultado.Erro;
                }
            }
        }

        private async System.Threading.Tasks.Task<int> SalvarMusicaNoBanco(SpotifyService.Faixa faixa)
        {
            int idArtista = await System.Threading.Tasks.Task.Run(() => ArtistaDAO.ObterOuInserir(faixa.Artistas));
            if (idArtista <= 0)
                return -1;

            int idMusica = await System.Threading.Tasks.Task.Run(() => MusicaDAO.ObterOuInserir(faixa, idArtista));
            if (idMusica <= 0)
                return -1;

            faixa.MusicaId = idMusica;
            return idMusica;
        }

        private async void Tocar(SpotifyService.Faixa faixa)
        {
            // Impede que cliques repetidos abram 2 reprodutores ao mesmo tempo.
            if (_carregandoMusica)
                return;

            _carregandoMusica = true;
            try
            {
                // Incrementa a geracao atual: qualquer evento/resultado de uma
                // reproducao anterior passa a ser ignorado ao trocar de musica.
                int token = ++_tocandoToken;

                // Para imediatamente a reproducao anterior para dar resposta
                // rapida ao usuario e liberar os recursos de audio.
                _suppressStop = true;
                PararReproducao();
                _suppressStop = false;

                btnPlayPause.Text = "▶";
                trkProgresso.Enabled = false;
                trkProgresso.Value = 0;
                AtualizarTempo(0, 0);
                lblStatus.Text = "Carregando: " + faixa.Nome + "...";

                string preview = faixa.PreviewUrl;
                bool previewItunes = false;

                // Spotify descontinuou o preview_url para a maioria das musicas.
                // Busca um preview (30s) na iTunes Search API quando nao ha preview.
                if (string.IsNullOrWhiteSpace(preview))
                {
                    lblStatus.Text = "Buscando preview: " + faixa.Nome + "...";
                    preview = await SpotifyService.BuscarPreviaItunesAsync(faixa.Nome, faixa.Artistas);

                    if (string.IsNullOrWhiteSpace(preview))
                    {
                        lblStatus.Text = "Sem preview disponivel para esta musica.";
                        return;
                    }

                    faixa.PreviewUrl = preview;
                    previewItunes = true;
                }

                // Baixa o preview para um arquivo local antes de tocar.
                // Isso torna o pause/retomar confiavel e evita falhas de streaming.
                string caminhoLocal = await BaixarPreviewAsync(preview);

                // Se o preview da faixa (Spotify) estiver fora do ar, tenta o fallback da iTunes.
                if (caminhoLocal == null && !previewItunes)
                {
                    lblStatus.Text = "Preview indisponivel, buscando na iTunes: " + faixa.Nome + "...";
                    string itunes = await SpotifyService.BuscarPreviaItunesAsync(faixa.Nome, faixa.Artistas);

                    if (!string.IsNullOrWhiteSpace(itunes))
                    {
                        caminhoLocal = await BaixarPreviewAsync(itunes);
                        faixa.PreviewUrl = itunes;
                    }
                }

                if (caminhoLocal == null)
                {
                    lblStatus.Text = "Sem preview disponivel para esta musica.";
                    return;
                }

                // O usuario trocou de musica enquanto esta carregava: descarta o resultado.
                if (token != _tocandoToken)
                {
                    TryApagarArquivo(caminhoLocal);
                    return;
                }

                try
                {
                    // Cria leitor e dispositivo de saida fora da thread de interface:
                    // Stop/Dispose/Init do NAudio podem bloquear e travariam a troca.
                    await System.Threading.Tasks.Task.Run(delegate
                    {
                        // Recria o dispositivo de saida: reutilizar o antigo apos um Stop
                        // causa erro de "ja inicializado" e trava a barra de progresso.
                        CriarPlayer();
                        _reader = new NAudio.Wave.MediaFoundationReader(caminhoLocal);
                        _output.Init(_reader);
                    });

                    // Outra faixa foi pedida no meio da criacao do reprodutor.
                    if (token != _tocandoToken)
                    {
                        TryApagarArquivo(caminhoLocal);
                        PararReproducao();
                        return;
                    }

                    _faixaTocando = faixa;
                    _arquivoTemp = caminhoLocal;

                    await AtualizarPlayerInfo(faixa);

                    _duracaoAtual = (int)Math.Round(_reader.TotalTime.TotalSeconds);
                    if (_duracaoAtual <= 0)
                        _duracaoAtual = 30;

                    _output.Play();

                    trkProgresso.Enabled = _duracaoAtual > 0;
                    trkProgresso.Value = 0;
                    AtualizarTempo(0, _duracaoAtual);
                    lblStatus.Text = "Tocando: " + faixa.Nome + " - " + faixa.Artistas;
                    btnPlayPause.Text = "⏸";
                    _timerPosicao.Start();
                    AtualizarEqualizer();
                }
                catch (Exception ex)
                {
                    _suppressStop = false;
                    PararReproducao();
                    lblStatus.Text = "Erro ao reproduzir preview: " + ex.Message;
                }
            }
            finally
            {
                _carregandoMusica = false;
            }
        }

        // Baixa o preview (URL) para um arquivo temporario e retorna o caminho local.
        // Retorna null em caso de falha de download ou URL invalida.
        private async Task<string> BaixarPreviewAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            string destino = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "tecfy_" + Guid.NewGuid().ToString("N") + ".m4a");

            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(25);
                    var dados = await client.GetByteArrayAsync(url);

                    if (dados == null || dados.Length < 1024)
                        return null;

                    System.IO.File.WriteAllBytes(destino, dados);
                    return destino;
                }
            }
            catch
            {
                return null;
            }
        }

        private void TimerPosicao_Tick(object sender, EventArgs e)
        {
            if (_reader == null || _output == null)
                return;

            try
            {
                int segundos = (int)_reader.CurrentTime.TotalSeconds;

                if (_duracaoAtual > 0 && segundos <= _duracaoAtual)
                {
                    trkProgresso.Value = Math.Min(100, (int)(segundos * 100.0 / _duracaoAtual));
                }

                // Mostra o estado (Playing/Paused/Stopped) para diagnostico.
                lblTempo.Text = FormatarDuracao(segundos) + " / " + FormatarDuracao(_duracaoAtual)
                    + "   [" + _output.PlaybackState + "]";
            }
            catch
            {
            }
        }

        private void AtualizarTempo(int decorrido, int total)
        {
            lblTempo.Text = FormatarDuracao(decorrido) + " / " + FormatarDuracao(total);
        }

        private void BtnPlayPause_Click(object sender, EventArgs e)
        {
            if (_faixaTocando == null)
            {
                lblStatus.Text = "Nenhuma musica para pausar.";
                return;
            }

            // Sem output ativo, recria o reprodutor e começa a tocar do inicio.
            if (_output == null || _output.PlaybackState == NAudio.Wave.PlaybackState.Stopped)
            {
                BeginInvoke((Action)delegate { Tocar(_faixaTocando); });
                return;
            }

            if (_output.PlaybackState == NAudio.Wave.PlaybackState.Paused)
            {
                // Retoma a reproducao e mostra o simbolo de pause.
                _output.Play();
                btnPlayPause.Text = "⏸";
                lblStatus.Text = "Tocando: " + _faixaTocando.Nome + " - " + _faixaTocando.Artistas;
                AtualizarEqualizer();
            }
            else if (_output.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                // Faz a pausa e mostra o simbolo de play.
                _output.Pause();
                btnPlayPause.Text = "▶";
                lblStatus.Text = "Pausado: " + _faixaTocando.Nome;
                AtualizarEqualizer();
            }
        }

        private void BtnParar_Click(object sender, EventArgs e)
        {
            _suppressStop = true;
            PararReproducao();
            _suppressStop = false;
        }

        private void PararReproducao()
        {
            try
            {
                _timerPosicao?.Stop();
            }
            catch
            {
            }

            // Desconecta o dispositivo atual ANTES de encerra-lo em segundo plano,
            // para que um PlaybackStopped de um device antigo nunca afete o novo.
            var saidaParaFechar = _output;
            var leitorParaFechar = _reader;
            var arquivoParaApagar = _arquivoTemp;
            _output = null;
            _reader = null;
            _arquivoTemp = null;

            // Stop/Dispose de dispositivos de audio podem bloquear; roda-os em
            // thread separada para nao travar a interface ao trocar de musica.
            try
            {
                System.Threading.Thread pool = new System.Threading.Thread(new System.Threading.ThreadStart(delegate
                {
                    try
                    {
                        if (saidaParaFechar != null)
                        {
                            if (saidaParaFechar.PlaybackState != NAudio.Wave.PlaybackState.Stopped)
                                saidaParaFechar.Stop();
                            saidaParaFechar.Dispose();
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        if (leitorParaFechar != null)
                            leitorParaFechar.Dispose();
                    }
                    catch
                    {
                    }

                    // Remove o arquivo temporario do preview baixado.
                    if (!string.IsNullOrEmpty(arquivoParaApagar))
                    {
                        try
                        {
                            if (System.IO.File.Exists(arquivoParaApagar))
                                System.IO.File.Delete(arquivoParaApagar);
                        }
                        catch
                        {
                        }
                    }
                }));
                pool.IsBackground = true;
                pool.Start();
            }
            catch
            {
            }

            btnPlayPause.Text = "▶";
            lblStatus.Text = "Nenhuma musica tocando";
            lblNomeMusica.Text = "Nenhuma musica";
            picCapa.Image = DesenharCapaVazia();
            trkProgresso.Enabled = false;
            trkProgresso.Value = 0;
            AtualizarTempo(0, _duracaoAtual);
            AtualizarEqualizer();
        }

        private void TryApagarArquivo(string arquivo)
        {
            try
            {
                if (!string.IsNullOrEmpty(arquivo) && System.IO.File.Exists(arquivo))
                    System.IO.File.Delete(arquivo);
            }
            catch
            {
            }
        }

        // Preenche a capa e o nome/artista mostrados no player da faixa em reproducao.
        private async System.Threading.Tasks.Task AtualizarPlayerInfo(SpotifyService.Faixa faixa)
        {
            if (faixa == null)
            {
                lblNomeMusica.Text = "Nenhuma musica";
                picCapa.Image = DesenharCapaVazia();
                return;
            }

            lblNomeMusica.Text = faixa.Nome;
            lblStatus.Text = "Tocando: " + faixa.Nome + " - " + faixa.Artistas;

            // Seta a capa da faixa; se nao houver, busca na iTunes como fallback.
            picCapa.Image = DesenharCapaVazia();

            string urlCapa = faixa.ImagemUrl;
            if (string.IsNullOrWhiteSpace(urlCapa))
            {
                try
                {
                    urlCapa = await SpotifyService.BuscarCapaItunesAsync(faixa.Nome, faixa.Artistas);
                }
                catch
                {
                    urlCapa = null;
                }
            }

            if (!string.IsNullOrWhiteSpace(urlCapa))
            {
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var dados = client.GetByteArrayAsync(urlCapa).Result;
                        using (var ms = new System.IO.MemoryStream(dados))
                        {
                            picCapa.Image = new Bitmap(System.Drawing.Image.FromStream(ms));
                        }
                    }
                }
                catch
                {
                }
            }
        }

        // Desenha uma capa "padrao" (disco de musica) para quando nao ha imagem da faixa.
        private Bitmap DesenharCapaVazia()
        {
            var bmp = new Bitmap(54, 54);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(45, 20, 65));

                using (var borracha = new SolidBrush(Color.FromArgb(40, 25, 60)))
                {
                    g.FillEllipse(borracha, 6, 6, 42, 42);
                }

                using (var pincel = new SolidBrush(Color.FromArgb(168, 85, 247)))
                {
                    g.FillEllipse(pincel, 23, 23, 8, 8);
                }
            }
            return bmp;
        }

        // Dispara quando a reproducao para (fim da preview ou Stop).
        private void Output_PlaybackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            // So reage se for o dispositivo atualmente ativo. Um dispositivo
            // antigo encerrado em segundo plano nao deve mexer na interface.
            if (sender != _output || _suppressStop || !this.IsHandleCreated)
                return;

            try
            {
                this.BeginInvoke((Action)delegate
                {
                    _timerPosicao?.Stop();
                    btnPlayPause.Text = "▶";
                    trkProgresso.Enabled = false;
                    AtualizarEqualizer();
                });
            }
            catch { }
        }

        private void guna2CirclePictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void guna2CirclePictureBox2_Click(object sender, EventArgs e)
        {

        }

        private string ObterSaudacao()
        {
            int hora = DateTime.Now.Hour;
            if (hora >= 5 && hora < 12) return "Bom dia";
            if (hora >= 12 && hora < 18) return "Boa tarde";
            return "Boa noite";
        }

        // Atalhos de teclado: Espaco alterna play/pause e ↑/↓ ajustam o volume
        // (valem em qualquer lugar, exceto enquanto digita no campo de busca).
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Up || keyData == Keys.Down)
            {
                if (EstaDigitandoBusca())
                    return base.ProcessCmdKey(ref msg, keyData);

                if (keyData == Keys.Space)
                {
                    BtnPlayPause_Click(this, EventArgs.Empty);
                    return true;
                }

                float incremento = keyData == Keys.Up ? 0.1f : -0.1f;
                _volume = Math.Min(1f, Math.Max(0f, _volume + incremento));
                if (_output != null && _output.PlaybackState != NAudio.Wave.PlaybackState.Stopped)
                    _output.Volume = _volume;
                AtualizarTemaVolume();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Indica se o foco esta dentro de um campo de texto (busca principal,
        // busca de artistas, etc.) para nao sequestrar teclas de digitacao.
        private bool EstaDigitandoBusca()
        {
            var ativo = ActiveControl;
            while (ativo != null)
            {
                if (ativo is TextBoxBase || ativo == txtBusca)
                    return true;
                ativo = ativo.Parent;
            }
            return false;
        }

        private void AtualizarTemaVolume()
        {
            lblStatus.Text = "Volume: " + Math.Round(_volume * 100) + "%";
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            CarregarPerfil();
            if (string.IsNullOrWhiteSpace(_nomeUsuario) || _nomeUsuario == "Usuario")
                _nomeUsuario = _nomeUsuario == null ? "Usuario" : _nomeUsuario;
            CriarAvatarPerfil();
            AtualizarSaudacao();
            CarregarFavoritosConhecidos();
            AtualizarContadorFavoritas();
            AtualizarPlaylistsLateral();
            MostrarAbaInicio();
        }

        protected override void OnFormClosed(System.Windows.Forms.FormClosedEventArgs e)
        {
            try
            {
                _timerPosicao?.Stop();
                _timerPosicao?.Dispose();
            }
            catch
            {
            }

            try
            {
                if (_output != null)
                {
                    if (_output.PlaybackState != NAudio.Wave.PlaybackState.Stopped)
                        _output.Stop();
                    _output.Dispose();
                    _output = null;
                }
            }
            catch
            {
            }

            try
            {
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }
            }
            catch
            {
            }

            if (!string.IsNullOrEmpty(_arquivoTemp))
            {
                try
                {
                    if (System.IO.File.Exists(_arquivoTemp))
                        System.IO.File.Delete(_arquivoTemp);
                }
                catch
                {
                }
                _arquivoTemp = null;
            }

            base.OnFormClosed(e);
        }

        private void CarregarFavoritosConhecidos()
        {
            if (_usuarioId <= 0)
                return;

            try
            {
                var favoritas = FavoritoDAO.ListarPorUsuario(_usuarioId);
                foreach (var faixa in favoritas)
                {
                    if (faixa.MusicaId.HasValue)
                        _idsFavoritos.Add(faixa.MusicaId.Value);
                }
            }
            catch
            {
            }
        }

        private void btnInter_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void llabelEsqueceu_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MostrarAbaPlaylists();
        }

        private void pnlPlaylists_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void guna2PictureBox7_Click(object sender, EventArgs e)
        {

        }

        private void guna2PictureBox17_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (_trocandoContextoBusca)
                return;
            _timerBusca.Stop();
            _timerBusca.Start();
        }

        // Botao (lupa) da busca: executa a pesquisa imediatamente, mesmo que o
        // mesmo termo ja tenha sido pesquisado antes (forca nova busca).
        private void guna2PictureBox21_Click(object sender, EventArgs e)
        {
            _ultimaBusca = "";
            _timerBusca.Stop();
            TimerBusca_Tick(sender, e);
        }

        // Tecla Enter no campo de busca executa a pesquisa como o botao da lupa.
        private void txtBusca_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                _ultimaBusca = "";
                _timerBusca.Stop();
                TimerBusca_Tick(sender, e);
            }
        }

        private async void TimerBusca_Tick(object sender, EventArgs e)
        {
            _timerBusca.Stop();

            string termo = txtBusca.Text.Trim();

            if (AbaArtistasAtiva)
            {
                _ctlArtistas.Pesquisar(termo);
                return;
            }

            if (string.IsNullOrWhiteSpace(termo))
            {
                pnlResultados.Controls.Clear();
                _cards.Clear();
                _ultimaBusca = "";
                return;
            }

            if (!SpotifyService.Configurado)
            {
                MostrarResultadosBusca();
                AdicionarAviso("Credenciais do Spotify nao configuradas em SpotifyService.cs.");
                return;
            }

            MostrarResultadosBusca();

            if (termo == _ultimaBusca)
            {
                // Mesmo termo ja pesquisado: nao dispara a API de novo, mas
                // garante que os resultados voltem a aparecer (ex.: o usuario
                // estava na aba de playlists e retornou a busca).
                ReexibirResultadosBusca();
                return;
            }

            _ultimaBusca = termo;
            AdicionarAviso("Buscando...");

            try
            {
                List<SpotifyService.Faixa> faixas;

                try
                {
                    faixas = await SpotifyService.BuscarFaixasAsync(termo);
                }
                catch
                {
                    // Spotify indisponivel/instavel: tenta a iTunes Search API.
                    faixas = await SpotifyService.BuscarFaixasItunesAsync(termo);
                }

                if (faixas == null || faixas.Count == 0)
                {
                    // Sem resultado no Spotify: busca no iTunes como fallback.
                    faixas = await SpotifyService.BuscarFaixasItunesAsync(termo);
                }

                pnlResultados.Controls.Clear();
            _cards.Clear();

                if (faixas == null || faixas.Count == 0)
                {
                    AdicionarAviso("Nenhuma musica encontrada para \"" + termo + "\".");
                    return;
                }

                _faixasAtuais = faixas;

                foreach (var faixa in faixas)
                {
                    CriarCard(faixa);
                }

                RedimensionarCards();
                Tema.Aplicar(pnlResultados);
            }
            catch (Exception ex)
            {
                pnlResultados.Controls.Clear();
                _cards.Clear();
                AdicionarAviso("Erro ao buscar: " + ex.Message);
            }
        }

        // Reexibe a area de resultados mesmo quando o termo nao mudou
        // (o usuario estava em outra aba e voltou a busca).
        private void ReexibirResultadosBusca()
        {
            MostrarResultadosBusca();
            if (_faixasAtuais != null && _faixasAtuais.Count > 0)
            {
                pnlResultados.Controls.Clear();
                _cards.Clear();
                foreach (var faixa in _faixasAtuais)
                {
                    CriarCard(faixa);
                }
                RedimensionarCards();
                Tema.Aplicar(pnlResultados);
            }
        }

        private void btnFavoritas_Click(object sender, EventArgs e)
        {
            ExibirFavoritas();
        }

        // Atualiza o numero de musicas favoritas no botao "Suas Favoritas".
        private void AtualizarContadorFavoritas()
        {
            if (btnFavoritas == null)
                return;
            btnFavoritas.Text = "Favoritas (" + _idsFavoritos.Count + ")";
        }

        private async void ExibirFavoritas()
        {
            if (_usuarioId <= 0)
            {
                MostrarResultadosBusca();
                AdicionarAviso("Faca login para ver suas favoritas.");
                return;
            }

            // Exibe o painel de resultados (esconde as abas) antes de carregar,
            // senao as favoritas ficam invisiveis atras da aba ativa.
            MostrarResultadosBusca();

            AdicionarAviso("Carregando favoritas...");

            var favoritas = await System.Threading.Tasks.Task.Run(() => FavoritoDAO.ListarPorUsuario(_usuarioId));

            pnlResultados.Controls.Clear();
            _cards.Clear();

            if (favoritas == null || favoritas.Count == 0)
            {
                AdicionarAviso("Voce ainda nao tem musicas favoritas.");
                return;
            }

            _idsFavoritos.Clear();
            foreach (var faixa in favoritas)
            {
                if (faixa.MusicaId.HasValue)
                    _idsFavoritos.Add(faixa.MusicaId.Value);
                CriarCard(faixa);
            }

            RedimensionarCards();
            AtualizarContadorFavoritas();
            Tema.Aplicar(pnlResultados);
        }

        // ── Perfil do usuário ──────────────────────────────────────────────

        private void CriarAvatarPerfil()
        {
            // Avatar circular roxo (60×60) posicionado à esquerda da saudação.
            _picAvatar = new Guna.UI2.WinForms.Guna2CirclePictureBox
            {
                Size = new Size(46, 46),
                Location = new Point(ClientSize.Width - 70, 18),
                FillColor = Color.FromArgb(124, 58, 237),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _picAvatar.Paint += PicAvatar_Paint;
            _picAvatar.Click += PicAvatar_Click;

            // Context menu do perfil.
            _menuPerfil = new ContextMenuStrip();
            _menuPerfil.BackColor = Color.FromArgb(28, 16, 42);
            _menuPerfil.ForeColor = Color.White;
            _menuPerfil.Renderer = new ToolStripProfessionalRenderer(
                new DarkColorTable());
            _menuPerfil.Font = new Font("Segoe UI", 9.5f);
            _menuPerfil.Items.Add("Editar perfil", null, (s, e) => AbrirModalEditarPerfil());
            _menuPerfil.Items.Add(new ToolStripSeparator());
            _menuPerfil.Items.Add("Sair", null, (s, e) =>
            {
                var login = new Form1();
                login.Show();
                Close();
            });

            _picAvatar.ContextMenuStrip = _menuPerfil;

            Controls.Add(_picAvatar);
            _picAvatar.BringToFront();
            RedesenharAvatar();
        }

        private void PicAvatar_Paint(object sender, PaintEventArgs e)
        {
            // Limita a area clicavel ao circulo do avatar. As iniciais sao
            // desenhadas num bitmap (AtualizarImagemAvatar) e exibidas via Image.
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rc = _picAvatar.ClientRectangle;

            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
            {
                path.AddEllipse(rc);
                _picAvatar.Region = new Region(path);
            }
        }

        private void AtualizarImagemAvatar()
        {
            if (_picAvatar == null)
                return;

            GarantirFotosPerfil();

            // Avatar com foto real da pasta fotoperfil (id >= 100).
            if (_avatarId >= 100 && _fotosPerfil != null)
            {
                int idx = _avatarId - 100;
                if (idx >= 0 && idx < _fotosPerfil.Count)
                {
                    _picAvatar.FillColor = Color.FromArgb(45, 20, 65);
                    var nova = new Bitmap(_fotosPerfil[idx]);
                    var antiga = _picAvatar.Image;
                    _picAvatar.Image = nova;
                    if (antiga != null)
                        antiga.Dispose();
                    return;
                }
            }

            _picAvatar.FillColor = CorAvatarAtual();
            var novaImg = CriarImagemIniciais(_nomeUsuario, _picAvatar.Width, _picAvatar.Height);
            var antigaImg = _picAvatar.Image;
            _picAvatar.Image = novaImg;
            if (antigaImg != null)
                antigaImg.Dispose();
        }

        // Carrega (uma unica vez) as fotos da pasta ..\..\..\..\..\fotoperfil
        // (a pasta fica dentro de E:\FEIRAA). Se a pasta nao existir, mantem
        // apenas os avatares de cor com iniciais.
        private void GarantirFotosPerfil()
        {
            if (_fotosPerfil != null)
                return;

            _fotosPerfil = new List<Image>();
            _fotosPerfilNomes = new List<string>();

            try
            {
                string pasta = System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(
                        Application.StartupPath,
                        @"..\..\..\..\..\fotoperfil"));

                if (!System.IO.Directory.Exists(pasta))
                    return;

                // Ordena por nome para manter os ids das fotos estaveis entre
                // execucoes (o id salvo e 100 + indice da lista).
                foreach (string arquivo in System.IO.Directory.GetFiles(pasta)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    string ext = System.IO.Path.GetExtension(arquivo).ToLowerInvariant();
                    if (ext != ".jpeg" && ext != ".jpg" && ext != ".png" && ext != ".gif")
                        continue;

                    try
                    {
                        _fotosPerfil.Add(CarregarImagemSegura(arquivo));
                        _fotosPerfilNomes.Add(System.IO.Path.GetFileName(arquivo));
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        // Cria um Bitmap proprio a partir do arquivo; o clone desvincula a imagem
        // do stream, evitando erro GDI+ quando o MemoryStream e descartado.
        private static Bitmap CarregarImagemSegura(string caminho)
        {
            using (var ms = new System.IO.MemoryStream(System.IO.File.ReadAllBytes(caminho)))
            using (var img = Image.FromStream(ms))
            {
                return new Bitmap(img);
            }
        }

        private static readonly Color[] AvataresDisponiveis =
        {
            Color.FromArgb(124, 58, 237),   // roxo
            Color.FromArgb(236, 72, 153),   // rosa
            Color.FromArgb(59, 130, 246),   // azul
            Color.FromArgb(16, 185, 129),   // verde
            Color.FromArgb(245, 158, 11),   // laranja
            Color.FromArgb(239, 68, 68),    // vermelho
            Color.FromArgb(20, 184, 166),   // turquesa
            Color.FromArgb(139, 92, 246)    // violeta
        };

        private Color CorAvatarAtual()
        {
            return AvataresDisponiveis[0];
        }

        private static Bitmap CriarImagemIniciais(string nome, int largura, int altura)
        {
            var bmp = new Bitmap(Math.Max(1, largura), Math.Max(1, altura));
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                g.Clear(Color.Transparent);

                string iniciais = ObterIniciais(nome);
                using (var fonte = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point))
                using (var brush = new SolidBrush(Color.White))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(iniciais, fonte, brush,
                        new RectangleF(0f, 0f, largura, altura), sf);
                }
            }
            return bmp;
        }

        private static string ObterIniciais(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return "U";
            var partes = nome.Trim().Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0)
                return "U";
            string inicial = (partes[0][0]).ToString().ToUpper();
            if (partes.Length > 1)
                inicial += partes[1][0];
            return inicial;
        }

        private void RedesenharAvatar()
        {
            AtualizarImagemAvatar();
        }

        private void PicAvatar_Click(object sender, EventArgs e)
        {
            _menuPerfil.Show(_picAvatar, new Point(0, _picAvatar.Height));
        }

        private void AbrirModalEditarPerfil()
        {
            using (var modal = new Form())
            {
                modal.Text = "Editar perfil";
                modal.FormBorderStyle = FormBorderStyle.FixedDialog;
                modal.StartPosition = FormStartPosition.CenterParent;
                modal.MaximizeBox = false;
                modal.MinimizeBox = false;
                modal.BackColor = Color.FromArgb(13, 7, 20);
                modal.ForeColor = Color.White;
                modal.ClientSize = new Size(480, 420);

                GarantirFotosPerfil();

                var lbl = new Label
                {
                    Text = "Nome do usuario:",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f),
                    Location = new Point(20, 16),
                    AutoSize = true
                };
                modal.Controls.Add(lbl);

                var txtNome = new Guna.UI2.WinForms.Guna2TextBox
                {
                    Location = new Point(20, 42),
                    Size = new Size(440, 38),
                    FillColor = Color.FromArgb(28, 16, 42),
                    ForeColor = Color.White,
                    BorderColor = Color.FromArgb(124, 58, 237),
                    PlaceholderText = "Digite seu nome",
                    Text = _nomeUsuario ?? "",
                    Font = new Font("Segoe UI", 10f)
                };
                modal.Controls.Add(txtNome);

                var lblAvatar = new Label
                {
                    Text = "Escolha seu avatar ou foto:",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f),
                    Location = new Point(20, 100),
                    AutoSize = true
                };

                modal.Controls.Add(lblAvatar);

                // Grade de opcoes grandes (moldura Panel em volta de cada circulo, pois
                // Guna2CirclePictureBox nao tem borda desenhavel). A primeira opcao
                // e o avatar com as iniciais (id 0); as demais sao as fotos da pasta
                // fotoperfil (ids 100+). `ids` guarda o _avatarId de cada moldura
                // para marcar corretamente a selecao.
                const int cols = 4;    // opcoes por linha
                const int pitch = 96;  // espaco entre molduras
                const int tamMoldura = 88;
                const int tamBox = 82;
                const int origemX = 16;
                const int origemY = 12;

                // Painel rolavel para que muitas fotos nao estourem a altura do modal.
                var pnlOpcoes = new Panel
                {
                    Location = new Point(14, 128),
                    Size = new Size(472, 248),
                    AutoScroll = true,
                    BackColor = Color.FromArgb(20, 11, 30)
                };
                modal.Controls.Add(pnlOpcoes);

                int selecionado = _avatarId >= 100 ? _avatarId : 0;
                var molduras = new List<Panel>();
                var ids = new List<int>();

                // 1. Avatar com as iniciais.
                var molduraIniciais = new Panel
                {
                    Size = new Size(tamMoldura, tamMoldura),
                    Location = new Point(origemX, origemY),
                    BackColor = Color.FromArgb(28, 16, 42)
                };
                var boxIniciais = new Guna.UI2.WinForms.Guna2CirclePictureBox
                {
                    Size = new Size(tamBox, tamBox),
                    Location = new Point(3, 3),
                    FillColor = AvataresDisponiveis[0],
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand,
                    SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                    Image = CriarImagemIniciais(_nomeUsuario, tamBox, tamBox)
                };
                boxIniciais.Click += (s, ev) =>
                {
                    selecionado = 0;
                    AtualizarSelecaoAvatar(molduras, ids, selecionado);
                };
                molduraIniciais.Controls.Add(boxIniciais);
                pnlOpcoes.Controls.Add(molduraIniciais);
                molduras.Add(molduraIniciais);
                ids.Add(0);

                // 2. Fotos da pasta fotoperfil.
                for (int i = 0; i < _fotosPerfil.Count; i++)
                {
                    int fotoAtual = i;
                    int opcao = 1 + i;                  // a inicial ja ocupa a posicao 0
                    int col = opcao % cols;
                    int linha = opcao / cols;

                    var moldura = new Panel
                    {
                        Size = new Size(tamMoldura, tamMoldura),
                        Location = new Point(origemX + (col * pitch), origemY + (linha * pitch)),
                        BackColor = Color.FromArgb(28, 16, 42)
                    };
                    var box = new Guna.UI2.WinForms.Guna2CirclePictureBox
                    {
                        Size = new Size(tamBox, tamBox),
                        Location = new Point(3, 3),
                        FillColor = Color.FromArgb(45, 20, 65),
                        BackColor = Color.Transparent,
                        Cursor = Cursors.Hand,
                        SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom,
                        Image = new Bitmap(_fotosPerfil[i])
                    };
                    box.Click += (s, ev) =>
                    {
                        selecionado = 100 + fotoAtual;
                        AtualizarSelecaoAvatar(molduras, ids, selecionado);
                    };
                    moldura.Controls.Add(box);
                    pnlOpcoes.Controls.Add(moldura);
                    molduras.Add(moldura);
                    ids.Add(100 + i);
                }
                AtualizarSelecaoAvatar(molduras, ids, selecionado);

                const int botoesY = 392;
                modal.ClientSize = new Size(500, 462);

                var btnSalvar = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "Salvar",
                    Location = new Point(20, botoesY),
                    Size = new Size(150, 40),
                    FillColor = Color.FromArgb(124, 58, 237),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSalvar.Click += (s, ev) =>
                {
                    string novo = txtNome.Text.Trim();
                    if (string.IsNullOrWhiteSpace(novo))
                    {
                        MessageBox.Show("O nome nao pode ficar vazio.",
                            "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    _nomeUsuario = novo;
                    _avatarId = selecionado;
                    SalvarPerfil();
                    AtualizarSaudacao();
                    modal.DialogResult = DialogResult.OK;
                };
                modal.Controls.Add(btnSalvar);

                var btnCancelar = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "Cancelar",
                    Location = new Point(190, botoesY),
                    Size = new Size(150, 40),
                    FillColor = Color.FromArgb(45, 20, 65),
                    ForeColor = Color.Silver,
                    Font = new Font("Segoe UI", 10f),
                    Cursor = Cursors.Hand
                };
                btnCancelar.Click += (s, ev) => { modal.DialogResult = DialogResult.Cancel; };
                modal.Controls.Add(btnCancelar);

                modal.AcceptButton = btnSalvar;
                modal.CancelButton = btnCancelar;
                modal.ShowDialog(this);
            }
        }

        private void AtualizarSelecaoAvatar(List<Panel> molduras, List<int> ids, int selecionado)
        {
            for (int i = 0; i < molduras.Count && i < ids.Count; i++)
            {
                molduras[i].BackColor = (ids[i] == selecionado)
                    ? Color.White
                    : Color.FromArgb(28, 16, 42);
            }
        }

        private void AtualizarSaudacao()
        {
            string nome = string.IsNullOrWhiteSpace(_nomeUsuario) ? "Usuario" : _nomeUsuario;
            lblBomdia.Text = ObterSaudacao() + ", " + nome + "!";
            if (_ctlInicio != null)
                _ctlInicio.AtualizarSaudacao(_nomeUsuario);
            RedesenharAvatar();
        }

        private void SalvarPerfil()
        {
            try
            {
                var obj = new Newtonsoft.Json.Linq.JObject
                {
                    ["nome"] = _nomeUsuario,
                    ["avatar"] = _avatarId
                };
                System.IO.File.WriteAllText(CaminhoPerfil, obj.ToString());
            }
            catch { }
        }

        private void CarregarPerfil()
        {
            try
            {
                if (System.IO.File.Exists(CaminhoPerfil))
                {
                    string json = System.IO.File.ReadAllText(CaminhoPerfil);
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    string nome = obj["nome"] == null ? "" : Convert.ToString(obj["nome"]);
                    if (!string.IsNullOrWhiteSpace(nome))
                        _nomeUsuario = nome;

                    int id = 0;
                    if (obj["avatar"] != null && int.TryParse(Convert.ToString(obj["avatar"]), out id))
                        _avatarId = id;
                }
            }
            catch { }
        }

        // Tabela de cores escura para o ContextMenuStrip do perfil.
        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => Color.FromArgb(28, 16, 42);
            public override Color ImageMarginGradientBegin => Color.FromArgb(28, 16, 42);
            public override Color ImageMarginGradientMiddle => Color.FromArgb(28, 16, 42);
            public override Color ImageMarginGradientEnd => Color.FromArgb(28, 16, 42);
            public override Color MenuBorder => Color.FromArgb(124, 58, 237);
            public override Color MenuItemBorder => Color.FromArgb(124, 58, 237);
            public override Color MenuItemSelected => Color.FromArgb(45, 20, 65);
            public override Color MenuStripGradientBegin => Color.FromArgb(28, 16, 42);
            public override Color MenuStripGradientEnd => Color.FromArgb(28, 16, 42);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(45, 20, 65);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(45, 20, 65);
            public override Color MenuItemPressedGradientBegin => Color.FromArgb(28, 16, 42);
            public override Color MenuItemPressedGradientEnd => Color.FromArgb(28, 16, 42);
            public override Color SeparatorDark => Color.FromArgb(60, 30, 90);
            public override Color SeparatorLight => Color.FromArgb(60, 30, 90);
        }
    }
}
